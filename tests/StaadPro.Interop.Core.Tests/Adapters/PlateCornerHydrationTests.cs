using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCornerHydrationTests
    {
        private static readonly string[] Operations = ["ids", "idsTask", "explicit", "explicitTask", "family"];
        private static OpenStaadWrapper Wrap(PlateCornerHydrationNative native) => new(new PlateCornerHydrationRoot(native));
        private static PlateCornerForces Invoke(OpenStaadWrapper wrapper, string op, int caseId = 1, int plate = 17,
            int node = 11, LoadCaseType family = LoadCaseType.PrimaryLoad, IOSOutput output = null,
            IOSGeometry geometry = null, IOSLoad load = null)
        {
            output ??= wrapper.Output; geometry ??= wrapper.Geometry; load ??= wrapper.Load;
            return op switch
            {
                "ids" => output.GetPlateCornerForces(caseId, plate, node),
                "idsTask" => output.GetPlateCornerForcesAsync(caseId, plate, node).GetAwaiter().GetResult(),
                "explicit" => output.GetPlateCornerForces(caseId, plate, node, geometry, load),
                "explicitTask" => output.GetPlateCornerForcesAsync(caseId, plate, node, geometry, load).GetAwaiter().GetResult(),
                "family" => output.GetPlateCornerForces(caseId, plate, node, family, geometry, load),
                _ => throw new ArgumentException(op)
            };
        }
        private sealed class ForeignGeometry : IOSGeometry
        {
            public OpenStaadWrapper Wrapper => throw new ApplicationException("Do not inspect a foreign adapter.");
            public dynamic ComObject => throw new ApplicationException("Do not access a foreign raw object.");
        }
        [TestCaseSource(nameof(Operations))]
        public void IdQueriesHydrateDetachedMetadataCopyForcesAndKeepCallerThread(string op)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            var first = Invoke(w,op); var second = Invoke(w,op);
            Assert.That(new[] { first.Fx,first.Fy,first.Fz,first.Mx,first.My,first.Mz },
                Is.EqualTo(new[] { 1d,-2d,3d,-4d,5d,-6d }));
            Assert.That(first.LoadCase.Id, Is.EqualTo(1));
            Assert.That(first.LoadCase.CaseType, Is.EqualTo(LoadCaseType.PrimaryLoad));
            Assert.That(first.LoadCase.Title, Is.EqualTo("  case\r\n"));
            Assert.That(first.Plate.Id, Is.EqualTo(17)); Assert.That(first.Plate.D, Is.Null);
            Assert.That(first.CornerNode, Is.SameAs(first.Plate.A));
            Assert.That(first.CornerNode.X, Is.EqualTo(11));
            Assert.That(second.Plate, Is.Not.SameAs(first.Plate));
            Assert.That(second.CornerNode, Is.Not.SameAs(first.CornerNode));
            Assert.That(second.LoadCase, Is.Not.SameAs(first.LoadCase));
            Assert.That(native.Calls[0], Is.EqualTo("incidence:17"));
            Assert.That(native.Calls.Take(4), Is.EqualTo(new[] { "incidence:17","coordinate:11","coordinate:12","coordinate:13" }));
            Assert.That(native.Calls.Last(), Is.EqualTo("force:17:11:1"));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            ((double[])native.ForceValues)[0] = 999;
            Assert.That(first.Fx, Is.EqualTo(1));
        }
        [TestCase("ids")]
        [TestCase("idsTask")]
        [TestCase("explicit")]
        [TestCase("explicitTask")]
        public void DefaultResolutionKeepsPrimaryCombinationReferencePrecedenceAndValidatesAllFamilies(string op)
        {
            var native = new PlateCornerHydrationNative();
            foreach (string family in new[] { "p","c","r" }) native.Ids[family] = new[] { 1 };
            using var w = Wrap(native);
            Assert.That(Invoke(w,op).LoadCase.CaseType, Is.EqualTo(LoadCaseType.PrimaryLoad));
            Assert.That(native.Calls, Does.Contain("cCount").And.Contain("rCount"));
            native.Counts["p"] = 0;
            Assert.That(Invoke(w,op).LoadCase.CaseType, Is.EqualTo(LoadCaseType.LoadCombination));
            native.Counts["c"] = 0;
            Assert.That(Invoke(w,op).LoadCase.CaseType, Is.EqualTo(LoadCaseType.ReferenceLoad));
            native.Counts["r"] = 0; native.Calls.Clear();
            Assert.Throws<InvalidOperationException>(() => Invoke(w,op));
            Assert.That(native.Calls.Any(x=>x.StartsWith("force:")), Is.False);
        }
        [TestCase(LoadCaseType.PrimaryLoad, 1, "p")]
        [TestCase(LoadCaseType.LoadCombination, 2, "c")]
        [TestCase(LoadCaseType.ReferenceLoad, 3, "r")]
        public void ExplicitFamilyScansOnlyThatInventoryAndRetainsItsClassification(LoadCaseType family, int id, string prefix)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            var result = Invoke(w,"family",id,family:family);
            Assert.That(result.LoadCase.CaseType, Is.EqualTo(family));
            Assert.That(result.LoadCase.Id, Is.EqualTo(id));
            var metadata = native.Calls.Skip(4).Where(x=>!x.StartsWith("force:")).ToArray();
            foreach(string other in new[] {"p","c","r"}.Where(s=>s!=prefix))
                Assert.That(metadata.Any(s=>s==other+"Count"||s==other+"Numbers"), Is.False);
            if(family==LoadCaseType.LoadCombination) Assert.That(result.LoadCase.Type, Is.EqualTo(LoadType.None));
            native.Calls.Clear();
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,"family",999,family:family));
            Assert.That(native.Calls.Any(s=>s.StartsWith("force:")),Is.False);
        }
        [TestCaseSource(nameof(Operations))]
        public void InvalidScalarIdsFailBeforeAnyNativeWork(string op)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            foreach(int invalid in new[] {0,-1,int.MinValue})
            {
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(w,op,caseId:invalid)).ParamName, Is.EqualTo("loadCaseId"));
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(w,op,plate:invalid)).ParamName, Is.EqualTo("plateId"));
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(w,op,node:invalid)).ParamName, Is.EqualTo("cornerNodeId"));
            }
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCase(LoadCaseType.RepeatLoad)]
        [TestCase((LoadCaseType)0)]
        [TestCase((LoadCaseType)99)]
        public void UnsupportedFamilyFailsBeforeHydration(LoadCaseType family)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(w,"family",family:family)).ParamName, Is.EqualTo("loadCaseType"));
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCase("explicit")]
        [TestCase("explicitTask")]
        [TestCase("family")]
        public void ExplicitAdaptersMustBeCurrentOwnedBindingsEvenWithEqualRawObjects(string op)
        {
            var native = new PlateCornerHydrationNative(); var root = new PlateCornerHydrationRoot(native);
            using var w = new OpenStaadWrapper(root); using var other = new OpenStaadWrapper(root);
            Assert.That(Assert.Throws<ArgumentException>(()=>Invoke(w,op,geometry:other.Geometry)).ParamName, Is.EqualTo("geometry"));
            Assert.That(Assert.Throws<ArgumentException>(()=>Invoke(w,op,load:other.Load)).ParamName, Is.EqualTo("load"));
            Assert.Throws<ArgumentException>(()=>Invoke(w,op,geometry:new OSGeometryAdapter(w)));
            Assert.Throws<ArgumentException>(()=>Invoke(w,op,load:new OSLoadAdapter(w)));
            Assert.Throws<ArgumentException>(()=>Invoke(w,op,geometry:new ForeignGeometry()));
            var oldGeometry = w.Geometry; var oldLoad = w.Load; w.RefreshComObjects();
            Assert.Throws<ArgumentException>(()=>Invoke(w,op,geometry:oldGeometry));
            Assert.Throws<ArgumentException>(()=>Invoke(w,op,load:oldLoad));
            Assert.That(native.Calls, Is.Empty);
        }
        [Test]
        public void NullOrUnavailableAdaptersFailBeforeHydrationAndTaskFaultIsSettled()
        {
            var native = new PlateCornerHydrationNative(); var root = new PlateCornerHydrationRoot(native);
            using(var w = new OpenStaadWrapper(root))
            {
                Assert.That(Assert.Throws<ArgumentNullException>(()=>w.Output.GetPlateCornerForces(1,17,11,null,w.Load)).ParamName,Is.EqualTo("geometry"));
                Assert.That(Assert.Throws<ArgumentNullException>(()=>w.Output.GetPlateCornerForces(1,17,11,w.Geometry,null)).ParamName,Is.EqualTo("load"));
                Assert.That(w.Output.GetPlateCornerForcesAsync(1,17,11,null,w.Load).IsFaulted,Is.True);
                Assert.That(w.Output.GetPlateCornerForcesAsync(1,17,11,w.Geometry,null).IsFaulted,Is.True);
            }
            root.Geometry = null;
            using(var w = new OpenStaadWrapper(root))
                Assert.Throws<ArgumentNullException>(()=>w.Output.GetPlateCornerForces(1,17,11));
            root.Geometry = native; root.Load = null;
            using(var w = new OpenStaadWrapper(root))
                Assert.Throws<ArgumentNullException>(()=>w.Output.GetPlateCornerForces(1,17,11));
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void RepeatedTriangleSlotsShareNodesAndNonIncidenceStopsBeforeLoadDiscovery(string op)
        {
            var native = new PlateCornerHydrationNative { D = 11 }; using var w = Wrap(native);
            var result = Invoke(w,op);
            Assert.That(result.Plate.D,Is.SameAs(result.Plate.A));
            Assert.That(native.Calls.Count(x=>x.StartsWith("coordinate:")),Is.EqualTo(3));
            native.Calls.Clear();
            Assert.That(Assert.Throws<ArgumentException>(()=>Invoke(w,op,node:99)).ParamName,Is.EqualTo("cornerNodeId"));
            Assert.That(native.Calls.Any(x=>x.EndsWith("Count")||x.StartsWith("force:")),Is.False);
        }
        [TestCaseSource(nameof(Operations))]
        public void GeometryStatusesSentinelsAndCoordinatesAreQualifiedBeforeMetadata(string op)
        {
            foreach(Action<PlateCornerHydrationNative> mutate in new Action<PlateCornerHydrationNative>[] {
                n=>n.IncidenceStatus=true, n=>n.IncidenceStatus=1, n=>n.WriteIncidence=false,
                n=>n.A=0, n=>n.B="12", n=>n.C=null, n=>n.D=-1,
                n=>n.WriteCoordinates=false, n=>n.Coordinates=id=>new object[]{double.NaN,0d,0d},
                n=>n.Coordinates=id=>new object[]{0d,0d,double.PositiveInfinity},
                n=>n.Coordinates=id=>new object[]{0f,0d,0d}
            })
            {
                var native = new PlateCornerHydrationNative(); mutate(native); using var w = Wrap(native);
                Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
                Assert.That(native.Calls.Any(x=>x.EndsWith("Count")||x.StartsWith("force:")),Is.False);
            }
        }
        [TestCaseSource(nameof(Operations))]
        public void LoadInventoryAndMetadataFailuresStopBeforeForceWithoutFallback(string op)
        {
            foreach(Action<PlateCornerHydrationNative> mutate in new Action<PlateCornerHydrationNative>[] {
                n=>n.Counts["p"]=-1, n=>n.Counts["p"]=100001, n=>n.Counts["p"]=true,
                n=>n.Ids["p"]=new[]{0}, n=>n.Ids["p"]=new double[]{1},
                n=>n.Responses["p"]=false, n=>n.Responses["p"]=2, n=>n.WriteIds=false,
                n=>n.Title=null, n=>n.Type=99,
                n=>n.CountAt=(family,count)=>family=="p"&&count>1?2:1
            })
            {
                var native = new PlateCornerHydrationNative(); mutate(native); using var w = Wrap(native);
                Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
                Assert.That(native.Calls.Any(x=>x.StartsWith("force:")),Is.False);
            }
        }
        [TestCase("ids")]
        [TestCase("idsTask")]
        [TestCase("explicit")]
        [TestCase("explicitTask")]
        public void LaterInvalidFamilyStillFailsAfterPrimaryMatchAndUnrequestedMetadataIsValidated(string op)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            native.Responses["r"]=true;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
            Assert.That(native.Calls.Any(s=>s.StartsWith("force:")),Is.False);
            native.Responses["r"]=1; native.Counts["p"]=2; native.Ids["p"]=new[]{1,9};
            native.Callback=step=> { if(step=="title:9") native.Title=null; };
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
            Assert.That(native.Calls.Any(s=>s.StartsWith("force:")),Is.False);
        }
        [TestCaseSource(nameof(Operations))]
        public void ForceFailureShapeAndCopiedNonzeroBoundValuesMatchExistingCornerReader(string op)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            foreach(object invalid in new object[]{null,new double[5],new float[6],new double[1,6],new[]{0d,0,0,0,0,double.NaN}})
            { native.ForceValues=invalid; Assert.Throws<InvalidOperationException>(()=>Invoke(w,op)); }
            native.ForceValues="bad"; native.ForceStatus=false;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
            native.ForceStatus=2; Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
            native.ForceStatus=1;
            var values=Array.CreateInstance(typeof(double),new[]{6},new[]{-2});
            for(int i=0;i<6;i++) values.SetValue(i-3d,i-2);
            native.ForceValues=values;
            var result=Invoke(w,op); values.SetValue(999d,-2);
            Assert.That(result.Fx,Is.EqualTo(-3)); Assert.That(result.Mz,Is.EqualTo(2));
        }
        [TestCaseSource(nameof(Operations))]
        public void ErrorsPreserveOriginalIdentityAtGeometryLoadAndForceStages(string op)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            foreach(string stage in new[]{"incidence:","coordinate:","pCount","title:","force:"})
            {
                var error=new COMException(stage,unchecked((int)0x80010108));
                native.FailureAt=stage; native.Failure=error; native.Calls.Clear();
                Assert.That(Assert.Throws<COMException>(()=>Invoke(w,op)),Is.SameAs(error));
                Assert.That(native.Calls.Last(),Does.StartWith(stage));
            }
            native.Failure=null;
            Assert.DoesNotThrow(()=>Invoke(w,op));
        }
        [TestCaseSource(nameof(Operations))]
        public void OneBindingLeaseProtectsEveryHydrationDiscoveryAndForceCallback(string op)
        {
            var native = new PlateCornerHydrationNative(); var w=Wrap(native); var old=w.Output;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,op,output:new OSOutputAdapter(w)));
            w.RefreshComObjects(); Assert.Throws<InvalidOperationException>(()=>Invoke(w,op,output:old));
            var crossThread=Task.Run(()=>Assert.Throws<InvalidOperationException>(()=>Invoke(w,op))).GetAwaiter().GetResult();
            Assert.That(crossThread,Is.Not.Null);
            native.Callback=step=>
            {
                Assert.Throws<InvalidOperationException>(()=>w.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(()=>Invoke(w,op));
                Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(()=>w.Dispose());
            };
            Assert.DoesNotThrow(()=>Invoke(w,op));
            var current=w.Output; w.Dispose();
            Assert.Throws<ObjectDisposedException>(()=>Invoke(w,op,output:current));
        }
        [TestCase("idsTask")]
        [TestCase("explicitTask")]
        public void TaskWorkFinishesOnCallerBeforeReturningSettledSuccessOrOriginalFault(string op)
        {
            var native = new PlateCornerHydrationNative(); using var w = Wrap(native);
            Task<PlateCornerForces> Start(int id)=>op=="idsTask"
                ?w.Output.GetPlateCornerForcesAsync(id,17,11)
                :w.Output.GetPlateCornerForcesAsync(id,17,11,w.Geometry,w.Load);
            Assert.That(Start(1).IsCompletedSuccessfully,Is.True);
            var validation=Start(0); Assert.That(validation.IsFaulted,Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(()=>validation.GetAwaiter().GetResult());
            var error=new COMException("native",-12345); native.Failure=error;
            Task<PlateCornerForces> fault=null;
            Assert.DoesNotThrow(()=>fault=Start(1)); Assert.That(fault.IsFaulted,Is.True);
            Assert.That(Assert.Throws<COMException>(()=>fault.GetAwaiter().GetResult()),Is.SameAs(error));
        }
        [Test]
        public void MissingNativeMembersPropagateInsteadOfAttemptingFallback()
        {
            var native=new PlateCornerHydrationNative();
            var root=new PlateCornerHydrationRoot(native){Geometry=new object()};
            using(var w=new OpenStaadWrapper(root)) Assert.Throws<RuntimeBinderException>(()=>Invoke(w,"ids"));
            root.Geometry=native; root.Load=new object();
            using(var w=new OpenStaadWrapper(root)) Assert.Throws<RuntimeBinderException>(()=>Invoke(w,"ids"));
        }
        [Test]
        public void FiveIdContractsHaveMatchingFullXmlAndSourceParameterOrder()
        {
            var xml=XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location,".xml"));
            var methods=typeof(IOSOutput).GetMethods().Where(m=>m.Name.Contains("PlateCornerForces")&&m.GetParameters()[0].ParameterType==typeof(int)).ToArray();
            Assert.That(methods,Has.Length.EqualTo(5));
            foreach(var method in methods)
            {
                string suffix="."+method.Name+"("+string.Join(",",method.GetParameters().Select(p=>p.ParameterType.FullName))+")";
                var contract=xml.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+typeof(IOSOutput).FullName+suffix);
                var implementation=xml.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+typeof(OSOutputAdapter).FullName+suffix);
                Assert.That(string.Concat(contract.Elements()),Is.EqualTo(string.Concat(implementation.Elements())));
                Assert.That(contract.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);
                Assert.That(method.GetParameters().Any(p=>p.ParameterType.IsByRef||p.IsOptional),Is.False);
                Assert.That(method.GetParameters().Take(3).Select(p=>p.Name),Is.EqualTo(new[]{"loadCaseId","plateId","cornerNodeId"}));
                foreach(string tag in new[]{"summary","param","returns","remarks","exception","example"}) Assert.That(contract.Element(tag),Is.Not.Null);
            }
        }
    }
}

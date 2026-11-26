using System;
using System.Collections;
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
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCornerBatchAndAliasTests
    {
        private static readonly string[] Operations=["familyTask","batch","batchTask","global","globalTask"];
        private static readonly string[] Batches=["batch","batchTask"];
        private static readonly string[] Aliases=["global","globalTask"];
        private static OpenStaadWrapper Wrap(PlateCornerHydrationNative native)=>new(new PlateCornerHydrationRoot(native));
        private static Plate Plate()=>new(new Node(11,0,0,0),new Node(12,0,1,0),new Node(13,0,0,1)){Id=17};
        private sealed class Once<T>(IEnumerable<T> items,Action start=null):IEnumerable<T>
        {
            public int Enumerations,Disposals;
            public IEnumerator<T> GetEnumerator()
            {
                if(++Enumerations!=1)throw new ApplicationException("Input re-enumerated.");
                start?.Invoke();
                try{foreach(var item in items)yield return item;}finally{Disposals++;}
            }
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
        private static object Invoke(OpenStaadWrapper wrapper,string operation,IEnumerable<int> ids=null,
            IOSOutput output=null,ILoadCase loadCase=null,Plate plate=null,Node corner=null,
            IOSGeometry geometry=null,IOSLoad load=null)
        {
            output??=wrapper.Output;geometry??=wrapper.Geometry;load??=wrapper.Load;
            loadCase??=new LoadCase(1,"Case");plate??=Plate();corner??=plate.A;
            return operation switch
            {
                "familyTask"=>output.GetPlateCornerForcesAsync(2,17,11,LoadCaseType.LoadCombination,geometry,load).GetAwaiter().GetResult(),
                "batch"=>output.GetPlateCornerForces(ids??[1,2],17,11,geometry,load),
                "batchTask"=>output.GetPlateCornerForcesAsync(ids??[1,2],17,11,geometry,load).GetAwaiter().GetResult(),
                "global"=>output.GetPlateCornerForcesInGlobalCoordinates(loadCase,plate,corner),
                "globalTask"=>output.GetPlateCornerForcesInGlobalCoordinatesAsync(loadCase,plate,corner).GetAwaiter().GetResult(),
                _=>throw new ArgumentException(operation)
            };
        }
        private static IEnumerable<PlateCornerForces> Rows(object value)=>
            value is PlateCornerForces one?[one]:((PlateCornerForcesResultSet)value).Results;
        [TestCaseSource(nameof(Operations))]
        public void FiveWorkflowsKeepNativeComponentsCallerThreadAndOwnedStorage(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            var value=Invoke(w,operation);var rows=Rows(value).ToArray();
            Assert.That(rows.Length,Is.EqualTo(operation.StartsWith("batch")?2:1));
            Assert.That(rows.All(r=>r.Fx==1&&r.Fy==-2&&r.Fz==3&&r.Mx==-4&&r.My==5&&r.Mz==-6),Is.True);
            Assert.That(native.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));
            ((double[])native.ForceValues)[0]=999;
            Assert.That(rows.All(r=>r.Fx==1),Is.True);
            Assert.That(native.Calls.Count(s=>s.StartsWith("force:")),Is.EqualTo(rows.Length));
        }
        [TestCaseSource(nameof(Batches))]
        public void BatchCapturesUniqueRequestedOrderResolvesBeforeGeometryAndHydratesOnce(string operation)
        {
            var native=new PlateCornerHydrationNative{D=11};using var w=Wrap(native);
            var ids=new Once<int>([3,1,2,3]);
            native.Callback=step=>Assert.That(ids.Disposals,Is.EqualTo(1),"Enumeration must complete before every native call.");
            var set=(PlateCornerForcesResultSet)Invoke(w,operation,ids);
            Assert.That(ids.Enumerations,Is.EqualTo(1));Assert.That(ids.Disposals,Is.EqualTo(1));
            Assert.That(set.Results.Select(r=>r.LoadCaseId),Is.EqualTo(new[]{3,1,2}));
            Assert.That(set.Results.Select(r=>r.LoadCase.CaseType),Is.EqualTo(new[]{LoadCaseType.ReferenceLoad,LoadCaseType.PrimaryLoad,LoadCaseType.LoadCombination}));
            Assert.That(native.Calls.First(),Is.EqualTo("pCount"));
            Assert.That(native.Calls.IndexOf("referenceType:3"),Is.LessThan(native.Calls.IndexOf("incidence:17")));
            Assert.That(native.Calls.Count(s=>s=="incidence:17"),Is.EqualTo(1));
            Assert.That(native.Calls.Count(s=>s.StartsWith("coordinate:")),Is.EqualTo(3));
            Assert.That(native.Calls.Where(s=>s.StartsWith("force:")),Is.EqualTo(new[]{"force:17:11:3","force:17:11:1","force:17:11:2"}));
            Assert.That(set[3,17,11].Plate,Is.SameAs(set[1,17,11].Plate));
            Assert.That(set[3,17,11].CornerNode,Is.SameAs(set[1,17,11].CornerNode));
            Assert.That(set[3,17,11].Plate.D,Is.SameAs(set[3,17,11].CornerNode));
            Assert.That(set.Results.Select(r=>r.LoadCase),Is.Unique);
            Assert.Throws<NotSupportedException>(()=>((IList<PlateCornerForces>)set.Results).Clear());
        }
        [TestCaseSource(nameof(Batches))]
        public void BatchCaptureAndIndexesSurviveCallerCollectionAndReturnedMetadataEdits(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            var ids=new List<int>{1,2};
            native.Callback=step=>{if(step=="pCount")ids[1]=999;};
            var set=(PlateCornerForcesResultSet)Invoke(w,operation,ids);
            Assert.That(native.Calls.Where(s=>s.StartsWith("force:")),Is.EqualTo(new[]{"force:17:11:1","force:17:11:2"}));
            var first=set[1,17,11];((LoadCase)first.LoadCase).Id=99;first.Plate.Id=98;first.CornerNode.Id=97;
            Assert.That(set[1,17,11],Is.SameAs(first));
            Assert.That(set.LoadCaseIds,Is.EqualTo(new[]{1,2}));
            Assert.That(set.PlateIds,Is.EqualTo(new[]{17}));
            Assert.That(set.GetForPlateCorner(17,11),Has.Count.EqualTo(2));
        }
        [TestCaseSource(nameof(Batches))]
        public void EmptyIdsValidateBindingsAndScalarIdsButSkipAllNativeWork(string operation)
        {
            var native=new PlateCornerHydrationNative{Title=null,IncidenceStatus=true};using var w=Wrap(native);
            var first=(PlateCornerForcesResultSet)Invoke(w,operation,Array.Empty<int>());
            var second=(PlateCornerForcesResultSet)Invoke(w,operation,Array.Empty<int>());
            Assert.That(first,Is.Empty);Assert.That(second,Is.Not.SameAs(first));Assert.That(native.Calls,Is.Empty);
            if(operation=="batch")
            {
                Assert.Throws<ArgumentOutOfRangeException>(()=>w.Output.GetPlateCornerForces(Array.Empty<int>(),0,11,w.Geometry,w.Load));
                Assert.Throws<ArgumentNullException>(()=>w.Output.GetPlateCornerForces(Array.Empty<int>(),17,11,null,w.Load));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(()=>w.Output.GetPlateCornerForcesAsync(Array.Empty<int>(),17,0,w.Geometry,w.Load).GetAwaiter().GetResult());
                Assert.Throws<ArgumentNullException>(()=>w.Output.GetPlateCornerForcesAsync(Array.Empty<int>(),17,11,w.Geometry,null).GetAwaiter().GetResult());
            }
            Assert.That(native.Calls,Is.Empty);
        }
        [TestCaseSource(nameof(Batches))]
        public void InvalidIdsEnumerationFailuresAndLimitsPrecedeNativeCalls(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            void NullCall()
            {
                if(operation=="batch")w.Output.GetPlateCornerForces((IEnumerable<int>)null,17,11,w.Geometry,w.Load);
                else w.Output.GetPlateCornerForcesAsync((IEnumerable<int>)null,17,11,w.Geometry,w.Load).GetAwaiter().GetResult();
            }
            Assert.That(Assert.Throws<ArgumentNullException>(NullCall).ParamName,Is.EqualTo("loadCaseIds"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(w,operation,new[]{1,2,0})).ParamName,Is.EqualTo("loadCaseIds"));
            var failure=new ApplicationException("enumeration");
            IEnumerable<int> Failing(){yield return 1;throw failure;}
            var once=new Once<int>(Failing());
            Assert.That(Assert.Throws<ApplicationException>(()=>Invoke(w,operation,once)),Is.SameAs(failure));
            Assert.That(once.Disposals,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>Invoke(w,operation,Enumerable.Repeat(1,100001)));
            Assert.That(native.Calls,Is.Empty);
            Assert.That(((PlateCornerForcesResultSet)Invoke(w,operation,Enumerable.Repeat(1,100000))).Count,Is.EqualTo(1));
        }
        [TestCaseSource(nameof(Batches))]
        public void MissingCasesOrInvalidLaterFamilyMetadataFailBeforeGeometryEvenAfterPrimaryMatch(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation,new[]{1,999}));
            Assert.That(native.Calls.Any(s=>s.StartsWith("incidence:")||s.StartsWith("force:")),Is.False);
            native.Responses["r"]=true;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation,new[]{1}));
            Assert.That(native.Calls.Any(s=>s.StartsWith("incidence:")||s.StartsWith("force:")),Is.False);
        }
        [TestCaseSource(nameof(Batches))]
        public void FamilyOverlapUsesPrimaryThenCombinationThenReferenceForAllRows(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            foreach(string family in new[]{"p","c","r"})native.Ids[family]=new[]{1};
            Assert.That(((PlateCornerForcesResultSet)Invoke(w,operation,new[]{1}))[1,17,11].LoadCase.CaseType,Is.EqualTo(LoadCaseType.PrimaryLoad));
            native.Counts["p"]=0;
            Assert.That(((PlateCornerForcesResultSet)Invoke(w,operation,new[]{1}))[1,17,11].LoadCase.CaseType,Is.EqualTo(LoadCaseType.LoadCombination));
            native.Counts["c"]=0;
            Assert.That(((PlateCornerForcesResultSet)Invoke(w,operation,new[]{1}))[1,17,11].LoadCase.CaseType,Is.EqualTo(LoadCaseType.ReferenceLoad));
        }
        [TestCaseSource(nameof(Batches))]
        public void OwnershipIsCheckedBeforeEnumeratingAndGeometryFailurePreventsForceReads(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);using var other=Wrap(native);
            var ids=new Once<int>([1]);
            Assert.Throws<ArgumentException>(()=>Invoke(w,operation,ids,geometry:other.Geometry));
            Assert.Throws<ArgumentException>(()=>Invoke(w,operation,ids,load:other.Load));
            Assert.Throws<ArgumentException>(()=>Invoke(w,operation,ids,geometry:new OSGeometryAdapter(w)));
            Assert.That(ids.Enumerations,Is.Zero);Assert.That(native.Calls,Is.Empty);
            native.IncidenceStatus=1;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation,new[]{1,2}));
            Assert.That(native.Calls.Any(s=>s.StartsWith("force:")),Is.False);
            native.IncidenceStatus=0;
            if(operation=="batch")Assert.Throws<ArgumentException>(()=>w.Output.GetPlateCornerForces(new[]{1},17,99,w.Geometry,w.Load));
            else Assert.Throws<ArgumentException>(()=>w.Output.GetPlateCornerForcesAsync(new[]{1},17,99,w.Geometry,w.Load).GetAwaiter().GetResult());
            Assert.That(native.Calls.Any(s=>s.StartsWith("force:")),Is.False);
        }
        [TestCaseSource(nameof(Batches))]
        public void LaterForceFailureStopsWithoutPublishingAPartialSet(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            native.Callback=step=>{if(step=="force:17:11:2")native.ForceStatus=false;};
            object returned=null;
            Assert.Throws<InvalidOperationException>(()=>returned=Invoke(w,operation,new[]{1,2,3}));
            Assert.That(returned,Is.Null);
            Assert.That(native.Calls.Where(s=>s.StartsWith("force:")),Is.EqualTo(new[]{"force:17:11:1","force:17:11:2"}));
        }
        [TestCaseSource(nameof(Aliases))]
        public void GlobalNamingAliasKeepsExactNativeValuesWithoutGeometryBasisOrRotation(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            var load=new LoadCase(7,"Title");var p=Plate();p.A.X=double.NaN;p.B.Z=double.PositiveInfinity;
            var result=(PlateCornerForces)Invoke(w,operation,loadCase:load,plate:p,corner:new Node{Id=11});
            Assert.That(native.Calls,Is.EqualTo(new[]{"force:17:11:7"}));
            Assert.That(result.LoadCase,Is.SameAs(load));Assert.That(result.Plate,Is.SameAs(p));Assert.That(result.CornerNode,Is.SameAs(p.A));
            Assert.That(new[]{result.Fx,result.Fy,result.Fz,result.Mx,result.My,result.Mz},Is.EqualTo(new[]{1d,-2d,3d,-4d,5d,-6d}));
            native.Calls.Clear();var incomplete=new Plate{Id=17};
            var hydrated=(PlateCornerForces)Invoke(w,operation,loadCase:load,plate:incomplete,corner:new Node{Id=11});
            Assert.That(incomplete.A,Is.Null);Assert.That(hydrated.Plate,Is.Not.SameAs(incomplete));
            Assert.That(hydrated.CornerNode,Is.SameAs(hydrated.Plate.A));
            Assert.That(native.Calls.Any(s=>s.EndsWith("Count")),Is.False);
        }
        [Test]
        public void FamilyTaskMatchesSyncFamilyIsolationAndReturnsValidationFaultsWithoutDispatch()
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            var good=w.Output.GetPlateCornerForcesAsync(2,17,11,LoadCaseType.LoadCombination,w.Geometry,w.Load);
            Assert.That(good.IsCompletedSuccessfully,Is.True);Assert.That(good.Result.LoadCase.CaseType,Is.EqualTo(LoadCaseType.LoadCombination));
            Assert.That(native.Calls.Any(s=>s=="pCount"||s=="rCount"),Is.False);
            native.Calls.Clear();
            foreach(var family in new[]{LoadCaseType.RepeatLoad,(LoadCaseType)99})
            {
                Task<PlateCornerForces> fault=null;
                Assert.DoesNotThrow(()=>fault=w.Output.GetPlateCornerForcesAsync(2,17,11,family,w.Geometry,w.Load));
                Assert.That(fault.IsFaulted,Is.True);
                Assert.Throws<ArgumentOutOfRangeException>(()=>fault.GetAwaiter().GetResult());
            }
            Assert.That(native.Calls,Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void NativeErrorsRetainIdentityAndUnsupportedStatusesOrBuffersNeverYieldData(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);
            var error=new COMException("native",unchecked((int)0x80010108));native.Failure=error;native.FailureAt="force:";
            Assert.That(Assert.Throws<COMException>(()=>Invoke(w,operation)),Is.SameAs(error));
            native.Failure=null;native.ForceStatus=2;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation));
            native.ForceStatus=true;native.ForceValues=new float[6];
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation));
            var array=Array.CreateInstance(typeof(double),new[]{6},new[]{-3});
            for(int i=0;i<6;i++)array.SetValue(i-2d,i-3);
            native.ForceValues=array;
            var result=Rows(Invoke(w,operation)).First();array.SetValue(999d,-3);
            Assert.That(result.Fx,Is.EqualTo(-2));Assert.That(result.Mz,Is.EqualTo(3));
        }
        [TestCaseSource(nameof(Operations))]
        public void LifetimeCurrentThreadAndLeaseGuardsCoverTheEntireNewWorkflow(string operation)
        {
            var native=new PlateCornerHydrationNative();var w=Wrap(native);var stale=w.Output;
            Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation,output:new OSOutputAdapter(w)));
            w.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation,output:stale));
            Assert.That(Task.Run(()=>Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation))).GetAwaiter().GetResult(),Is.Not.Null);
            void Reenter()
            {
                Assert.Throws<InvalidOperationException>(()=>Invoke(w,operation));
                Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(()=>w.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(()=>w.Dispose());
            }
            native.Callback=step=>Reenter();
            if(operation.StartsWith("batch"))Assert.DoesNotThrow(()=>Invoke(w,operation,new Once<int>([1,2],Reenter)));
            else Assert.DoesNotThrow(()=>Invoke(w,operation));
            var current=w.Output;w.Dispose();Assert.Throws<ObjectDisposedException>(()=>Invoke(w,operation,output:current));
        }
        [TestCase("familyTask")]
        [TestCase("batchTask")]
        [TestCase("globalTask")]
        public void AllTaskVariantsReturnSettledSuccessAndFaultTasksWithoutSynchronousThrow(string operation)
        {
            var native=new PlateCornerHydrationNative();using var w=Wrap(native);var p=Plate();var load=new LoadCase(1,"Case");
            Task Start()=>operation switch
            {
                "familyTask"=>w.Output.GetPlateCornerForcesAsync(2,17,11,LoadCaseType.LoadCombination,w.Geometry,w.Load),
                "batchTask"=>w.Output.GetPlateCornerForcesAsync(new[]{1,2},17,11,w.Geometry,w.Load),
                _=>w.Output.GetPlateCornerForcesInGlobalCoordinatesAsync(load,p,p.A)
            };
            Assert.That(Start().IsCompletedSuccessfully,Is.True);
            var failure=new COMException("task",-12345);native.Failure=failure;
            Task task=null;Assert.DoesNotThrow(()=>task=Start());Assert.That(task.IsFaulted,Is.True);
            Assert.That(Assert.Throws<COMException>(()=>task.GetAwaiter().GetResult()),Is.SameAs(failure));
        }
        [Test]
        public void AllFiveNewContractsShipMatchingExpandedXmlAndManagedSignatures()
        {
            var xml=XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location,".xml"));
            var methods=typeof(IOSOutput).GetMethods().Where(m=>
                m.Name.StartsWith("GetPlateCornerForcesInGlobalCoordinates") ||
                (m.Name=="GetPlateCornerForces"||m.Name=="GetPlateCornerForcesAsync")&&
                (m.GetParameters()[0].ParameterType==typeof(IEnumerable<int>) ||
                m.Name.EndsWith("Async")&&m.GetParameters()[0].ParameterType==typeof(int)&&m.GetParameters().Length==6)).ToArray();
            Assert.That(methods,Has.Length.EqualTo(5));
            string TypeId(Type t)=>t.IsGenericType?t.GetGenericTypeDefinition().FullName.Split((char)96)[0]+"{"+string.Join(",",t.GetGenericArguments().Select(TypeId))+"}":t.FullName;
            foreach(var method in methods)
            {
                string suffix="."+method.Name+"("+string.Join(",",method.GetParameters().Select(p=>TypeId(p.ParameterType)))+")";
                var contract=xml.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+typeof(IOSOutput).FullName+suffix);
                var concrete=xml.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+typeof(OSOutputAdapter).FullName+suffix);
                Assert.That(string.Concat(contract.Elements()),Is.EqualTo(string.Concat(concrete.Elements())));
                Assert.That(method.GetParameters().Any(p=>p.ParameterType.IsByRef||p.IsOptional),Is.False);
                foreach(string tag in new[]{"summary","param","returns","remarks","exception","example"})Assert.That(contract.Element(tag),Is.Not.Null);
                Assert.That(contract.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);
            }
        }
    }
}

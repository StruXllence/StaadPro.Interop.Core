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
using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Root;
using NativeRoot=StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;
namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture,NonParallelizable,Apartment(ApartmentState.STA)]
    public sealed class MemberBatchQueryTests
    {
        private static readonly string[] Operations=["force","beam","batch","populate","asyncBatch"];
        private static readonly string[] BatchOperations=["batch","populate","asyncBatch"];
        public sealed class Native
        {
            public object RatioStatus=true, ForceStatus=true, Ratio=1.25d, Forces=new[]{1d,-2d,3d,-4d,5d,-6d};
            public bool Write=true;
            public Func<int,object> RatioAt,StatusAt;
            public Action<int> OnRatio;
            public Action OnForce;
            public Exception Failure;
            public readonly List<int> Ids=[];
            public readonly List<int> Threads=[];
            public object GetMemberSteelDesignRatio(int id,ref object value)
            {
                Assert.That(value,Is.TypeOf<double>());Assert.That(double.IsNaN((double)value),Is.True);
                Ids.Add(id);Threads.Add(Environment.CurrentManagedThreadId);OnRatio?.Invoke(id);if(Failure!=null)throw Failure;
                if(Write)value=RatioAt==null?Ratio:RatioAt(id);return StatusAt==null?RatioStatus:StatusAt(id);
            }
            public object GetMemberEndForces(int id,int end,int load,ref object values,int axes)
            {
                Assert.That(id,Is.EqualTo(7));Assert.That(end,Is.EqualTo(1));Assert.That(load,Is.EqualTo(9));Assert.That(axes,Is.Zero);
                Assert.That(values,Is.TypeOf<double[]>());Assert.That(((double[])values).Length,Is.EqualTo(6));Assert.That(((double[])values).All(double.IsNaN),Is.True);
                Ids.Add(id);Threads.Add(Environment.CurrentManagedThreadId);OnForce?.Invoke();if(Failure!=null)throw Failure;if(Write)values=Forces;return ForceStatus;
            }
        }
        private static Beam B(int id)=>new(){Id=id,UtilizationRatio=42};
        private static HashSet<Beam> Set()=>new(){B(7),B(8)};
        private static OpenStaadWrapper Wrap(object native)=>new(new NativeRoot{Output=native});
        private static object Invoke(IOSOutput output,string operation,HashSet<Beam> beams=null)
        {
            beams??=Set();
            return operation switch
            {
                "force"=>output.GetMemberEndForcesAsync("  literal  ",7,9,MemberEnd.End).GetAwaiter().GetResult(),
                "beam"=>output.GetMemberSteelUtilizationRatioAsync(beams.First()).GetAwaiter().GetResult(),
                "batch"=>output.GetMemberSteelUtilizationRatios(beams),
                "asyncBatch"=>output.GetMemberSteelUtilizationRatiosAsync(beams).GetAwaiter().GetResult(),
                "populate"=>Populate(output,beams),_=>throw new ArgumentOutOfRangeException(nameof(operation))
            };
        }
        private static object Populate(IOSOutput output,HashSet<Beam> beams){output.PopulateMemberSteelUtilizationRatios(beams);return null;}
        [TestCaseSource(nameof(Operations))]
        public void SequentialNativeCallsUseOneOwningThreadAndDoNotHydrateGeometry(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);var beams=Set();var result=Invoke(wrapper.Output,operation,beams);
            Assert.That(native.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));Assert.That(native.Ids,Is.EqualTo(operation is "force" or "beam"?new[]{7}:new[]{7,8}));
            Assert.That(beams.All(b=>b.StartNode==null&&b.EndNode==null),Is.True);
            if(operation=="populate")Assert.That(beams.Select(b=>b.UtilizationRatio),Is.All.EqualTo(1.25d));else Assert.That(beams.Select(b=>b.UtilizationRatio),Is.All.EqualTo(42d));
            if(operation=="force"){var q=(MemberForceQueryResult)result;Assert.That(q.IsSuccessful,Is.True);Assert.That(q.CoordinateSystem,Is.EqualTo(LocalOrGlobal.Local));Assert.That(q.LoadCaseTitle,Is.EqualTo("  literal  "));Assert.That(q.Forces.My,Is.EqualTo(5));}
        }
        [TestCaseSource(nameof(BatchOperations))]
        public void EntireInputIsQualifiedBeforeTheFirstNativeCall(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);var beams=Set();beams.Add(null);
            Assert.Throws<ArgumentException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(native.Ids,Is.Empty);
            beams=Set();beams.Last().Id=0;Assert.Throws<ArgumentException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(native.Ids,Is.Empty);
            beams=Set();beams.Last().Id=beams.First().Id;Assert.Throws<ArgumentException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(native.Ids,Is.Empty);
        }
        [TestCaseSource(nameof(BatchOperations))]
        public void EmptyAndNullCollectionsHaveExplicitContracts(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);var empty=new HashSet<Beam>();var result=Invoke(wrapper.Output,operation,empty);
            if(operation!="populate")Assert.That((Dictionary<Beam,double>)result,Is.Empty);Assert.That(native.Ids,Is.Empty);
            string parameter=operation=="populate"?"forBeams":"beams";
            if(operation=="populate")Assert.That(Assert.Throws<ArgumentNullException>(()=>wrapper.Output.PopulateMemberSteelUtilizationRatios(null)).ParamName,Is.EqualTo(parameter));
            else if(operation=="batch")Assert.That(Assert.Throws<ArgumentNullException>(()=>wrapper.Output.GetMemberSteelUtilizationRatios(null)).ParamName,Is.EqualTo(parameter));
            else {var task=wrapper.Output.GetMemberSteelUtilizationRatiosAsync(null);Assert.That(task.IsFaulted,Is.True);Assert.That(Assert.Throws<ArgumentNullException>(()=>task.GetAwaiter().GetResult()).ParamName,Is.EqualTo(parameter));}
        }
        [TestCaseSource(nameof(BatchOperations))]
        public void DuplicateIdsWithReferenceComparersAreRejectedBeforeCalls(string operation)
        {var native=new Native();using var wrapper=Wrap(native);var beams=new HashSet<Beam>(ReferenceEqualityComparer.Instance){B(7),B(7)};Assert.Throws<ArgumentException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(native.Ids,Is.Empty);}
        [Test]
        public void CurrentIdsInStaleHashBucketsAreReadWithoutContainsDependence()
        {var native=new Native{RatioAt=id=>(double)id};using var wrapper=Wrap(native);var beams=Set();beams.First().Id=101;var result=wrapper.Output.GetMemberSteelUtilizationRatios(beams);Assert.That(native.Ids,Is.EqualTo(new[]{101,8}));Assert.That(result[beams.First()],Is.EqualTo(101));}
        [TestCase(false)] [TestCase(true)]
        public void DictionaryOwnsMembershipAndUsesOriginalReferencesAfterKeyIdsChange(bool async)
        {
            var native=new Native();using var wrapper=Wrap(native);var beams=Set();var first=beams.First();var second=beams.Last();
            var result=async?wrapper.Output.GetMemberSteelUtilizationRatiosAsync(beams).Result:wrapper.Output.GetMemberSteelUtilizationRatios(beams);first.Id=second.Id;beams.Clear();
            Assert.That(result.Count,Is.EqualTo(2));Assert.That(result.ContainsKey(first),Is.True);Assert.That(result.ContainsKey(second),Is.True);Assert.That(result.ContainsKey(B(second.Id)),Is.False);
            result[first]=3;Assert.That(result[second],Is.EqualTo(1.25));Assert.That(wrapper.Output.GetMemberSteelUtilizationRatios(new()),Is.Not.SameAs(result));
        }
        [TestCaseSource(nameof(BatchOperations))]
        public void CollectionMembershipIsCapturedBeforeNativeCallbacks(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);var beams=Set();var originals=beams.ToArray();native.OnRatio=_=>{beams.Clear();beams.Add(B(99));};
            var result=Invoke(wrapper.Output,operation,beams);Assert.That(native.Ids,Is.EqualTo(new[]{7,8}));
            if(operation=="populate")Assert.That(originals.Select(b=>b.UtilizationRatio),Is.All.EqualTo(1.25));else Assert.That(((Dictionary<Beam,double>)result).Keys,Is.EquivalentTo(originals));
        }
        [TestCaseSource(nameof(BatchOperations))]
        public void ObservedIdChangesDuringNativeReadsFailWithoutPopulation(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);var beams=Set();var first=beams.First();native.OnRatio=id=>{if(id==8)first.Id=99;};
            Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(beams.Select(b=>b.UtilizationRatio),Is.All.EqualTo(42));
            first.Id=7;native.OnRatio=id=>{if(id==7)beams.Last().Id=99;};native.Ids.Clear();Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(native.Ids,Is.EqualTo(new[]{7}));
        }
        [TestCaseSource(nameof(BatchOperations))]
        public void LaterNativeFailuresNeverReturnPartialBatchesOrWriteRatios(string operation)
        {
            var native=new Native{StatusAt=id=>id==7?true:false};using var wrapper=Wrap(native);var beams=Set();Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(beams.Select(b=>b.UtilizationRatio),Is.All.EqualTo(42));
            native.StatusAt=null;native.RatioAt=id=>id==8?double.NaN:1d;Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,operation,beams));Assert.That(beams.Select(b=>b.UtilizationRatio),Is.All.EqualTo(42));
        }
        [TestCase(-999d)] [TestCase(-1d)] [TestCase(0d)] [TestCase(2d)]
        public void EveryRatioRouteRetainsDocumentedValues(double ratio)
        {var native=new Native{Ratio=ratio};using var wrapper=Wrap(native);var beams=Set();Assert.That(wrapper.Output.GetMemberSteelUtilizationRatioAsync(beams.First()).Result,Is.EqualTo(ratio));Assert.That(wrapper.Output.GetMemberSteelUtilizationRatios(beams).Values,Is.All.EqualTo(ratio));Assert.That(wrapper.Output.GetMemberSteelUtilizationRatiosAsync(beams).Result.Values,Is.All.EqualTo(ratio));wrapper.Output.PopulateMemberSteelUtilizationRatios(beams);Assert.That(beams.Select(b=>b.UtilizationRatio),Is.All.EqualTo(ratio));}
        [Test]
        public void PopulationReadsEverythingBeforeNotifyingAndHoldsTheSharedLeaseDuringWrites()
        {
            var native=new Native{RatioAt=id=>(double)id};using var wrapper=Wrap(native);var beams=Set();var notifications=new List<int>();
            foreach(var beam in beams)beam.PropertyChanged+=(_,e)=>{Assert.That(e.PropertyName,Is.EqualTo("UtilizationRatio"));Assert.That(native.Ids,Is.EqualTo(new[]{7,8}));Assert.Throws<InvalidOperationException>(()=>wrapper.Root.GetBaseUnit());Assert.Throws<InvalidOperationException>(()=>wrapper.Output.GetMemberSteelUtilizationRatios(beams));Assert.That(wrapper.Output.GetMemberSteelUtilizationRatiosAsync(beams).IsFaulted,Is.True);notifications.Add(beam.Id);};
            wrapper.Output.PopulateMemberSteelUtilizationRatios(beams);Assert.That(notifications,Is.EqualTo(new[]{7,8}));Assert.That(beams.Select(b=>b.UtilizationRatio),Is.EqualTo(new[]{7d,8d}));
        }
        [Test]
        public void NotificationFailurePropagatesAfterTheCurrentWriteWithoutInventedRollback()
        {
            var native=new Native{Ratio=2d};using var wrapper=Wrap(native);var beams=Set();var failure=new ApplicationException("handler failed");beams.First().PropertyChanged+=(_,_)=>throw failure;
            Assert.That(Assert.Throws<ApplicationException>(()=>wrapper.Output.PopulateMemberSteelUtilizationRatios(beams)),Is.SameAs(failure));Assert.That(native.Ids,Is.EqualTo(new[]{7,8}));Assert.That(beams.First().UtilizationRatio,Is.EqualTo(2));Assert.That(beams.Last().UtilizationRatio,Is.EqualTo(42));Assert.DoesNotThrow(()=>wrapper.Output.GetMemberSteelUtilizationRatios(beams));
        }
        [Test]
        public void NotificationIdentityChangesAreCheckedBeforeTheNextAssignment()
        {var native=new Native();using var wrapper=Wrap(native);var beams=Set();beams.First().PropertyChanged+=(_,_)=>beams.Last().Id=99;Assert.Throws<InvalidOperationException>(()=>wrapper.Output.PopulateMemberSteelUtilizationRatios(beams));Assert.That(beams.First().UtilizationRatio,Is.EqualTo(1.25));Assert.That(beams.Last().UtilizationRatio,Is.EqualTo(42));}
        [Test]
        public void BatchBoundIsEnforcedBeforeNativeAccessAndAcceptsTheBoundary()
        {
            var native=new Native();using var wrapper=Wrap(native);var beams=new HashSet<Beam>(Enumerable.Range(1,100001).Select(B));Assert.Throws<ArgumentException>(()=>wrapper.Output.GetMemberSteelUtilizationRatios(beams));Assert.That(native.Ids,Is.Empty);
            beams.Remove(beams.Last());var result=wrapper.Output.GetMemberSteelUtilizationRatios(beams);Assert.That(result.Count,Is.EqualTo(100000));Assert.That(native.Ids.Count,Is.EqualTo(100000));
        }
        [Test]
        public void BeamAsyncCapturesIdOnceWithoutReadingGeometryOrPopulating()
        {var native=new Native();using var wrapper=Wrap(native);var beam=B(7);native.OnRatio=_=>beam.Id=99;var task=wrapper.Output.GetMemberSteelUtilizationRatioAsync(beam);Assert.That(task.IsCompletedSuccessfully,Is.True);Assert.That(task.Result,Is.EqualTo(1.25));Assert.That(native.Ids,Is.EqualTo(new[]{7}));Assert.That(beam.UtilizationRatio,Is.EqualTo(42));}
        [TestCase(0)] [TestCase(-1)]
        public void InvalidAsyncInputsFaultTasksBeforeDispatch(int id)
        {
            var native=new Native();using var wrapper=Wrap(native);var beam=B(id);Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetMemberSteelUtilizationRatioAsync(beam).GetAwaiter().GetResult());Assert.Throws<ArgumentNullException>(()=>wrapper.Output.GetMemberSteelUtilizationRatioAsync((Beam)null).GetAwaiter().GetResult());
            Assert.Throws<ArgumentNullException>(()=>wrapper.Output.GetMemberEndForcesAsync(null,7,9,MemberEnd.End).GetAwaiter().GetResult());Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetMemberEndForcesAsync("",id,9,MemberEnd.End).GetAwaiter().GetResult());Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetMemberEndForcesAsync("",7,id,MemberEnd.End).GetAwaiter().GetResult());Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetMemberEndForcesAsync("",7,9,(MemberEnd)999).GetAwaiter().GetResult());Assert.That(native.Ids,Is.Empty);
        }
        [TestCase(false)] [TestCase(true)]
        public void AsyncForcePreservesNativeFalseAndQualifiedSuccess(bool success)
        {var native=new Native{ForceStatus=success,Write=success};using var wrapper=Wrap(native);var task=wrapper.Output.GetMemberEndForcesAsync("  literal  ",7,9,MemberEnd.End);Assert.That(task.IsCompletedSuccessfully,Is.True);var q=task.Result;Assert.That(q.IsSuccessful,Is.EqualTo(success));Assert.That(q.Forces==null,Is.EqualTo(!success));Assert.That(q.BeamId,Is.EqualTo(7));Assert.That(q.LoadCaseId,Is.EqualTo(9));}
        private static IEnumerable<object> BadForceArrays()=>new object[]{null,1d,new double[5],new int[6],new double[2,3],new[]{0d,0d,0d,0d,0d,double.NaN},new[]{0d,0d,0d,0d,0d,double.PositiveInfinity}};
        [TestCaseSource(nameof(BadForceArrays))]
        public void SuccessfulAsyncForceRejectsMalformedOrUnwrittenValues(object buffer)
        {var native=new Native{Forces=buffer};using var wrapper=Wrap(native);Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,"force"));}
        [Test]
        public void AsyncForceCopiesNonzeroBoundDataAndNeverInventsUnwrittenSuccess()
        {
            var array=Array.CreateInstance(typeof(double),new[]{6},new[]{-2});for(int i=0;i<6;i++)array.SetValue((double)(i+1),i-2);var native=new Native{Forces=array};using var wrapper=Wrap(native);var q=(MemberForceQueryResult)Invoke(wrapper.Output,"force");array.SetValue(99d,-2);Assert.That(q.Forces.Fx,Is.EqualTo(1));native.Write=false;Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,"force"));
        }
        [TestCaseSource(nameof(Operations))]
        public void GuardedRoutesRejectNoncurrentStaleDisposedAndOtherThreadUse(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);var output=wrapper.Output;Assert.Throws<InvalidOperationException>(()=>Invoke(new OSOutputAdapter(wrapper),operation));Exception failure=null;
            var thread=new Thread(()=>{try{Invoke(output,operation);}catch(Exception e){failure=e;}});thread.Start();Assert.That(thread.Join(5000),Is.True);Assert.That(failure,Is.TypeOf<InvalidOperationException>());wrapper.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>Invoke(output,operation));output=wrapper.Output;wrapper.Dispose();Assert.Throws<ObjectDisposedException>(()=>Invoke(output,operation));Assert.That(native.Ids,Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void LeasesRejectCallbacksAndReleaseAfterOriginalNativeFailures(string operation)
        {
            var native=new Native();using var wrapper=Wrap(native);Action callback=()=>{foreach(string nested in Operations)Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,nested));Assert.Throws<InvalidOperationException>(()=>wrapper.RefreshComObjects());Assert.Throws<InvalidOperationException>(()=>wrapper.Dispose());};native.OnForce=callback;native.OnRatio=_=>callback();Invoke(wrapper.Output,operation);native.OnForce=null;native.OnRatio=null;
            var failure=new COMException("native failed",unchecked((int)0x80010108));native.Failure=failure;Assert.That(Assert.Throws<COMException>(()=>Invoke(wrapper.Output,operation)),Is.SameAs(failure));native.Failure=null;Assert.DoesNotThrow(()=>Invoke(wrapper.Output,operation));using var missing=Wrap(new object());Assert.Throws<RuntimeBinderException>(()=>Invoke(missing.Output,operation));
        }
        [Test]
        public void SupportingBeamMetadataPreservesIdEqualityAndConstructorNamesWithoutGeometrySideEffects()
        {
            var a=new Node(1,0,0,0);var b=new Node(2,1,0,0);var beam=new Beam(7,a,b);Assert.That(beam,Is.InstanceOf<IEntity>());Assert.That(beam.StartNode,Is.SameAs(a));Assert.That(beam.EndNode,Is.SameAs(b));Assert.That(beam.UtilizationRatio,Is.Zero);Assert.That(new Beam().StartNode,Is.Null);Assert.That(new Beam(null,null).EndNode,Is.Null);
            Assert.That(beam.Equals(B(7)),Is.True);Assert.That(beam==B(7),Is.True);Assert.That(beam!=B(8),Is.True);Assert.That(beam.Equals(null),Is.False);Assert.That(beam.Equals("7"),Is.False);Assert.That(beam.GetHashCode(),Is.EqualTo(7.GetHashCode()));Assert.That((Beam)null==(Beam)null,Is.True);
            var names=new List<string>();beam.PropertyChanged+=(_,e)=>names.Add(e.PropertyName);beam.Id=8;beam.UtilizationRatio=2;beam.UtilizationRatio=2;Assert.That(names,Is.EqualTo(new[]{"Id","UtilizationRatio"}));Assert.That(typeof(Beam).GetConstructor(new[]{typeof(int),typeof(Node),typeof(Node)}).GetParameters().Select(p=>p.Name),Is.EqualTo(new[]{"number","startNode","endNode"}));
        }
        [TestCaseSource(nameof(Operations))]
        public void PublicContractsAndMatchingCompiledXmlRetainExactOverloads(string operation)
        {
            string name=operation switch{"force"=>"GetMemberEndForcesAsync","beam"=>"GetMemberSteelUtilizationRatioAsync","batch"=>"GetMemberSteelUtilizationRatios","populate"=>"PopulateMemberSteelUtilizationRatios",_=>"GetMemberSteelUtilizationRatiosAsync"};
            Type[] types=operation=="force"?[typeof(string),typeof(int),typeof(int),typeof(MemberEnd)]:operation=="beam"?[typeof(Beam)]:[typeof(HashSet<Beam>)];string[] names=operation=="force"?["lcTitle","beamId","loadCaseId","memberEnd"]:operation=="beam"?["b"]:operation=="populate"?["forBeams"]:["beams"];
            Type result=operation switch{"force"=>typeof(Task<MemberForceQueryResult>),"beam"=>typeof(Task<double>),"batch"=>typeof(Dictionary<Beam,double>),"populate"=>typeof(void),_=>typeof(Task<Dictionary<Beam,double>>)};
            var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location,".xml"));XElement previous=null;
            foreach(var owner in new[]{typeof(IOSOutput),typeof(OSOutputAdapter)})
            {var method=owner.GetMethod(name,types);Assert.That(method.ReturnType,Is.EqualTo(result));Assert.That(method.GetParameters().Select(p=>p.Name),Is.EqualTo(names));Assert.That(method.GetParameters().Any(p=>p.IsOptional||p.ParameterType.IsByRef),Is.False);
             string suffix=operation=="beam"?"(StaadPro.Interop.Core.Entities.Beam)":operation=="force"?"(System.String,System.Int32,System.Int32,StaadPro.Interop.Core.Enums.MemberEnd)":"(System.Collections.Generic.HashSet{StaadPro.Interop.Core.Entities.Beam})";
             var doc=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+owner.FullName+"."+name+suffix);foreach(string tag in new[]{"summary","returns","remarks","exception","example"})Assert.That(doc.Element(tag),Is.Not.Null);Assert.That(doc.Elements("param").Select(p=>(string)p.Attribute("name")),Is.EqualTo(names));Assert.That(doc.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);if(previous!=null)Assert.That(doc.Elements().Select(e=>e.ToString()),Is.EqualTo(previous.Elements().Select(e=>e.ToString())));previous=doc;}
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using BaseRoot=StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;
namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture,NonParallelizable,Apartment(ApartmentState.STA)]
    public sealed class MemberForceFoundationTests
    {
        public sealed class Output
        {
            public object Available=true,Flag=true;
            public readonly List<string> Calls=[];
            public Action Callback;
            public Func<int,int,int,object> Response;
            public Func<object,object> Buffer;
            public Exception Failure;
            public object AreResultsAvailable(){Calls.Add("available");Callback?.Invoke();if(Failure!=null)throw Failure;return Available;}
            public object GetMemberEndForces(int beam,int end,int load,ref object buffer,int axes)
            {
                Calls.Add($"{beam}:{end}:{load}:{axes}");Callback?.Invoke();if(Failure!=null)throw Failure;
                Assert.That(buffer,Is.TypeOf<double[]>());Assert.That((double[])buffer,Is.All.NaN);
                buffer=Buffer==null?new double[]{1,2,3,4,5,6}:Buffer(buffer);return Response?.Invoke(beam,end,load)??Flag;
            }
        }
        public sealed class Load
        {
            public readonly List<int> Calls=[];public object Title=" Title \r\n";public Action Callback;
            public object GetLoadCaseTitle(int id){Calls.Add(id);Callback?.Invoke();return Title;}
        }
        private static OpenStaadWrapper Wrapper(Output output,Load load)=>new(new BaseRoot{Output=output,Load=load});
        private static readonly object[] ValidTrue=[true,(byte)1,(short)1,1,1L,1UL];
        private static readonly object[] InvalidFlags=[null,"1",1d,1m,-1,2,ulong.MaxValue];
        private static readonly string[] Operations=["batch","title","available","local","global"];
        private static Action Capture(OpenStaadWrapper wrapper,string name)
        {
            var r=wrapper.Root;var o=wrapper.Output;var l=wrapper.Load;
            return name switch {"batch"=>()=>r.RetrieveMemberForces(new[]{1},new[]{1}),"title"=>()=>l.GetLoadCaseTitle(1),"available"=>()=>o.AreResultsAvailable(),"local"=>()=>o.GetMemberEndForces("t",1,1,MemberEnd.Start),_=>()=>o.GetMemberEndForces("t",1,1,MemberEnd.End,LocalOrGlobal.Global)};
        }
        [TestCaseSource(nameof(ValidTrue))]
        public void ValidFlagsAndExactSelectorsPreserveDataAndPrivateOwnership(object flag)
        {
            var output=new Output{Flag=flag,Available=flag};var load=new Load();using var w=Wrapper(output,load);
            Assert.That(w.Output.AreResultsAvailable(),Is.True);var q=w.Output.GetMemberEndForces(" Caller ",7,9,MemberEnd.End,LocalOrGlobal.Global);
            Assert.That(q.IsSuccessful,Is.True);Assert.That(q.CoordinateSystem,Is.EqualTo(LocalOrGlobal.Global));Assert.That(q.Forces.Fx,Is.EqualTo(1));Assert.That(q.Forces.Mz,Is.EqualTo(6));
            Assert.That(q.LoadCaseTitle,Is.EqualTo(" Caller "));Assert.That(output.Calls,Is.EqualTo(new[]{"available","7:1:9:1"}));Assert.That(load.Calls,Is.Empty);
        }
        [TestCaseSource(nameof(InvalidFlags))]
        public void MalformedFlagsNeverBecomeAvailabilityOrForceSuccess(object flag)
        {
            var output=new Output{Available=flag,Flag=flag};using var w=Wrapper(output,new Load());
            Assert.Throws<InvalidOperationException>(()=>w.Output.AreResultsAvailable());
            Assert.Throws<InvalidOperationException>(()=>w.Output.GetMemberEndForces("t",1,1,MemberEnd.Start));
        }
        [TestCase(false)] [TestCase(0)]
        public void NativeFalseDiscardsMalformedPartialBuffer(object flag)
        {
            var output=new Output{Flag=flag,Buffer=_=>null};using var w=Wrapper(output,new Load());
            var q=w.Output.GetMemberEndForces("t",1,1,MemberEnd.Start);Assert.That(q.IsSuccessful,Is.False);Assert.That(q.Forces,Is.Null);Assert.That(q.BeamId,Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void NativeSuccessRequiresSixFiniteWrittenDoubles(int shape)
        {
            var output=new Output{Buffer=original=>shape switch{0=>null,1=>new double[5],2=>new float[6],3=>new double[2,3],4=>original,5=>new[]{1d,2,3,4,5,double.PositiveInfinity},_=>new object[]{1d,2d,3d,4d,5d,6d}}};
            using var w=Wrapper(output,new Load());Assert.Throws<InvalidOperationException>(()=>w.Output.GetMemberEndForces("t",1,1,MemberEnd.Start));
        }
        [Test]
        public void ReplacedNonzeroBoundArrayIsCopiedWithoutLosingComponents()
        {
            Array a=Array.CreateInstance(typeof(double),new[]{6},new[]{-2});for(int i=0;i<6;i++)a.SetValue((double)i,-2+i);
            using var w=Wrapper(new Output{Buffer=_=>a},new Load());var q=w.Output.GetMemberEndForces("",1,2,MemberEnd.Start);a.SetValue(99d,-2);
            Assert.That(q.Forces.Fx,Is.Zero);Assert.That(q.Forces.Mz,Is.EqualTo(5));
        }
        [TestCase(0,1,0,0)] [TestCase(1,0,0,0)] [TestCase(1,1,-1,0)] [TestCase(1,1,2,0)] [TestCase(1,1,0,2)]
        public void InvalidIdentifiersAndEnumsNeverDispatch(int beam,int load,int end,int axes)
        {
            var output=new Output();using var w=Wrapper(output,new Load());Assert.Throws<ArgumentOutOfRangeException>(()=>w.Output.GetMemberEndForces("t",beam,load,(MemberEnd)end,(LocalOrGlobal)axes));Assert.That(output.Calls,Is.Empty);
        }
        [Test]
        public void InvalidTitlesAndIdsAreNamedAndNativeEmptyTitleIsPreserved()
        {
            var output=new Output();var load=new Load{Title=""};using var w=Wrapper(output,load);
            Assert.That(Assert.Throws<ArgumentNullException>(()=>w.Output.GetMemberEndForces(null,1,1,MemberEnd.Start)).ParamName,Is.EqualTo("lcTitle"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>w.Load.GetLoadCaseTitle(0)).ParamName,Is.EqualTo("lcId"));
            Assert.That(w.Load.GetLoadCaseTitle(1),Is.Empty);load.Title=null;Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCaseTitle(1));Assert.That(output.Calls,Is.Empty);
        }
        [Test]
        public void BatchSnapshotsOnceDeduplicatesAndRetainsEqualComponentsWithDistinctIdentities()
        {
            var output=new Output();var load=new Load();using var w=Wrapper(output,load);int enumerations=0;
            IEnumerable<int> Beams(){enumerations++;yield return 2;yield return 1;yield return 2;}
            var r=w.Root.RetrieveMemberForces(Beams(),new[]{4,3,4});
            Assert.That(enumerations,Is.EqualTo(1));Assert.That(r.IsSuccessful,Is.True);Assert.That(r.Queries,Has.Count.EqualTo(8));Assert.That(r.Forces,Has.Count.EqualTo(8));
            Assert.That(load.Calls,Is.EqualTo(new[]{4,3}));Assert.That(output.Calls.Take(5),Is.EqualTo(new[]{"available","2:0:4:0","2:1:4:0","1:0:4:0","1:1:4:0"}));
            r.Forces[0].Fx=99;Assert.That(r.Forces[1].Fx,Is.EqualTo(1));Assert.Throws<NotSupportedException>(()=>((IList<StaadPro.Interop.Core.Models.Root.MemberForceQueryResult>)r.Queries).Clear());
        }
        [Test]
        public void BatchFalseAvailabilityAndPerEndFailuresHaveDifferentOutcomes()
        {
            var output=new Output{Available=false};var load=new Load();using var w=Wrapper(output,load);
            var r=w.Root.RetrieveMemberForces(new[]{1},new[]{1});Assert.That(r.IsSuccessful,Is.False);Assert.That(r.Queries,Is.Empty);Assert.That(load.Calls,Is.Empty);
            output.Available=true;output.Response=(_,end,_)=>end==0;output.Buffer=_=>new double[]{1,2,3,4,5,6};r=w.Root.RetrieveMemberForces(new[]{1,2},new[]{1});
            Assert.That(r.IsSuccessful,Is.False);Assert.That(r.Queries,Has.Count.EqualTo(4));Assert.That(r.Forces,Has.Count.EqualTo(2));Assert.That(r.Queries[1].Forces,Is.Null);
        }
        [Test]
        public void EmptyBatchHasNoAvailabilityClaimAndInvalidInputsPreventNativeWork()
        {
            var output=new Output();var load=new Load();using var w=Wrapper(output,load);
            var empty=w.Root.RetrieveMemberForces(Array.Empty<int>(),new[]{1});Assert.That(empty.IsSuccessful,Is.True);Assert.That(empty.AreResultsAvailable,Is.Null);
            Assert.That(Assert.Throws<ArgumentNullException>(()=>w.Root.RetrieveMemberForces(null,new[]{1})).ParamName,Is.EqualTo("beams"));
            Assert.That(Assert.Throws<ArgumentNullException>(()=>w.Root.RetrieveMemberForces(new[]{1},null)).ParamName,Is.EqualTo("loadCases"));
            Assert.Throws<ArgumentException>(()=>w.Root.RetrieveMemberForces(new[]{1,0},new[]{1}));
            Assert.Throws<ArgumentException>(()=>w.Root.RetrieveMemberForces(Enumerable.Range(1,501),Enumerable.Range(1,100)));
            Assert.That(output.Calls,Is.Empty);Assert.That(load.Calls,Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void StaleDisposedOtherThreadAndReentrantCrossDomainCallsNeverDispatch(string operation)
        {
            var output=new Output();var load=new Load();using var w=Wrapper(output,load);var action=Capture(w,operation);Exception error=null;
            var worker=new Thread(()=>{try{action();}catch(Exception e){error=e;}});worker.Start();Assert.That(worker.Join(5000),Is.True);Assert.That(error,Is.TypeOf<InvalidOperationException>());
            Action callback=()=>{foreach(string n in Operations)Assert.Throws<InvalidOperationException>(()=>Capture(w,n)());Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());Assert.Throws<InvalidOperationException>(()=>w.Dispose());};
            output.Callback=load.Callback=callback;action();output.Callback=load.Callback=null;output.Calls.Clear();load.Calls.Clear();
            w.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>action());action=Capture(w,operation);w.Dispose();Assert.Throws<ObjectDisposedException>(()=>action());Assert.That(output.Calls,Is.Empty);Assert.That(load.Calls,Is.Empty);
        }
        [Test]
        public void OriginalNativeFailureAndEnumeratorFailurePropagateWithoutPartialBatchOrRetry()
        {
            var failure=new COMException("disconnected",unchecked((int)0x80010108));var output=new Output{Failure=failure};using var w=Wrapper(output,new Load());
            Assert.That(Assert.Throws<COMException>(()=>w.Root.RetrieveMemberForces(new[]{1},new[]{1})),Is.SameAs(failure));Assert.That(output.Calls,Has.Count.EqualTo(1));output.Calls.Clear();
            IEnumerable<int> Fail(){yield return 1;throw failure;}
            Assert.That(Assert.Throws<COMException>(()=>w.Root.RetrieveMemberForces(Fail(),new[]{1})),Is.SameAs(failure));Assert.That(output.Calls,Is.Empty);
        }
        [Test]
        public void ManualNoncurrentLoadAndOutputBindingsFailBeforeNativeWork()
        {
            var output=new Output();var load=new Load();using var w=Wrapper(output,load);
            Assert.Throws<InvalidOperationException>(()=>new OSLoadAdapter(w).GetLoadCaseTitle(1));Assert.Throws<InvalidOperationException>(()=>new OSOutputAdapter(w).AreResultsAvailable());Assert.That(output.Calls,Is.Empty);Assert.That(load.Calls,Is.Empty);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using Root=StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;
namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture,NonParallelizable,Apartment(ApartmentState.STA)]
    public sealed class LoadCaseMetadataTests
    {
        public sealed class Native
        {
            public object Count=2,PrimaryResult=true,ReferenceResult=2,Title="  Case\r\n",TypeCode=23;
            public object ArrayValue=new[]{7,3};public bool Write=true;public int Counts;public Func<int,object> CountAt;
            public Exception Failure;public Action Callback;public readonly List<string> Calls=[];
            private void Step(string name){Calls.Add(name);Callback?.Invoke();if(Failure!=null)throw Failure;}
            private object GetCount(string family){Step(family+"Count");return CountAt?.Invoke(++Counts)??Count;}
            public object GetPrimaryLoadCaseCount()=>GetCount("p");public object GetReferenceLoadCaseCount()=>GetCount("r");
            public object GetPrimaryLoadCaseNumbers(ref object buffer){Step("pNumbers");Assert.That(buffer,Is.TypeOf<int[]>());Assert.That((int[])buffer,Is.All.EqualTo(int.MinValue));if(Write)buffer=ArrayValue;return PrimaryResult;}
            public object GetReferenceLoadCaseNumbers(ref object buffer){Step("rNumbers");if(Write)buffer=ArrayValue;return ReferenceResult;}
            public object GetLoadCaseTitle(int id){Step("pTitle:"+id);return Title;}
            public object GetReferenceLoadCaseTitle(int id){Step("rTitle:"+id);return Title;}
            public object GetLoadType(int id){Step("pType:"+id);return TypeCode;}
            public object GetReferenceLoadType(int id){Step("rType:"+id);return TypeCode;}
        }
        private static OpenStaadWrapper Wrap(Native native)=>new(new Root{Load=native});
        private static readonly string[] Ops=["allPrimary","allReference","primary","reference","list"];
        private static Action Capture(OpenStaadWrapper w,string op)
        {
            var load=w.Load;return op switch{"allPrimary"=>()=>load.GetAllPrimaryLoadCases(),"allReference"=>()=>load.GetAllReferenceLoadCases(),"primary"=>()=>load.GetPrimaryLoadCaseFromId(7),"reference"=>()=>load.GetReferenceLoadCaseFromId(7),_=>()=>load.GetPrimaryLoadCasesFromIds(new[]{7,3})};
        }
        [Test]
        public void BooleanPrimarySuccessAndIntegralCountRemainDistinctAndReferenceUsesOwnFamily()
        {
            var n=new Native();using var w=Wrap(n);var primary=w.Load.GetAllPrimaryLoadCases();
            Assert.That(primary.Select(x=>x.Id),Is.EquivalentTo(new[]{7,3}));Assert.That(primary.All(x=>x.CaseType==LoadCaseType.PrimaryLoad&&x.Type==LoadType.None),Is.True);
            Assert.That(n.Calls,Is.EqualTo(new[]{"pCount","pNumbers","pCount","pTitle:7","pType:7","pTitle:3","pType:3"}));
            n.Calls.Clear();n.PrimaryResult=2;w.Load.GetAllPrimaryLoadCases();Assert.That(n.Calls.Count(x=>x=="pCount"),Is.EqualTo(1));
            n.Calls.Clear();var reference=w.Load.GetAllReferenceLoadCases();Assert.That(reference.All(x=>x.CaseType==LoadCaseType.ReferenceLoad),Is.True);Assert.That(n.Calls.All(x=>x.StartsWith("r")),Is.True);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)] [TestCase(16)] [TestCase(17)]
        [TestCase(18)] [TestCase(19)] [TestCase(20)] [TestCase(21)] [TestCase(22)] [TestCase(23)]
        public void EveryDocumentedEngineeringTypeAndExactTitleIsRetainedInIndependentModels(int code)
        {
            var n=new Native{TypeCode=code};using var w=Wrap(n);var a=w.Load.GetPrimaryLoadCaseFromId(7);var b=w.Load.GetReferenceLoadCaseFromId(7);
            Assert.That(a.Type,Is.EqualTo((LoadType)code));Assert.That(b.Type,Is.EqualTo((LoadType)code));Assert.That(a.Title,Is.EqualTo(n.Title));Assert.That(a,Is.Not.SameAs(b));
            Assert.That(a.CaseType,Is.EqualTo(LoadCaseType.PrimaryLoad));Assert.That(b.CaseType,Is.EqualTo(LoadCaseType.ReferenceLoad));
            n.Title="";Assert.That(w.Load.GetPrimaryLoadCaseFromId(7).Title,Is.Empty);n.Title="NONE";Assert.That(w.Load.GetPrimaryLoadCaseFromId(7).Title,Is.EqualTo("NONE"));
        }
        [TestCase(null)] [TestCase(true)] [TestCase(-1)] [TestCase(100001)] [TestCase(1d)] [TestCase(long.MaxValue)]
        public void InvalidNativeCountsRejectBeforeAllocationOrNumberQueries(object count)
        {
            var n=new Native{Count=count};using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllPrimaryLoadCases());Assert.That(n.Calls,Is.EqualTo(new[]{"pCount"}));
        }
        [TestCase(false)] [TestCase(0)] [TestCase(1)] [TestCase("2")] [TestCase(2d)] [TestCase(null)]
        public void InvalidPrimaryNumberStatusDoesNotBecomeASuccessfulInventory(object status)
        {
            var n=new Native{PrimaryResult=status};using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllPrimaryLoadCases());Assert.That(n.Calls.Any(x=>x.Contains("Title")),Is.False);
        }
        [Test]
        public void ReferenceBooleanIsRejectedAndBooleanPrimaryRequiresFreshCountAgreement()
        {
            var n=new Native{ReferenceResult=true};using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllReferenceLoadCases());n.Calls.Clear();
            n.CountAt=i=>i==1?2:3;Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllPrimaryLoadCases());Assert.That(n.Calls.Any(x=>x.Contains("Title")),Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void MalformedUnwrittenOrDuplicateIdsPreventAllMetadataReads(int shape)
        {
            var n=new Native();n.ArrayValue=shape switch{0=>null,1=>new[]{1},2=>new object[]{1,2},3=>new int[1,2],4=>new[]{7,7},5=>new[]{7,0},_=>new[]{7,-1}};
            using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllPrimaryLoadCases());Assert.That(n.Calls.Any(x=>x.Contains("Title")),Is.False);
            n.Write=false;Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllPrimaryLoadCases());
        }
        [Test]
        public void NonzeroBoundIdsAndZeroCountBehaveWithoutInventedData()
        {
            Array a=Array.CreateInstance(typeof(int),new[]{2},new[]{-1});a.SetValue(7,-1);a.SetValue(3,0);var n=new Native{ArrayValue=a};using var w=Wrap(n);
            Assert.That(w.Load.GetAllPrimaryLoadCases().Select(x=>x.Id),Is.EquivalentTo(new[]{7,3}));n.Count=0;n.Calls.Clear();
            var empty=w.Load.GetAllReferenceLoadCases();Assert.That(empty,Is.Empty);Assert.That(n.Calls,Is.EqualTo(new[]{"rCount"}));Assert.That(w.Load.GetAllReferenceLoadCases(),Is.Not.SameAs(empty));
        }
        [TestCase(-1)] [TestCase(24)] [TestCase(true)] [TestCase(null)] [TestCase("0")] [TestCase(0d)]
        public void UndefinedOrMalformedEngineeringTypesThrowAfterLiteralTitleRead(object code)
        {
            var n=new Native{TypeCode=code};using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Load.GetPrimaryLoadCaseFromId(7));Assert.That(n.Calls,Is.EqualTo(new[]{"pTitle:7","pType:7"}));
        }
        [TestCase(null)] [TestCase(1)] [TestCase(false)]
        public void InvalidTitleDoesNotReachTypeRead(object title)
        {
            var n=new Native{Title=title};using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Load.GetReferenceLoadCaseFromId(7));Assert.That(n.Calls,Is.EqualTo(new[]{"rTitle:7"}));
        }
        [Test]
        public void OrderedInputPreservesDuplicatesAndCapturesOnceBeforeNativeCalls()
        {
            var n=new Native();using var w=Wrap(n);int enumerations=0;IEnumerable<int> Ids(){enumerations++;yield return 7;yield return 3;yield return 7;}
            var result=w.Load.GetPrimaryLoadCasesFromIds(Ids());Assert.That(enumerations,Is.EqualTo(1));Assert.That(result.Select(x=>x.Id),Is.EqualTo(new[]{7,3,7}));Assert.That(result[0],Is.Not.SameAs(result[2]));
            result[0].Type=LoadType.Wind;Assert.That(result[2].Type,Is.EqualTo(LoadType.None));n.Calls.Clear();Assert.That(w.Load.GetPrimaryLoadCasesFromIds(Array.Empty<int>()),Is.Empty);Assert.That(n.Calls,Is.Empty);
        }
        [Test]
        public void AllInvalidInputsAndEnumeratorFailuresPreventMetadataAccess()
        {
            var n=new Native();using var w=Wrap(n);Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>w.Load.GetPrimaryLoadCaseFromId(0)).ParamName,Is.EqualTo("lcId"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>w.Load.GetReferenceLoadCaseFromId(-1));Assert.Throws<ArgumentNullException>(()=>w.Load.GetPrimaryLoadCasesFromIds(null));
            Assert.Throws<ArgumentOutOfRangeException>(()=>w.Load.GetPrimaryLoadCasesFromIds(new[]{7,0}));Assert.Throws<ArgumentException>(()=>w.Load.GetPrimaryLoadCasesFromIds(Enumerable.Range(1,100001)));
            var failure=new Exception("enumeration");IEnumerable<int> Fail(){yield return 7;throw failure;}
            Assert.That(Assert.Throws<Exception>(()=>w.Load.GetPrimaryLoadCasesFromIds(Fail())),Is.SameAs(failure));Assert.That(n.Calls,Is.Empty);
        }
        [TestCaseSource(nameof(Ops))]
        public void GuardedLifetimeThreadsAndReentrancyAndOriginalFailuresArePreserved(string operation)
        {
            var n=new Native();using var w=Wrap(n);var action=Capture(w,operation);Exception error=null;var worker=new Thread(()=>{try{action();}catch(Exception e){error=e;}});worker.Start();Assert.That(worker.Join(5000),Is.True);Assert.That(error,Is.TypeOf<InvalidOperationException>());
            n.Callback=()=>{foreach(string op in Ops)Assert.Throws<InvalidOperationException>(()=>Capture(w,op)());Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());Assert.Throws<InvalidOperationException>(()=>w.Dispose());};action();n.Callback=null;n.Calls.Clear();
            var failure=new COMException("native failure",unchecked((int)0x80004005));n.Failure=failure;Assert.That(Assert.Throws<COMException>(()=>action()),Is.SameAs(failure));Assert.That(n.Calls,Has.Count.EqualTo(1));n.Failure=null;n.Calls.Clear();
            w.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>action());action=Capture(w,operation);w.Dispose();Assert.Throws<ObjectDisposedException>(()=>action());Assert.That(n.Calls,Is.Empty);
        }
        [Test]
        public void LoadCaseConstructorEqualityDefaultsAndNotificationsStayDetached()
        {
            var a=new LoadCase();Assert.That(a.Type,Is.EqualTo(LoadType.None));Assert.That(a.CaseType,Is.EqualTo(LoadCaseType.PrimaryLoad));int notifications=0;a.PropertyChanged+=(_,_)=>notifications++;a.Id=7;a.Title="one";a.Type=LoadType.Dead;
            var b=new LoadCase(7,"other",LoadCaseType.PrimaryLoad,LoadType.Dead);Assert.That(a==b,Is.True);Assert.That(a.GetHashCode(),Is.EqualTo(b.GetHashCode()));Assert.That(notifications,Is.EqualTo(3));Assert.That(a!=null,Is.True);Assert.That((LoadCase)null==null,Is.True);
        }
    }
}

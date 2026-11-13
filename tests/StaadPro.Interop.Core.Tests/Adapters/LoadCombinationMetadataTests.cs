using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class LoadCombinationMetadataTests
    {
        private static readonly string[] Operations = ["reference", "single", "list", "all", "family"];
        private static readonly object[] GoodNumberReturns = [true, (sbyte)2, (byte)2, (short)2, (ushort)2, 2, 2u, 2L, 2UL];
        private static readonly object[] BadNumberReturns = [false, null, 0, 1, -1, "2", 2d, 2m, DayOfWeek.Tuesday, ulong.MaxValue];
        private static readonly object[] BadCounts = [null, true, false, -1, 100001, uint.MaxValue, "2", 2d, 2m, DayOfWeek.Tuesday];
        private static readonly object[] BadArrays = [null, new int[0], new[]{201}, new[]{201,201}, new[]{201,0},
            new[]{201,-1}, new[]{201,int.MinValue}, new long[]{201,301}, new object[]{201,301}, new int[1,2]];

        public sealed class Native
        {
            public object Count = 2, CombinationResult = true, PrimaryResult = true, ReferenceResult = 2;
            public object CombinationIds = new[]{201,301}, PrimaryIds = new[]{7,3}, ReferenceIds = new[]{7,3};
            public object Title = "  Ω case\r\n ", TypeCode = 23;
            public bool WriteNumbers = true;
            public Exception Failure;
            public Action<string> OnCall;
            public Func<string,int,object> CountAt;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            private readonly Dictionary<string,int> countCalls = [];
            private void Step(string name)
            {
                Calls.Add(name); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(name); if(Failure != null) throw Failure;
            }
            private object GetCount(string family)
            {
                Step(family+"Count");
                countCalls.TryGetValue(family,out int times); countCalls[family]=++times;
                return CountAt==null?Count:CountAt(family,times);
            }
            private object Numbers(string family, ref object buffer, object ids, object flag)
            {
                Assert.That(buffer, Is.TypeOf<int[]>()); Assert.That((int[])buffer, Is.All.EqualTo(int.MinValue));
                Step(family+"Numbers"); if(WriteNumbers)buffer=ids; return flag;
            }
            public object GetPrimaryLoadCaseCount()=>GetCount("p");
            public object GetReferenceLoadCaseCount()=>GetCount("r");
            public object GetLoadCombinationCaseCount()=>GetCount("c");
            public object GetPrimaryLoadCaseNumbers(ref object ids)=>Numbers("p",ref ids,PrimaryIds,PrimaryResult);
            public object GetReferenceLoadCaseNumbers(ref object ids)=>Numbers("r",ref ids,ReferenceIds,ReferenceResult);
            public object GetLoadCombinationCaseNumbers(ref object ids)=>Numbers("c",ref ids,CombinationIds,CombinationResult);
            public object GetLoadCaseTitle(int id){Step("title:"+id);return Title;}
            public object GetReferenceLoadCaseTitle(int id){Step("rTitle:"+id);return Title;}
            public object GetLoadType(int id){Step("type:"+id);return TypeCode;}
            public object GetReferenceLoadType(int id){Step("rType:"+id);return TypeCode;}
        }

        private static OpenStaadWrapper Wrap(Native native)=>new(new MetadataRoot{Load=native});
        private static IEnumerable<ILoadCase> Request(IOSLoad load,string operation)=>operation switch
        {
            "reference"=>load.GetReferenceLoadCasesFromIds(new[]{7,3,7}),
            "single"=>new[]{load.GetLoadCombinationCaseFromId(201)},
            "list"=>load.GetLoadCombinationCasesFromIds(new[]{201,301,201}),
            "all"=>load.GetAllLoadCombinationCases(),
            "family"=>load.GetAllLoadCasesOfType(LoadCaseType.LoadCombination),
            _=>throw new ArgumentOutOfRangeException(nameof(operation))
        };

        [Test]
        public void ReferenceListsPreserveOrderDuplicatesLiteralTitlesAndIndependentEditableObjects()
        {
            var n=new Native{Title=" NONE Ω\r\n "};using var w=Wrap(n);
            var cases=w.Load.GetReferenceLoadCasesFromIds(new Once<int>([7,3,7]));
            Assert.That(cases.Select(c=>c.Id),Is.EqualTo(new[]{7,3,7}));
            Assert.That(cases.All(c=>c.CaseType==LoadCaseType.ReferenceLoad&&c.Type==LoadType.None&&c.Title==" NONE Ω\r\n "),Is.True);
            Assert.That(cases[0],Is.Not.SameAs(cases[2]));((LoadCase)cases[0]).Title="edited";Assert.That(cases[2].Title,Is.EqualTo(n.Title));
            Assert.That(n.Calls,Is.EqualTo(new[]{"rTitle:7","rType:7","rTitle:3","rType:3","rTitle:7","rType:7"}));
            int before=n.Calls.Count;Assert.That(cases.Count(),Is.EqualTo(3));Assert.That(n.Calls,Has.Count.EqualTo(before));
        }

        [TestCaseSource(nameof(GoodNumberReturns))]
        public void CombinationEnumerationDistinguishesBooleanSuccessFromMatchingIntegralCounts(object flag)
        {
            var n=new Native{CombinationResult=flag,TypeCode=999};using var w=Wrap(n);
            var cases=w.Load.GetAllLoadCombinationCases();
            Assert.That(cases.Select(c=>c.Id),Is.EquivalentTo(new[]{201,301}));
            Assert.That(cases.All(c=>c.Type==LoadType.None&&c.CaseType==LoadCaseType.LoadCombination),Is.True);
            Assert.That(n.Calls,Is.EqualTo(flag is bool?new[]{"cCount","cNumbers","cCount","title:201","title:301"}:
                new[]{"cCount","cNumbers","title:201","title:301"}));
        }

        [TestCaseSource(nameof(BadNumberReturns))]
        public void InvalidCombinationNumberReturnsNeverBecomeSuccessOrPartialMetadata(object flag)
        {
            var n=new Native{CombinationResult=flag};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllLoadCombinationCases());
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cNumbers"}));
        }

        [TestCaseSource(nameof(BadCounts))]
        public void InvalidAndOversizedCountsRejectBeforeArrayAllocationAndNumberCalls(object count)
        {
            var n=new Native{Count=count};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllLoadCombinationCases());
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount"}));
        }

        [TestCaseSource(nameof(BadArrays))]
        public void MalformedUnwrittenDuplicateOrNonpositiveInventoryIdsRejectBeforeTitles(object ids)
        {
            var n=new Native{CombinationIds=ids};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllLoadCombinationCases());
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cNumbers"}));
        }

        [Test]
        public void UnwrittenAndChangedInventoriesCannotMasqueradeAsValidCombinationLists()
        {
            var n=new Native{WriteNumbers=false};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllLoadCombinationCases());
            n.WriteNumbers=true;n.Calls.Clear();n.CountAt=(_,times)=>times<=2?2:3;
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllLoadCombinationCases());
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cNumbers","cCount"}));
        }

        [Test]
        public void ReplacementArraysWithNonzeroLowerBoundsAndMaximumPositiveIdsAreSupported()
        {
            var array=Array.CreateInstance(typeof(int),new[]{2},new[]{-3});array.SetValue(int.MaxValue,-3);array.SetValue(301,-2);
            var n=new Native{CombinationIds=array};using var w=Wrap(n);
            Assert.That(w.Load.GetAllLoadCombinationCases().Select(c=>c.Id),Is.EquivalentTo(new[]{int.MaxValue,301}));
            array.SetValue(999,-3);Assert.That(w.Load.GetLoadCombinationCaseFromId(999).Id,Is.EqualTo(999));
        }

        [Test]
        public void SingleAndRequestedCombinationsVerifyMembershipBeforeAnyTitleAndDoNotReadOtherTitles()
        {
            var n=new Native();using var w=Wrap(n);
            var single=w.Load.GetLoadCombinationCaseFromId(301);Assert.That(single.Id,Is.EqualTo(301));
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cNumbers","cCount","title:301"}));
            n.Calls.Clear();Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCombinationCaseFromId(7));
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cNumbers","cCount"}));
            n.Calls.Clear();Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCombinationCasesFromIds(new[]{201,999}));
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cNumbers","cCount"}));
        }

        [Test]
        public void CombinationInputIsEnumeratedOnceDeduplicatedAndMaterializedBeforeReturn()
        {
            var n=new Native();using var w=Wrap(n);var ids=new List<int>{301,201,301};
            var cases=w.Load.GetLoadCombinationCasesFromIds(new Once<int>(ids));ids.Clear();
            Assert.That(cases.Select(c=>c.Id),Is.EquivalentTo(new[]{201,301}));
            Assert.That(n.Calls.Count(c=>c=="cCount"),Is.EqualTo(2));
            Assert.That(n.Calls.Count(c=>c.StartsWith("title:")),Is.EqualTo(2));
            int count=n.Calls.Count;Assert.That(cases.Count(),Is.EqualTo(2));Assert.That(n.Calls,Has.Count.EqualTo(count));
            var next=w.Load.GetLoadCombinationCasesFromIds(new[]{201});
            Assert.That(next.Single(),Is.Not.SameAs(cases.Single(c=>c.Id==201)));
        }

        [Test]
        public void EmptyRequestsMakeNoNativeCallsWhileZeroNativeInventoriesOnlyReadCount()
        {
            var n=new Native{Count=0};using var w=Wrap(n);
            Assert.That(w.Load.GetReferenceLoadCasesFromIds(Array.Empty<int>()),Is.Empty);
            Assert.That(w.Load.GetLoadCombinationCasesFromIds(Array.Empty<int>()),Is.Empty);Assert.That(n.Calls,Is.Empty);
            var first=w.Load.GetAllLoadCombinationCases();var second=w.Load.GetAllLoadCombinationCases();
            Assert.That(first,Is.Empty);Assert.That(second,Is.Not.SameAs(first));
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCombinationCaseFromId(201));
            Assert.That(n.Calls,Is.EqualTo(new[]{"cCount","cCount","cCount"}));
        }

        [TestCase("reference"),TestCase("list")]
        public void RequestedListsValidateCompletelyBeforeNativeReadsIncludingLimitAndSourceFailures(string operation)
        {
            var n=new Native();using var w=Wrap(n);
            void Invoke(IEnumerable<int> ids){if(operation=="reference")w.Load.GetReferenceLoadCasesFromIds(ids);else w.Load.GetLoadCombinationCasesFromIds(ids);}
            string parameter=operation=="reference"?"loadCasesIds":"lcIds";
            Assert.That(Assert.Throws<ArgumentNullException>(()=>Invoke(null)).ParamName,Is.EqualTo(parameter));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(new[]{201,0})).ParamName,Is.EqualTo(parameter));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Invoke(new[]{201,-1}));
            Assert.That(Assert.Throws<ArgumentException>(()=>Invoke(Enumerable.Repeat(201,100001))).ParamName,Is.EqualTo(parameter));
            Assert.Throws<ApplicationException>(()=>Invoke(Broken()));
            Assert.That(n.Calls,Is.Empty);
        }

        [TestCase(0),TestCase(-1)]
        public void InvalidSingleIdFailsBeforeInventoryReads(int id)
        {
            var n=new Native();using var w=Wrap(n);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>w.Load.GetLoadCombinationCaseFromId(id)).ParamName,Is.EqualTo("lcId"));
            Assert.That(n.Calls,Is.Empty);
        }

        [TestCase(LoadCaseType.PrimaryLoad),TestCase(LoadCaseType.ReferenceLoad),TestCase(LoadCaseType.LoadCombination)]
        public void FamilyRoutingUsesTheCorrectInventoryTitleAndEngineeringClassification(LoadCaseType family)
        {
            var n=new Native{TypeCode=0};using var w=Wrap(n);var cases=w.Load.GetAllLoadCasesOfType(family).ToArray();
            string prefix=family==LoadCaseType.PrimaryLoad?"p":family==LoadCaseType.ReferenceLoad?"r":"c";
            Assert.That(n.Calls[0],Is.EqualTo(prefix+"Count"));Assert.That(cases,Has.Length.EqualTo(2));
            Assert.That(cases.All(c=>c.CaseType==family&&c.Type==(family==LoadCaseType.LoadCombination?LoadType.None:LoadType.Dead)),Is.True);
            if(family==LoadCaseType.ReferenceLoad)Assert.That(n.Calls,Does.Contain("rTitle:7"));
            if(family==LoadCaseType.LoadCombination)Assert.That(n.Calls.Any(c=>c.Contains("Type")||c.StartsWith("type:")),Is.False);
        }

        [TestCase(LoadCaseType.RepeatLoad),TestCase((LoadCaseType)0),TestCase((LoadCaseType)int.MaxValue)]
        public void UnsupportedFamiliesRejectBeforeNativeAccess(LoadCaseType family)
        {
            var n=new Native();using var w=Wrap(n);
            Assert.Throws<NotSupportedException>(()=>w.Load.GetAllLoadCasesOfType(family));Assert.That(n.Calls,Is.Empty);
        }

        [TestCase(null),TestCase(1),TestCase(true)]
        public void MalformedCombinationTitlesRemainFailures(object title)
        {
            var n=new Native{Title=title};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCombinationCaseFromId(201));
            Assert.That(n.Calls.Last(),Is.EqualTo("title:201"));
        }

        [TestCase(""),TestCase(" NONE "),TestCase("Ω\r\n")]
        public void ExplicitNativeTitleTextIsPreservedExactly(string title)
        {
            var n=new Native{Title=title};using var w=Wrap(n);
            Assert.That(w.Load.GetLoadCombinationCaseFromId(201).Title,Is.EqualTo(title));
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeExceptionsPreserveIdentityAndHResultWithoutRetryAndReleaseLease(string operation)
        {
            var error=new COMException("native failure",unchecked((int)0x80010108));
            var n=new Native{Failure=error};using var w=Wrap(n);
            Assert.That(Assert.Throws<COMException>(()=>Request(w.Load,operation)),Is.SameAs(error));
            Assert.That(n.Calls,Has.Count.EqualTo(1));n.Failure=null;
            Assert.DoesNotThrow(()=>Request(w.Load,operation));Assert.DoesNotThrow(()=>w.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingNativeMembersRemainFailuresWithoutFallback(string operation)
        {
            using var w=new OpenStaadWrapper(new MetadataRoot{Load=new object()});
            Assert.Throws<RuntimeBinderException>(()=>Request(w.Load,operation));Assert.DoesNotThrow(()=>w.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void CurrentBindingThreadDisposalAndManualAdapterGuardsPrecedeAllNativeCalls(string operation)
        {
            var n=new Native();using var w=Wrap(n);var adapter=w.Load;
            Assert.Throws<InvalidOperationException>(()=>Request(new OSLoadAdapter(w),operation));
            Exception error=null;var worker=new Thread(()=>{try{Request(adapter,operation);}catch(Exception ex){error=ex;}});
            worker.SetApartmentState(ApartmentState.MTA);worker.Start();Assert.That(worker.Join(5000),Is.True);Assert.That(error,Is.TypeOf<InvalidOperationException>());
            w.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>Request(adapter,operation));
            adapter=w.Load;w.Dispose();Assert.Throws<ObjectDisposedException>(()=>Request(adapter,operation));Assert.That(n.Calls,Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void LeaseRejectsNestedDomainRootRefreshAndDisposalWork(string operation)
        {
            var n=new Native();using var w=Wrap(n);
            n.OnCall=_=>
            {
                foreach(string op in Operations)Assert.Throws<InvalidOperationException>(()=>Request(w.Load,op));
                Assert.Throws<InvalidOperationException>(()=>w.Root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(()=>w.Output.AreResultsAvailable());
                Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(()=>w.RefreshUnits());
                Assert.Throws<InvalidOperationException>(()=>w.Dispose());
            };
            Request(w.Load,operation);
            Assert.That(n.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));n.OnCall=null;Assert.DoesNotThrow(()=>w.Root.GetBaseUnit());
        }

        [Test]
        public void InputEnumerationRunsInsideTheSameLeaseBeforeAnyNativeRead()
        {
            var n=new Native();using var w=Wrap(n);
            var ids=new Once<int>([201],()=>{Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());Assert.That(n.Calls,Is.Empty);});
            Assert.That(w.Load.GetLoadCombinationCasesFromIds(ids).Single().Id,Is.EqualTo(201));
        }

        [TestCaseSource(nameof(Operations))]
        public void ExactSignaturesAndExpandedIntelliSenseMatchOnInterfaceAndAdapter(string operation)
        {
            string name=operation switch{"reference"=>"GetReferenceLoadCasesFromIds","single"=>"GetLoadCombinationCaseFromId",
                "list"=>"GetLoadCombinationCasesFromIds","all"=>"GetAllLoadCombinationCases",_=>"GetAllLoadCasesOfType"};
            Type[] parameters=operation=="reference"||operation=="list"?[typeof(IEnumerable<int>)]:operation=="single"?[typeof(int)]:operation=="family"?[typeof(LoadCaseType)]:[];
            string[] names=operation=="reference"?["loadCasesIds"]:operation=="list"?["lcIds"]:operation=="single"?["lcId"]:operation=="family"?["loadCaseType"]:[];
            Type result=operation=="reference"?typeof(List<ILoadCase>):operation=="single"?typeof(ILoadCase):operation=="all"?typeof(HashSet<ILoadCase>):typeof(IEnumerable<ILoadCase>);
            var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSLoad).Assembly.Location,".xml"));XElement previous=null;
            foreach(Type owner in new[]{typeof(IOSLoad),typeof(OSLoadAdapter)})
            {
                var method=owner.GetMethod(name,parameters);Assert.That(method,Is.Not.Null);Assert.That(method.ReturnType,Is.EqualTo(result));
                Assert.That(method.GetParameters().Select(p=>p.Name),Is.EqualTo(names));Assert.That(method.GetParameters().Any(p=>p.IsOut||p.ParameterType.IsByRef),Is.False);
                if(operation=="family"){Assert.That(method.GetParameters()[0].IsOptional,Is.True);Assert.That(method.GetParameters()[0].DefaultValue,Is.EqualTo(LoadCaseType.PrimaryLoad));}
                else Assert.That(method.GetParameters().Any(p=>p.IsOptional),Is.False);
                string prefix="M:"+owner.FullName+"."+name;
                var doc=docs.Descendants("member").Single(e=>((string)e.Attribute("name")).StartsWith(prefix+"(")||(string)e.Attribute("name")==prefix);
                foreach(string tag in new[]{"summary","returns","remarks","exception","example"})Assert.That(doc.Element(tag),Is.Not.Null);
                Assert.That(doc.Elements("param").Select(e=>(string)e.Attribute("name")),Is.EqualTo(names));
                Assert.That(doc.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);
                if(previous!=null)Assert.That(doc.Elements().Select(e=>e.ToString()),Is.EqualTo(previous.Elements().Select(e=>e.ToString())));
                previous=doc;
            }
        }

        private static IEnumerable<int> Broken(){yield return 201;throw new ApplicationException("enumerator failed");}
        private sealed class Once<T>:IEnumerable<T>
        {
            private readonly IEnumerable<T> values;private readonly Action before;private bool used;
            internal Once(IEnumerable<T> values,Action before=null){this.values=values;this.before=before;}
            public IEnumerator<T> GetEnumerator(){if(used)throw new InvalidOperationException("Enumerated twice.");used=true;before?.Invoke();return values.GetEnumerator();}
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
    }
}

using System;
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
using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Interfaces;
using StaadPro.Interop.Core.Models;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class LoadDefinitionsAndBucklingTests
    {
        private static readonly string[] Operations = ["detail", "all", "combination", "available", "count"];
        private static readonly object[] BadStatuses = [null, true, false, 0, -1, 2, "1", 1d, 1m, DayOfWeek.Monday, ulong.MaxValue];
        private static readonly object[] BadCounts = [null, true, false, -1, 100001, "2", 2d, 2m, uint.MaxValue, DayOfWeek.Monday];
        private static readonly object[] BadIds = [null, new int[0], new[]{7}, new[]{7,7}, new[]{7,0}, new[]{7,-1},
            new[]{7,int.MinValue}, new long[]{7,3}, new object[]{7,3}, new int[1,2]];
        private static readonly object[] BadFactors = [null, new double[0], new[]{1d}, new[]{1d,double.NaN},
            new[]{double.PositiveInfinity,1d}, new[]{1d,double.NegativeInfinity}, new[]{1m,2m}, new object[]{1d,2d}, new double[1,2], new double[4]];
        private static readonly object[] SuccessIntegers = [(sbyte)1,(byte)1,(short)1,(ushort)1,1,1u,1L,1UL];
        private static readonly object[] AvailabilityFlags = [true,false,0,1,(short)0,1L,0u,(byte)1];
        private static readonly object[] BadAvailability = [null,-1,2,"1",1d,1m,DayOfWeek.Monday,ulong.MaxValue];
        private static readonly object[] BadBucklingCounts = [null,true,false,-1,"0",0d,0m,2147483648L,uint.MaxValue,DayOfWeek.Monday];

        public sealed class Native
        {
            public object EnvelopeCount = 2, EnvelopeStatus = 1, EnvelopeIds = new[]{9,2};
            public int EnvelopeType = 5, MemberCount = 2;
            public object DetailsStatus = 1, MembersStatus = 1, Members = new[]{7,3};
            public bool WriteType = true, WriteCount = true, WriteMembers = true;
            public Func<int,int,int> TypeAt, CountAt;
            public Func<int,object> MembersAt;
            public object PairCount = 2, PairStatus = true, PairIds = new[]{7,3}, Factors = new[]{1.5,-.7};
            public bool WritePairIds = true, WriteFactors = true;
            public Func<int,object> PairCountAt;
            public object CombinationStatus = true, PrimaryStatus = true;
            public object CombinationIds = new[]{201,301}, PrimaryIds = new[]{7,3};
            public object PrimaryCount = 2, TypeCode = 23, Title = "  literal case  ";
            public Func<int,object> TitleAt;
            public object BucklingAvailable = true, BucklingCount = 2;
            public Exception Failure;
            public Action<string> OnCall;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            private readonly Dictionary<int,int> detailCalls = [];
            private int pairCalls;
            private void Step(string name)
            {
                Calls.Add(name);
                Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(name);
                if (Failure != null) throw Failure;
            }
            public object GetEnvelopeCount() { Step("envelopeCount"); return EnvelopeCount; }
            public object GetEnvelopeIDs(ref object buffer) { Step("envelopeIds"); buffer = EnvelopeIds; return EnvelopeStatus; }
            public object GetLoadEnvelopeDetails(int id, ref int type, ref int count)
            {
                Assert.That(type, Is.EqualTo(int.MinValue));
                Assert.That(count, Is.EqualTo(int.MinValue));
                Step("detail:" + id);
                detailCalls.TryGetValue(id, out int times);
                detailCalls[id] = ++times;
                if (WriteType) type = TypeAt == null ? EnvelopeType : TypeAt(id,times);
                if (WriteCount) count = CountAt == null ? MemberCount : CountAt(id,times);
                return DetailsStatus;
            }
            public object GetLoadListfromLoadEnvelope(int id, ref object buffer)
            {
                Assert.That(buffer, Is.TypeOf<int[]>());
                Assert.That((int[])buffer, Is.All.EqualTo(int.MinValue));
                Step("members:" + id);
                if (WriteMembers) buffer = MembersAt == null ? Members : MembersAt(id);
                return MembersStatus;
            }
            public object GetNoOfLoadAndFactorPairsForCombination(int id)
            { Step("pairs:" + id); return PairCountAt == null ? PairCount : PairCountAt(++pairCalls); }
            public object GetLoadAndFactorForCombination(int id, ref object numbers, ref object factors)
            {
                Assert.That(numbers, Is.TypeOf<int[]>());
                Assert.That((int[])numbers, Is.All.EqualTo(int.MinValue));
                Assert.That(factors, Is.TypeOf<double[]>());
                Assert.That((double[])factors, Is.All.NaN);
                Assert.That(((double[])factors).Length, Is.EqualTo(((int[])numbers).Length + 1));
                Step("factors:" + id);
                if (WritePairIds) numbers = PairIds;
                if (WriteFactors) factors = Factors;
                return PairStatus;
            }
            public object GetLoadCombinationCaseCount() { Step("combinationCount"); return 2; }
            public object GetLoadCombinationCaseNumbers(ref object numbers)
            { Step("combinationIds"); numbers = CombinationIds; return CombinationStatus; }
            public object GetPrimaryLoadCaseCount() { Step("primaryCount"); return PrimaryCount; }
            public object GetPrimaryLoadCaseNumbers(ref object numbers)
            { Step("primaryIds"); numbers = PrimaryIds; return PrimaryStatus; }
            public object GetLoadCaseTitle(int id) { Step("title:" + id); return TitleAt == null ? Title : TitleAt(id); }
            public object GetLoadType(int id) { Step("type:" + id); return TypeCode; }
            public object IsBucklingAnalysisResultsAvailable() { Step("bucklingAvailable"); return BucklingAvailable; }
            public object GetNoOfBucklingFactors() { Step("bucklingCount"); return BucklingCount; }
        }

        private static OpenStaadWrapper Wrap(object native) => new(new NativeRoot { Load = native, Output = native });
        private static object Invoke(IOSLoad load, IOSOutput output, string operation) => operation switch
        {
            "detail" => load.GetLoadEnvelopeDetail(9),
            "all" => load.GetAllLoadEnvelopeDetails(),
            "combination" => load.GetLoadCombination(201),
            "available" => output.AreBucklingAnalysisResultsAvailable(),
            "count" => output.GetNumberOfBucklingFactorsComputed(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        [TestCase(0)] [TestCase(-1)]
        public void InvalidIdentityFailsBeforeNativeCalls(int id)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Load.GetLoadEnvelopeDetail(id)).ParamName, Is.EqualTo("envId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Load.GetLoadCombination(id)).ParamName, Is.EqualTo("id"));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void EnvelopeClassificationsAndOwnedMembershipArePreserved(int code)
        {
            var native = new Native { EnvelopeType = code }; using var wrapper = Wrap(native);
            var first = wrapper.Load.GetLoadEnvelopeDetail(9);
            Assert.That(first.Id, Is.EqualTo(9));
            Assert.That(first.EnvelopType, Is.EqualTo((LoadEnvelopType)code));
            Assert.That(first.LoadCasesCount, Is.EqualTo(2));
            Assert.That(first.LoadCaseIds, Is.EquivalentTo(new[]{7,3}));
            Assert.That(native.Calls, Is.EqualTo(new[]{"detail:9","members:9","detail:9"}));
            var second = wrapper.Load.GetLoadEnvelopeDetail(9);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.LoadCaseIds, Is.Not.SameAs(second.LoadCaseIds));
            ((int[])native.Members)[0] = 99;
            first.LoadCaseIds.Clear();
            Assert.That(second.LoadCaseIds, Does.Contain(7));
            Assert.That(first.LoadCasesCount, Is.EqualTo(2));
        }

        [TestCaseSource(nameof(SuccessIntegers))]
        public void EnvelopeNativeLongSuccessSupportsIntegralOne(object success)
        {
            var native = new Native { DetailsStatus = success, MembersStatus = success, EnvelopeStatus = success };
            using var wrapper = Wrap(native);
            Assert.That(wrapper.Load.GetLoadEnvelopeDetail(9).LoadCaseIds, Has.Count.EqualTo(2));
            Assert.That(wrapper.Load.GetAllLoadEnvelopeDetails(), Has.Count.EqualTo(2));
        }

        [TestCaseSource(nameof(BadStatuses))]
        public void EnvelopeStatusesRejectCoercionAndStopBeforeFurtherReads(object status)
        {
            var native = new Native { DetailsStatus = status }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            Assert.That(native.Calls, Is.EqualTo(new[]{"detail:9"}));
            native.DetailsStatus = 1; native.MembersStatus = status; native.Calls.Clear();
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            Assert.That(native.Calls, Is.EqualTo(new[]{"detail:9","members:9"}));
        }

        [TestCase(-1)] [TestCase(7)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void UndefinedOrUnwrittenEnvelopeTypeRejectsEvenForZeroCount(int type)
        {
            var native = new Native { EnvelopeType = type, MemberCount = 0 }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            Assert.That(native.Calls, Has.Count.EqualTo(1));
        }

        [TestCase(-1)] [TestCase(int.MinValue)] [TestCase(100001)]
        public void InvalidEnvelopeCountsFailBeforeMembershipAllocation(int count)
        {
            var native = new Native { MemberCount = count }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            Assert.That(native.Calls, Has.Count.EqualTo(1));
        }

        [TestCase(false,true)] [TestCase(true,false)]
        public void UnwrittenEnvelopeReferenceOutputsFail(bool type, bool count)
        {
            var native = new Native { WriteType = type, WriteCount = count }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
        }

        [Test]
        public void ZeroMembershipAndZeroInventorySkipTheirFetches()
        {
            var native = new Native { MemberCount = 0 }; using var wrapper = Wrap(native);
            Assert.That(wrapper.Load.GetLoadEnvelopeDetail(9).LoadCaseIds, Is.Empty);
            Assert.That(native.Calls, Is.EqualTo(new[]{"detail:9"}));
            native.EnvelopeCount = 0; native.Calls.Clear();
            var first = wrapper.Load.GetAllLoadEnvelopeDetails();
            Assert.That(first, Is.Empty);
            Assert.That(first, Is.Not.SameAs(wrapper.Load.GetAllLoadEnvelopeDetails()));
            Assert.That(native.Calls, Is.EqualTo(new[]{"envelopeCount","envelopeCount"}));
        }

        [TestCaseSource(nameof(BadIds))]
        public void EnvelopeMembershipArraysRequireExactWrittenUniqueInt32Ids(object ids)
        {
            var native = new Native { Members = ids }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            Assert.That(native.Calls, Is.EqualTo(new[]{"detail:9","members:9"}));
        }

        [Test]
        public void UnwrittenAndChangingEnvelopeMembershipFail()
        {
            var native = new Native { WriteMembers = false }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            native.WriteMembers = true;
            native.TypeAt = (_,read) => read % 2 == 0 ? 4 : 5;
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
            native.TypeAt = null; native.CountAt = (_,read) => read % 2 == 0 ? 3 : 2;
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadEnvelopeDetail(9));
        }

        [Test]
        public void EnvelopeArraysSupportNonzeroBoundsAndAllRecordsOwnTheirSets()
        {
            var buffer = Array.CreateInstance(typeof(int), [2], [-3]); buffer.SetValue(7,-3); buffer.SetValue(3,-2);
            var native = new Native { Members = buffer }; using var wrapper = Wrap(native);
            var all = wrapper.Load.GetAllLoadEnvelopeDetails();
            Assert.That(all.Select(item => item.Id), Is.EquivalentTo(new[]{9,2}));
            var records = all.ToArray();
            Assert.That(records[0].LoadCaseIds, Is.Not.SameAs(records[1].LoadCaseIds));
            records[0].LoadCaseIds.Clear();
            Assert.That(records[1].LoadCaseIds, Has.Count.EqualTo(2));
        }

        [Test]
        public void AggregateEnvelopeBudgetStopsBeforeFetchingAnExcessMembershipArray()
        {
            var native = new Native
            {
                CountAt = (id,_) => id == 9 ? 100000 : 1,
                MembersAt = id => id == 9 ? Enumerable.Range(1,100000).ToArray() : new[]{7}
            };
            using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetAllLoadEnvelopeDetails());
            Assert.That(native.Calls, Does.Contain("members:9"));
            Assert.That(native.Calls, Does.Not.Contain("members:2"));
        }

        [Test]
        public void AllEnvelopeFailureReturnsNoPartialSetAndReleasesTheLease()
        {
            var failure = new COMException("second detail failed");
            var native = new Native { OnCall = name => { if (name == "detail:2") throw failure; } };
            using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => wrapper.Load.GetAllLoadEnvelopeDetails()), Is.SameAs(failure));
            native.OnCall = null;
            Assert.DoesNotThrow(() => wrapper.Load.GetAllLoadEnvelopeDetails());
        }

        [TestCaseSource(nameof(BadCounts))]
        public void CombinationPairCountIsIntegralNonnegativeAndBounded(object count)
        {
            var native = new Native { PairCount = count }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
            Assert.That(native.Calls, Is.EqualTo(new[]{"pairs:201"}));
        }

        [Test]
        public void ZeroPairsReturnNullWithoutTitleInventoryOrFactors()
        {
            var native = new Native { PairCount = 0 }; using var wrapper = Wrap(native);
            Assert.That(wrapper.Load.GetLoadCombination(201), Is.Null);
            Assert.That(native.Calls, Is.EqualTo(new[]{"pairs:201"}));
        }

        [Test]
        public void OrdinaryCombinationPreservesSignedCoefficientsTitlesAndIndependentSourceRecords()
        {
            var native = new Native { Title = "  literal \r\n title  " }; using var wrapper = Wrap(native);
            var first = wrapper.Load.GetLoadCombination(201);
            Assert.That(first.Id, Is.EqualTo(201));
            Assert.That(first.Title, Is.EqualTo(native.Title));
            Assert.That(first.Factors.Single(pair => pair.Key.Id == 7).Value, Is.EqualTo(1.5));
            Assert.That(first.Factors.Single(pair => pair.Key.Id == 3).Value, Is.EqualTo(-.7));
            Assert.That(first.Factors.Keys.All(item => item.CaseType == LoadCaseType.PrimaryLoad), Is.True);
            Assert.That(native.Calls.Count(name => name == "primaryIds"), Is.EqualTo(1));
            var second = wrapper.Load.GetLoadCombination(201);
            Assert.That(first.Factors, Is.Not.SameAs(second.Factors));
            Assert.That(first.Factors.Keys.First(), Is.Not.SameAs(second.Factors.Keys.First()));
            ((double[])native.Factors)[0] = 99;
            Assert.That(first.Factors.Single(pair => pair.Key.Id == 7).Value, Is.EqualTo(1.5));
        }

        [Test]
        public void CombinationIdentityAndEveryPrimarySourceMustBeQualified()
        {
            var native = new Native { CombinationIds = new[]{202,301} }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
            Assert.That(native.Calls, Does.Not.Contain("factors:201"));
            native.CombinationIds = new[]{201,301}; native.PairIds = new[]{7,999};
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
        }

        [TestCaseSource(nameof(BadIds))]
        public void CombinationSourceIdsAreQualifiedBeforePrimaryMetadata(object ids)
        {
            var native = new Native { PairIds = ids }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
            Assert.That(native.Calls, Does.Not.Contain("primaryCount"));
        }

        [TestCaseSource(nameof(BadFactors))]
        public void CombinationFactorsRejectShapeUnwrittenAndNonfiniteValues(object factors)
        {
            var native = new Native { Factors = factors }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
            Assert.That(native.Calls, Does.Not.Contain("primaryCount"));
        }

        [TestCase(0d)] [TestCase(1d)] [TestCase(-2d)] [TestCase(double.PositiveInfinity)]
        public void WrittenSrssOverallSlotsAreNeverDiscarded(double overall)
        {
            var native = new Native { Factors = new[]{1.5,-.7,overall} }; using var wrapper = Wrap(native);
            Assert.Throws<NotSupportedException>(() => wrapper.Load.GetLoadCombination(201));
            Assert.That(native.Calls, Does.Not.Contain("primaryCount"));
        }

        [Test]
        public void NaNSentinelExtraSlotAndNonzeroBoundArraysSupportOrdinaryCombinations()
        {
            var ids = Array.CreateInstance(typeof(int), [2], [-2]); ids.SetValue(7,-2); ids.SetValue(3,-1);
            var factors = Array.CreateInstance(typeof(double), [3], [4]); factors.SetValue(0d,4); factors.SetValue(-2d,5); factors.SetValue(double.NaN,6);
            var native = new Native { PairIds = ids, Factors = factors }; using var wrapper = Wrap(native);
            Assert.That(wrapper.Load.GetLoadCombination(201).Factors.Values, Is.EquivalentTo(new[]{0d,-2d}));
        }

        [TestCase(false,true)] [TestCase(true,false)]
        public void UnwrittenCombinationArraysFail(bool ids, bool factors)
        {
            var native = new Native { WritePairIds = ids, WriteFactors = factors }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
        }

        [TestCase(null)] [TestCase(false)] [TestCase(0)] [TestCase(-1)] [TestCase(2)] [TestCase("1")] [TestCase(1d)]
        public void CombinationSuccessFlagIsStrict(object status)
        {
            var native = new Native { PairStatus = status }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
        }

        [Test]
        public void CombinationCountRacesAndUnrelatedPrimaryFailuresPropagate()
        {
            var native = new Native { PairCountAt = read => read == 1 ? 2 : 1 }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCombination(201));
            native.PairCountAt = null;
            var error = new COMException("unrelated primary title failed");
            native.PairCount = 1; native.PairIds = new[]{7}; native.Factors = new[]{1d};
            native.OnCall = name => { if (name == "title:3") throw error; };
            Assert.That(Assert.Throws<COMException>(() => wrapper.Load.GetLoadCombination(201)), Is.SameAs(error));
        }

        [TestCaseSource(nameof(AvailabilityFlags))]
        public void BucklingAvailabilityPreservesValidFlagsWithoutExtraQueries(object flag)
        {
            var native = new Native { BucklingAvailable = flag }; using var wrapper = Wrap(native);
            Assert.That(wrapper.Output.AreBucklingAnalysisResultsAvailable(), Is.EqualTo(Convert.ToBoolean(flag)));
            Assert.That(native.Calls, Is.EqualTo(new[]{"bucklingAvailable"}));
        }

        [TestCaseSource(nameof(BadAvailability))]
        public void BucklingAvailabilityRejectsMalformedCodes(object flag)
        {
            var native = new Native { BucklingAvailable = flag }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.AreBucklingAnalysisResultsAvailable());
        }

        [TestCase(0)] [TestCase(1)] [TestCase(int.MaxValue)]
        public void BucklingCountsPreserveTheFullNonnegativeInt32Range(int count)
        {
            var native = new Native { BucklingCount = count }; using var wrapper = Wrap(native);
            Assert.That(wrapper.Output.GetNumberOfBucklingFactorsComputed(), Is.EqualTo(count));
            Assert.That(native.Calls, Is.EqualTo(new[]{"bucklingCount"}));
        }

        [TestCaseSource(nameof(BadBucklingCounts))]
        public void BucklingCountsRejectMalformedAndOutOfRangeValues(object count)
        {
            var native = new Native { BucklingCount = count }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetNumberOfBucklingFactorsComputed());
        }

        [TestCaseSource(nameof(Operations))]
        public void BindingThreadAndLifetimeGuardsPrecedeNativeCalls(string operation)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            var load = wrapper.Load; var output = wrapper.Output;
            if (operation is "available" or "count")
                Assert.Throws<InvalidOperationException>(() => Invoke(load,new OSOutputAdapter(wrapper),operation));
            else Assert.Throws<InvalidOperationException>(() => Invoke(new OSLoadAdapter(wrapper),output,operation));
            Exception error = null;
            var thread = new Thread(() => { try { Invoke(load,output,operation); } catch (Exception failure) { error = failure; } });
            thread.Start(); Assert.That(thread.Join(5000), Is.True); Assert.That(error, Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Invoke(load,output,operation));
            load = wrapper.Load; output = wrapper.Output; wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Invoke(load,output,operation));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void SharedLeaseRejectsCrossDomainCallbacksAndReleasesAfterErrors(string operation)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            native.OnCall = _ =>
            {
                foreach (string nested in Operations) Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Load,wrapper.Output,nested));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Invoke(wrapper.Load,wrapper.Output,operation);
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            native.OnCall = null; native.Calls.Clear();
            var failure = new COMException("native failure", unchecked((int)0x80010108)); native.Failure = failure;
            Assert.That(Assert.Throws<COMException>(() => Invoke(wrapper.Load,wrapper.Output,operation)), Is.SameAs(failure));
            Assert.That(native.Calls, Has.Count.EqualTo(1));
            native.Failure = null;
            Assert.DoesNotThrow(() => Invoke(wrapper.Load,wrapper.Output,operation));
            using var missing = Wrap(new object());
            Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Load,missing.Output,operation));
        }

        [Test]
        public void DetachedModelsPreserveDefaultsOwnershipNotificationsAndImmutableCombinationIdentity()
        {
            var first = new LoadEnvelopeDetail(); var second = new LoadEnvelopeDetail(9,LoadEnvelopType.Strength,2);
            Assert.That(first.Id, Is.Zero); Assert.That(first.LoadCaseIds, Is.Empty);
            Assert.That(second.LoadCaseIds, Is.Empty); Assert.That(first.LoadCaseIds, Is.Not.SameAs(second.LoadCaseIds));
            var names = new List<string>(); first.PropertyChanged += (_,args) => names.Add(args.PropertyName);
            first.Id = 9; first.EnvelopType = LoadEnvelopType.Strength; first.LoadCasesCount = 2; first.LoadCaseIds = new HashSet<int>{7,3};
            Assert.That(names, Is.EqualTo(new[]{"Id","EnvelopType","LoadCasesCount","LoadCaseIds"}));
            first.LoadCaseIds.Clear(); Assert.That(first.LoadCasesCount, Is.EqualTo(2)); Assert.That(names, Has.Count.EqualTo(4));
            var set = new HashSet<int>{7}; Assert.That(new LoadEnvelopeDetail(9,LoadEnvelopType.None,1,set).LoadCaseIds, Is.SameAs(set));
            var combination = new StaadLoadCombination(201," title ");
            Assert.That(combination, Is.InstanceOf<ILoadCombination>()); Assert.That(combination, Is.InstanceOf<LoadCombination>());
            Assert.That(combination.Factors, Is.Empty); Assert.That(combination.Title, Is.EqualTo(" title "));
            Assert.That(typeof(LoadCombination).GetProperty("Id").GetSetMethod(), Is.Null);
            Assert.That(typeof(LoadCombination).GetProperty("Title").GetSetMethod(), Is.Null);
        }

        [TestCaseSource(nameof(Operations))]
        public void SignaturesAndExpandedInterfaceAdapterXmlMatch(string operation)
        {
            bool output = operation is "available" or "count";
            string name = operation switch { "detail" => "GetLoadEnvelopeDetail", "all" => "GetAllLoadEnvelopeDetails", "combination" => "GetLoadCombination", "available" => "AreBucklingAnalysisResultsAvailable", _ => "GetNumberOfBucklingFactorsComputed" };
            Type result = operation switch { "detail" => typeof(ILoadEnvelopeDetail), "all" => typeof(HashSet<ILoadEnvelopeDetail>), "combination" => typeof(StaadLoadCombination), "available" => typeof(bool), _ => typeof(int) };
            string[] names = operation == "detail" ? ["envId"] : operation == "combination" ? ["id"] : [];
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSLoad).Assembly.Location,".xml"));
            XElement previous = null;
            foreach (Type owner in output ? new[]{typeof(IOSOutput),typeof(OSOutputAdapter)} : new[]{typeof(IOSLoad),typeof(OSLoadAdapter)})
            {
                var method = owner.GetMethod(name); Assert.That(method.ReturnType, Is.EqualTo(result));
                Assert.That(method.GetParameters().Select(p => p.Name), Is.EqualTo(names));
                Assert.That(method.GetParameters().All(p => p.ParameterType == typeof(int) && !p.IsOptional && !p.ParameterType.IsByRef), Is.True);
                string prefix = "M:" + owner.FullName + "." + name;
                var doc = docs.Descendants("member").Single(e => (string)e.Attribute("name") == prefix || ((string)e.Attribute("name")).StartsWith(prefix+"("));
                foreach (string tag in new[]{"summary","returns","remarks","exception","example"}) Assert.That(doc.Element(tag), Is.Not.Null);
                Assert.That(doc.Elements("param").Select(p => (string)p.Attribute("name")), Is.EqualTo(names));
                Assert.That(doc.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
                if (previous != null) Assert.That(doc.Elements().Select(e => e.ToString()), Is.EqualTo(previous.Elements().Select(e => e.ToString())));
                previous = doc;
            }
        }
    }
}

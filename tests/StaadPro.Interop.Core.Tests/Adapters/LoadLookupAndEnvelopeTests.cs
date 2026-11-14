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
using FamilyNative = StaadPro.Interop.Core.Tests.Adapters.LoadCombinationMetadataTests.Native;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class LoadLookupAndEnvelopeTests
    {
        private static readonly string[] Operations = ["lookup", "range", "filter", "list", "envelopes"];
        private static readonly LoadCaseType[] Families = [LoadCaseType.PrimaryLoad, LoadCaseType.ReferenceLoad, LoadCaseType.LoadCombination];
        private static readonly object[] InvalidCounts = [null, true, -1, 100001, "2", 2d, 2m, uint.MaxValue, DayOfWeek.Monday];
        private static readonly object[] InvalidStatuses = [null, true, false, 0, -1, 2, "1", 1d, 1m, DayOfWeek.Monday, ulong.MaxValue];
        private static readonly object[] ValidStatuses = [(sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL];
        private static readonly object[] InvalidBuffers = [null, new int[0], new[]{9}, new[]{9,9}, new[]{9,0}, new[]{9,-1},
            new[]{9,int.MinValue}, new long[]{9,2}, new object[]{9,2}, new int[1,2]];

        public sealed class EnvelopeNative
        {
            public object Count = 2;
            public object Status = 1;
            public object Ids = new[]{9,2};
            public object RecheckedCount = 2;
            public bool Write = true;
            public Exception Failure;
            public Action<string> OnCall;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public object GetEnvelopeCount()
            {
                Step("count");
                return Calls.Count(name => name == "count") == 1 ? Count : RecheckedCount;
            }
            public object GetEnvelopeIDs(ref object buffer)
            {
                Assert.That(buffer, Is.TypeOf<int[]>());
                Assert.That((int[])buffer, Is.All.EqualTo(int.MinValue));
                Step("ids");
                if (Write) buffer = Ids;
                return Status;
            }
            private void Step(string name)
            {
                Calls.Add(name);
                Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(name);
                if (Failure != null) throw Failure;
            }
        }

        private static OpenStaadWrapper Wrap(object native) => new(new NativeRoot { Load = native });
        private static object NativeFor(string operation) => operation == "envelopes" ? new EnvelopeNative() : new FamilyNative();
        private static List<string> Calls(object native) => (List<string>)((dynamic)native).Calls;
        private static object Invoke(IOSLoad load, string operation) => operation switch
        {
            "lookup" => load.GetLoadCase(7, LoadCaseType.PrimaryLoad),
            "range" => load.GetLoadCases(1, int.MaxValue, LoadCaseType.PrimaryLoad),
            "filter" => load.GetLoadCasesFromIds(new[]{7}, LoadCaseType.PrimaryLoad),
            "list" => load.GetLoadCasesOfType(LoadCaseType.PrimaryLoad),
            "envelopes" => load.GetEnvelopeIds(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        [TestCaseSource(nameof(Families))]
        public void LookupReadsFullFamilyAndReturnsNullForAbsentId(LoadCaseType family)
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            int id = family == LoadCaseType.LoadCombination ? 201 : 7;
            var result = wrapper.Load.GetLoadCase(id, family);
            Assert.That(result.Id, Is.EqualTo(id));
            Assert.That(result.CaseType, Is.EqualTo(family));
            Assert.That(native.Calls.Count(name => name.StartsWith("title:") || name.StartsWith("rTitle:")), Is.EqualTo(2));
            Assert.That(wrapper.Load.GetLoadCase(999, family), Is.Null);
            native.Count = 0;
            native.Calls.Clear();
            Assert.That(wrapper.Load.GetLoadCase(id, family), Is.Null);
            Assert.That(native.Calls, Has.Count.EqualTo(1));
        }

        [TestCaseSource(nameof(Families))]
        public void RangesAreInclusiveAndDoNotIterateEveryInteger(LoadCaseType family)
        {
            using var wrapper = Wrap(new FamilyNative());
            int id = family == LoadCaseType.LoadCombination ? 201 : 7;
            Assert.That(wrapper.Load.GetLoadCases(id, id, family).Single().Id, Is.EqualTo(id));
            Assert.That(wrapper.Load.GetLoadCases(1, int.MaxValue, family).Count(), Is.EqualTo(2));
            Assert.That(wrapper.Load.GetLoadCases(900, 1000, family), Is.Empty);
        }

        [TestCaseSource(nameof(Families))]
        public void FamilyFiltersOmitAbsentIdsAndCollapseDuplicates(LoadCaseType family)
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            int id = family == LoadCaseType.LoadCombination ? 201 : 7;
            var input = new Once([999, id, id]);
            var results = wrapper.Load.GetLoadCasesFromIds(input, family);
            Assert.That(results.Single().Id, Is.EqualTo(id));
            int calls = native.Calls.Count;
            Assert.That(results.Single().CaseType, Is.EqualTo(family));
            Assert.That(native.Calls, Has.Count.EqualTo(calls));
            Assert.That(input.Enumerations, Is.EqualTo(1));
            Assert.That(native.Calls.Count(name => name.StartsWith("title:") || name.StartsWith("rTitle:")), Is.EqualTo(2));
        }

        [TestCaseSource(nameof(Families))]
        public void ListsAndRecordsAreIndependentAndRetainFamilyClassifications(LoadCaseType family)
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            var first = wrapper.Load.GetLoadCasesOfType(family);
            var second = wrapper.Load.GetLoadCasesOfType(family);
            Assert.That(first, Is.TypeOf<List<ILoadCase>>());
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second.Single(item => item.Id == first[0].Id)));
            Assert.That(first.Select(item => item.CaseType), Is.All.EqualTo(family));
            Assert.That(first.Select(item => item.Type), Is.All.EqualTo(LoadType.None));
            int calls = native.Calls.Count;
            first.Clear();
            Assert.That(second, Has.Count.EqualTo(2));
            Assert.That(native.Calls, Has.Count.EqualTo(calls));
        }

        [TestCase(0)] [TestCase(-1)]
        public void InvalidIdsAndRangeBoundsFailBeforeNativeReads(int id)
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Load.GetLoadCase(id, LoadCaseType.PrimaryLoad)).ParamName, Is.EqualTo("lcId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Load.GetLoadCases(id, 7, LoadCaseType.PrimaryLoad)).ParamName, Is.EqualTo("fromId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Load.GetLoadCases(1, id, LoadCaseType.PrimaryLoad)).ParamName, Is.EqualTo("toId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Load.GetLoadCasesFromIds(new[]{7,id}, LoadCaseType.PrimaryLoad)).ParamName, Is.EqualTo("loadCasesIds"));
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void InvalidFilterInputsAndReversedRangesFailBeforeNativeReads()
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<ArgumentException>(() => wrapper.Load.GetLoadCases(7, 3, LoadCaseType.PrimaryLoad)).ParamName, Is.EqualTo("toId"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => wrapper.Load.GetLoadCasesFromIds(null, LoadCaseType.PrimaryLoad)).ParamName, Is.EqualTo("loadCasesIds"));
            Assert.Throws<ArgumentException>(() => wrapper.Load.GetLoadCasesFromIds(Enumerable.Repeat(7, 100001), LoadCaseType.PrimaryLoad));
            var failure = new ApplicationException("input failed");
            Assert.That(Assert.Throws<ApplicationException>(() => wrapper.Load.GetLoadCasesFromIds(Broken(failure), LoadCaseType.PrimaryLoad)), Is.SameAs(failure));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase(LoadCaseType.RepeatLoad)] [TestCase((LoadCaseType)0)] [TestCase((LoadCaseType)999)]
        public void UnsupportedFamiliesRejectEvenForEmptyFilterWithoutEnumeratingInput(LoadCaseType family)
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            var input = new Once([]);
            Assert.Throws<NotSupportedException>(() => wrapper.Load.GetLoadCase(7, family));
            Assert.Throws<NotSupportedException>(() => wrapper.Load.GetLoadCases(1, 9, family));
            Assert.Throws<NotSupportedException>(() => wrapper.Load.GetLoadCasesFromIds(input, family));
            Assert.Throws<NotSupportedException>(() => wrapper.Load.GetLoadCasesOfType(family));
            Assert.That(input.Enumerations, Is.Zero);
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void EmptyFilterAvoidsNativeReadsAndMaximumInputIncludesDuplicates()
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            var first = wrapper.Load.GetLoadCasesFromIds([], LoadCaseType.PrimaryLoad);
            var second = wrapper.Load.GetLoadCasesFromIds([], LoadCaseType.PrimaryLoad);
            Assert.That(first, Is.Empty);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(native.Calls, Is.Empty);
            Assert.That(wrapper.Load.GetLoadCasesFromIds(Enumerable.Repeat(7, 100000), LoadCaseType.PrimaryLoad).Single().Id, Is.EqualTo(7));
        }

        [TestCase("lookup")] [TestCase("range")] [TestCase("filter")] [TestCase("list")]
        public void UnselectedCaseMetadataFailuresPropagateBeforeResultsReturn(string operation)
        {
            var error = new COMException("other case failed", unchecked((int)0x80010108));
            var native = new FamilyNative();
            native.OnCall = name => { if (name == "title:3") throw error; };
            using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => Invoke(wrapper.Load, operation)), Is.SameAs(error));
        }

        [Test]
        public void InputEnumerationSharesTheLoadLeaseAndSnapshotsMutableIds()
        {
            var native = new FamilyNative();
            using var wrapper = Wrap(native);
            var ids = new[]{7};
            native.OnCall = _ => ids[0] = 999;
            var input = new Once(ids, () =>
            {
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetBaseUnit());
                Assert.That(native.Calls, Is.Empty);
            });
            Assert.That(wrapper.Load.GetLoadCasesFromIds(input, LoadCaseType.PrimaryLoad).Single().Id, Is.EqualTo(7));
        }

        [TestCaseSource(nameof(InvalidCounts))]
        public void EnvelopeCountsAreStrictBoundedIntegers(object count)
        {
            var native = new EnvelopeNative { Count = count };
            using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetEnvelopeIds());
            Assert.That(native.Calls, Is.EqualTo(new[]{"count"}));
        }

        [TestCaseSource(nameof(InvalidStatuses))]
        public void EnvelopeSuccessRequiresIntegerOne(object status)
        {
            var native = new EnvelopeNative { Status = status };
            using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetEnvelopeIds());
            Assert.That(native.Calls, Is.EqualTo(new[]{"count","ids"}));
        }

        [TestCaseSource(nameof(ValidStatuses))]
        public void IntegralSuccessReturnsIndependentEnvelopeIdsAndRechecksCount(object status)
        {
            var ids = new[]{9,2};
            var native = new EnvelopeNative { Status = status, Ids = ids };
            using var wrapper = Wrap(native);
            var result = wrapper.Load.GetEnvelopeIds();
            Assert.That(result, Is.EquivalentTo(ids));
            Assert.That(native.Calls, Is.EqualTo(new[]{"count","ids","count"}));
            ids[0] = 99;
            Assert.That(result, Does.Contain(9));
        }

        [TestCaseSource(nameof(InvalidBuffers))]
        public void EnvelopeIdsMustHaveExactShapeAndUniquePositiveWrittenValues(object buffer)
        {
            var native = new EnvelopeNative { Ids = buffer };
            using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetEnvelopeIds());
            Assert.That(native.Calls, Is.EqualTo(new[]{"count","ids"}));
        }

        [Test]
        public void UnwrittenEnvelopeIdsFailAndZeroCountReturnsFreshEmptySets()
        {
            var native = new EnvelopeNative { Write = false };
            using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetEnvelopeIds());
            native.Count = 0;
            native.RecheckedCount = 0;
            native.Calls.Clear();
            var first = wrapper.Load.GetEnvelopeIds();
            Assert.That(first, Is.Empty);
            Assert.That(first, Is.Not.SameAs(wrapper.Load.GetEnvelopeIds()));
            Assert.That(native.Calls, Is.EqualTo(new[]{"count","count"}));
        }

        [TestCase(3)] [TestCase(0)] [TestCase(-1)] [TestCase(null)] [TestCase(true)]
        public void ChangedOrMalformedEnvelopeCountRecheckFails(object count)
        {
            var native = new EnvelopeNative { RecheckedCount = count };
            using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetEnvelopeIds());
            Assert.That(native.Calls, Is.EqualTo(new[]{"count","ids","count"}));
        }

        [Test]
        public void EnvelopeArraysSupportNonzeroBoundsAndMaximumInventory()
        {
            var array = Array.CreateInstance(typeof(int), [2], [-4]);
            array.SetValue(9, -4);
            array.SetValue(2, -3);
            var native = new EnvelopeNative { Ids = array };
            using var wrapper = Wrap(native);
            Assert.That(wrapper.Load.GetEnvelopeIds(), Is.EquivalentTo(new[]{9,2}));
            native.Calls.Clear();
            native.Count = native.RecheckedCount = 100000;
            native.Ids = Enumerable.Range(1, 100000).ToArray();
            Assert.That(wrapper.Load.GetEnvelopeIds(), Has.Count.EqualTo(100000));
        }

        [TestCaseSource(nameof(Operations))]
        public void ExistingLifetimeThreadCurrentBindingAndReentrancyGuardsApply(string operation)
        {
            object native = NativeFor(operation);
            using var wrapper = Wrap(native);
            var adapter = wrapper.Load;
            Assert.Throws<InvalidOperationException>(() => Invoke(new OSLoadAdapter(wrapper), operation));
            Exception error = null;
            var thread = new Thread(() => { try { Invoke(adapter, operation); } catch (Exception failure) { error = failure; } });
            thread.Start();
            Assert.That(thread.Join(5000), Is.True);
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Invoke(adapter, operation));
            Assert.That(Calls(native), Is.Empty);
            Action<string> callback = _ =>
            {
                foreach (string nested in Operations) Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Load, nested));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(() => wrapper.Output.AreResultsAvailable());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            ((dynamic)native).OnCall = callback;
            Invoke(wrapper.Load, operation);
            ((dynamic)native).OnCall = null;
            adapter = wrapper.Load;
            wrapper.Dispose();
            int before = Calls(native).Count;
            Assert.Throws<ObjectDisposedException>(() => Invoke(adapter, operation));
            Assert.That(Calls(native), Has.Count.EqualTo(before));
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeErrorsAndMissingMembersPropagateAndReleaseTheLease(string operation)
        {
            object native = NativeFor(operation);
            var failure = new COMException("native failure", unchecked((int)0x80010108));
            ((dynamic)native).Failure = failure;
            using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => Invoke(wrapper.Load, operation)), Is.SameAs(failure));
            Assert.That(Calls(native), Has.Count.EqualTo(1));
            ((dynamic)native).Failure = null;
            Assert.DoesNotThrow(() => Invoke(wrapper.Load, operation));
            using var missing = Wrap(new object());
            Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Load, operation));
            Assert.DoesNotThrow(() => missing.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void PublicSignaturesAndExpandedInterfaceAdapterDocumentationMatch(string operation)
        {
            string name = operation switch { "lookup" => "GetLoadCase", "range" => "GetLoadCases", "filter" => "GetLoadCasesFromIds", "list" => "GetLoadCasesOfType", _ => "GetEnvelopeIds" };
            string[] names = operation switch { "lookup" => ["lcId","loadCaseType"], "range" => ["fromId","toId","loadCaseType"], "filter" => ["loadCasesIds","loadCaseType"], "list" => ["loadCaseType"], _ => [] };
            Type[] types = operation switch { "lookup" => [typeof(int),typeof(LoadCaseType)], "range" => [typeof(int),typeof(int),typeof(LoadCaseType)], "filter" => [typeof(IEnumerable<int>),typeof(LoadCaseType)], "list" => [typeof(LoadCaseType)], _ => [] };
            Type result = operation switch { "lookup" => typeof(ILoadCase), "list" => typeof(List<ILoadCase>), "envelopes" => typeof(HashSet<int>), _ => typeof(IEnumerable<ILoadCase>) };
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSLoad).Assembly.Location, ".xml"));
            XElement previous = null;
            foreach (Type owner in new[]{typeof(IOSLoad),typeof(OSLoadAdapter)})
            {
                var method = owner.GetMethod(name, types);
                Assert.That(method, Is.Not.Null);
                Assert.That(method.ReturnType, Is.EqualTo(result));
                Assert.That(method.GetParameters().Select(p => p.Name), Is.EqualTo(names));
                Assert.That(method.GetParameters().Any(p => p.IsOptional || p.ParameterType.IsByRef), Is.False);
                string prefix = "M:" + owner.FullName + "." + name;
                var member = docs.Descendants("member").Single(e => (string)e.Attribute("name") == prefix || ((string)e.Attribute("name")).StartsWith(prefix + "("));
                foreach (string tag in new[]{"summary","returns","remarks","exception","example"}) Assert.That(member.Element(tag), Is.Not.Null);
                Assert.That(member.Elements("param").Select(e => (string)e.Attribute("name")), Is.EqualTo(names));
                Assert.That(member.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
                if (previous != null) Assert.That(member.Elements().Select(e => e.ToString()), Is.EqualTo(previous.Elements().Select(e => e.ToString())));
                previous = member;
            }
        }

        private static IEnumerable<int> Broken(Exception failure) { yield return 7; throw failure; }
        private sealed class Once(IEnumerable<int> values, Action before = null) : IEnumerable<int>
        {
            internal int Enumerations;
            public IEnumerator<int> GetEnumerator()
            {
                if (++Enumerations != 1) throw new InvalidOperationException("Input enumerated twice.");
                before?.Invoke();
                return values.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}

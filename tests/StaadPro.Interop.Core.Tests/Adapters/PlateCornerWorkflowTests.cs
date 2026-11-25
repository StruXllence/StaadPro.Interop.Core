using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCornerWorkflowTests
    {
        private static readonly string[] Operations = ["platesTask", "cartesian", "cartesianTask", "requests", "requestsTask"];
        private static readonly string[] Cartesian = ["cartesian", "cartesianTask"];
        private static readonly string[] Sparse = ["requests", "requestsTask"];
        private sealed class Once<T>(IEnumerable<T> items, Action start = null) : IEnumerable<T>
        {
            public int Enumerations, Disposals;
            public IEnumerator<T> GetEnumerator()
            {
                if (++Enumerations != 1) throw new InvalidOperationException("Sequence was enumerated twice.");
                start?.Invoke();
                try { foreach (var item in items) yield return item; }
                finally { Disposals++; }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        private sealed class OneReadCase : ILoadCase
        {
            public int Reads;
            public int Id { get { Reads++; return Reads == 1 ? 7 : 999; } }
            public string Title => throw new ApplicationException();
            public LoadType Type { get => throw new ApplicationException(); set => throw new ApplicationException(); }
            public LoadCaseType CaseType { get => throw new ApplicationException(); set => throw new ApplicationException(); }
        }
        private static Plate Plate(int id = 17, int first = 11) =>
            new(new Node { Id = first }, new Node { Id = first + 1 }, new Node { Id = first + 2 }, new Node { Id = first }) { Id = id };
        private static OpenStaadWrapper Wrap(object native) => new(new NativeRoot { Output = native, Geometry = new object() });
        private static PlateCornerForcesResultSet Invoke(IOSOutput output, string op,
            IEnumerable<ILoadCase> cases = null, IEnumerable<Plate> plates = null,
            IEnumerable<PlateCornerForcesRequest> requests = null)
        {
            var lc = new LoadCase(7, "Seven"); var p = Plate();
            return op switch
            {
                "platesTask" => output.GetPlateCornerForcesAsync(lc, plates ?? [p]).GetAwaiter().GetResult(),
                "cartesian" => output.GetPlateCornerForces(cases ?? [lc], plates ?? [p]),
                "cartesianTask" => output.GetPlateCornerForcesAsync(cases ?? [lc], plates ?? [p]).GetAwaiter().GetResult(),
                "requests" => output.GetPlateCornerForces(requests ?? [new(lc, p, p.A), new(lc, p, p.B), new(lc, p, p.C)]),
                "requestsTask" => output.GetPlateCornerForcesAsync(requests ?? [new(lc, p, p.A), new(lc, p, p.B), new(lc, p, p.C)]).GetAwaiter().GetResult(),
                _ => throw new ArgumentException(op)
            };
        }
        [TestCaseSource(nameof(Operations))]
        public void WorkflowsCopyNativeValuesAndPreserveOwningThreadAndIndexes(string op)
        {
            var native = new PlateCornerTests.Native();
            using var wrapper = Wrap(native);
            var set = Invoke(wrapper.Output, op);
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:7", "17:12:7", "17:13:7" }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(set.Count, Is.EqualTo(3));
            Assert.That(set[7, 17, 11].Mz, Is.EqualTo(-6));
            ((double[])native.Values)[0] = 99;
            Assert.That(set[7, 17, 11].Fx, Is.EqualTo(1));
            Assert.That(set.GetForPlateCorner(17, 11).Single(), Is.SameAs(set[7, 17, 11]));
            Assert.Throws<NotSupportedException>(() => ((IList<PlateCornerForces>)set.Results).Clear());
        }
        [TestCaseSource(nameof(Cartesian))]
        public void CartesianCapturesBothSequencesOnceBeforeDispatchAndOrdersCasePlateCorner(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            var first = new LoadCase(8, "first"); var p = Plate(); var q = Plate(18, 21);
            var cases = new Once<ILoadCase>([first, new LoadCase(7, ""), new LoadCase(8, "duplicate")]);
            var plates = new Once<Plate>([p, q, Plate(17, 31)], () => Assert.That(cases.Disposals, Is.EqualTo(1)));
            native.Callback = () => { Assert.That(plates.Disposals, Is.EqualTo(1)); p.Id = 99; q.A.Id = 999; };
            var set = Invoke(w.Output, op, cases, plates);
            Assert.That(cases.Enumerations, Is.EqualTo(1)); Assert.That(plates.Enumerations, Is.EqualTo(1));
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:8", "17:12:8", "17:13:8", "18:21:8", "18:22:8", "18:23:8", "17:11:7", "17:12:7", "17:13:7", "18:21:7", "18:22:7", "18:23:7" }));
            Assert.That(set[8, 17, 11].LoadCase, Is.SameAs(first));
            Assert.That(set.GetForPlate(17).Select(r => r.Plate), Is.All.SameAs(p));
            Assert.That(set.PlateIds, Is.EqualTo(new[] { 17, 18 }));
        }
        [TestCaseSource(nameof(Cartesian))]
        public void CartesianEmptyDimensionStillValidatesOtherInputs(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            Assert.That(Invoke(w.Output, op, Array.Empty<ILoadCase>(), [Plate()]), Is.Empty);
            Assert.That(Invoke(w.Output, op, [new LoadCase(7, "")], Array.Empty<Plate>()), Is.Empty);
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op, Array.Empty<ILoadCase>(), [new Plate { Id = 17 }]));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke(w.Output, op, [new LoadCase(0, "")], Array.Empty<Plate>()));
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op, Array.Empty<ILoadCase>(), [Plate(), new Plate { Id = 17 }]));
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Cartesian))]
        public void CartesianNullSequenceReportsExactArgument(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            void Call(IEnumerable<ILoadCase> cases, IEnumerable<Plate> plates)
            {
                if (op == "cartesian") w.Output.GetPlateCornerForces(cases, plates);
                else w.Output.GetPlateCornerForcesAsync(cases, plates).GetAwaiter().GetResult();
            }
            Assert.That(Assert.Throws<ArgumentNullException>(() => Call(null, [Plate()])).ParamName, Is.EqualTo("loadCases"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => Call([], null)).ParamName, Is.EqualTo("plates"));
            Assert.Throws<ArgumentException>(() => Call([null], []));
            Assert.Throws<ArgumentException>(() => Call([], [null]));
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Cartesian))]
        public void CartesianObservationProductUsesWideArithmeticAndOccurrenceBounds(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op,
                Enumerable.Range(1, 65536).Select(i => new LoadCase(i, "")),
                Enumerable.Range(1, 22000).Select(i => Plate(i))));
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op, Enumerable.Repeat<ILoadCase>(new LoadCase(7, ""), 100001), []));
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op, [], Enumerable.Repeat(Plate(), 100001)));
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Sparse))]
        public void SparseDeduplicatesOnlyFullKeysRetainingEachFirstRequestContext(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            var lcFirst = new LoadCase(7, "first"); var lcOther = new LoadCase(7, "other");
            var pFirst = Plate(); var pOther = Plate(); var pDuplicate = Plate();
            var requests = new Once<PlateCornerForcesRequest>([
                new(lcFirst, pFirst, new Node { Id = 12 }),
                new(lcOther, pOther, pOther.A),
                new(new LoadCase(7, "duplicate"), pDuplicate, pDuplicate.B)]);
            var set = Invoke(w.Output, op, requests: requests);
            Assert.That(requests.Enumerations, Is.EqualTo(1)); Assert.That(requests.Disposals, Is.EqualTo(1));
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:12:7", "17:11:7" }));
            Assert.That(set[7, 17, 12].LoadCase, Is.SameAs(lcFirst));
            Assert.That(set[7, 17, 12].Plate, Is.SameAs(pFirst));
            Assert.That(set[7, 17, 12].CornerNode, Is.SameAs(pFirst.B));
            Assert.That(set[7, 17, 11].LoadCase, Is.SameAs(lcOther));
            Assert.That(set[7, 17, 11].Plate, Is.SameAs(pOther));
            Assert.That(set[7, 17, 11].CornerNode, Is.SameAs(pOther.A));
        }
        [TestCaseSource(nameof(Sparse))]
        public void SparseCapturesReferencesAndIdsOnceAndDoesNotReReadMutableRequests(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            var lc = new OneReadCase(); var p = Plate(); var originalA = p.A;
            var request = new PlateCornerForcesRequest(lc, p, new Node { Id = 11 });
            IEnumerable<PlateCornerForcesRequest> Items()
            {
                yield return request;
                request.LoadCase = null; request.Plate = null; request.CornerNode = null;
                p.Id = 99; originalA.Id = 999; p.A = null;
            }
            var set = Invoke(w.Output, op, requests: Items());
            Assert.That(lc.Reads, Is.EqualTo(1));
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:7" }));
            Assert.That(set[7, 17, 11].LoadCase, Is.SameAs(lc));
            Assert.That(set[7, 17, 11].Plate, Is.SameAs(p));
            Assert.That(set[7, 17, 11].CornerNode, Is.SameAs(originalA));
        }
        [TestCaseSource(nameof(Sparse))]
        public void SparseRejectsInvalidEntriesIncludingDuplicatesBeforeAnyNativeCalls(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            var lc = new LoadCase(7, ""); var p = Plate(); var good = new PlateCornerForcesRequest(lc, p, p.A);
            var absentA = Plate(); absentA.A = null;
            var badD = Plate(); badD.D = new Node { Id = 0 };
            var invalid = new PlateCornerForcesRequest[] {
                null, new(), new(null,p,p.A), new(lc,null,p.A), new(lc,p,null),
                new(new LoadCase(0,""),p,p.A), new(lc,new Plate { Id = 0 },p.A),
                new(lc,p,new Node { Id = -1 }), new(lc,p,new Node { Id = 99 }),
                new(lc,absentA,p.A), new(lc,badD,p.A)
            };
            foreach (var item in invalid)
                Assert.That(Assert.Catch<ArgumentException>(() => Invoke(w.Output, op, requests: [good, item])).ParamName, Is.EqualTo("requests"));
            Assert.That(native.Calls, Is.Empty);
            Assert.That(Invoke(w.Output, op, requests: Array.Empty<PlateCornerForcesRequest>()), Is.Empty);
        }
        [TestCaseSource(nameof(Sparse))]
        public void SparseNullAndOccurrenceBoundaryAreQualifiedBeforeDispatch(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            void CallNull()
            {
                if (op == "requests") w.Output.GetPlateCornerForces((IEnumerable<PlateCornerForcesRequest>)null);
                else w.Output.GetPlateCornerForcesAsync((IEnumerable<PlateCornerForcesRequest>)null).GetAwaiter().GetResult();
            }
            Assert.That(Assert.Throws<ArgumentNullException>(CallNull).ParamName, Is.EqualTo("requests"));
            var p = Plate(); var r = new PlateCornerForcesRequest(new LoadCase(7, ""), p, p.A);
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op, requests: Enumerable.Repeat(r, 100001)));
            Assert.That(native.Calls, Is.Empty);
            Assert.That(Invoke(w.Output, op, requests: Enumerable.Repeat(r, 100000)).Count, Is.EqualTo(1));
        }
        [TestCaseSource(nameof(Operations))]
        public void EnumerationFailuresDisposeInputsAndPrecedeNativeWork(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            var error = new ApplicationException("enumeration"); var p = Plate(); var lc = new LoadCase(7, "");
            IEnumerable<T> Failing<T>(T item) { yield return item; throw error; }
            if (op.StartsWith("requests"))
            {
                var once = new Once<PlateCornerForcesRequest>(Failing(new PlateCornerForcesRequest(lc,p,p.A)));
                Assert.That(Assert.Throws<ApplicationException>(() => Invoke(w.Output, op, requests: once)), Is.SameAs(error));
                Assert.That(once.Disposals, Is.EqualTo(1));
            }
            else
            {
                var once = new Once<Plate>(Failing(p));
                Assert.That(Assert.Throws<ApplicationException>(() => Invoke(w.Output, op, plates: once)), Is.SameAs(error));
                Assert.That(once.Disposals, Is.EqualTo(1));
            }
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void LaterNativeFailureStopsWithoutPartialSetAndOriginalComFailureIsPreserved(string op)
        {
            var native = new PlateCornerTests.Native { FailAt = 2 }; using var w = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op));
            Assert.That(native.Calls, Has.Count.EqualTo(2));
            var error = new COMException("native", unchecked((int)0x80004005));
            native.FailAt = -1; native.Failure = error;
            Assert.That(Assert.Throws<COMException>(() => Invoke(w.Output, op)), Is.SameAs(error));
            native.Failure = null;
            Assert.DoesNotThrow(() => Invoke(w.Output, op));
        }
        [TestCaseSource(nameof(Operations))]
        public void BindingLeaseCoversEnumerationNativeCallbacksAndLifetime(string op)
        {
            var native = new PlateCornerTests.Native(); var w = Wrap(native); var stale = w.Output;
            Assert.Throws<InvalidOperationException>(() => Invoke(new OSOutputAdapter(w), op));
            w.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Invoke(stale, op));
            var crossThread = Task.Run(() => Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op))).GetAwaiter().GetResult();
            Assert.That(crossThread, Is.Not.Null);
            void Reenter()
            {
                Assert.Throws<InvalidOperationException>(() => w.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(() => w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op));
                Assert.Throws<InvalidOperationException>(() => w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => w.Dispose());
            }
            native.Callback = Reenter; var p = Plate();
            if (op.StartsWith("requests"))
                Invoke(w.Output, op, requests: new Once<PlateCornerForcesRequest>([new(new LoadCase(7,""),p,p.A)],Reenter));
            else Invoke(w.Output, op, plates: new Once<Plate>([p], Reenter));
            var current = w.Output; w.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Invoke(current, op));
        }
        [TestCase("platesTask")]
        [TestCase("cartesianTask")]
        [TestCase("requestsTask")]
        public void TasksAreAlreadySettledAndNeverThrowBeforeReturning(string op)
        {
            var native = new PlateCornerTests.Native(); using var w = Wrap(native);
            var lc = new LoadCase(7, ""); var p = Plate();
            Task<PlateCornerForcesResultSet> Start(bool invalid) => op switch
            {
                "platesTask" => w.Output.GetPlateCornerForcesAsync(lc, invalid ? null : new[] { p }),
                "cartesianTask" => w.Output.GetPlateCornerForcesAsync(invalid ? null : new[] { lc }, new[] { p }),
                _ => w.Output.GetPlateCornerForcesAsync(invalid ? null : new[] { new PlateCornerForcesRequest(lc,p,p.A) })
            };
            Assert.That(Start(false).IsCompletedSuccessfully, Is.True);
            Task<PlateCornerForcesResultSet> failed = null;
            Assert.DoesNotThrow(() => failed = Start(true));
            Assert.That(failed.IsFaulted, Is.True);
            Assert.Throws<ArgumentNullException>(() => failed.GetAwaiter().GetResult());
            var error = new COMException("native", -12345); native.Failure = error;
            Assert.DoesNotThrow(() => failed = Start(false));
            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => failed.GetAwaiter().GetResult()), Is.SameAs(error));
        }
        [Test]
        public void RequestModelKeepsSourceConstructionAndCurrentKeySemantics()
        {
            var empty = new PlateCornerForcesRequest();
            Assert.Throws<InvalidOperationException>(() => { var key = empty.Key; });
            var p = Plate(); var lc = new LoadCase(7,""); var node = new Node { Id = 99 };
            var request = new PlateCornerForcesRequest(lc,p,node);
            Assert.That(request.Key, Is.EqualTo(new PlateCornerForcesKey(7,17,99)));
            Assert.That(request.LoadCase, Is.SameAs(lc)); Assert.That(request.Plate, Is.SameAs(p));
            node.Id = 11;
            Assert.That(request.Key.CornerNodeId, Is.EqualTo(11));
            node.Id = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => { var key = request.Key; });
            Assert.DoesNotThrow(() => new PlateCornerForcesRequest(null,null,null));
        }
        [Test]
        public void RequestComputedKeyIsExcludedFromBuiltInJsonSerialization()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new PlateCornerForcesRequest());
            using var document = System.Text.Json.JsonDocument.Parse(json);
            Assert.That(document.RootElement.EnumerateObject().Select(p => p.Name),
                Is.EquivalentTo(new[] { "LoadCase", "Plate", "CornerNode" }));
        }
        [Test]
        public void FiveWorkflowContractsProvideMatchingExpandedIntelliSense()
        {
            var xml = XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location, ".xml"));
            var methods = typeof(IOSOutput).GetMethods().Where(m => m.Name.Contains("PlateCornerForces") &&
                (m.GetParameters().Length == 1 || m.GetParameters()[1].ParameterType == typeof(IEnumerable<Plate>) &&
                (m.GetParameters()[0].ParameterType == typeof(IEnumerable<ILoadCase>) || m.Name.EndsWith("Async")))).ToArray();
            Assert.That(methods, Has.Length.EqualTo(5));
            foreach (var method in methods)
            {
                string TypeId(Type t) => t.IsGenericType ? t.GetGenericTypeDefinition().FullName.Split((char)96)[0] +
                    "{" + string.Join(",",t.GetGenericArguments().Select(TypeId)) + "}" : t.FullName;
                string suffix = "." + method.Name + "(" + string.Join(",",method.GetParameters().Select(p => TypeId(p.ParameterType))) + ")";
                var contract = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(IOSOutput).FullName + suffix);
                var implementation = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(OSOutputAdapter).FullName + suffix);
                Assert.That(string.Concat(contract.Elements()), Is.EqualTo(string.Concat(implementation.Elements())));
                Assert.That(contract.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
                Assert.That(method.GetParameters().Any(p => p.ParameterType.IsByRef || p.IsOptional), Is.False);
                foreach (string tag in new[] { "summary", "param", "returns", "remarks", "exception", "example" })
                    Assert.That(contract.Element(tag), Is.Not.Null);
            }
        }
    }
}

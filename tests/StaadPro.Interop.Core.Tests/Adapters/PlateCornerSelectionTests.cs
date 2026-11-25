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
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCornerSelectionTests
    {
        private static readonly string[] Operations =
            ["casesCorner", "casesCornerTask", "casesAll", "casesAllTask", "casePlates"];

        private sealed class Once<T>(IEnumerable<T> items, Action onStart = null) : IEnumerable<T>
        {
            public int Enumerations, Disposals;
            public IEnumerator<T> GetEnumerator()
            {
                if (++Enumerations != 1) throw new InvalidOperationException("Sequence was re-enumerated.");
                onStart?.Invoke();
                try { foreach (var item in items) yield return item; }
                finally { Disposals++; }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CapturedCase : ILoadCase
        {
            internal int Reads;
            public int Id { get { Reads++; return Reads == 1 ? 7 : 999; } }
            public string Title => throw new ApplicationException("Unused title.");
            public LoadType Type { get => throw new ApplicationException(); set => throw new ApplicationException(); }
            public LoadCaseType CaseType { get => throw new ApplicationException(); set => throw new ApplicationException(); }
        }

        private static OpenStaadWrapper Wrap(object output) =>
            new(new NativeRoot { Output = output, Geometry = new object() });

        private static Plate TriPlate(int id = 17, int startNodeId = 11) =>
            new(new Node { Id = startNodeId }, new Node { Id = startNodeId + 1 }, new Node { Id = startNodeId + 2 }) { Id = id };

        private static Plate QuadPlate(int id = 18, int startNodeId = 21) =>
            new(new Node { Id = startNodeId }, new Node { Id = startNodeId + 1 }, new Node { Id = startNodeId + 2 }, new Node { Id = startNodeId + 3 }) { Id = id };

        private static PlateCornerForcesResultSet Invoke(
            IOSOutput output,
            string op,
            IEnumerable<ILoadCase> cases = null,
            ILoadCase loadCase = null,
            Plate plate = null,
            Node cornerNode = null,
            IEnumerable<Plate> plates = null)
        {
            loadCase ??= new LoadCase(7, "Case");
            cases ??= [loadCase];
            plate ??= TriPlate();
            cornerNode ??= plate.A;
            plates ??= [plate];
            return InvokeRaw(output, op, cases, loadCase, plate, cornerNode, plates);
        }

        private static PlateCornerForcesResultSet InvokeRaw(
            IOSOutput output,
            string op,
            IEnumerable<ILoadCase> cases,
            ILoadCase loadCase,
            Plate plate,
            Node cornerNode,
            IEnumerable<Plate> plates)
        {
            return op switch
            {
                "casesCorner" => output.GetPlateCornerForces(cases, plate, cornerNode),
                "casesCornerTask" => output.GetPlateCornerForcesAsync(cases, plate, cornerNode).GetAwaiter().GetResult(),
                "casesAll" => output.GetPlateCornerForces(cases, plate),
                "casesAllTask" => output.GetPlateCornerForcesAsync(cases, plate).GetAwaiter().GetResult(),
                "casePlates" => output.GetPlateCornerForces(loadCase, plates),
                _ => throw new ArgumentException(op)
            };
        }

        [TestCaseSource(nameof(Operations))]
        public void SelectionsPreserveOrderReferencesIndependentStorageAndOwningThread(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc = new LoadCase(7, "Dead");
            var p = TriPlate();
            var requestedCorner = new Node { Id = 11 };

            var set = Invoke(w.Output, op, [lc], lc, p, requestedCorner, [p]);

            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(set.Count, Is.EqualTo(op.StartsWith("casesCorner") ? 1 : 3));
            var first = set[7, 17, 11];
            Assert.That(new[] { first.Fx, first.Fy, first.Fz, first.Mx, first.My, first.Mz }, Is.EqualTo(new[] { 1d, -2d, 3d, -4d, 5d, -6d }));
            Assert.That(first.LoadCase, Is.SameAs(lc));
            Assert.That(first.Plate, Is.SameAs(p));
            Assert.That(first.CornerNode, Is.SameAs(p.A));
            ((double[])native.Values)[0] = 999;
            Assert.That(first.Fx, Is.EqualTo(1));
        }

        [TestCase("casesCorner")]
        [TestCase("casesCornerTask")]
        public void CaseSelectionAtOneCornerDeduplicatesIdsKeepsOrderAndEnumeratesOnce(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc7First = new LoadCase(7, "First");
            var lc8 = new LoadCase(8, "Second");
            var lc7Duplicate = new LoadCase(7, "Duplicate");
            var cases = new Once<ILoadCase>([lc7First, lc8, lc7Duplicate]);
            var plate = TriPlate();
            plate.D = new Node { Id = 11 };

            var set = Invoke(w.Output, op, cases: cases, plate: plate, cornerNode: new Node { Id = 11 });

            Assert.That(cases.Enumerations, Is.EqualTo(1));
            Assert.That(cases.Disposals, Is.EqualTo(1));
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:7", "17:11:8" }));
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Results.Select(r => r.LoadCaseId), Is.EqualTo(new[] { 7, 8 }));
            Assert.That(set[7, 17, 11].LoadCase, Is.SameAs(lc7First));
            Assert.That(set[7, 17, 11].CornerNode, Is.SameAs(plate.A));
            Assert.That(set.GetForPlateCorner(17, 11).Select(r => r.LoadCaseId), Is.EqualTo(new[] { 7, 8 }));
        }

        [TestCase("casesAll")]
        [TestCase("casesAllTask")]
        public void CaseAndAllCornerSelectionOrdersByCaseThenUniqueIncidence(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc8 = new LoadCase(8, "Eight");
            var lc7 = new LoadCase(7, "Seven");
            var lc8Dup = new LoadCase(8, "EightDup");
            var cases = new Once<ILoadCase>([lc8, lc7, lc8Dup]);
            var plate = TriPlate();
            plate.D = new Node { Id = 11 };

            var set = Invoke(w.Output, op, cases: cases, plate: plate);

            Assert.That(cases.Enumerations, Is.EqualTo(1));
            Assert.That(cases.Disposals, Is.EqualTo(1));
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:8", "17:12:8", "17:13:8", "17:11:7", "17:12:7", "17:13:7" }));
            Assert.That(set.Count, Is.EqualTo(6));
            Assert.That(set[8, 17, 11].LoadCase, Is.SameAs(lc8));
            Assert.That(set.GetForLoadCaseAndPlate(7, 17).Select(r => r.CornerNodeId), Is.EqualTo(new[] { 11, 12, 13 }));
            Assert.That(set.GetForPlateCorner(17, 12).Select(r => r.LoadCaseId), Is.EqualTo(new[] { 8, 7 }));
        }

        [Test]
        public void PlateBatchSelectionDeduplicatesPlatesOrdersByPlateThenIncidenceAndEnumeratesOnce()
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc = new LoadCase(7, "Dead");
            var p17First = TriPlate(17, 11);
            p17First.D = new Node { Id = 11 };
            var p18Quad = QuadPlate(18, 21);
            var p17Duplicate = TriPlate(17, 31);
            var plates = new Once<Plate>([p17First, p18Quad, p17Duplicate]);

            var set = w.Output.GetPlateCornerForces(lc, plates);

            Assert.That(plates.Enumerations, Is.EqualTo(1));
            Assert.That(plates.Disposals, Is.EqualTo(1));
            Assert.That(native.Calls, Is.EqualTo(new[]
            {
                "17:11:7", "17:12:7", "17:13:7",
                "18:21:7", "18:22:7", "18:23:7", "18:24:7"
            }));
            Assert.That(set.Count, Is.EqualTo(7));
            Assert.That(set.PlateIds, Is.EqualTo(new[] { 17, 18 }));
            Assert.That(set.GetForPlate(17).Select(r => r.Plate), Is.All.SameAs(p17First));
            Assert.That(set[7, 18, 24].CornerNode, Is.SameAs(p18Quad.D));
        }

        [TestCaseSource(nameof(Operations))]
        public void CapturedIdentitiesAndResultSetIndexesSurviveMetadataAndCallbackMutations(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc = new CapturedCase();
            var p = TriPlate();
            var a = p.A;
            native.Callback = () =>
            {
                p.Id = 999;
                a.Id = 111;
                p.B = null;
            };

            var set = Invoke(w.Output, op, [lc], lc, p, a, [p]);

            Assert.That(lc.Reads, Is.EqualTo(1));
            Assert.That(native.Calls.All(c => c.StartsWith("17:") && c.EndsWith(":7")), Is.True);
            Assert.That(set.LoadCaseIds, Is.EqualTo(new[] { 7 }));
            Assert.That(set.PlateIds, Is.EqualTo(new[] { 17 }));
            Assert.That(set[7, 17, 11].CornerNode, Is.SameAs(a));
            Assert.That(set.GetForPlateCorner(17, 11).Single(), Is.SameAs(set[7, 17, 11]));
            Assert.That(set.GetForLoadCaseAndPlate(7, 17), Has.Count.EqualTo(set.Count));
            Assert.That(set[7, 17, 11].Key.LoadCaseId, Is.EqualTo(999));
            Assert.Throws<NotSupportedException>(() => ((IList<PlateCornerForces>)set.Results).Clear());
        }

        [TestCaseSource(nameof(Operations))]
        public void EmptySequencesRequireNoNativeCallsAfterValidatingOtherArguments(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);

            if (op == "casePlates")
            {
                Assert.Throws<ArgumentNullException>(() => w.Output.GetPlateCornerForces(null, Array.Empty<Plate>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => w.Output.GetPlateCornerForces(new LoadCase(0, ""), Array.Empty<Plate>()));
                var empty = w.Output.GetPlateCornerForces(new LoadCase(7, ""), Array.Empty<Plate>());
                Assert.That(empty, Is.Empty);
            }
            else
            {
                var p = TriPlate();
                Assert.Throws<ArgumentNullException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, null, p.A, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, new Plate { Id = 0, A = new Node { Id = 1 }, B = new Node { Id = 2 }, C = new Node { Id = 3 } }, p.A, null));
                Assert.Throws<ArgumentException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, new Plate { Id = 17 }, new Node { Id = 11 }, null));
                var badD = TriPlate();
                badD.D = new Node { Id = 0 };
                Assert.Throws<ArgumentException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, badD, badD.A, null));
                if (op.StartsWith("casesCorner"))
                {
                    Assert.Throws<ArgumentNullException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, p, null, null));
                    Assert.Throws<ArgumentOutOfRangeException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, p, new Node { Id = 0 }, null));
                    Assert.Throws<ArgumentException>(() => InvokeRaw(w.Output, op, Array.Empty<ILoadCase>(), null, p, new Node { Id = 99 }, null));
                }
                var empty = Invoke(w.Output, op, cases: Array.Empty<ILoadCase>());
                Assert.That(empty, Is.Empty);
            }

            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void NullsInvalidEntriesInvalidDuplicatesAndEnumerationErrorsFailBeforeNativeCalls(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc = new LoadCase(7, "Dead");
            var p = TriPlate();

            if (op == "casePlates")
            {
                Assert.That(Assert.Throws<ArgumentNullException>(() => w.Output.GetPlateCornerForces(lc, (IEnumerable<Plate>)null)).ParamName, Is.EqualTo("plates"));
                Assert.That(Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(lc, [p, null])).ParamName, Is.EqualTo("plates"));
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => w.Output.GetPlateCornerForces(lc, [p, new Plate { Id = 0 }])).ParamName, Is.EqualTo("plates"));
                Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(lc, [p, new Plate { Id = 17 }]));
                var badDuplicate = TriPlate();
                badDuplicate.D = new Node { Id = -1 };
                Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(lc, [p, badDuplicate]));
            }
            else
            {
                Assert.That(Assert.Throws<ArgumentNullException>(() => InvokeRaw(w.Output, op, null, null, p, p.A, null)).ParamName, Is.EqualTo("loadCases"));
                Assert.That(Assert.Throws<ArgumentException>(() => InvokeRaw(w.Output, op, [lc, null], null, p, p.A, null)).ParamName, Is.EqualTo("loadCases"));
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => InvokeRaw(w.Output, op, [lc, new LoadCase(0, "")], null, p, p.A, null)).ParamName, Is.EqualTo("loadCases"));
                Assert.Throws<ArgumentException>(() => InvokeRaw(w.Output, op, [lc], null, new Plate { Id = 17 }, new Node { Id = 11 }, null));
            }

            var enumError = new ApplicationException("boom");
            if (op == "casePlates")
            {
                IEnumerable<Plate> FailingPlates() { yield return p; throw enumError; }
                var once = new Once<Plate>(FailingPlates());
                Assert.That(Assert.Throws<ApplicationException>(() => w.Output.GetPlateCornerForces(lc, once)), Is.SameAs(enumError));
                Assert.That(once.Disposals, Is.EqualTo(1));
            }
            else
            {
                IEnumerable<ILoadCase> FailingCases() { yield return lc; throw enumError; }
                var once = new Once<ILoadCase>(FailingCases());
                Assert.That(Assert.Throws<ApplicationException>(() => Invoke(w.Output, op, cases: once)), Is.SameAs(enumError));
                Assert.That(once.Disposals, Is.EqualTo(1));
            }

            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void OccurrenceAndObservationLimitsPrecedeNativeCallsAndAcceptExactBoundary()
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc = new LoadCase(7, "Dead");
            var tri = TriPlate();
            var quad = QuadPlate();

            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(Enumerable.Repeat(lc, 100001), tri, tri.A));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForcesAsync(Enumerable.Repeat(lc, 100001), tri, tri.A).GetAwaiter().GetResult());
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(Enumerable.Repeat(lc, 100001), tri));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(Enumerable.Range(1, 25001).Select(id => new LoadCase(id, "")), quad));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForcesAsync(Enumerable.Range(1, 25001).Select(id => new LoadCase(id, "")), quad).GetAwaiter().GetResult());
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(lc, Enumerable.Repeat(quad, 100001)));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCornerForces(lc, Enumerable.Range(1, 25001).Select(id => QuadPlate(id))));
            Assert.That(native.Calls, Is.Empty);

            Assert.That(w.Output.GetPlateCornerForces(Enumerable.Repeat(lc, 100000), tri, tri.A).Count, Is.EqualTo(1));
            Assert.That(w.Output.GetPlateCornerForces(Enumerable.Repeat(lc, 100000), tri).Count, Is.EqualTo(3));
            Assert.That(w.Output.GetPlateCornerForces(lc, Enumerable.Repeat(quad, 100000)).Count, Is.EqualTo(4));
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeStatusesShapesSentinelsFailureStoppingAndFaultPropagationMatchCoreContract(string op)
        {
            var native = new PlateCornerTests.Native { Status = false };
            using var w = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op));
            native.Status = 2;
            Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op));

            native.Status = true;
            native.InPlace = true;
            native.Unwritten = 3;
            Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op));

            native.InPlace = false;
            native.Unwritten = -1;
            native.Values = new[] { 0d, -0d, double.MaxValue, -double.MaxValue, double.Epsilon, -double.Epsilon };
            Assert.DoesNotThrow(() => Invoke(w.Output, op));

            var array = Array.CreateInstance(typeof(double), [6], [-3]);
            for (int i = 0; i < 6; i++) array.SetValue((double)(i + 1), i - 3);
            native.Values = array;
            native.Status = 1;
            var copied = Invoke(w.Output, op);
            array.SetValue(777d, -3);
            Assert.That(copied.Results[0].Fx, Is.EqualTo(1));
            Assert.That(copied.Results[0].Mz, Is.EqualTo(6));

            native.Calls.Clear();
            native.FailAt = 2;
            var twoCases = new ILoadCase[] { new LoadCase(7, "A"), new LoadCase(8, "B") };
            var twoPlates = new[] { TriPlate(17, 11), TriPlate(18, 21) };
            Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op, twoCases, twoCases[0], plates: twoPlates));
            Assert.That(native.Calls, Has.Count.EqualTo(2));

            var comError = new COMException("native", -12345);
            native.FailAt = -1;
            native.Failure = comError;
            Assert.That(Assert.Throws<COMException>(() => Invoke(w.Output, op)), Is.SameAs(comError));
            native.Failure = null;
            Assert.DoesNotThrow(() => Invoke(w.Output, op));

            using var missing = Wrap(new object());
            Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Output, op));
        }

        [TestCaseSource(nameof(Operations))]
        public void StaleDisposedOtherThreadAndSharedReentryGuardsApplyAcrossEnumerationAndReads(string op)
        {
            var native = new PlateCornerTests.Native();
            var w = Wrap(native);
            var old = w.Output;

            Assert.Throws<InvalidOperationException>(() => Invoke(new OSOutputAdapter(w), op));
            w.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Invoke(old, op));

            var wrongThread = Task.Run(() =>
            {
                try { Invoke(w.Output, op); return null; }
                catch (Exception error) { return error; }
            }).GetAwaiter().GetResult();
            Assert.That(wrongThread, Is.TypeOf<InvalidOperationException>());

            void Reenter()
            {
                Assert.Throws<InvalidOperationException>(() => w.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(() => w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op));
                Assert.Throws<InvalidOperationException>(() => w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => w.Dispose());
            }

            native.Callback = Reenter;
            if (op == "casePlates")
            {
                var plates = new Once<Plate>([TriPlate()], Reenter);
                Assert.DoesNotThrow(() => Invoke(w.Output, op, plates: plates));
            }
            else
            {
                var cases = new Once<ILoadCase>([new LoadCase(7, "")], Reenter);
                Assert.DoesNotThrow(() => Invoke(w.Output, op, cases: cases));
            }

            var current = w.Output;
            w.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Invoke(current, op));
        }

        [TestCase("casesCornerTask")]
        [TestCase("casesAllTask")]
        public void TaskMethodsCompleteSynchronouslyAndFaultWithoutThrowingSynchronously(string op)
        {
            var native = new PlateCornerTests.Native();
            using var w = Wrap(native);
            var lc = new LoadCase(7, "Dead");
            var p = TriPlate();

            Task<PlateCornerForcesResultSet> Call(IEnumerable<ILoadCase> cases) =>
                op == "casesCornerTask"
                    ? w.Output.GetPlateCornerForcesAsync(cases, p, p.A)
                    : w.Output.GetPlateCornerForcesAsync(cases, p);

            var ok = Call([lc]);
            Assert.That(ok.IsCompletedSuccessfully, Is.True);
            Assert.That(native.Calls, Has.Count.EqualTo(op == "casesCornerTask" ? 1 : 3));

            var nullFault = Call(null);
            Assert.That(nullFault.IsFaulted, Is.True);
            Assert.Throws<ArgumentNullException>(() => nullFault.GetAwaiter().GetResult());

            var comError = new COMException("native", -54321);
            native.Failure = comError;
            Task<PlateCornerForcesResultSet> nativeFault = null;
            Assert.DoesNotThrow(() => nativeFault = Call([lc]));
            Assert.That(nativeFault.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => nativeFault.GetAwaiter().GetResult()), Is.SameAs(comError));
        }

        [Test]
        public void FiveNewSelectionContractsShipWithFullMatchingExpandedXml()
        {
            var xml = XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location, ".xml"));
            var methods = typeof(IOSOutput).GetMethods()
                .Where(m => m.Name.Contains("PlateCornerForces") && m.GetParameters().Any(p => p.ParameterType.IsGenericType))
                .ToArray();
            Assert.That(methods, Has.Length.EqualTo(5));

            foreach (var m in methods)
            {
                string TypeId(Type t) => t.IsGenericType
                    ? t.GetGenericTypeDefinition().FullName.Split((char)96)[0] + "{" + string.Join(",", t.GetGenericArguments().Select(TypeId)) + "}"
                    : t.FullName;
                string suffix = "." + m.Name + "(" + string.Join(",", m.GetParameters().Select(p => TypeId(p.ParameterType))) + ")";
                var contract = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(IOSOutput).FullName + suffix);
                var implementation = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(OSOutputAdapter).FullName + suffix);
                Assert.That(string.Concat(contract.Elements()), Is.EqualTo(string.Concat(implementation.Elements())));
                Assert.That(contract.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
                Assert.That(m.GetParameters().Any(p => p.ParameterType.IsByRef || p.IsOptional), Is.False);
                foreach (string tag in new[] { "summary", "param", "returns", "remarks", "exception", "example" })
                    Assert.That(contract.Element(tag), Is.Not.Null);
            }
        }
    }
}

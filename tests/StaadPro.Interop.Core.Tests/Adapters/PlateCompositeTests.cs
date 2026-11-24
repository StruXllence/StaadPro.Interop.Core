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
    public sealed class PlateCompositeTests
    {
        private static readonly string[] Operations = ["single", "singleTask", "batch", "batchTask"];
        public sealed class Native
        {
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public Action Callback;
            public int FailAt = -1, UnwrittenAt = -1;
            public object FailureStatus = false;
            public Exception Failure;
            public double[] Forces = [1d, -2d, 3d, -4d, 5d], Moments = [6d, -7d, 8d];
            private object Step(string name, int plate, int load)
            {
                Calls.Add(name + ":" + plate + ":" + load); Threads.Add(Environment.CurrentManagedThreadId);
                Callback?.Invoke();
                if (Calls.Count == FailAt) { if (Failure != null) throw Failure; return FailureStatus; }
                return true;
            }
            private void Vector(ref object buffer, double[] data)
            {
                Assert.That(buffer, Is.TypeOf<double[]>());
                Assert.That(((double[])buffer).All(double.IsNaN), Is.True);
                if (Calls.Count != UnwrittenAt) buffer = data;
            }
            public object GetAllPlateCenterForces(int plate, int load, ref object buffer)
            { var s = Step("forces", plate, load); Vector(ref buffer, Forces); return s; }
            public object GetAllPlateCenterMoments(int plate, int load, ref object buffer)
            { var s = Step("moments", plate, load); Vector(ref buffer, Moments); return s; }
            public object GetPlateCenterVonMisesStresses(int plate, int load, ref object top, ref object bottom)
            {
                Assert.That(double.IsNaN((double)top) && double.IsNaN((double)bottom), Is.True);
                var s = Step("von", plate, load); if (Calls.Count != UnwrittenAt) { top = 9d; bottom = -10d; } return s;
            }
            public object GetPlateCenterNormalPrincipalStresses(int plate, int load,
                ref object maxTop, ref object minTop, ref object maxBottom, ref object minBottom)
            {
                Assert.That(new[] { maxTop, minTop, maxBottom, minBottom }.All(x => x is double d && double.IsNaN(d)), Is.True);
                var s = Step("principal", plate, load);
                if (Calls.Count != UnwrittenAt) { maxTop = 11d; minTop = -12d; maxBottom = 13d; minBottom = -14d; } return s;
            }
        }
        private static OpenStaadWrapper Wrap(object output) => new(new NativeRoot { Output = output });
        private static ILoadCase Case(int id = 7) => new LoadCase(id, "Case");
        private static Plate Plate(int id = 17) => new() { Id = id };
        private static IEnumerable<PlateCenterResults> Invoke(IOSOutput output, string op, ILoadCase load = null, Plate plate = null,
            IEnumerable<ILoadCase> cases = null, IEnumerable<Plate> plates = null)
        {
            load ??= Case(); plate ??= Plate(); cases ??= new[] { load }; plates ??= new[] { plate };
            return op switch
            {
                "single" => new[] { output.GetPlateCenterResults(load, plate) },
                "singleTask" => new[] { output.GetPlateCenterResultsAsync(load, plate).GetAwaiter().GetResult() },
                "batch" => output.GetPlateCenterResults(cases, plates),
                "batchTask" => output.GetPlateCenterResultsAsync(cases, plates).GetAwaiter().GetResult(),
                _ => throw new ArgumentException(op)
            };
        }
        [TestCaseSource(nameof(Operations))]
        public void FourFamiliesPreserveOrderCopiedScalarsContextAndOwningThread(string op)
        {
            var native = new Native(); using var w = Wrap(native); var lc = Case(); var p = Plate();
            var result = Invoke(w.Output, op, lc, p).Single();
            Assert.That(native.Calls, Is.EqualTo(new[] { "forces:17:7", "moments:17:7", "von:17:7", "principal:17:7" }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(new[] { result.Forces.Sqx, result.Forces.Sqy, result.Forces.Sx, result.Forces.Sy, result.Forces.Sxy,
                result.Moments.Mx, result.Moments.My, result.Moments.Mxy, result.VonMises.Top, result.VonMises.Bottom,
                result.Principal.SmaxTop, result.Principal.SminTop, result.Principal.SmaxBottom, result.Principal.SminBottom },
                Is.EqualTo(new[] { 1d, -2d, 3d, -4d, 5d, 6d, -7d, 8d, 9d, -10d, 11d, -12d, 13d, -14d }));
            Assert.That(result.LoadCase, Is.SameAs(lc)); Assert.That(result.Plate, Is.SameAs(p));
            native.Forces[0] = 900; native.Moments[0] = 900;
            Assert.That(result.Forces.Sqx, Is.EqualTo(1)); Assert.That(result.Moments.Mx, Is.EqualTo(6));
        }
        private sealed class CapturedCase : ILoadCase
        {
            public int Reads;
            public int Id { get { Reads++; return Reads == 1 ? 7 : 99; } }
            public string Title => throw new Exception("Title must not be read.");
            public LoadType Type { get => throw new Exception(); set => throw new Exception(); }
            public LoadCaseType CaseType { get => throw new Exception(); set => throw new Exception(); }
        }
        [TestCaseSource(nameof(Operations))]
        public void AllCallsUseCapturedIdentityDespiteMutableMetadata(string op)
        {
            var native = new Native(); using var w = Wrap(native); var lc = new CapturedCase(); var p = Plate();
            native.Callback = () => p.Id = 999;
            var result = Invoke(w.Output, op, lc, p).Single();
            Assert.That(lc.Reads, Is.EqualTo(1)); Assert.That(native.Calls.All(x => x.EndsWith(":17:7")), Is.True);
            Assert.That(result.Plate.Id, Is.EqualTo(999)); Assert.That(result.Principal.Plate, Is.SameAs(p));
        }
        private sealed class Once<T>(IEnumerable<T> values, Action onStart = null) : IEnumerable<T>
        {
            public int Enumerations, Disposals;
            public IEnumerator<T> GetEnumerator()
            {
                if (++Enumerations != 1) throw new Exception("Re-enumeration");
                onStart?.Invoke();
                try { foreach (var value in values) yield return value; } finally { Disposals++; }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        [TestCase("batch")] [TestCase("batchTask")]
        public void CartesianBatchIsEagerOrderedDuplicatePreservingAndReadOnly(string op)
        {
            var native = new Native(); using var w = Wrap(native); var lc = Case(); var p = Plate();
            var cases = new Once<ILoadCase>(new[] { lc, Case(8), lc });
            var plates = new Once<Plate>(new[] { p, Plate(18), p }, () => Assert.That(cases.Disposals, Is.EqualTo(1)));
            native.Callback = () => Assert.That(plates.Disposals, Is.EqualTo(1));
            var results = Invoke(w.Output, op, cases: cases, plates: plates); var rows = results.ToArray();
            Assert.That(rows, Has.Length.EqualTo(9)); Assert.That(results.ToArray()[0], Is.SameAs(rows[0]));
            Assert.That(native.Calls, Has.Count.EqualTo(36));
            Assert.That(native.Calls.Where((_, i) => i % 4 == 0), Is.EqualTo(new[] {
                "forces:17:7", "forces:18:7", "forces:17:7", "forces:17:8", "forces:18:8", "forces:17:8", "forces:17:7", "forces:18:7", "forces:17:7" }));
            Assert.That(rows[0], Is.Not.SameAs(rows[2])); Assert.That(rows[0].Moments, Is.Not.SameAs(rows[2].Moments));
            rows[0].Moments.Mx = 42; Assert.That(rows[2].Moments.Mx, Is.EqualTo(6));
            Assert.That(((ICollection<PlateCenterResults>)results).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<PlateCenterResults>)results).RemoveAt(0));
        }
        [TestCase("batch")] [TestCase("batchTask")]
        public void BatchCapturesEveryOccurrenceBeforeDispatchEvenWithRepeatedReferences(string op)
        {
            var native = new Native(); using var w = Wrap(native); var lc = new CapturedCase(); var p = Plate();
            native.Callback = () => p.Id = 444;
            var rows = Invoke(w.Output, op, cases: new[] { lc, lc }, plates: new[] { p, p }).ToArray();
            Assert.That(lc.Reads, Is.EqualTo(2)); Assert.That(native.Calls.Take(8).All(x => x.EndsWith(":17:7")), Is.True);
            Assert.That(native.Calls.Skip(8).All(x => x.EndsWith(":17:99")), Is.True); Assert.That(rows, Has.Length.EqualTo(4));
        }
        private static IEnumerable<TestCaseData> StageFailures()
        {
            foreach (string op in Operations)
                foreach (int stage in new[] { 1, 2, 3, 4 })
                    foreach (object status in new object[] { false, 0, null, "1", 1d, 2 }) yield return new(op, stage, status);
        }
        [TestCaseSource(nameof(StageFailures))]
        public void FailedOrUnsupportedStagesStopImmediatelyWithoutPartialResults(string op, int stage, object status)
        {
            var native = new Native { FailAt = stage, FailureStatus = status }; using var w = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); Assert.That(native.Calls, Has.Count.EqualTo(stage));
            native.FailAt = -1; Assert.DoesNotThrow(() => Invoke(w.Output, op));
        }
        [TestCase("batch")] [TestCase("batchTask")]
        public void LaterPairFailureStopsBatchWithoutPublishingCollection(string op)
        {
            var failure = new COMException("native", unchecked((int)0x80004005));
            var native = new Native { FailAt = 6, Failure = failure }; using var w = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => Invoke(w.Output, op, plates: new[] { Plate(), Plate(18), Plate(19) })), Is.SameAs(failure));
            Assert.That(native.Calls, Has.Count.EqualTo(6));
        }
        [TestCaseSource(nameof(Operations))]
        public void UnwrittenFamiliesAndOriginalNativeFailuresRemainFailures(string op)
        {
            var native = new Native(); using var w = Wrap(native);
            foreach (int stage in new[] { 1, 2, 3, 4 })
            {
                native.Calls.Clear(); native.UnwrittenAt = stage;
                Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); Assert.That(native.Calls, Has.Count.EqualTo(stage));
                native.Calls.Clear(); native.UnwrittenAt = -1; native.FailAt = stage; native.Failure = new COMException("native", -12345);
                Assert.That(Assert.Throws<COMException>(() => Invoke(w.Output, op)), Is.SameAs(native.Failure)); native.FailAt = -1;
            }
            using var missing = Wrap(new object()); Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Output, op));
        }
        [TestCase("single")] [TestCase("singleTask")]
        public void SingleArgumentsAreValidatedBeforeNativeAccess(string op)
        {
            var native = new Native(); using var w = Wrap(native);
            void Call(ILoadCase lc, Plate p) { if (op == "single") w.Output.GetPlateCenterResults(lc, p); else w.Output.GetPlateCenterResultsAsync(lc, p).GetAwaiter().GetResult(); }
            Assert.That(Assert.Throws<ArgumentNullException>(() => Call(null, Plate())).ParamName, Is.EqualTo("loadCase"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => Call(Case(), null)).ParamName, Is.EqualTo("plate"));
            foreach (int id in new[] { 0, -1, int.MinValue })
            {
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Call(Case(id), Plate())).ParamName, Is.EqualTo("loadCase"));
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Call(Case(), Plate(id))).ParamName, Is.EqualTo("plate"));
            }
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCase("batch")] [TestCase("batchTask")]
        public void BatchValidationChecksNullEntriesIdsAndBothDimensionsEvenWhenEmpty(string op)
        {
            var native = new Native(); using var w = Wrap(native);
            void Call(IEnumerable<ILoadCase> cases, IEnumerable<Plate> plates) { if (op == "batch") w.Output.GetPlateCenterResults(cases, plates); else w.Output.GetPlateCenterResultsAsync(cases, plates).GetAwaiter().GetResult(); }
            Assert.That(Assert.Throws<ArgumentNullException>(() => Call(null, new[] { Plate() })).ParamName, Is.EqualTo("loadCases"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => Call(new[] { Case() }, null)).ParamName, Is.EqualTo("plates"));
            Assert.Throws<ArgumentException>(() => Call(new[] { Case(), null }, new[] { Plate() }));
            Assert.Throws<ArgumentException>(() => Call(new[] { Case() }, new[] { Plate(), null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Call(new[] { Case(), Case(0) }, Array.Empty<Plate>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => Call(Array.Empty<ILoadCase>(), new[] { Plate(-1) }));
            Call(Array.Empty<ILoadCase>(), new[] { Plate() }); Call(new[] { Case() }, Array.Empty<Plate>());
            Assert.That(native.Calls, Is.Empty);
        }
        [Test]
        public void OccurrenceAndCartesianLimitsPrecedeNativeCallsAndAcceptExactEmptyBoundary()
        {
            var native = new Native(); using var w = Wrap(native);
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCenterResults(Enumerable.Repeat(Case(), 100001), Array.Empty<Plate>()));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCenterResults(Array.Empty<ILoadCase>(), Enumerable.Repeat(Plate(), 100001)));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCenterResults(Enumerable.Repeat(Case(), 317), Enumerable.Repeat(Plate(), 316)));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCenterResults(Enumerable.Repeat(Case(), 65536), Enumerable.Repeat(Plate(), 65536)));
            Assert.That(w.Output.GetPlateCenterResults(Enumerable.Repeat(Case(), 100000), Array.Empty<Plate>()), Is.Empty);
            Assert.That(w.Output.GetPlateCenterResults(Array.Empty<ILoadCase>(), Enumerable.Repeat(Plate(), 100000)), Is.Empty);
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCase("batch")] [TestCase("batchTask")]
        public void EnumerationFailuresDisposeInputsWithoutNativeCalls(string op)
        {
            var native = new Native(); using var w = Wrap(native); var failure = new ApplicationException("enumeration");
            IEnumerable<Plate> Fail() { yield return Plate(); throw failure; }
            var cases = new Once<ILoadCase>(new[] { Case() }); var plates = new Once<Plate>(Fail());
            Assert.That(Assert.Throws<ApplicationException>(() => Invoke(w.Output, op, cases: cases, plates: plates)), Is.SameAs(failure));
            Assert.That(cases.Disposals, Is.EqualTo(1)); Assert.That(plates.Disposals, Is.EqualTo(1)); Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void BindingThreadAndDisposalGuardsApplyToEveryEntry(string op)
        {
            var native = new Native(); var w = Wrap(native); var old = w.Output;
            Assert.Throws<InvalidOperationException>(() => Invoke(new OSOutputAdapter(w), op)); w.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Invoke(old, op));
            var error = Task.Run(() => { try { Invoke(w.Output, op); return null; } catch (Exception e) { return e; } }).GetAwaiter().GetResult();
            Assert.That(error, Is.TypeOf<InvalidOperationException>()); var current = w.Output; w.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Invoke(current, op)); Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void OneLeaseSpansEnumerationAndAllNativeCallsAndRecoversAfterFailure(string op)
        {
            var native = new Native(); using var w = Wrap(native);
            void Reentry()
            {
                Assert.Throws<InvalidOperationException>(() => w.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(() => w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(() => w.Output.GetPlateCenterResults(Case(), Plate()));
                Assert.Throws<InvalidOperationException>(() => w.RefreshComObjects()); Assert.Throws<InvalidOperationException>(() => w.Dispose());
            }
            native.Callback = Reentry;
            Assert.DoesNotThrow(() => Invoke(w.Output, op, cases: new Once<ILoadCase>(new[] { Case() }, Reentry), plates: new Once<Plate>(new[] { Plate() }, Reentry)));
            Assert.That(native.Calls, Has.Count.EqualTo(4));
            native.Callback = () => throw new ApplicationException("callback"); Assert.Throws<ApplicationException>(() => Invoke(w.Output, op));
            native.Callback = null; Assert.DoesNotThrow(() => Invoke(w.Output, op));
        }
        [TestCase("singleTask")] [TestCase("batchTask")]
        public void TasksCompleteWorkBeforeReturnAndCaptureValidationFaults(string op)
        {
            var native = new Native(); using var w = Wrap(native);
            Task Start(ILoadCase lc) => op == "singleTask" ? w.Output.GetPlateCenterResultsAsync(lc, Plate()) : w.Output.GetPlateCenterResultsAsync(new[] { lc }, new[] { Plate() });
            Task success = Start(Case()); Assert.That(success.IsCompletedSuccessfully, Is.True); Assert.That(native.Calls, Has.Count.EqualTo(4));
            Task fault = null; Assert.DoesNotThrow(() => fault = Start(null)); Assert.That(fault.IsFaulted, Is.True);
            Assert.That(() => fault.GetAwaiter().GetResult(), Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void ContainerPreservesCaseReferenceAndPlateIdCompatibilityAndManualScalars()
        {
            var lc = Case(); var p = Plate(); var f = new PlateCenterForces(lc, p); var m = new PlateCenterMoments(lc, Plate());
            var v = new PlateCenterVonMises(lc, Plate()); var s = new PlatePrincipalStresses(lc, Plate());
            var result = PlateCenterResults.Create(f, m, v, s); Assert.That(result.Forces, Is.SameAs(f)); Assert.That(result.Plate, Is.SameAs(p));
            m.Mx = double.NaN; Assert.That(double.IsNaN(result.Moments.Mx), Is.True);
            Assert.Throws<InvalidOperationException>(() => new PlateCenterResults(f, new PlateCenterMoments(Case(), p), v, s));
            Assert.Throws<InvalidOperationException>(() => new PlateCenterResults(f, new PlateCenterMoments(lc, Plate(18)), v, s));
            Assert.Throws<InvalidOperationException>(() => new PlateCenterResults(f, new PlateCenterMoments(lc, null), v, s));
            Assert.That(new PlateCenterResults(new(null, null), new(null, null), new(null, null), new(null, null)).LoadCase, Is.Null);
            Assert.Throws<ArgumentNullException>(() => new PlateCenterResults(null, m, v, s)); Assert.Throws<ArgumentNullException>(() => new PlateCenterResults(f, null, v, s));
            Assert.Throws<ArgumentNullException>(() => new PlateCenterResults(f, m, null, s)); Assert.Throws<ArgumentNullException>(() => new PlateCenterResults(f, m, v, null));
        }
        [Test]
        public void FiveNewContractsHaveCompleteMatchingExpandedXmlAndExactSourceSignatures()
        {
            var xml = XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location, ".xml"));
            var names = new[] { "GetPlateCenterResults", "GetPlateCenterResultsAsync", "GetPlatePrincipalStressesAsync" };
            var methods = typeof(IOSOutput).GetMethods().Where(m => names.Contains(m.Name) && m.GetParameters().Length == 2).ToArray(); Assert.That(methods, Has.Length.EqualTo(5));
            foreach (var method in methods)
            {
                Assert.That(method.GetParameters().Any(p => p.IsOptional || p.ParameterType.IsByRef), Is.False);
                string XmlType(Type t) => t.IsGenericType ? t.GetGenericTypeDefinition().FullName.Split((char)96)[0] + "{" + string.Join(",", t.GetGenericArguments().Select(XmlType)) + "}" : t.FullName;
                string suffix = "." + method.Name + "(" + string.Join(",", method.GetParameters().Select(p => XmlType(p.ParameterType))) + ")";
                var contract = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(IOSOutput).FullName + suffix);
                var adapter = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(OSOutputAdapter).FullName + suffix);
                Assert.That(string.Concat(contract.Elements()), Is.EqualTo(string.Concat(adapter.Elements())));
                foreach (string tag in new[] { "summary", "param", "returns", "remarks", "exception", "example" }) Assert.That(contract.Elements(tag), Is.Not.Empty);
                Assert.That(contract.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
                Type model = method.Name.Contains("Principal") ? typeof(PlatePrincipalStresses) : typeof(PlateCenterResults);
                bool batch = method.GetParameters()[0].ParameterType.IsGenericType;
                Type result = batch ? typeof(IEnumerable<PlateCenterResults>) : model;
                Assert.That(method.ReturnType, Is.EqualTo(method.Name.EndsWith("Async") ? typeof(Task<>).MakeGenericType(result) : result));
                Assert.That(method.GetParameters().Select(p => p.Name), Is.EqualTo(batch ? new[] { "loadCases", "plates" } : new[] { "loadCase", "plate" }));
            }
        }
    }
}

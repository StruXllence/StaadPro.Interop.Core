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
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCornerTests
    {
        private static readonly string[] Operations = ["one", "oneTask", "all", "allTask"];
        public sealed class Native
        {
            public object Status = true, Values = new[] { 1d, -2d, 3d, -4d, 5d, -6d };
            public bool Write = true, InPlace;
            public int Unwritten = -1, FailAt = -1;
            public Exception Failure;
            public Action Callback;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public object GetPlateCornerForces(int plate, int node, int load, ref object buffer)
            {
                Assert.That(buffer, Is.TypeOf<double[]>()); Assert.That((double[])buffer, Has.Length.EqualTo(6));
                Assert.That(((double[])buffer).All(double.IsNaN), Is.True);
                Calls.Add($"{plate}:{node}:{load}"); Threads.Add(Environment.CurrentManagedThreadId); Callback?.Invoke();
                if (Failure != null && (FailAt < 0 || FailAt == Calls.Count)) throw Failure;
                if (Write)
                {
                    if (InPlace) { for (int i = 0; i < 6; i++) if (i != Unwritten) ((double[])buffer)[i] = ((double[])Values)[i]; }
                    else buffer = Values;
                }
                return FailAt == Calls.Count ? false : Status;
            }
        }
        public sealed class Geometry
        {
            public object Status = 0;
            public object[] Ids = [11, 12, 13, 0];
            public object[] Coordinates = [1d, 2d, 3d];
            public bool WriteIds = true, WriteCoordinates = true;
            public Action Callback;
            public Exception Failure;
            public readonly List<string> Calls = [];
            public object GetPlateIncidence(int plate, ref object a, ref object b, ref object c, ref object d)
            {
                Assert.That(new[] { a, b, c, d }, Is.All.EqualTo(int.MinValue)); Calls.Add("incidence:" + plate);
                Callback?.Invoke(); if (Failure != null) throw Failure;
                if (WriteIds) { a = Ids[0]; b = Ids[1]; c = Ids[2]; d = Ids[3]; }
                return Status;
            }
            public void GetNodeCoordinates(int node, ref object x, ref object y, ref object z)
            {
                Assert.That(new[] { x, y, z }.All(v => v is double d && double.IsNaN(d)), Is.True);
                Calls.Add("node:" + node); Callback?.Invoke();
                if (WriteCoordinates) { x = Coordinates[0]; y = Coordinates[1]; z = Coordinates[2]; }
            }
        }
        private static OpenStaadWrapper Wrap(object output, object geometry = null) => new(new NativeRoot { Output = output, Geometry = geometry ?? new object() });
        private static Plate Plate() => new(new Node { Id = 11 }, new Node { Id = 12 }, new Node { Id = 13 }) { Id = 17 };
        private static object Invoke(IOSOutput output, string op, ILoadCase load = null, Plate plate = null, Node node = null)
        {
            load ??= new LoadCase(7, "Case"); plate ??= Plate(); node ??= plate.A;
            return op switch
            {
                "one" => output.GetPlateCornerForces(load, plate, node),
                "oneTask" => output.GetPlateCornerForcesAsync(load, plate, node).GetAwaiter().GetResult(),
                "all" => output.GetPlateCornerForces(load, plate),
                "allTask" => output.GetPlateCornerForcesAsync(load, plate).GetAwaiter().GetResult(),
                _ => throw new ArgumentException(op)
            };
        }
        private static PlateCornerForces First(object value) => value is PlateCornerForces one ? one : ((PlateCornerForcesResultSet)value).Results[0];
        [TestCaseSource(nameof(Operations))]
        public void NativeOrderComponentsReferencesAndIndependentBuffersArePreserved(string op)
        {
            var native = new Native(); using var w = Wrap(native); var p = Plate(); var load = new LoadCase(7, "Case");
            var result = First(Invoke(w.Output, op, load, p, new Node { Id = 11 }));
            Assert.That(native.Calls[0], Is.EqualTo("17:11:7")); Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(new[] { result.Fx, result.Fy, result.Fz, result.Mx, result.My, result.Mz }, Is.EqualTo(new[] { 1d, -2d, 3d, -4d, 5d, -6d }));
            Assert.That(result.CornerNode, Is.SameAs(p.A)); Assert.That(result.LoadCase, Is.SameAs(load)); Assert.That(result.Plate, Is.SameAs(p));
            ((double[])native.Values)[0] = 999; Assert.That(result.Fx, Is.EqualTo(1));
        }
        [TestCase("all")] [TestCase("allTask")]
        public void AllCornersRetainFirstIdOrderAndCapturedIndexesDespiteContextEdits(string op)
        {
            var native = new Native(); using var w = Wrap(native); var p = Plate(); var a = p.A; p.D = new Node { Id = 11 };
            var lc = new LoadCase(7, "Case"); native.Callback = () => { lc.Id = 99; p.Id = 999; a.Id = 111; p.B = null; };
            var set = (PlateCornerForcesResultSet)Invoke(w.Output, op, lc, p, a);
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:7", "17:12:7", "17:13:7" }));
            Assert.That(set.Count, Is.EqualTo(3)); Assert.That(set.LoadCaseIds, Is.EqualTo(new[] { 7 }));
            Assert.That(set.CornerNodeIds, Is.EqualTo(new[] { 11, 12, 13 })); Assert.That(set[7, 17, 11].CornerNode, Is.SameAs(a));
            Assert.That(set.GetForPlateCorner(17, 11).Single(), Is.SameAs(set[7, 17, 11]));
            Assert.That(set.GetForLoadCaseAndPlate(7, 17), Has.Count.EqualTo(3)); Assert.That(set[7, 17, 11].Key.LoadCaseId, Is.EqualTo(99));
            Assert.Throws<NotSupportedException>(() => ((IList<PlateCornerForces>)set.Results).Clear());
            Assert.That(set.Results[0], Is.Not.SameAs(set.Results[1])); set.Results[0].Fx = 44; Assert.That(set.Results[1].Fx, Is.EqualTo(1));
        }
        private static IEnumerable<TestCaseData> BadStatuses()
        { foreach (string op in Operations) foreach (object v in new object[] { false, 0, -1, 2, null, "1", 1d, 1m, DayOfWeek.Monday, ulong.MaxValue }) yield return new(op, v); }
        [TestCaseSource(nameof(BadStatuses))]
        public void UnsupportedOrFailedOutputStatusesCannotReturnData(string op, object status)
        { var native = new Native { Status = status }; using var w = Wrap(native); Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); Assert.That(native.Calls, Has.Count.EqualTo(1)); }
        private static IEnumerable<TestCaseData> BadArrays()
        {
            foreach (string op in Operations)
                foreach (object v in new object[] { null, new double[5], new double[7], new double[2, 3], new object[] { 1d, 2d, 3d, 4d, 5d, 6d }, new int[6], new[] { double.NaN, 0d, 0, 0, 0, 0 }, new[] { 0d, 0, 0, 0, 0, double.PositiveInfinity } }) yield return new(op, v);
        }
        [TestCaseSource(nameof(BadArrays))]
        public void WrongShapeTypeAndNonfiniteArraysFailBeforePublishing(string op, object values)
        { var native = new Native { Values = values }; using var w = Wrap(native); Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); }
        [TestCaseSource(nameof(Operations))]
        public void EveryUnwrittenComponentFailsAndExplicitWrittenZerosRemainValid(string op)
        {
            var native = new Native { InPlace = true }; using var w = Wrap(native);
            for (int i = 0; i < 6; i++) { native.Unwritten = i; Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); }
            native.Unwritten = -1; native.Values = new double[6]; Assert.That(First(Invoke(w.Output, op)).Mz, Is.Zero);
            native.Values = new[] { double.MaxValue, -double.MaxValue, double.Epsilon, -double.Epsilon, 0d, -0d };
            Assert.DoesNotThrow(() => Invoke(w.Output, op));
        }
        [TestCaseSource(nameof(Operations))]
        public void ReplacementArraysWithNonzeroBoundsAreCopied(string op)
        {
            var array = Array.CreateInstance(typeof(double), new[] { 6 }, new[] { -8 });
            for (int i = 0; i < 6; i++) array.SetValue((double)(i + 1), i - 8);
            var native = new Native { Values = array, Status = 1L }; using var w = Wrap(native); var result = First(Invoke(w.Output, op));
            array.SetValue(100d, -8); Assert.That(result.Fx, Is.EqualTo(1)); Assert.That(result.Mz, Is.EqualTo(6));
        }
        [TestCase("one")] [TestCase("oneTask")]
        public void MissingIncidenceHydratesANewPlateWithZeroSuccessAndVoidCoordinateReads(string op)
        {
            var native = new Native(); var geometry = new Geometry { Ids = [11, 12, 13, 11] }; using var w = Wrap(native, geometry);
            var input = new Plate { Id = 17 }; var requested = new Node { Id = 12 }; var lc = new LoadCase(7, "Case");
            geometry.Callback = () => { input.Id = 99; requested.Id = 99; lc.Id = 99; };
            var result = (PlateCornerForces)Invoke(w.Output, op, lc, input, requested);
            Assert.That(geometry.Calls, Is.EqualTo(new[] { "incidence:17", "node:11", "node:12", "node:13" }));
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:12:7" })); Assert.That(input.A, Is.Null);
            Assert.That(result.Plate, Is.Not.SameAs(input)); Assert.That(result.Plate.Id, Is.EqualTo(17));
            Assert.That(result.CornerNode, Is.SameAs(result.Plate.B)); Assert.That(result.CornerNode.X, Is.EqualTo(1)); Assert.That(result.Plate.D, Is.SameAs(result.Plate.A));
        }
        [TestCase("one")] [TestCase("oneTask")]
        public void HydrationIsCoveredByTheSharedLeaseAndRejectsMissingCornerMembership(string op)
        {
            var native = new Native(); var geo = new Geometry(); using var w = Wrap(native, geo);
            geo.Callback = () =>
            {
                Assert.Throws<InvalidOperationException>(() => w.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(() => w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(() => w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => w.Dispose());
            };
            Assert.Throws<ArgumentException>(() => Invoke(w.Output, op, plate: new Plate { Id = 17 }, node: new Node { Id = 99 }));
            Assert.That(native.Calls, Is.Empty); Assert.That(geo.Calls, Has.Count.EqualTo(4));
            Assert.DoesNotThrow(() => Invoke(w.Output, op, plate: new Plate { Id = 17 }, node: new Node { Id = 11 }));
        }
        private sealed class CapturedCase : ILoadCase
        {
            internal int Reads;
            public int Id { get { Reads++; return Reads == 1 ? 7 : 999; } }
            public string Title => throw new ApplicationException("Unused title");
            public StaadPro.Interop.Core.Enums.LoadType Type { get => throw new ApplicationException(); set => throw new ApplicationException(); }
            public StaadPro.Interop.Core.Enums.LoadCaseType CaseType { get => throw new ApplicationException(); set => throw new ApplicationException(); }
        }
        [TestCaseSource(nameof(Operations))]
        public void QueryAndIndexCreationReadTheLoadIdentifierOnlyOnce(string op)
        {
            var native = new Native(); using var w = Wrap(native); var load = new CapturedCase();
            var result = Invoke(w.Output, op, load); Assert.That(load.Reads, Is.EqualTo(1));
            Assert.That(native.Calls.All(c => c.EndsWith(":7")), Is.True);
            if (result is PlateCornerForcesResultSet set) Assert.That(set.LoadCaseIds, Is.EqualTo(new[] { 7 }));
        }
        [TestCase("all")] [TestCase("allTask")]
        public void QuadrilateralIncidenceUsesAllFourIdsWithoutReadingUnusedCoordinates(string op)
        {
            var native = new Native(); using var w = Wrap(native); var p = Plate(); p.A.X = double.NaN; p.D = new Node { Id = 14, Z = double.PositiveInfinity };
            var set = (PlateCornerForcesResultSet)Invoke(w.Output, op, plate: p);
            Assert.That(native.Calls, Is.EqualTo(new[] { "17:11:7", "17:12:7", "17:13:7", "17:14:7" }));
            Assert.That(set.Count, Is.EqualTo(4)); Assert.That(set.Results[3].CornerNode, Is.SameAs(p.D));
        }
        private static IEnumerable<TestCaseData> BadIncidence()
        {
            foreach (object s in new object[] { -1, -4001, 1, true, null, "0", 0d }) yield return new(s, new object[] { 11, 12, 13, 0 });
            foreach (object[] ids in new[] { new object[] { 0, 12, 13, 0 }, new object[] { 11, -1, 13, 0 }, new object[] { 11, 12, 13, -1 }, new object[] { 11d, 12, 13, 0 }, new object[] { 11, 12, 13, null } }) yield return new(0, ids);
        }
        [TestCaseSource(nameof(BadIncidence))]
        public void IncidenceStatusAndIdsAreQualifiedUsingTheGeometryContract(object status, object[] ids)
        {
            var native = new Native(); var geo = new Geometry { Status = status, Ids = ids }; using var w = Wrap(native, geo);
            Assert.Throws<InvalidOperationException>(() => w.Output.GetPlateCornerForces(new LoadCase(7, ""), new Plate { Id = 17 }, new Node { Id = 11 }));
            Assert.That(native.Calls, Is.Empty); Assert.That(geo.Calls, Has.Count.EqualTo(1));
        }
        [Test]
        public void UnwrittenIncidenceAndVoidCoordinatesCannotMasqueradeAsZeroGeometry()
        {
            var native = new Native(); var geo = new Geometry { WriteIds = false }; using var w = Wrap(native, geo);
            void Call() => w.Output.GetPlateCornerForces(new LoadCase(7, ""), new Plate { Id = 17 }, new Node { Id = 11 });
            Assert.Throws<InvalidOperationException>(() => Call()); geo.WriteIds = true; geo.WriteCoordinates = false;
            Assert.Throws<InvalidOperationException>(() => Call()); geo.WriteCoordinates = true;
            foreach (object bad in new object[] { null, "1", 1, double.NaN, double.PositiveInfinity }) { geo.Coordinates = [1d, bad, 3d]; Assert.Throws<InvalidOperationException>(() => Call()); }
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void NullsIdsIncidenceAndMembershipFailBeforeForceDispatch(string op)
        {
            var native = new Native(); using var w = Wrap(native);
            object Call(ILoadCase lc, Plate p, Node node) => op switch { "one" => w.Output.GetPlateCornerForces(lc, p, node), "oneTask" => w.Output.GetPlateCornerForcesAsync(lc, p, node).GetAwaiter().GetResult(), "all" => w.Output.GetPlateCornerForces(lc, p), _ => w.Output.GetPlateCornerForcesAsync(lc, p).GetAwaiter().GetResult() };
            Assert.Throws<ArgumentNullException>(() => Call(null, Plate(), new Node { Id = 11 }));
            Assert.Throws<ArgumentNullException>(() => Call(new LoadCase(7, ""), null, new Node { Id = 11 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Call(new LoadCase(0, ""), Plate(), new Node { Id = 11 }));
            var p = Plate(); p.D = new Node { Id = -1 }; Assert.Throws<ArgumentException>(() => Call(new LoadCase(7, ""), p, p.A));
            if (op.StartsWith("one"))
            { Assert.Throws<ArgumentNullException>(() => Call(new LoadCase(7, ""), Plate(), null)); Assert.Throws<ArgumentOutOfRangeException>(() => Call(new LoadCase(7, ""), Plate(), new Node())); Assert.Throws<ArgumentException>(() => Call(new LoadCase(7, ""), Plate(), new Node { Id = 99 })); }
            else Assert.Throws<ArgumentException>(() => Call(new LoadCase(7, ""), new Plate { Id = 17 }, null));
            Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void StaleDisposedOtherThreadAndSharedReentryGuardsApplyAcrossAllNativeReads(string op)
        {
            var native = new Native(); var w = Wrap(native); var old = w.Output;
            Assert.Throws<InvalidOperationException>(() => Invoke(new OSOutputAdapter(w), op)); w.RefreshComObjects(); Assert.Throws<InvalidOperationException>(() => Invoke(old, op));
            var error = Task.Run(() => { try { Invoke(w.Output, op); return null; } catch (Exception e) { return e; } }).GetAwaiter().GetResult(); Assert.That(error, Is.TypeOf<InvalidOperationException>());
            native.Callback = () => { Assert.Throws<InvalidOperationException>(() => w.Root.GetApplicationVersion()); Assert.Throws<InvalidOperationException>(() => w.Load.GetLoadCaseTitle(1)); Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); Assert.Throws<InvalidOperationException>(() => w.RefreshComObjects()); Assert.Throws<InvalidOperationException>(() => w.Dispose()); };
            Assert.DoesNotThrow(() => Invoke(w.Output, op)); var current = w.Output; w.Dispose(); Assert.Throws<ObjectDisposedException>(() => Invoke(current, op));
        }
        [TestCaseSource(nameof(Operations))]
        public void NativeFaultsPreserveOriginalExceptionsAndReleaseTheLease(string op)
        {
            var failure = new COMException("native", -12345); var native = new Native { Failure = failure }; using var w = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => Invoke(w.Output, op)), Is.SameAs(failure)); native.Failure = null; Assert.DoesNotThrow(() => Invoke(w.Output, op));
            using var missing = Wrap(new object()); Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Output, op));
        }
        [TestCase("all")] [TestCase("allTask")]
        public void LaterCornerFailuresStopWithoutReturningAPartialSet(string op)
        { var native = new Native { FailAt = 2 }; using var w = Wrap(native); Assert.Throws<InvalidOperationException>(() => Invoke(w.Output, op)); Assert.That(native.Calls, Has.Count.EqualTo(2)); }
        [TestCase("oneTask")] [TestCase("allTask")]
        public void TaskCallsFinishNativeWorkBeforeReturningAndPreserveAwaitFaults(string op)
        {
            var native = new Native(); using var w = Wrap(native); var p = Plate();
            Task Start() => op == "oneTask" ? w.Output.GetPlateCornerForcesAsync(new LoadCase(7, ""), p, p.A) : w.Output.GetPlateCornerForcesAsync(new LoadCase(7, ""), p);
            var success = Start(); Assert.That(success.IsCompletedSuccessfully, Is.True); Assert.That(native.Calls, Has.Count.EqualTo(op == "oneTask" ? 1 : 3));
            native.Failure = new COMException("native", -54321); Task fault = null; Assert.DoesNotThrow(() => fault = Start()); Assert.That(fault.IsFaulted, Is.True); Assert.That(Assert.Throws<COMException>(() => fault.GetAwaiter().GetResult()), Is.SameAs(native.Failure));
        }
        [Test]
        public void CompositeThreadHintIsBoundedSequentialAndValidatedBeforeEnumeration()
        {
            var native = new PlateCompositeTests.Native(); using var w = Wrap(native);
            ILoadCase Fail() => throw new ApplicationException("Must not enumerate");
            IEnumerable<ILoadCase> Never() { yield return Fail(); }
            foreach (int hint in new[] { 0, -1, 513, int.MinValue, int.MaxValue }) Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => w.Output.GetPlateCenterResults(Never(), Array.Empty<Plate>(), hint)).ParamName, Is.EqualTo("nThreads"));
            foreach (int hint in new[] { 1, 512 }) { var rows = w.Output.GetPlateCenterResults(new[] { new LoadCase(7, "") }, new[] { Plate() }, hint).ToArray(); Assert.That(rows, Has.Length.EqualTo(1)); }
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.Throws<ArgumentException>(() => w.Output.GetPlateCenterResults(Enumerable.Repeat<ILoadCase>(new LoadCase(7, ""), 100001), Array.Empty<Plate>(), 1));
        }
        [Test]
        public void ManualModelsKeysIndexesAndCopiedArrayOwnershipFollowSourceMeaning()
        {
            var lc = new LoadCase(7, ""); var p = Plate(); var a = p.A; var values = new[] { 1d, 2d, 3d, 4d, 5d, 6d };
            var result = PlateCornerForces.ParseFromArray(values, lc, p, a); var set = new PlateCornerForcesResultSet(new[] { result }); values[0] = 99;
            Assert.That(result.Fx, Is.EqualTo(1)); var key = result.Key; Assert.That(key, Is.EqualTo(new PlateCornerForcesKey(7, 17, 11))); Assert.That(key.GetHashCode(), Is.EqualTo(new PlateCornerForcesKey(7, 17, 11).GetHashCode()));
            lc.Id = 77; a.Id = 111; Assert.That(set[key], Is.SameAs(result)); Assert.That(set.GetForPlateCorner(17, 11).Single(), Is.SameAs(result)); Assert.That(set.GetForLoadCaseAndPlate(7, 17).Single(), Is.SameAs(result));
            Assert.That(set.GetForLoadCase(123), Is.Empty); Assert.That(set.GetForPlate(17), Has.Count.EqualTo(1)); Assert.That(set.GetForCornerNode(11), Has.Count.EqualTo(1));
            Assert.Throws<KeyNotFoundException>(() => { var ignored = set[7, 17, 12]; }); Assert.Throws<ArgumentException>(() => new PlateCornerForcesResultSet(new[] { result, result }));
            Assert.Throws<ArgumentNullException>(() => new PlateCornerForcesResultSet((IEnumerable<PlateCornerForces>)null)); Assert.Throws<ArgumentNullException>(() => new PlateCornerForcesResultSet(new PlateCornerForces[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlateCornerForcesKey(0, 1, 1)); Assert.Throws<ArgumentException>(() => new PlateCornerForces(lc, p, a, new double[5]));
            Assert.Throws<ArgumentNullException>(() => new PlateCornerForces(lc, p, a, (double[])null)); Assert.That(new PlateCornerForces(lc, p, a, (Forces)null).Fx, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var ignored = new PlateCornerForces().Key; }); Assert.That(new PlateCornerForcesResultSet(), Is.Empty);
        }
        [Test]
        public void FiveSourceContractsShipWithFullMatchingExpandedXml()
        {
            var xml = XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location, ".xml"));
            var methods = typeof(IOSOutput).GetMethods().Where(m => m.Name.Contains("PlateCornerForces") || m.Name == "GetPlateCenterResults" && m.GetParameters().Length == 3).ToArray(); Assert.That(methods, Has.Length.EqualTo(5));
            foreach (var m in methods)
            {
                string TypeId(Type t) => t.IsGenericType ? t.GetGenericTypeDefinition().FullName.Split((char)96)[0] + "{" + string.Join(",", t.GetGenericArguments().Select(TypeId)) + "}" : t.FullName;
                string suffix = "." + m.Name + "(" + string.Join(",", m.GetParameters().Select(p => TypeId(p.ParameterType))) + ")";
                var contract = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(IOSOutput).FullName + suffix); var implementation = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(OSOutputAdapter).FullName + suffix);
                Assert.That(string.Concat(contract.Elements()), Is.EqualTo(string.Concat(implementation.Elements()))); Assert.That(m.GetParameters().Any(p => p.ParameterType.IsByRef || p.IsOptional), Is.False);
                foreach (string tag in new[] { "summary", "param", "returns", "remarks", "exception", "example" }) Assert.That(contract.Element(tag), Is.Not.Null);
            }
        }
    }
}

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
using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateSurfaceTests
    {
        private static readonly string[] Operations = ["moments", "momentsTask", "von", "vonTask", "principal", "principalTask"];

        public sealed class Native
        {
            public object Status = true, Moments = new[] { 11d, -12d, 13d };
            public object[] Von = [17d, -19d], Principal = [23d, -29d, 31d, -37d];
            public bool Write = true, InPlace;
            public int Unwritten = -1;
            public Action Callback;
            public Exception Failure;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];

            private void Step(string name, object plate, object load)
            {
                Assert.That(plate, Is.TypeOf<int>()); Assert.That(load, Is.TypeOf<int>());
                Calls.Add($"{name}:{plate}:{load}"); Threads.Add(Environment.CurrentManagedThreadId);
                Callback?.Invoke(); if (Failure != null) throw Failure;
            }
            private static void Check(object value) { Assert.That(value, Is.TypeOf<double>()); Assert.That(double.IsNaN((double)value), Is.True); }
            public object GetAllPlateCenterMoments(object plate, object load, ref object moments)
            {
                Assert.That(moments, Is.TypeOf<double[]>()); Assert.That((double[])moments, Has.Length.EqualTo(3));
                Assert.That(((double[])moments).All(double.IsNaN), Is.True); Step("moments", plate, load);
                if (Write)
                {
                    if (InPlace) { for (int i = 0; i < 3; i++) if (Unwritten != i) ((double[])moments)[i] = ((double[])Moments)[i]; }
                    else moments = Moments;
                }
                return Status;
            }
            public object GetPlateCenterVonMisesStresses(object plate, object load, ref object top, ref object bottom)
            {
                Check(top); Check(bottom); Step("von", plate, load);
                if (Write) { if (Unwritten != 0) top = Von[0]; if (Unwritten != 1) bottom = Von[1]; }
                return Status;
            }
            public object GetPlateCenterNormalPrincipalStresses(object plate, object load,
                ref object maxTop, ref object minTop, ref object maxBottom, ref object minBottom)
            {
                Check(maxTop); Check(minTop); Check(maxBottom); Check(minBottom); Step("principal", plate, load);
                if (Write)
                {
                    if (Unwritten != 0) maxTop = Principal[0]; if (Unwritten != 1) minTop = Principal[1];
                    if (Unwritten != 2) maxBottom = Principal[2]; if (Unwritten != 3) minBottom = Principal[3];
                }
                return Status;
            }
        }
        private static OpenStaadWrapper Wrap(object output) => new(new NativeRoot { Output = output });
        private static object Invoke(IOSOutput output, string op, ILoadCase loadCase = null, Plate plate = null, bool defaults = true)
        {
            if (defaults) { loadCase ??= new LoadCase(7, "Case"); plate ??= new Plate { Id = 17 }; }
            return op switch
            {
                "moments" => output.GetPlateCenterMoments(loadCase, plate),
                "momentsTask" => output.GetPlateCenterMomentsAsync(loadCase, plate).GetAwaiter().GetResult(),
                "von" => output.GetPlateCenterVonMises(loadCase, plate),
                "vonTask" => output.GetPlateCenterVonMisesAsync(loadCase, plate).GetAwaiter().GetResult(),
                "principal" => output.GetPlatePrincipalStresses(loadCase, plate),
                "principalTask" => output.GetPlatePrincipalStressesAsync(loadCase, plate).GetAwaiter().GetResult(),
                _ => throw new ArgumentOutOfRangeException(nameof(op))
            };
        }
        private static double[] Components(object result) => result switch
        {
            PlateCenterMoments m => [m.Mx, m.My, m.Mxy],
            PlateCenterVonMises v => [v.Top, v.Bottom],
            PlatePrincipalStresses p => [p.SmaxTop, p.SminTop, p.SmaxBottom, p.SminBottom],
            _ => throw new ArgumentException("Unexpected record.")
        };
        private static IEnumerable<TestCaseData> BadStatuses()
        {
            foreach (string op in Operations)
                foreach (object value in new object[] { false, 0, null, -1, 2, "true", "1", 1d, 1f, 1m, DayOfWeek.Monday, ulong.MaxValue, DBNull.Value })
                    yield return new TestCaseData(op, value);
        }
        private static IEnumerable<TestCaseData> BadMomentBuffers()
        {
            foreach (string op in new[] { "moments", "momentsTask" })
                foreach (object value in new object[] { null, 0d, new double[0], new double[2], new double[4], new double[1, 3],
                    new int[3], new float[3], new object[] { 1d, 2d, 3d }, new[] { 1d, 2d, double.NaN },
                    new[] { 1d, double.PositiveInfinity, 3d }, new[] { double.NegativeInfinity, 2d, 3d } })
                    yield return new TestCaseData(op, value);
        }
        private static IEnumerable<TestCaseData> BadScalars()
        {
            foreach (string op in new[] { "von", "vonTask", "principal" })
                for (int i = 0; i < (op == "principal" ? 4 : 2); i++)
                    foreach (object value in new object[] { null, true, 1, 1L, "1", 1f, 1m, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                        yield return new TestCaseData(op, i, value);
        }

        [TestCaseSource(nameof(Operations))]
        public void ExactNativeArgumentOrderStatusAndComponentMappingArePreserved(string op)
        {
            foreach (object status in new object[] { true, (sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL })
            {
                var native = new Native { Status = status }; using var wrapper = Wrap(native);
                var lc = new LoadCase(7, "Case"); var plate = new Plate { Id = 17 };
                var result = (PlateStress)Invoke(wrapper.Output, op, lc, plate);
                Assert.That(native.Calls, Is.EqualTo(new[] { (op.StartsWith("moments") ? "moments" : op.StartsWith("von") ? "von" : "principal") + ":17:7" }));
                Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
                Assert.That(result.LoadCase, Is.SameAs(lc)); Assert.That(result.Plate, Is.SameAs(plate));
                double[] expected = op.StartsWith("moments") ? [11, -12, 13] : op.StartsWith("von") ? [17, -19] : [23, -29, 31, -37];
                Assert.That(Components(result), Is.EqualTo(expected));
            }
        }
        [TestCaseSource(nameof(BadStatuses))]
        public void UnsuccessfulOrMalformedStatusReturnsNoRecordAndReleasesLease(string op, object status)
        {
            var native = new Native { Status = status, Write = false }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op)); Assert.That(native.Calls, Has.Count.EqualTo(1));
            native.Status = true; native.Write = true; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }
        [TestCaseSource(nameof(BadMomentBuffers))]
        public void MomentArraysRequireExactWrittenThreeDoubles(string op, object value)
        {
            var native = new Native { Moments = value }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
        }
        [TestCaseSource(nameof(BadScalars))]
        public void EverySurfaceScalarRequiresWrittenFiniteDouble(string op, int slot, object value)
        {
            var native = new Native(); if (op == "principal") native.Principal[slot] = value; else native.Von[slot] = value;
            using var wrapper = Wrap(native); Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
        }
        [TestCaseSource(nameof(Operations))]
        public void UnwrittenOutputsCannotBecomeInitializedZeros(string op)
        {
            var native = new Native { Write = false }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
            native.Write = true; native.InPlace = true;
            for (int i = 0; i < (op.StartsWith("moments") ? 3 : op == "principal" ? 4 : 2); i++)
            {
                native.Unwritten = i; Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
            }
            native.Unwritten = -1; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }
        [TestCase("moments")] [TestCase("momentsTask")]
        public void InPlaceAndReplacedNonzeroBoundArraysAreCopiedIndependently(string op)
        {
            var native = new Native { InPlace = true }; using var wrapper = Wrap(native);
            var inPlace = (PlateCenterMoments)Invoke(wrapper.Output, op); Assert.That(inPlace.Mxy, Is.EqualTo(13));
            var array = Array.CreateInstance(typeof(double), [3], [-7]);
            for (int i = 0; i < 3; i++) array.SetValue(i - 2d, -7 + i);
            native.Moments = array; native.InPlace = false;
            var first = (PlateCenterMoments)Invoke(wrapper.Output, op); var second = (PlateCenterMoments)Invoke(wrapper.Output, op);
            array.SetValue(900d, -7); first.Mx = 77;
            Assert.That(second.Mx, Is.EqualTo(-2)); Assert.That(first, Is.Not.SameAs(second)); Assert.That(second.Mxy, Is.Zero);
        }
        [TestCaseSource(nameof(Operations))]
        public void WrittenZerosAndSignedExtremeFiniteDataAreNotNormalizedOrInferred(string op)
        {
            var native = new Native { Moments = new double[3], Von = [0d, -0d], Principal = [0d, 0d, 0d, 0d] };
            using var wrapper = Wrap(native); Assert.That(Components(Invoke(wrapper.Output, op)), Is.All.EqualTo(0));
            native.Moments = new[] { double.MaxValue, -double.MaxValue, double.Epsilon };
            native.Von = [-double.MaxValue, double.Epsilon];
            native.Principal = [-double.MaxValue, double.MaxValue, -double.Epsilon, double.Epsilon];
            Assert.DoesNotThrow(() => Invoke(wrapper.Output, op)); // No principal reordering or utilization inference.
        }
        [TestCaseSource(nameof(Operations))]
        public void NullAndNonpositiveMetadataFailBeforeDispatch(string op)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<ArgumentNullException>(() => Invoke(wrapper.Output, op, null, new Plate { Id = 17 }, false)).ParamName, Is.EqualTo("loadCase"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => Invoke(wrapper.Output, op, new LoadCase(7, "Case"), null, false)).ParamName, Is.EqualTo("plate"));
            foreach (int id in new[] { 0, -1, int.MinValue })
            {
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Invoke(wrapper.Output, op, new LoadCase(id, ""))).ParamName, Is.EqualTo("loadCase"));
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Invoke(wrapper.Output, op, null, new Plate { Id = id })).ParamName, Is.EqualTo("plate"));
            }
            Assert.That(native.Calls, Is.Empty);
        }
        private sealed class CapturedCase : ILoadCase
        {
            internal int Reads;
            public int Id { get { Reads++; return Reads == 1 ? 7 : 999; } }
            public string Title => throw new ApplicationException("Title must not be read.");
            public LoadType Type { get => throw new ApplicationException(); set => throw new ApplicationException(); }
            public LoadCaseType CaseType { get => throw new ApplicationException(); set => throw new ApplicationException(); }
        }
        [TestCaseSource(nameof(Operations))]
        public void IdsAreCapturedOnceAndContextReferencesRemainEditableWithoutRequery(string op)
        {
            var native = new Native(); using var wrapper = Wrap(native); var lc = new CapturedCase(); var plate = new Plate { Id = 17 };
            native.Callback = () => plate.Id = 99;
            var result = (PlateStress)Invoke(wrapper.Output, op, lc, plate);
            Assert.That(lc.Reads, Is.EqualTo(1)); Assert.That(native.Calls.Single(), Does.EndWith(":17:7"));
            Assert.That(result.LoadCase, Is.SameAs(lc)); Assert.That(result.Plate.Id, Is.EqualTo(99));
            Assert.That(Components(result)[0], Is.Not.EqualTo(0)); Assert.That(native.Calls, Has.Count.EqualTo(1));
        }
        [TestCase("momentsTask")] [TestCase("vonTask")] [TestCase("principalTask")]
        public void TasksPerformNativeWorkBeforeReturnAndCaptureOriginalFaults(string op)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            Task Start() => op == "momentsTask" ? wrapper.Output.GetPlateCenterMomentsAsync(new LoadCase(7, ""), new Plate { Id = 17 })
                : op == "vonTask" ? wrapper.Output.GetPlateCenterVonMisesAsync(new LoadCase(7, ""), new Plate { Id = 17 })
                : wrapper.Output.GetPlatePrincipalStressesAsync(new LoadCase(7, ""), new Plate { Id = 17 });
            Task success = Start(); Assert.That(success.IsCompletedSuccessfully, Is.True); Assert.That(native.Calls, Has.Count.EqualTo(1));
            var failure = new COMException("native", unchecked((int)0x80004005)); native.Failure = failure;
            Task fault = null; Assert.DoesNotThrow(() => fault = Start()); Assert.That(fault.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => fault.GetAwaiter().GetResult()), Is.SameAs(failure));
        }
        [TestCaseSource(nameof(Operations))]
        public void OriginalFailuresAndMissingNativeSignaturesArePreserved(string op)
        {
            var failure = new COMException("native", unchecked((int)0x80004005)); var native = new Native { Failure = failure }; using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => Invoke(wrapper.Output, op)), Is.SameAs(failure));
            native.Failure = null; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
            using var missing = Wrap(new object()); Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Output, op));
        }
        [TestCaseSource(nameof(Operations))]
        public void CurrentBindingLifetimeAndOwningThreadGuardsPrecedeNativeAccess(string op)
        {
            var native = new Native(); var wrapper = Wrap(native); var old = wrapper.Output;
            Assert.Throws<InvalidOperationException>(() => Invoke(new OSOutputAdapter(wrapper), op));
            wrapper.RefreshComObjects(); Assert.Throws<InvalidOperationException>(() => Invoke(old, op));
            var error = Task.Run(() => { try { Invoke(wrapper.Output, op); return null; } catch (Exception e) { return e; } }).GetAwaiter().GetResult();
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            var current = wrapper.Output; wrapper.Dispose(); Assert.Throws<ObjectDisposedException>(() => Invoke(current, op)); Assert.That(native.Calls, Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void SharedLeaseRejectsCrossDomainReentryAndRecoversAfterCallbackFailures(string op)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            native.Callback = () =>
            {
                Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetApplicationVersion());
                Assert.Throws<InvalidOperationException>(() => wrapper.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
            native.Callback = () => throw new ApplicationException("callback"); Assert.Throws<ApplicationException>(() => Invoke(wrapper.Output, op));
            native.Callback = null; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }
        [Test]
        public void ScalarModelConstructorsRemainUnvalidatedAndVonMaximumTracksCurrentValues()
        {
            var lc = new LoadCase(1, "Case"); var plate = new Plate { Id = 17 };
            var moments = new PlateCenterMoments(lc, plate, -1, 2, 3);
            Assert.That(Components(moments), Is.EqualTo(new[] { -1d, 2d, 3d })); Assert.That(moments.LoadCase, Is.SameAs(lc));
            moments.Mx = double.NaN; Assert.That(double.IsNaN(moments.Mx), Is.True);
            var von = new PlateCenterVonMises(lc, plate, -9, 4); Assert.That(von.AbsoluteMaximum, Is.EqualTo(9));
            von.Bottom = -12; Assert.That(von.AbsoluteMaximum, Is.EqualTo(12)); von.Top = double.NaN; Assert.That(double.IsNaN(von.AbsoluteMaximum), Is.True);
            Assert.That(new PlateCenterMoments(null, null).Plate, Is.Null);
            Assert.That(new PlateCenterVonMises(null, null).LoadCase, Is.Null);
            var principal = new PlatePrincipalStresses(null, null) { SmaxTop = -4, SminTop = 7, SmaxBottom = double.PositiveInfinity, SminBottom = double.NaN };
            Assert.That(principal.SmaxTop, Is.EqualTo(-4)); Assert.That(double.IsNaN(principal.SminBottom), Is.True);
        }
        [Test]
        public void FiveExactSourceSignaturesHaveMatchingExpandedInterfaceAdapterXml()
        {
            var xml = XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location, ".xml"));
            foreach (string name in new[] { "GetPlateCenterMoments", "GetPlateCenterMomentsAsync", "GetPlateCenterVonMises", "GetPlateCenterVonMisesAsync", "GetPlatePrincipalStresses" })
            {
                var method = typeof(IOSOutput).GetMethod(name); var parameters = method.GetParameters();
                Assert.That(parameters.Select(p => p.Name), Is.EqualTo(new[] { "loadCase", "plate" }));
                Assert.That(parameters.Select(p => p.ParameterType), Is.EqualTo(new[] { typeof(ILoadCase), typeof(Plate) }));
                Assert.That(parameters.Any(p => p.IsOptional), Is.False);
                Type model = name.Contains("Moments") ? typeof(PlateCenterMoments) : name.Contains("VonMises") ? typeof(PlateCenterVonMises) : typeof(PlatePrincipalStresses);
                Assert.That(method.ReturnType, Is.EqualTo(name.EndsWith("Async") ? typeof(Task<>).MakeGenericType(model) : model));
                string suffix = "." + name + "(" + string.Join(",", parameters.Select(p => p.ParameterType.FullName)) + ")";
                var contract = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(IOSOutput).FullName + suffix);
                var adapter = xml.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(OSOutputAdapter).FullName + suffix);
                Assert.That(string.Concat(contract.Elements()), Is.EqualTo(string.Concat(adapter.Elements())));
                foreach (string tag in new[] { "summary", "param", "returns", "remarks", "exception", "example" }) Assert.That(contract.Elements(tag), Is.Not.Empty);
                Assert.That(contract.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
            }
        }
    }
}

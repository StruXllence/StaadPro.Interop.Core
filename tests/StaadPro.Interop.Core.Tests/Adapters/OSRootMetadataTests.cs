using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services;
using StaadPro.Interop.Core.Services.Wrapper;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;
using CallbackRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperLifetimeTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootMetadataTests
    {
        private static readonly string[] Operations = {
            "GetApplicationVersion", "GetBaseUnit", "GetInputUnitForForce", "GetInputUnitForLength", "GetMainWindowHandle"
        };

        private static readonly TestCaseData[] RefreshReads = Operations.SelectMany(operation => new[] {
            new TestCaseData(operation, false), new TestCaseData(operation, true)
        }).ToArray();

        private static object Read(IOSRoot root, string operation) => operation switch
        {
            "GetApplicationVersion" => root.GetApplicationVersion(),
            "GetBaseUnit" => root.GetBaseUnit(),
            "GetInputUnitForForce" => root.GetInputUnitForForce(),
            "GetInputUnitForLength" => root.GetInputUnitForLength(),
            "GetMainWindowHandle" => root.GetMainWindowHandle(),
            _ => throw new ArgumentException(nameof(operation))
        };

        private sealed class WindowService : IStaadModelServices
        {
            internal int Calls;
            public IntPtr GetStaadApplicationWindowHandle() { Calls++; return new IntPtr(987); }
        }

        [Test]
        public void MetadataReadsUseNativeSnapshotsWithoutAdditionalDispatch()
        {
            var native = new NativeRoot();
            using var wrapper = new OpenStaadWrapper(native);
            int calls = native.Queries.Count;
            native.VersionText = "27.01.02.003"; native.BaseUnit = 1;
            native.ForceText = "kip"; native.LengthText = "ft"; native.Handle = 999;
            var root = wrapper.Root;
            Assert.That(root.GetApplicationVersion().Display, Is.EqualTo("v26.00.00.340"));
            Assert.That(root.GetBaseUnit(), Is.EqualTo((BaseUnitSystem)2));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloNewton));
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Meter));
            Assert.That(root.GetMainWindowHandle(), Is.EqualTo(new IntPtr(12345)));
            Assert.That(native.Queries.Count, Is.EqualTo(calls), "Cached reads must not dispatch COM or implicitly refresh.");
        }

        [Test]
        public void NativeVersionIsAnIndependentCopyAndNeverTheHostVersion()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { VersionText = "26.01.02.003" });
            var first = wrapper.Root.GetApplicationVersion();
            var second = wrapper.Root.GetApplicationVersion();
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Display, Is.EqualTo("v26.01.02.003"));
            first.Major = 999; first.Update = 888; first.Revision = 777; first.Build = 666;
            Assert.That(second.Display, Is.EqualTo("v26.01.02.003"));
            Assert.That(wrapper.StaadVersion.Display, Is.EqualTo("v26.01.02.003"));
            Assert.That(wrapper.Root.GetApplicationVersion().Display, Is.EqualTo("v26.01.02.003"));
            Assert.That(wrapper.Root.GetApplicationVersion(), Is.Not.SameAs(wrapper.ApplicationVersion));
        }

        [Test]
        public void RefreshUnitsUpdatesUnitObservationsWithoutReplacingRootOrWindowAndVersion()
        {
            var native = new NativeRoot();
            using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root;
            native.BaseUnit = 1; native.ForceText = "kip"; native.LengthText = "ft";
            native.VersionText = "27.00.00.001"; native.Handle = 999;
            wrapper.RefreshUnits();
            Assert.That(wrapper.Root, Is.SameAs(root));
            Assert.That(root.GetBaseUnit(), Is.EqualTo(BaseUnitSystem.Imperial));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloPound));
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Feet));
            Assert.That(root.GetApplicationVersion().Major, Is.EqualTo(26));
            Assert.That(root.GetMainWindowHandle(), Is.EqualTo(new IntPtr(12345)));
        }

        [Test]
        public void RefreshComObjectsReplacesRootAndUpdatesAllMetadata()
        {
            var native = new NativeRoot();
            using var wrapper = new OpenStaadWrapper(native);
            var oldRoot = wrapper.Root;
            native.BaseUnit = 1; native.ForceText = "kip"; native.LengthText = "ft";
            native.VersionText = "27.01.02.003"; native.Handle = 999;
            wrapper.RefreshComObjects();
            var root = wrapper.Root;
            Assert.That(root, Is.Not.SameAs(oldRoot));
            Assert.That(root.GetApplicationVersion().Display, Is.EqualTo("v27.01.02.003"));
            Assert.That(root.GetBaseUnit(), Is.EqualTo(BaseUnitSystem.Imperial));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloPound));
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Feet));
            Assert.That(root.GetMainWindowHandle(), Is.EqualTo(new IntPtr(999)));
        }

        [TestCaseSource(nameof(Operations))]
        public void DisposedRootCannotExposeRetainedMetadata(string operation)
        {
            var native = new NativeRoot(); var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            wrapper.Dispose(); int calls = native.Queries.Count;
            Assert.Throws<ObjectDisposedException>(() => Read(root, operation));
            Assert.That(native.Queries.Count, Is.EqualTo(calls));
        }

        [TestCaseSource(nameof(Operations))]
        public void ReplacedRootIsRejectedEvenThoughTheSameNativeRootRemains(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            wrapper.RefreshComObjects(); int calls = native.Queries.Count;
            Assert.Throws<InvalidOperationException>(() => Read(root, operation));
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
            Assert.That(native.Queries.Count, Is.EqualTo(calls));
        }

        [TestCaseSource(nameof(Operations))]
        public void AnotherThreadCannotObserveWrapperMetadata(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            int calls = native.Queries.Count; Exception observed = null;
            var worker = new Thread(() => { try { Read(root, operation); } catch (Exception error) { observed = error; } });
            worker.Start(); Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(observed, Is.TypeOf<InvalidOperationException>());
            Assert.That(native.Queries.Count, Is.EqualTo(calls));
            Assert.DoesNotThrow(() => Read(root, operation), "Rejected worker must not retain the owner's operation lease.");
        }

        [TestCaseSource(nameof(RefreshReads))]
        public void NativeCallbackCannotReadDuringRefreshAndFailedRefreshReleasesGuard(string operation, bool rebind)
        {
            var native = new CallbackRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.BeforeBaseUnit = () => Read(wrapper.Root, operation);
            if (rebind) Assert.Throws<InvalidOperationException>(wrapper.RefreshComObjects);
            else Assert.Throws<InvalidOperationException>(wrapper.RefreshUnits);
            native.BeforeBaseUnit = null;
            Assert.DoesNotThrow(wrapper.RefreshUnits);
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCase(1, ForceInputUnit.KiloPound, LengthInputUnit.Feet)]
        [TestCase(2, ForceInputUnit.KiloNewton, LengthInputUnit.Meter)]
        [TestCase(0, ForceInputUnit.KiloNewton, LengthInputUnit.Meter)]
        public void UnparseableUnitsExposeTheDocumentedCompatibilityFallback(int baseUnit, ForceInputUnit force, LengthInputUnit length)
        {
            var native = new NativeRoot { BaseUnit = baseUnit, ForceText = "unavailable", LengthText = "unavailable", ForceReturn = true, LengthReturn = false };
            using var wrapper = new OpenStaadWrapper(native);
            Assert.That(wrapper.Root.GetInputUnitForForce(), Is.EqualTo(force));
            Assert.That(wrapper.Root.GetInputUnitForLength(), Is.EqualTo(length));
        }

        [Test]
        public void Version24NumericSuccessFlagsDoNotBecomeUnitCodes()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { VersionText = "24.00.00.001", BaseUnit = 2, ForceText = "", LengthText = "", ForceReturn = 1, LengthReturn = 1 });
            Assert.That(wrapper.Root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloNewton));
            Assert.That(wrapper.Root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Meter));
        }

        [Test]
        public void ReferenceOutputPrecedesReturnCodesAndUnknownBaseCodeIsPreserved()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { BaseUnit = 987, ForceText = "kN", LengthText = "m", ForceReturn = 1, LengthReturn = 1 });
            Assert.That((int)wrapper.Root.GetBaseUnit(), Is.EqualTo(987));
            Assert.That(wrapper.Root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloNewton));
            Assert.That(wrapper.Root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Meter));
        }

        [Test]
        public void MissingOptionalMetadataRemainsUnavailableRatherThanInventingNativeEvidence()
        {
            using var wrapper = new OpenStaadWrapper(new object());
            Assert.That(wrapper.Root.GetApplicationVersion(), Is.Null);
            Assert.That(wrapper.Root.GetBaseUnit(), Is.EqualTo(BaseUnitSystem.Unknown));
            Assert.That(wrapper.Root.GetMainWindowHandle(), Is.EqualTo(IntPtr.Zero));
            Assert.That(wrapper.Root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloNewton));
            Assert.That(wrapper.Root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Meter));
        }

        [TestCase("20.07.11.033")]
        [TestCase("26.00.00.340")]
        public void WindowObservationPreservesEditionGateAndInjectedService(string version)
        {
            var service = new WindowService(); var native = new NativeRoot { VersionText = version };
            using var wrapper = new OpenStaadWrapper(native, false, service, StaadInteropHostServices.Interaction);
            bool connect = version.StartsWith("26", StringComparison.Ordinal);
            int calls = service.Calls;
            Assert.That(wrapper.Root.GetMainWindowHandle(), Is.EqualTo(connect ? new IntPtr(987) : IntPtr.Zero));
            Assert.That(calls, Is.EqualTo(connect ? 1 : 0));
            Assert.That(service.Calls, Is.EqualTo(calls), "Observation must not call the supplied window service again.");
        }

        [Test]
        public void ManuallyConstructedNoncurrentAdapterDoesNotImpersonateWrapperRoot()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var separate = new OSRootAdapter(wrapper);
            Assert.Throws<InvalidOperationException>(() => separate.GetBaseUnit());
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void CompiledInterfaceAndConcreteIntelliSenseIsCompleteAndIdentical(string operation)
        {
            string xmlPath = Path.ChangeExtension(typeof(OpenStaadWrapper).Assembly.Location, ".xml");
            var members = XDocument.Load(xmlPath).Descendants("member").ToArray();
            var contract = members.Single(m => (string)m.Attribute("name") == "M:StaadPro.Interop.Core.Adapters.Interfaces.IOSRoot." + operation);
            var implementation = members.Single(m => (string)m.Attribute("name") == "M:StaadPro.Interop.Core.Adapters.Models.OSRootAdapter." + operation);
            foreach (string element in new[] { "summary", "returns", "remarks", "exception", "example" })
                Assert.That(contract.Element(element), Is.Not.Null, operation + ": " + element);
            Assert.That(contract.Elements().Select(e => e.ToString()), Is.EqualTo(implementation.Elements().Select(e => e.ToString())));
            Assert.That(contract.Descendants("include"), Is.Empty);
            Assert.That(contract.Descendants("inheritdoc"), Is.Empty);
        }
    }
}

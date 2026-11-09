using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Interfaces;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services;
using StaadPro.Interop.Core.Services.Acquisition;
using FakeEnvironment = StaadPro.Interop.Core.Tests.Services.OpenStaadSessionProviderTests.FakeEnvironment;
using RootCom = StaadPro.Interop.Core.Tests.Services.OpenStaadSessionProviderTests.RootCom;
namespace StaadPro.Interop.Core.Tests.Services
{
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class OpenStaadProviderFoundationTests
    {
        private static OpenStaadAcquisitionOptions Options(string executable = @"C:\STAAD\Bentley.Staad.exe") => new OpenStaadAcquisitionOptions { StaadExecutablePath = executable, Timeout = TimeSpan.FromSeconds(2), PollInterval = TimeSpan.FromMilliseconds(10), DiscoveryGracePeriod = TimeSpan.Zero, DiscoveryPolicy = OpenStaadDiscoveryPolicy.RequireCompleteDiscovery };
        [Test]
        public void ExistingModelPreventsTemporaryPathGenerationOrStartup()
        {
            var environment = new FakeEnvironment(); var native = new RootCom(1, @"C:\Models\Existing.std"); environment.Add(native);
            var result = OpenStaadWrapperProvider.AcquireRequest(null, Options(), o => new OpenStaadSessionProvider(o, environment), () => throw new AssertionException("A path must not be generated."), default);
            using (result.Wrapper) { Assert.That(result.Outcome, Is.EqualTo(OpenStaadAcquisitionOutcome.ExistingSession)); Assert.That(result.Wrapper.RawOpenStaad, Is.SameAs(native)); }
            Assert.That(environment.Launches, Is.Zero);
        }
        [Test]
        public void NoRunningApplicationUsesOneVerifiedTemporaryTargetStartup()
        {
            var initial = new FakeEnvironment(); var fallback = new FakeEnvironment(); int created = 0;
            var options = Options(); var originalBudget = options.Timeout;
            var result = OpenStaadWrapperProvider.AcquireRequest(null, options, o => new OpenStaadSessionProvider(o, created++ == 0 ? initial : fallback), () => @"C:\Models\Generated.std", default);
            using (result.Wrapper) { Assert.That(result.Outcome, Is.EqualTo(OpenStaadAcquisitionOutcome.OpenedTarget)); Assert.That(result.ActualPath, Is.EqualTo(@"C:\Models\Generated.std")); }
            Assert.That(created, Is.EqualTo(2)); Assert.That(initial.Launches, Is.Zero); Assert.That(fallback.Launches, Is.EqualTo(1)); Assert.That(options.Timeout, Is.EqualTo(originalBudget)); Assert.That(initial.Disposed && fallback.Disposed, Is.True);
        }
        [Test]
        public void MissingLauncherDoesNotCreateAFileOrGenerateATarget()
        {
            var environment = new FakeEnvironment();
            var result = OpenStaadWrapperProvider.AcquireRequest(null, Options(null), o => new OpenStaadSessionProvider(o, environment), () => throw new AssertionException("No target should be generated."), default);
            Assert.That(result.Failure, Is.EqualTo(OpenStaadAcquisitionFailure.LaunchFailed)); Assert.That(environment.Prepares, Is.Zero);
        }
        [Test]
        public void BusyExistingApplicationDoesNotTriggerNewSessionFallback()
        {
            var environment = new FakeEnvironment(); environment.Add(new RootCom(1, @"C:\Models\Busy.std") { BrokenGeometry = true }); int created = 0;
            var result = OpenStaadWrapperProvider.AcquireRequest(null, Options(), o => { created++; return new OpenStaadSessionProvider(o, environment); }, () => throw new AssertionException("A busy application must not be replaced."), default);
            Assert.That(result.Succeeded, Is.False); Assert.That(created, Is.EqualTo(1)); Assert.That(environment.Launches, Is.Zero);
        }
        [Test]
        public void ExplicitTargetNeverUsesTheUnnamedFallbackPath()
        {
            var environment = new FakeEnvironment();
            var result = OpenStaadWrapperProvider.AcquireRequest(@"C:\Models\Target.std", Options(), o => new OpenStaadSessionProvider(o, environment), () => throw new AssertionException("Unexpected path generation."), default);
            using (result.Wrapper) Assert.That(result.ActualPath, Is.EqualTo(@"C:\Models\Target.std")); Assert.That(environment.Launches, Is.EqualTo(1));
        }
        private sealed class EmptyResolver : IOpenStaadWrapperResolver { public OpenStaadWrapper Get(string fileFullPath = null) => null; }
        [Test]
        public void DirectGetDoesNotReturnASilentNullFromCustomResolver()
        {
            var previous = OpenStaadWrapperProvider.Resolver;
            try { OpenStaadWrapperProvider.Resolver = new EmptyResolver(); var error = Assert.Throws<OpenStaadAcquisitionException>(() => OpenStaadWrapperProvider.Get()); Assert.That(error.Result.Failure, Is.EqualTo(OpenStaadAcquisitionFailure.DiscoveryFailed)); }
            finally { OpenStaadWrapperProvider.Resolver = previous; }
        }
        [Test]
        public void AcquisitionExceptionRetainsTheFailurePacketAndRejectsNull()
        {
            var result = OpenStaadWrapperProvider.Failure(OpenStaadAcquisitionFailure.Cancelled, "Cancelled before startup.");
            var error = new OpenStaadAcquisitionException(result); Assert.That(error.Result, Is.SameAs(result)); Assert.That(error.Message, Does.Contain("Cancelled before startup")); Assert.Throws<ArgumentNullException>(() => new OpenStaadAcquisitionException(null));
        }
        [Test]
        public void InstallationLookupPrefersNewestConventionalVersion()
        {
            string programFolder = @"C:\Programs";
            string executable = OpenStaadInstallationLocator.FindConventional(new[] { programFolder }, p => p.Contains("2025") || p.Contains("2024"));
            Assert.That(executable, Does.Contain("STAAD.Pro 2025")); Assert.That(Path.GetFileName(executable), Is.EqualTo("Bentley.Staad.exe"));
            Assert.That(OpenStaadInstallationLocator.FindConventional(new[] { programFolder }, _ => false), Is.Null);
        }
        [Test]
        public void OnlyRootExposesTheThirtyFiveDocumentedOperations()
        {
            var interfaces = typeof(IOSBase).Assembly.GetTypes().Where(t => t.IsInterface && t.Namespace == typeof(IOSBase).Namespace).ToArray();
            Assert.That(interfaces.Length, Is.EqualTo(12));
            foreach (Type contract in interfaces)
            {
                var operations = contract.GetMethods().Where(m => !m.IsSpecialName).Select(m => m.Name).ToArray();
                if (contract == typeof(IOSRoot))
                    Assert.That(operations, Is.EquivalentTo(new[] { "GetApplicationVersion", "GetBaseUnit", "GetInputUnitForForce", "GetInputUnitForLength", "GetMainWindowHandle",
                        "GetStaadFileFullPath", "GetStaadFileFolder", "IsPhysicalModel", "GetErrorMessage", "GetProcessId",
                        "GetStaadFileNameOnly", "GetAnlFileFullPath", "GetOpenedStaadFilename", "IsTarget", "ModelIsOpen", "SetSilentMode", "SaveModel", "OpenSTAADFile", "CloseSTAADFile", "ShowApplication", "GetShortJobInfo", "GetFullJobInfo", "MinimizeStaadMainWindow", "MaximizeStaadMainWindow", "RestoreStaadMainWindow", "SetInputUnitsEx", "AwaitModelHandle", "OpenStaadModel", "CloseCurrentStaadModel", "CloseWriteReopenStaadFile", "AnalyzeEx", "Analyze", "IsAnalyzing", "GetAnalysisStatus", "GetAnalysisStatus" }));
                else Assert.That(operations, Is.Empty, contract.Name);
            }
        }
        [Test]
        public void FoundationHasNoWpfFrameworkOrThirdPartyRuntimeReference()
        {
            var references = typeof(OpenStaadWrapper).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.That(references, Does.Not.Contain("Newtonsoft.Json")); Assert.That(references, Does.Not.Contain("WindowsBase")); Assert.That(references, Does.Not.Contain("PresentationCore"));
        }
    }
}

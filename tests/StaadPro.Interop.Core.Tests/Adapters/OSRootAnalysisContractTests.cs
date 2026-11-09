using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Root;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootAnalysisContractTests
    {
        private static readonly string[] Operations = ["extended", "analyze", "running", "explicit", "current"];
        public sealed class NativeRoot : MetadataRoot
        {
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public Action<string> OnCall;
            public Exception Failure;
            public object ExtendedStatus = 2, Status = 2, Running = false, FileName = @"C:\Models\Frame.std";
            public string RequestedPath;
            public bool WriteCounts = true;
            private void Step(string operation)
            {
                Calls.Add(operation); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(operation);
                if (Failure != null) throw Failure;
            }
            public void Analyze() => Step("analyze");
            public object AnalyzeEx(int silent, int hidden, int wait) { Step("extended"); return ExtendedStatus; }
            public object IsAnalyzing() { Step("running"); return Running; }
            public void GetSTAADFile(ref object output, object full)
            {
                Assert.That(full, Is.True); Assert.That(output, Is.TypeOf<string>());
                Step("filename"); output = FileName;
            }
            public object GetAnalysisStatus(string path, ref int warnings, ref int errors, ref double seconds)
            {
                Assert.That(warnings, Is.EqualTo(int.MinValue)); Assert.That(errors, Is.EqualTo(int.MinValue));
                Assert.That(double.IsNaN(seconds), Is.True);
                Step("status"); RequestedPath = path;
                if (WriteCounts) { warnings = 1; errors = 0; seconds = .25; }
                return Status;
            }
        }

        private static object Request(IOSRoot root, string operation) => operation switch
        {
            "extended" => root.AnalyzeEx(SilentMode.Enable, HiddenMode.Enable, WaitAnalysisMode.Return),
            "analyze" => Analyze(root),
            "running" => root.IsAnalyzing(),
            "explicit" => root.GetAnalysisStatus(@"C:\Models\Other.std"),
            "current" => root.GetAnalysisStatus(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        private static object Analyze(IOSRoot root) { root.Analyze(); return null; }

        [TestCaseSource(nameof(Operations))]
        public void OperationsDispatchOnlyTheirRequiredMembersOnTheConstructingThread(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; native.Queries.Clear(); object geometry = wrapper.RawGeometry;
            Request(root, operation);
            Assert.That(native.Calls, Is.EqualTo(operation == "current" ? new[] { "filename", "status" }
                : new[] { operation == "explicit" ? "status" : operation }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(native.Queries, Is.Empty);
            Assert.That(wrapper.Root, Is.SameAs(root)); Assert.That((object)wrapper.RawGeometry, Is.SameAs(geometry));
        }

        [TestCaseSource(nameof(Operations))]
        public void NoncurrentAndDisposedBindingsRejectBeforeNativeAccess(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var manual = new OSRootAdapter(wrapper); var previous = wrapper.Root;
            Assert.Throws<InvalidOperationException>(() => Request(manual, operation));
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Request(previous, operation));
            var root = wrapper.Root; wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Request(root, operation));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void OtherThreadCallsRejectWithoutDispatch(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Exception error = null;
            var worker = new Thread(() => { try { Request(wrapper.Root, operation); } catch (Exception e) { error = e; } });
            worker.Start(); Assert.That(worker.Join(5000), Is.True);
            Assert.That(error, Is.TypeOf<InvalidOperationException>()); Assert.That(native.Calls, Is.Empty);
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void OneLeaseCoversEveryNativeCallbackWithoutNestedRootOrLifecycleAccess(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            int callbacks = 0;
            native.OnCall = _ =>
            {
                callbacks++;
                foreach (string nested in Operations) Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, nested));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Request(wrapper.Root, operation);
            Assert.That(callbacks, Is.EqualTo(operation == "current" ? 2 : 1));
            native.OnCall = null; Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeErrorsPropagateWithoutRetryOrStrandingTheLease(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var failure = new COMException("native request may have started", unchecked((int)0x80010108)); native.Failure = failure;
            Assert.That(Assert.Throws<COMException>(() => Request(wrapper.Root, operation)), Is.SameAs(failure));
            Assert.That(native.Calls.Count, Is.EqualTo(1)); native.Failure = null;
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [Test]
        public void CurrentStatusFailureAfterFilenameDoesNotRepeatEitherQuery()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var failure = new COMException("status failed");
            native.OnCall = stage => { if (stage == "status") native.Failure = failure; };
            Assert.That(Assert.Throws<COMException>(() => wrapper.Root.GetAnalysisStatus()), Is.SameAs(failure));
            Assert.That(native.Calls, Is.EqualTo(new[] { "filename", "status" }));
            native.Failure = null; native.OnCall = null;
            Assert.DoesNotThrow(() => wrapper.Root.GetAnalysisStatus());
        }

        [Test]
        public void CurrentModelPathUsesTheDirectoryCapturedBeforeNativeDispatch()
        {
            string directory = Environment.CurrentDirectory;
            var native = new NativeRoot { FileName = @"sub\..\Frame.std" }; using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = stage => { if (stage == "filename") Environment.CurrentDirectory = Path.GetTempPath(); };
            try
            {
                var result = wrapper.Root.GetAnalysisStatus();
                Assert.That(result.ModelFilePath, Is.EqualTo(Path.GetFullPath(@"sub\..\Frame.std", directory)));
                Assert.That(native.RequestedPath, Is.EqualTo(result.ModelFilePath));
            }
            finally { Environment.CurrentDirectory = directory; }
        }

        [Test]
        public void CurrentModelStatusReportsTheCapturedModelEvenIfTheSessionChangesAfterTheRead()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = stage => { if (stage == "status") native.FileName = @"D:\Other.std"; };
            var status = wrapper.Root.GetAnalysisStatus();
            Assert.That(status.ModelFilePath, Is.EqualTo(@"C:\Models\Frame.std"));
            Assert.That(native.FileName, Is.EqualTo(@"D:\Other.std"));
            Assert.That(native.Calls, Is.EqualTo(new[] { "filename", "status" }));
        }

        [TestCase("extended")]
        [TestCase("running")]
        [TestCase("explicit")]
        public void InvalidResponsesReleaseTheLeaseWithoutHidingOrRetryingTheFailure(string operation)
        {
            var native = new NativeRoot { ExtendedStatus = true, Running = 2, Status = true }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, operation));
            Assert.That(native.Calls.Count, Is.EqualTo(1)); Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCase("extended")]
        [TestCase("analyze")]
        public void AnalysisRequestsPreserveCachedBindingsUntilExplicitRefresh(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; object geometry = wrapper.RawGeometry; var replacement = new object();
            native.OnCall = _ => { native.Geometry = replacement; native.ForceText = "kip"; };
            Request(root, operation);
            Assert.That((object)wrapper.RawGeometry, Is.SameAs(geometry));
            Assert.That(wrapper.InputForceUnit, Is.EqualTo(ForceInputUnit.KiloNewton));
            wrapper.RefreshComObjects();
            Assert.That((object)wrapper.RawGeometry, Is.SameAs(replacement));
            Assert.That(wrapper.InputForceUnit, Is.EqualTo(ForceInputUnit.KiloPound));
            Assert.Throws<InvalidOperationException>(() => root.IsAnalyzing());
        }

        private static IEnumerable<TestCaseData> InvalidCompletionOutputs()
        {
            foreach (int code in new[] { 2, 3, 4 })
                foreach (string field in new[] { "warnings", "errors", "cpu" }) yield return new TestCaseData(code, field);
        }

        [TestCaseSource(nameof(InvalidCompletionOutputs))]
        public void CompletedStatusRequiresEachIndividualOutputToBeWritten(int code, string field)
        {
            var native = new OSRootAnalysisQueryTests.NativeRoot { Status = code };
            if (field == "warnings") native.Warnings = null;
            if (field == "errors") native.Errors = null;
            if (field == "cpu") native.CpuTime = null;
            using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetAnalysisStatus("Frame.std"));
            Assert.That(native.Calls, Is.EqualTo(new[] { "status" }));
        }

        [TestCaseSource(nameof(Operations))]
        public void ExactPublicSignaturesAndFullyExpandedDocumentationMatch(string operation)
        {
            string name = operation switch { "extended" => "AnalyzeEx", "analyze" => "Analyze", "running" => "IsAnalyzing", _ => "GetAnalysisStatus" };
            Type[] types = operation == "extended" ? [typeof(SilentMode), typeof(HiddenMode), typeof(WaitAnalysisMode)] : operation == "explicit" ? [typeof(string)] : [];
            Type returns = operation == "extended" ? typeof(AnalysisResult) : operation == "analyze" ? typeof(void) : operation == "running" ? typeof(bool) : typeof(AnalysisStatusResult);
            string[] names = operation == "extended" ? ["silentMode", "hiddenMode", "waitMode"] : operation == "explicit" ? ["modelFilePath"] : [];
            foreach (Type type in new[] { typeof(IOSRoot), typeof(OSRootAdapter) })
            {
                var method = type.GetMethod(name, types);
                Assert.That(method, Is.Not.Null); Assert.That(method.ReturnType, Is.EqualTo(returns));
                Assert.That(method.GetParameters().Select(p => p.Name), Is.EqualTo(names));
                Assert.That(method.GetParameters().Any(p => p.IsOptional || p.IsOut || p.ParameterType.IsByRef), Is.False);
            }
            string suffix = name + (types.Length == 0 ? "" : "(" + string.Join(",", types.Select(t => t.FullName)) + ")");
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location, ".xml"));
            var contract = docs.Descendants("member").Single(m => (string)m.Attribute("name") == "M:" + typeof(IOSRoot).FullName + "." + suffix);
            var implementation = docs.Descendants("member").Single(m => (string)m.Attribute("name") == "M:" + typeof(OSRootAdapter).FullName + "." + suffix);
            Assert.That(contract.Elements().Select(e => e.ToString()), Is.EqualTo(implementation.Elements().Select(e => e.ToString())));
            foreach (string section in new[] { "summary", "remarks", "exception", "example" }) Assert.That(contract.Element(section)?.Value, Is.Not.Null.And.Not.Empty);
            if (returns != typeof(void)) Assert.That(contract.Element("returns")?.Value, Is.Not.Null.And.Not.Empty);
            Assert.That(contract.Elements("param").Select(e => (string)e.Attribute("name")), Is.EqualTo(names));
            Assert.That(contract.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
        }

        [Test]
        public void EveryNewEnumNameAndCodeHasCompiledIntelliSense()
        {
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location, ".xml"));
            foreach (Type type in new[] { typeof(AnalysisResult), typeof(HiddenMode), typeof(WaitAnalysisMode), typeof(AnalysisStatus) })
            {
                Assert.That(docs.Descendants("member").Single(m => (string)m.Attribute("name") == "T:" + type.FullName).Element("summary")?.Value, Is.Not.Null.And.Not.Empty);
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    Assert.That(docs.Descendants("member").Single(m => (string)m.Attribute("name") == "F:" + type.FullName + "." + field.Name).Element("summary")?.Value, Is.Not.Null.And.Not.Empty);
            }
            Assert.That(Enum.GetValues<AnalysisResult>().Select(x => (int)x), Is.EquivalentTo(new[] { -1, 0, 1, 2, 3, 4, 5 }));
            Assert.That(Enum.GetValues<HiddenMode>().Select(x => (int)x), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(Enum.GetValues<WaitAnalysisMode>().Select(x => (int)x), Is.EqualTo(new[] { 0, 1 }));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Root.Common;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootWorkflowContractTests
    {
        private const string Target = @"C:\Models\Frame.std";
        private static readonly string[] Names = ["SetInputUnitsEx", "AwaitModelHandle", "OpenStaadModel", "CloseCurrentStaadModel", "CloseWriteReopenStaadFile"];

        public sealed class NativeRoot : MetadataRoot
        {
            public object PathValue = Target;
            public bool WriteOutput = true;
            public readonly List<string> Events = [];
            public readonly List<int> Threads = [];
            public object[] Units;
            public Action<string> OnCall;
            public Exception Failure;
            private void Step(string name)
            {
                Events.Add(name); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(name);
                if (Failure != null) throw Failure;
            }
            public void GetSTAADFile(ref object value, object full)
            {
                Assert.That(value, Is.TypeOf<string>()); Assert.That(full, Is.True);
                Step("read"); if (WriteOutput) value = PathValue;
            }
            public void SetInputUnits(object length, object force) { Step("units"); Units = [length, force]; }
            public object SetSilentMode(int mode) { Assert.That(mode, Is.EqualTo(1)); Step("silent"); return false; }
            public void CloseSTAADFile() { Step("close"); PathValue = ""; }
            public void OpenSTAADFile(string path) { Step("open"); PathValue = path; }
        }

        private static object Request(IOSRoot root, string name) => name switch
        {
            "SetInputUnitsEx" => root.SetInputUnitsEx(LengthInputUnit.Meter, ForceInputUnit.KiloNewton),
            "AwaitModelHandle" => root.AwaitModelHandle(Target, .01, .001),
            "OpenStaadModel" => root.OpenStaadModel(Target, 1, 1),
            "CloseCurrentStaadModel" => Close(root),
            "CloseWriteReopenStaadFile" => root.CloseWriteReopenStaadFile(Target, ["STAAD SPACE", "FINISH"], 1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        private static object Close(IOSRoot root) { root.CloseCurrentStaadModel(); return null; }

        internal sealed class Files : IRootModelFiles
        {
            public int Calls;
            public void ValidateExistingFile(string path) => Calls++;
            public bool IsLocked(string path) { Calls++; return false; }
            public void WriteAtomically(string path, string[] lines) => Calls++;
        }

        private static IEnumerable<TestCaseData> UnitPairs()
        {
            foreach (var length in Enum.GetValues<LengthInputUnit>())
                foreach (var force in Enum.GetValues<ForceInputUnit>()) yield return new TestCaseData(length, force);
        }

        [TestCaseSource(nameof(UnitPairs))]
        public void UnitCodesAreInt32LengthThenForceAndReturnTheSameBinding(LengthInputUnit length, ForceInputUnit force)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.Queries.Clear();
            Assert.That(wrapper.Root.SetInputUnitsEx(length, force), Is.SameAs(wrapper.Root));
            Assert.That(native.Units, Is.EqualTo(new object[] { (int)length, (int)force }));
            Assert.That(native.Units, Is.All.TypeOf<int>());
            Assert.That(native.Events, Is.EqualTo(new[] { "units" }));
            Assert.That(native.Threads, Is.EqualTo(new[] { Environment.CurrentManagedThreadId }));
            Assert.That(native.Queries, Is.Empty);
        }

        [TestCase(1, 5, "lengthUnit")]
        [TestCase(-1, 5, "lengthUnit")]
        [TestCase(8, 5, "lengthUnit")]
        [TestCase(int.MaxValue, 5, "lengthUnit")]
        [TestCase(4, -1, "forceUnit")]
        [TestCase(4, 8, "forceUnit")]
        [TestCase(4, int.MaxValue, "forceUnit")]
        public void UndefinedUnitsAreRejectedBeforeAnyDispatch(int length, int force, string parameter)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Root.SetInputUnitsEx((LengthInputUnit)length, (ForceInputUnit)force)).ParamName, Is.EqualTo(parameter));
            Assert.That(native.Events, Is.Empty);
        }

        [Test]
        public void SettingUnitsDoesNotSilentlyRefreshSnapshotsOrReplaceAdapters()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; var geometry = wrapper.Geometry;
            native.OnCall = _ => { native.LengthText = "ft"; native.ForceText = "kip"; };
            root.SetInputUnitsEx(LengthInputUnit.Feet, ForceInputUnit.KiloPound);
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Meter));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloNewton));
            wrapper.RefreshUnits();
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Feet));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloPound));
            Assert.That(wrapper.Root, Is.SameAs(root)); Assert.That(wrapper.Geometry, Is.SameAs(geometry));
        }

        [TestCaseSource(nameof(Names))]
        public void StaleDisposedAndWrongThreadBindingsRejectBeforeNativeOrFiles(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = (OSRootAdapter)wrapper.Root; var files = new Files(); root.ModelFiles = files;
            Exception threadError = null;
            var thread = new Thread(() => { try { Request(root, name); } catch (Exception e) { threadError = e; } });
            thread.Start(); Assert.That(thread.Join(5000), Is.True); Assert.That(threadError, Is.TypeOf<InvalidOperationException>());
            Assert.That(native.Events, Is.Empty); Assert.That(files.Calls, Is.Zero);
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Request(root, name));
            Assert.That(native.Events, Is.Empty); Assert.That(files.Calls, Is.Zero);
            var current = (OSRootAdapter)wrapper.Root; current.ModelFiles = files; wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Request(current, name));
            Assert.That(native.Events, Is.Empty); Assert.That(files.Calls, Is.Zero);
        }

        [TestCaseSource(nameof(Names))]
        public void WorkflowHoldsOneLeaseAcrossCallbacksAndReleasesItAfterward(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = (OSRootAdapter)wrapper.Root; root.ModelFiles = new Files(); native.Queries.Clear();
            int callbacks = 0;
            native.OnCall = _ =>
            {
                callbacks++;
                foreach (string nested in Names) Assert.Throws<InvalidOperationException>(() => Request(root, nested));
                Assert.Throws<InvalidOperationException>(() => root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Request(root, name);
            Assert.That(callbacks, Is.GreaterThan(0)); Assert.That(native.Queries, Is.Empty);
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            native.OnCall = null; Assert.DoesNotThrow(() => root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Names))]
        public void OriginalNativeExceptionsPropagateAndDoNotStrandTheLease(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = (OSRootAdapter)wrapper.Root; root.ModelFiles = new Files();
            var error = new COMException("native transition failed", unchecked((int)0x80010108)); native.Failure = error;
            Assert.That(Assert.Throws<COMException>(() => Request(root, name)), Is.SameAs(error));
            Assert.That(native.Events.Count, Is.EqualTo(1)); native.Failure = null;
            Assert.DoesNotThrow(() => root.GetBaseUnit());
        }

        [TestCase("AwaitModelHandle")]
        [TestCase("OpenStaadModel")]
        [TestCase("CloseWriteReopenStaadFile")]
        public void WorkflowFreezesRelativePathBaseEvenWhenNativeChangesTheWorkingDirectory(string name)
        {
            string original = Environment.CurrentDirectory;
            string other = Path.GetTempPath();
            var native = new NativeRoot { PathValue = "Frame.std" };
            using var wrapper = new OpenStaadWrapper(native);
            var files = new Files(); ((OSRootAdapter)wrapper.Root).ModelFiles = files;
            native.OnCall = stage =>
            {
                Environment.CurrentDirectory = other;
                if (stage == "read" && native.PathValue is string text && text.Length > 0) native.PathValue = "Frame.std";
            };
            try
            {
                bool result = name == "AwaitModelHandle" ? wrapper.Root.AwaitModelHandle("Frame.std", 1, .01)
                    : name == "OpenStaadModel" ? wrapper.Root.OpenStaadModel("Frame.std", 1, 1)
                    : wrapper.Root.CloseWriteReopenStaadFile("Frame.std", ["FINISH"], 1, 0);
                Assert.That(result, Is.True);
                Assert.That(files.Calls, Is.EqualTo(name == "CloseWriteReopenStaadFile" ? 3 : 0));
            }
            finally { Environment.CurrentDirectory = original; }
        }

        [TestCase("AwaitModelHandle")]
        [TestCase("OpenStaadModel")]
        [TestCase("CloseCurrentStaadModel")]
        [TestCase("CloseWriteReopenStaadFile")]
        public void UnwrittenFilenameIsAnErrorWithoutFilesystemOrSubsequentMutation(string name)
        {
            var native = new NativeRoot { WriteOutput = false }; using var wrapper = new OpenStaadWrapper(native);
            var files = new Files(); ((OSRootAdapter)wrapper.Root).ModelFiles = files;
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, name));
            Assert.That(native.Events, Is.EqualTo(name == "OpenStaadModel" ? new[] { "open", "read" } : new[] { "read" }));
            Assert.That(files.Calls, Is.Zero);
        }

        private static IEnumerable<TestCaseData> BadPaths()
        {
            foreach (string name in Names.Where(n => n != "SetInputUnitsEx"))
                foreach (object value in new object[] { null, DBNull.Value, 1, true, new object() }) yield return new TestCaseData(name, value);
        }

        [TestCaseSource(nameof(BadPaths))]
        public void MalformedFilenameOutputsThrowInsteadOfReportingTimeoutOrClosing(string name, object value)
        {
            var native = new NativeRoot { PathValue = value }; using var wrapper = new OpenStaadWrapper(native);
            var root = (OSRootAdapter)wrapper.Root; var files = new Files(); root.ModelFiles = files;
            // Open intentionally requests one open first; preserve the malformed native read afterward.
            native.OnCall = stage => { if (stage == "read") native.PathValue = value; };
            Assert.Throws<InvalidOperationException>(() => Request(root, name));
            Assert.That(native.Events, Is.EqualTo(name == "OpenStaadModel" ? new[] { "open", "read" } : new[] { "read" }));
            Assert.That(files.Calls, Is.Zero);
        }

        [TestCaseSource(nameof(Names))]
        public void PublicSignaturesDefaultsAndExpandedXmlMatchOnBothSurfaces(string name)
        {
            var contract = typeof(IOSRoot).GetMethod(name); var implementation = typeof(OSRootAdapter).GetMethod(name);
            Type[] types = name switch
            {
                "SetInputUnitsEx" => [typeof(LengthInputUnit), typeof(ForceInputUnit)],
                "AwaitModelHandle" => [typeof(string), typeof(double), typeof(double)],
                "OpenStaadModel" => [typeof(string), typeof(int), typeof(int)],
                "CloseCurrentStaadModel" => [],
                _ => [typeof(string), typeof(List<string>), typeof(int), typeof(int)]
            };
            string[] names = name switch
            {
                "SetInputUnitsEx" => ["lengthUnit", "forceUnit"],
                "AwaitModelHandle" => ["fileFullPath", "timeoutDurationInSeconds", "sleepIntervalInSeconds"],
                "OpenStaadModel" => ["staadFileFullPath", "sleepInterval", "maxWaitTime"],
                "CloseCurrentStaadModel" => [],
                _ => ["filePath", "fileContents", "sleepTimeMilliseconds", "maxTimeMilliseconds"]
            };
            Type returns = name == "SetInputUnitsEx" ? typeof(IOSRoot) : name == "CloseCurrentStaadModel" ? typeof(void) : typeof(bool);
            foreach (var method in new[] { contract, implementation })
            {
                Assert.That(method.ReturnType, Is.EqualTo(returns));
                Assert.That(method.GetParameters().Select(p => p.ParameterType), Is.EqualTo(types));
                Assert.That(method.GetParameters().Select(p => p.Name), Is.EqualTo(names));
                var defaults = method.GetParameters().Where(p => p.IsOptional).Select(p => p.DefaultValue).ToArray();
                Assert.That(defaults, Is.EqualTo(name == "AwaitModelHandle" ? new object[] { 60d, .1d } : name == "OpenStaadModel" ? new object[] { 600 } : name == "CloseWriteReopenStaadFile" ? new object[] { 200, 5000 } : []));
            }
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location, ".xml"));
            var entries = docs.Descendants("member").Where(e => ((string)e.Attribute("name")).StartsWith("M:") &&
                new[] { typeof(IOSRoot), typeof(OSRootAdapter) }.Any(t => ((string)e.Attribute("name")).StartsWith("M:" + t.FullName + "." + name + "(") || (string)e.Attribute("name") == "M:" + t.FullName + "." + name)).ToArray();
            Assert.That(entries.Length, Is.EqualTo(2));
            Assert.That(entries[0].Elements().Select(e => e.ToString()), Is.EqualTo(entries[1].Elements().Select(e => e.ToString())));
            foreach (var entry in entries)
            {
                foreach (string tag in new[] { "summary", "remarks", "example" }) Assert.That(entry.Element(tag)?.Value, Is.Not.Null.And.Not.Empty);
                Assert.That(entry.Elements("param").Select(e => (string)e.Attribute("name")), Is.EqualTo(names));
                if (returns != typeof(void)) Assert.That(entry.Element("returns")?.Value, Is.Not.Null.And.Not.Empty);
            }
        }
    }
}

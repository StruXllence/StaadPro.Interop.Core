using System;
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
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootLifecycleTests
    {
        private static readonly string[] Names = ["SetSilentMode", "SaveModel", "OpenSTAADFile", "CloseSTAADFile", "ShowApplication"];

        public sealed class NativeRoot : MetadataRoot
        {
            public readonly List<string> Calls = [];
            public readonly List<object> Inputs = [];
            public readonly List<int> Threads = [];
            public object PreviousMode;
            public Exception Failure;
            public Action OnCall;
            private void Record(string name, object input = null)
            {
                Calls.Add(name); Inputs.Add(input); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke();
                if (Failure != null) throw Failure;
            }
            public object SetSilentMode(object mode) { Record("SetSilentMode", mode); return PreviousMode; }
            public void SaveModel(object mode) => Record("SaveModel", mode);
            public void OpenSTAADFile(object path) => Record("OpenSTAADFile", path);
            public void CloseSTAADFile() => Record("CloseSTAADFile");
            public object ShowApplication() { Record("ShowApplication"); return false; }
        }

        private static object Request(IOSRoot root, string name) => name switch
        {
            "SetSilentMode" => root.SetSilentMode(SilentMode.Enable),
            "SaveModel" => root.SaveModel(SilentMode.Enable),
            "OpenSTAADFile" => root.OpenSTAADFile(@"C:\Models\Frame.std"),
            "CloseSTAADFile" => root.CloseSTAADFile(),
            "ShowApplication" => root.ShowApplication(),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };

        [TestCaseSource(nameof(Names))]
        public void EachOperationUsesOneCapturedRootCallAndNoMetadataRefresh(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; native.Queries.Clear();
            object result = Request(root, name);
            Assert.That(result, name == "SetSilentMode" ? Is.EqualTo(true) : Is.SameAs(root));
            Assert.That(native.Calls, Is.EqualTo(new[] { name }));
            Assert.That(native.Threads, Is.EqualTo(new[] { Environment.CurrentManagedThreadId }));
            Assert.That(native.Queries, Is.Empty);
            Assert.That(wrapper.Root, Is.SameAs(root));
        }

        [TestCase(SilentMode.Disable)]
        [TestCase(SilentMode.Enable)]
        public void BothModeOperationsPassExactInt32WithoutChangingEachOthersSetting(SilentMode mode)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.That(wrapper.Root.SetSilentMode(mode), Is.True);
            wrapper.Root.SaveModel(mode);
            Assert.That(native.Calls, Is.EqualTo(new[] { "SetSilentMode", "SaveModel" }));
            Assert.That(native.Inputs, Is.All.TypeOf<int>());
            Assert.That(native.Inputs, Is.EqualTo(new object[] { (int)mode, (int)mode }));
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase("previous setting")]
        public void SetSilentModeDoesNotTreatThePreviousModeAsSuccess(object previous)
        {
            var native = new NativeRoot { PreviousMode = previous }; using var wrapper = new OpenStaadWrapper(native);
            Assert.That(wrapper.Root.SetSilentMode(SilentMode.Enable), Is.True);
            Assert.That(native.Calls.Count, Is.EqualTo(1));
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void InvalidModesThrowWithoutBeingSwallowedOrDispatched(int value)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Root.SetSilentMode((SilentMode)value)).ParamName, Is.EqualTo("silentMode"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Root.SaveModel((SilentMode)value)).ParamName, Is.EqualTo("silentMode"));
            Assert.That(native.Calls, Is.Empty);
            Assert.That(wrapper.Root.SetSilentMode(SilentMode.Disable), Is.True);
        }

        [TestCaseSource(nameof(Names))]
        public void NativeErrorsAreNotRetriedAndTheLeaseIsReleased(string name)
        {
            var error = new COMException("native mutation may have happened", unchecked((int)0x80004005));
            var native = new NativeRoot { Failure = error }; using var wrapper = new OpenStaadWrapper(native);
            if (name == "SetSilentMode") Assert.That(Request(wrapper.Root, name), Is.EqualTo(false));
            else Assert.That(Assert.Throws<COMException>(() => Request(wrapper.Root, name)), Is.SameAs(error));
            Assert.That(native.Calls, Is.EqualTo(new[] { name }));
            native.Failure = null;
            Assert.DoesNotThrow(() => Request(wrapper.Root, name));
            Assert.That(native.Calls, Is.EqualTo(new[] { name, name }));
        }

        [TestCaseSource(nameof(Names))]
        public void MissingMembersPreserveTheOperationSpecificErrorContract(string name)
        {
            using var wrapper = new OpenStaadWrapper(new MetadataRoot());
            if (name == "SetSilentMode") Assert.That(Request(wrapper.Root, name), Is.EqualTo(false));
            else Assert.Throws<RuntimeBinderException>(() => Request(wrapper.Root, name));
            Assert.That(wrapper.Root.GetBaseUnit(), Is.EqualTo(BaseUnitSystem.Metric));
        }

        [TestCaseSource(nameof(Names))]
        public void StaleBindingsAreRejectedBeforeDispatchEvenForSetSilentMode(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var stale = wrapper.Root; wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Request(stale, name));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Names))]
        public void DisposedBindingsAreRejectedBeforeDispatchEvenForSetSilentMode(string name)
        {
            var native = new NativeRoot(); var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Request(root, name));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Names))]
        public void OtherThreadCallsAreRejectedWithoutNativeAccess(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Exception error = null;
            var thread = new Thread(() => { try { Request(wrapper.Root, name); } catch (Exception caught) { error = caught; } });
            thread.Start(); Assert.That(thread.Join(5000), Is.True);
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Names))]
        public void CallbacksCannotReenterRootRefreshOrDisposal(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = () => {
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.SetSilentMode(SilentMode.Enable));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.SaveModel(SilentMode.Enable));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(wrapper.RefreshUnits);
                Assert.Throws<InvalidOperationException>(wrapper.RefreshComObjects);
                Assert.Throws<InvalidOperationException>(wrapper.Dispose);
            };
            Request(wrapper.Root, name);
            Assert.That(native.Calls, Is.EqualTo(new[] { name }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        [TestCase("bad|name.std")]
        [TestCase("bad<name.std")]
        [TestCase("bad>name.std")]
        [TestCase("bad\"name.std")]
        [TestCase("bad\0name.std")]
        public void InvalidOpenPathsFailBeforeDispatchAndKeepTheParameterName(string path)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            ArgumentException error = path == null
                ? Assert.Throws<ArgumentNullException>(() => wrapper.Root.OpenSTAADFile(path))
                : Assert.Throws<ArgumentException>(() => wrapper.Root.OpenSTAADFile(path));
            Assert.That(error.ParamName, Is.EqualTo("filePath"));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase(@"relative\..\Frame.std")]
        [TestCase(@"C:\Models\Frame v2.STD")]
        [TestCase(@"C:\Models\不存在.模型")]
        [TestCase(@"\\server\share\Frame.std")]
        [TestCase(@"C:\Models\not-a-std.txt")]
        public void OpenPathsAreNormalizedOnceWithoutExistenceOrExtensionChecks(string path)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            string expected = Path.GetFullPath(path, Environment.CurrentDirectory);
            wrapper.Root.OpenSTAADFile(path);
            Assert.That(native.Inputs.Single(), Is.TypeOf<string>().And.EqualTo(expected));
        }

        [TestCase("OpenSTAADFile")]
        [TestCase("CloseSTAADFile")]
        public void ModelTransitionsLeaveCachesAndBindingsUntilTheCallerExplicitlyRefreshes(string name)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; object oldGeometry = wrapper.RawGeometry;
            var replacement = new object(); native.Queries.Clear();
            native.OnCall = () => { native.Geometry = replacement; native.ForceText = "kip"; };
            Request(root, name);
            Assert.That((object)wrapper.RawGeometry, Is.SameAs(oldGeometry));
            Assert.That(wrapper.InputForceUnit, Is.EqualTo(ForceInputUnit.KiloNewton));
            Assert.That(native.Queries, Is.Empty);
            wrapper.RefreshComObjects();
            Assert.That((object)wrapper.RawGeometry, Is.SameAs(replacement));
            Assert.That(wrapper.InputForceUnit, Is.EqualTo(ForceInputUnit.KiloPound));
            Assert.Throws<InvalidOperationException>(() => root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Names))]
        public void InterfaceAndAdapterHaveExactSignaturesAndMatchingExpandedDocumentation(string name)
        {
            var method = typeof(IOSRoot).GetMethod(name); var adapter = typeof(OSRootAdapter).GetMethod(name);
            var expected = name == "SetSilentMode" || name == "SaveModel" ? typeof(SilentMode) : name == "OpenSTAADFile" ? typeof(string) : null;
            Assert.That(method.ReturnType, Is.EqualTo(name == "SetSilentMode" ? typeof(bool) : typeof(IOSRoot)));
            Assert.That(adapter.ReturnType, Is.EqualTo(method.ReturnType));
            Assert.That(method.GetParameters().Select(p => p.ParameterType), Is.EqualTo(expected == null ? Type.EmptyTypes : new[] { expected }));
            Assert.That(method.GetParameters().Any(p => p.IsOptional || p.ParameterType.IsByRef), Is.False);
            if (expected != null) Assert.That(method.GetParameters().Single().Name, Is.EqualTo(expected == typeof(string) ? "filePath" : "silentMode"));
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location, ".xml"));
            var entries = docs.Descendants("member").Where(m => ((string)m.Attribute("name")).StartsWith("M:") &&
                (((string)m.Attribute("name")).StartsWith("M:" + typeof(IOSRoot).FullName + "." + name + "(") ||
                 ((string)m.Attribute("name")) == "M:" + typeof(IOSRoot).FullName + "." + name ||
                 ((string)m.Attribute("name")).StartsWith("M:" + typeof(OSRootAdapter).FullName + "." + name + "(") ||
                 ((string)m.Attribute("name")) == "M:" + typeof(OSRootAdapter).FullName + "." + name)).ToArray();
            Assert.That(entries.Length, Is.EqualTo(2));
            Assert.That(entries[0].Elements().Select(e => e.ToString()), Is.EqualTo(entries[1].Elements().Select(e => e.ToString())));
            foreach (string tag in new[] { "summary", "returns", "remarks", "example" }) Assert.That(entries[0].Element(tag)?.Value, Is.Not.Null.And.Not.Empty);
            Assert.That(entries.SelectMany(e => e.Descendants()).Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
        }
    }
}

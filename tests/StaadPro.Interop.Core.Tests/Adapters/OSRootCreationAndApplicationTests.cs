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
using StaadPro.Interop.Core.Models.Root;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootCreationAndApplicationTests
    {
        private static readonly string[] Operations = ["handle", "new", "create", "save", "quit"];
        private static readonly object[] Handles = [(sbyte)-1, (byte)255, (short)-32768, (ushort)65535, int.MinValue,
            uint.MaxValue, long.MinValue, ulong.MaxValue, 0, 1L, new IntPtr(-1), new UIntPtr(ulong.MaxValue)];
        private static readonly object[] InvalidHandles = [null, true, "123", 1d, 1f, 1m, DayOfWeek.Monday, DBNull.Value, new object()];
        private string folder, target;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "StaadCoreRoot-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder); target = Path.Combine(folder, "NewFrame.std");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(folder, true);

        public sealed class NativeRoot : MetadataRoot
        {
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public object HandleValue = 123, ModelPath = "";
            public bool WritePath = true;
            public object[] Arguments;
            public Exception Failure;
            public Action<string> OnCall;
            private void Step(string operation)
            {
                Calls.Add(operation); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(operation);
                if (Failure != null) throw Failure;
            }
            public object GetProcessHandle() { Step("handle"); return HandleValue; }
            public void GetSTAADFile(ref object path, object fullPath)
            {
                Assert.That(path, Is.TypeOf<string>()); Assert.That(fullPath, Is.EqualTo(true));
                Step("identity"); if (WritePath) path = ModelPath;
            }
            public void NewSTAADFile(object path, object length, object force)
            {
                Arguments = [path, length, force]; Step("new");
            }
            public void CreateNamedView(object name, object flag) { Arguments = [name, flag]; Step("create"); }
            public void SaveNamedView(object name) { Arguments = [name]; Step("save"); }
            public void Quit() => Step("quit");
        }

        // Deliberately incompatible help-style signatures; no alternate-arity dispatch is allowed.
        public sealed class AlternateViewRoot : MetadataRoot
        {
            public int Calls;
            public void CreateNamedView(object name, object flag, ref object error) { Calls++; }
            public void SaveNamedView(object name, ref object error) { Calls++; }
        }

        private void Request(IOSRoot root, string operation)
        {
            switch (operation)
            {
                case "handle": root.GetProcessHandle(); break;
                case "new": root.NewSTAADFile(target, LengthInputUnit.Meter, ForceInputUnit.KiloNewton); break;
                case "create": root.CreateNamedView(" Frame Ω ", int.MinValue); break;
                case "save": root.SaveNamedView(" Frame Ω "); break;
                case "quit": root.Quit(); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        [TestCaseSource(nameof(Handles))]
        public void PreservesOpaqueIntegralAndPointerHandleBitsWithoutOwnership(object value)
        {
            var native = new NativeRoot { HandleValue = value };
            using var wrapper = new OpenStaadWrapper(native); native.Queries.Clear();
            ulong expected = value is ulong u ? u : value is UIntPtr up ? up.ToUInt64() :
                value is IntPtr ip ? unchecked((ulong)ip.ToInt64()) : unchecked((ulong)Convert.ToInt64(value));
            var first = wrapper.Root.GetProcessHandle(); var second = wrapper.Root.GetProcessHandle();
            Assert.That(first.RawValue, Is.EqualTo(expected)); Assert.That(second, Is.Not.SameAs(first));
            Assert.That(native.Calls, Is.EqualTo(new[] {"handle", "handle"})); Assert.That(native.Queries, Is.Empty);
            Assert.That(typeof(NativeProcessHandleValue).IsSealed, Is.True);
            Assert.That(typeof(NativeProcessHandleValue).GetConstructors(), Is.Empty);
            Assert.That(typeof(NativeProcessHandleValue).GetProperties().All(p => p.GetSetMethod(true) == null), Is.True);
            Assert.That(typeof(IDisposable).IsAssignableFrom(typeof(NativeProcessHandleValue)), Is.False);
        }

        [TestCaseSource(nameof(InvalidHandles))]
        public void RejectsCoercibleOrUnspecifiedHandleRepresentationsAndReleasesLease(object value)
        {
            var native = new NativeRoot { HandleValue = value }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetProcessHandle());
            native.HandleValue = 7; Assert.That(wrapper.Root.GetProcessHandle().RawValue, Is.EqualTo(7));
            Assert.That(native.Calls, Is.EqualTo(new[] {"handle", "handle"}));
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5), TestCase(6), TestCase(7)]
        public void SendsAbsolutePathAndExactInt32UnitsWithoutCreatingManagedFiles(int code)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; object geometry = wrapper.RawGeometry; native.Queries.Clear();
            root.NewSTAADFile(target, (LengthInputUnit)code, (ForceInputUnit)code);
            Assert.That(native.Arguments, Is.EqualTo(new object[] {target, code, code}));
            Assert.That(native.Arguments[1], Is.TypeOf<int>()); Assert.That(native.Arguments[2], Is.TypeOf<int>());
            Assert.That(native.Calls, Is.EqualTo(new[] {"identity", "new"}));
            Assert.That(File.Exists(target), Is.False); Assert.That(wrapper.Root, Is.SameAs(root));
            Assert.That(wrapper.RawGeometry, Is.SameAs(geometry)); Assert.That(native.Queries, Is.Empty);
        }

        [TestCase(-1), TestCase(8), TestCase(int.MaxValue)]
        public void InvalidUnitsStopBeforeFilesystemAndNativeInspection(int code)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            string absentParent = Path.Combine(folder, "absent", "new.std");
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                wrapper.Root.NewSTAADFile(absentParent, (LengthInputUnit)code, ForceInputUnit.KiloNewton)).ParamName, Is.EqualTo("lengthUnit"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                wrapper.Root.NewSTAADFile(absentParent, LengthInputUnit.Meter, (ForceInputUnit)code)).ParamName, Is.EqualTo("forceUnit"));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase(null), TestCase(""), TestCase(" "), TestCase("bad.txt"), TestCase("CON.std"), TestCase("prn.extra.std"),
         TestCase("NUL.std"), TestCase("COM1.std"), TestCase("LPT9.std"), TestCase("COM¹.std"), TestCase("name:stream.std"),
         TestCase("bad?.std"), TestCase("bad*.std"), TestCase("bad|.std"), TestCase("bad<.std"), TestCase("bad>.std")]
        public void InvalidDestinationsNeverReachNative(string filename)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            string path = filename == null || string.IsNullOrWhiteSpace(filename) ? filename : Path.Combine(folder, filename);
            Assert.That(() => wrapper.Root.NewSTAADFile(path, LengthInputUnit.Meter, ForceInputUnit.KiloNewton),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void ExistingFileDirectoryMissingParentAndFileParentAreRejectedWithoutChangingBytes()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            File.WriteAllText(target, "existing");
            Assert.Throws<IOException>(() => Request(wrapper.Root, "new"));
            Assert.That(File.ReadAllText(target), Is.EqualTo("existing"));
            string directoryTarget = Path.Combine(folder, "directory.std"); Directory.CreateDirectory(directoryTarget);
            Assert.Throws<IOException>(() => wrapper.Root.NewSTAADFile(directoryTarget, LengthInputUnit.Meter, ForceInputUnit.KiloNewton));
            Assert.That(() => wrapper.Root.NewSTAADFile(Path.Combine(folder,"absent","new.std"),LengthInputUnit.Meter,ForceInputUnit.KiloNewton), Throws.InstanceOf<IOException>());
            Assert.That(() => wrapper.Root.NewSTAADFile(Path.Combine(target,"new.std"),LengthInputUnit.Meter,ForceInputUnit.KiloNewton), Throws.InstanceOf<IOException>());
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase("already.std"), TestCase("  already.std  ")]
        public void NamedSessionCannotBeReplaced(string path)
        {
            var native = new NativeRoot { ModelPath = path }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, "new"));
            Assert.That(native.Calls, Is.EqualTo(new[] {"identity"})); Assert.That(File.Exists(target), Is.False);
        }

        [TestCase(null), TestCase(1), TestCase(true)]
        public void InvalidIdentityOutputIsNotTreatedAsAnUnnamedSession(object output)
        {
            var native = new NativeRoot { ModelPath = output }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, "new"));
            Assert.That(native.Calls, Is.EqualTo(new[] {"identity"}));
        }

        [Test]
        public void UnwrittenIdentityIsRejectedAndWhitespaceIdentityIsExplicitlyAccepted()
        {
            var native = new NativeRoot { WritePath = false }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, "new"));
            native.WritePath = true; native.ModelPath = " \t"; native.Calls.Clear(); Request(wrapper.Root,"new");
            Assert.That(native.Calls, Is.EqualTo(new[] {"identity", "new"}));
        }

        [Test]
        public void RechecksTargetAfterIdentityReadAndPreservesCompetingFile()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = operation => { if(operation == "identity") File.WriteAllText(target, "competitor"); };
            Assert.Throws<IOException>(() => Request(wrapper.Root,"new"));
            Assert.That(File.ReadAllText(target), Is.EqualTo("competitor")); Assert.That(native.Calls, Is.EqualTo(new[] {"identity"}));
        }

        [Test]
        public void RelativeDestinationIsCapturedBeforeNativeCallbackChangesWorkingDirectory()
        {
            string original = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = folder;
                var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
                native.OnCall = operation => { if(operation == "identity") Environment.CurrentDirectory = original; };
                wrapper.Root.NewSTAADFile("NewFrame.STD", LengthInputUnit.Meter, ForceInputUnit.KiloNewton);
                Assert.That(native.Arguments[0], Is.EqualTo(Path.Combine(folder,"NewFrame.STD")));
            }
            finally { Environment.CurrentDirectory = original; }
        }

        [TestCase("create"), TestCase("save")]
        public void NamedViewsRequireNamesAndPreserveLiteralText(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            foreach (string name in new string[] {null, "", " \t\r\n"})
            {
                Assert.That(() => { if(operation == "create") wrapper.Root.CreateNamedView(name, 1); else wrapper.Root.SaveNamedView(name); }, Throws.InstanceOf<ArgumentException>());
            }
            Assert.That(native.Calls, Is.Empty);
            Request(wrapper.Root,operation);
            Assert.That(native.Arguments[0], Is.EqualTo(" Frame Ω ")); Assert.That(native.Calls, Is.EqualTo(new[] {operation}));
        }

        [TestCase(int.MinValue), TestCase(-1), TestCase(0), TestCase(1), TestCase(int.MaxValue)]
        public void NamedViewFlagsAreNotGuessedOrDefaulted(int flag)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            wrapper.Root.CreateNamedView("view",flag);
            Assert.That(native.Arguments, Is.EqualTo(new object[] {"view",flag})); Assert.That(native.Arguments[1], Is.TypeOf<int>());
        }

        [TestCase("create"), TestCase("save")]
        public void DoesNotRetryContradictoryHelpSignatures(string operation)
        {
            var native = new AlternateViewRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<RuntimeBinderException>(() => Request(wrapper.Root,operation)); Assert.That(native.Calls, Is.Zero);
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingMembersRemainFailuresAndDoNotAcquireAnotherApplication(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new MetadataRoot());
            Assert.Throws<RuntimeBinderException>(() => Request(wrapper.Root,operation));
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void ExceptionsAndHResultsPropagateWithoutRetryAndLeaseIsReleased(string operation)
        {
            var failure = new COMException("native failed", unchecked((int)0x80010108));
            var native = new NativeRoot { Failure = failure }; using var wrapper = new OpenStaadWrapper(native);
            var error = Assert.Throws<COMException>(() => Request(wrapper.Root,operation));
            Assert.That(error, Is.SameAs(failure)); Assert.That(error.HResult, Is.EqualTo(failure.HResult));
            Assert.That(native.Calls, Has.Count.EqualTo(1));
            native.Failure = null; Assert.DoesNotThrow(() => Request(wrapper.Root,operation));
        }

        [Test]
        public void CreationFailureAfterDispatchIsNotRetriedOrRolledBack()
        {
            var failure = new COMException("creation failed after effects", unchecked((int)0x80004005));
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = operation => { if(operation=="new") { File.WriteAllText(target,"native partial contents"); throw failure; } };
            Assert.That(Assert.Throws<COMException>(() => Request(wrapper.Root,"new")), Is.SameAs(failure));
            Assert.That(native.Calls, Is.EqualTo(new[] {"identity", "new"}));
            Assert.That(File.ReadAllText(target), Is.EqualTo("native partial contents"));
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void GuardsRejectWrongThreadStaleManualAndDisposedBindingsBeforeAnyNativeWork(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            Assert.Throws<InvalidOperationException>(() => Request(new OSRootAdapter(wrapper),operation));
            Exception error = null;
            var worker = new Thread(() => {try { Request(root,operation); } catch(Exception ex) {error = ex;}});
            worker.SetApartmentState(ApartmentState.MTA); worker.Start();
            Assert.That(worker.Join(5000), Is.True); Assert.That(error, Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects(); Assert.Throws<InvalidOperationException>(() => Request(root,operation));
            root = wrapper.Root; wrapper.Dispose(); Assert.Throws<ObjectDisposedException>(() => Request(root,operation));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void LeaseCoversIdentityValidationAndDispatchAndRejectsNestedLifecycleWork(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = _ =>
            {
                foreach(string nested in Operations) Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root,nested));
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Request(wrapper.Root,operation);
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(native.Calls, Is.EqualTo(operation == "new" ? new[] {"identity", "new"} : new[] {operation}));
            native.OnCall = null; Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [Test]
        public void QuitDoesNotDisposeRetargetSaveCloseOrVerifyExitAndDisposeDoesNotQuit()
        {
            var native = new NativeRoot(); var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            native.Queries.Clear(); root.Quit();
            Assert.That(wrapper.Root, Is.SameAs(root)); Assert.That(wrapper.IsConnected, Is.True);
            Assert.That(wrapper.RawOpenStaad, Is.SameAs(native)); Assert.That(native.Calls, Is.EqualTo(new[] {"quit"}));
            wrapper.Dispose(); Assert.That(native.Calls, Is.EqualTo(new[] {"quit"})); Assert.That(native.Queries, Is.Empty);
            var untouched = new NativeRoot(); new OpenStaadWrapper(untouched).Dispose(); Assert.That(untouched.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void ExactManagedSignaturesAndExpandedDocumentationMatch(string operation)
        {
            string name = operation == "handle" ? "GetProcessHandle" : operation == "new" ? "NewSTAADFile" :
                operation == "create" ? "CreateNamedView" : operation == "save" ? "SaveNamedView" : "Quit";
            Type[] types = operation == "new" ? [typeof(string),typeof(LengthInputUnit),typeof(ForceInputUnit)] :
                operation == "create" ? [typeof(string),typeof(int)] : operation == "save" ? [typeof(string)] : [];
            string[] names = operation == "new" ? ["filePath","lengthUnit","forceUnit"] : operation == "create" ? ["viewName","creationFlag"] : operation == "save" ? ["viewName"] : [];
            var xml = XDocument.Load(Path.ChangeExtension(typeof(OpenStaadWrapper).Assembly.Location,".xml"));
            XElement previous = null;
            foreach(Type owner in new[] {typeof(IOSRoot),typeof(OSRootAdapter)})
            {
                var method = owner.GetMethod(name,types);
                Assert.That(method, Is.Not.Null); Assert.That(method.ReturnType, Is.EqualTo(operation == "handle" ? typeof(NativeProcessHandleValue) : typeof(void)));
                Assert.That(method.GetParameters().Select(p=>p.Name), Is.EqualTo(names));
                Assert.That(method.GetParameters().Any(p=>p.ParameterType.IsByRef||p.IsOut||p.IsOptional), Is.False);
                string id = "M:" + owner.FullName + "." + name + (types.Length==0 ? "" : "("+string.Join(",",types.Select(t=>t.FullName))+")");
                var doc = xml.Descendants("member").Single(e => (string)e.Attribute("name")==id);
                Assert.That(doc.Element("summary"), Is.Not.Null); Assert.That(doc.Element("remarks"), Is.Not.Null);
                Assert.That(doc.Element("example")?.Element("code"), Is.Not.Null);
                Assert.That(doc.Elements("param").Select(e=>(string)e.Attribute("name")), Is.EqualTo(names));
                Assert.That(doc.Elements("exception").Any(e=>(string)e.Attribute("cref")=="T:System.ObjectDisposedException"), Is.True);
                if(previous!=null) Assert.That(doc.Elements().Select(e=>e.ToString()), Is.EqualTo(previous.Elements().Select(e=>e.ToString())));
                previous = doc;
            }
        }
    }
}

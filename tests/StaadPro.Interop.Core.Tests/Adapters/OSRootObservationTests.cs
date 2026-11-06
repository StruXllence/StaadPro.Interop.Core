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
using StaadPro.Interop.Core.Models;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootObservationTests
    {
        private static readonly string[] Operations = {
            "GetStaadFileFullPath", "GetStaadFileFolder", "IsPhysicalModel", "GetErrorMessage", "GetProcessId"
        };

        public sealed class NativeRoot : MetadataRoot
        {
            public object FilePath = @"C:\Models\Structure.std", Folder = @"C:\Models\";
            public object Physical = true, Error = "", ProcessId = 123u;
            public bool WriteOutput = true;
            public readonly List<string> Observations = new();
            public Action BeforeRead;
            public Exception Failure;
            private void Observe(string operation)
            {
                Observations.Add(operation);
                BeforeRead?.Invoke();
                if (Failure != null) throw Failure;
            }
            public void GetSTAADFile(ref object fileName, object includeFullPath)
            {
                Assert.That(fileName, Is.TypeOf<string>(), "Output VARIANT must start as BSTR, not null/DBNull.");
                Assert.That(fileName, Is.Not.EqualTo(""), "An unchanged empty buffer must not masquerade as blank.");
                Assert.That(includeFullPath, Is.TypeOf<bool>().And.EqualTo(true));
                Observe("GetStaadFileFullPath"); if (WriteOutput) fileName = FilePath;
            }
            public void GetSTAADFileFolder(ref object folder)
            {
                Assert.That(folder, Is.TypeOf<string>());
                Assert.That(folder, Is.Not.EqualTo(""));
                Observe("GetStaadFileFolder"); if (WriteOutput) folder = Folder;
            }
            public object IsPhysicalModel() { Observe("IsPhysicalModel"); return Physical; }
            public object GetErrorMessage() { Observe("GetErrorMessage"); return Error; }
            public object GetProcessId() { Observe("GetProcessId"); return ProcessId; }
        }

        private static object Read(IOSRoot root, string operation) => operation switch
        {
            "GetStaadFileFullPath" => root.GetStaadFileFullPath(),
            "GetStaadFileFolder" => root.GetStaadFileFolder(),
            "IsPhysicalModel" => root.IsPhysicalModel(),
            "GetErrorMessage" => root.GetErrorMessage(),
            "GetProcessId" => root.GetProcessId(),
            _ => throw new ArgumentException(nameof(operation))
        };

        private static IEnumerable<TestCaseData> TextCases =>
            new[] { "", "  ", "Δομή 日本語\r\nline\t ", @"\\server\Share\sub folder\.\name.v2.STD", @"Z:/Missing/../model.std " }
            .SelectMany(text => new[] { "GetStaadFileFullPath", "GetStaadFileFolder", "GetErrorMessage" }
                .Select(operation => new TestCaseData(operation, text)));

        [TestCaseSource(nameof(TextCases))]
        public void TextIsReturnedExactlyWithoutNormalizationOrFilesystemChecks(string operation, string text)
        {
            var native = new NativeRoot { FilePath = text, Folder = text, Error = text };
            using var wrapper = new OpenStaadWrapper(native); int metadataCalls = native.Queries.Count;
            Assert.That(Read(wrapper.Root, operation), Is.EqualTo(text));
            Assert.That(native.Observations, Is.EqualTo(new[] { operation }));
            Assert.That(native.Queries.Count, Is.EqualTo(metadataCalls));
        }

        [Test]
        public void EachReadUsesTheBoundRootAndObservesExternalChangesWithoutRefreshingCaches()
        {
            var first = new NativeRoot(); var other = new NativeRoot { FilePath = "decoy.std" };
            using var wrapper = new OpenStaadWrapper(first); using var decoy = new OpenStaadWrapper(other);
            int metadataCalls = first.Queries.Count;
            foreach (string operation in Operations) Read(wrapper.Root, operation);
            first.FilePath = "changed.std"; first.Folder = "different folder/";
            first.Physical = false; first.Error = "new error"; first.ProcessId = uint.MaxValue;
            Assert.That(wrapper.Root.GetStaadFileFullPath(), Is.EqualTo("changed.std"));
            Assert.That(wrapper.Root.GetStaadFileFolder(), Is.EqualTo("different folder/"));
            Assert.That(wrapper.Root.IsPhysicalModel(), Is.False);
            Assert.That(wrapper.Root.GetErrorMessage(), Is.EqualTo("new error"));
            Assert.That(wrapper.Root.GetProcessId(), Is.EqualTo(uint.MaxValue));
            Assert.That(first.Observations, Is.EqualTo(Operations.Concat(Operations)));
            Assert.That(other.Observations, Is.Empty);
            Assert.That(first.Queries.Count, Is.EqualTo(metadataCalls));
        }

        private static IEnumerable<TestCaseData> InvalidTextCases =>
            new object[] { null, DBNull.Value, 0, true, new[] { "path" } }.SelectMany(value =>
                new[] { "GetStaadFileFullPath", "GetStaadFileFolder", "GetErrorMessage" }
                    .Select(operation => new TestCaseData(operation, value)));

        [TestCaseSource(nameof(InvalidTextCases))]
        public void NonStringTextIsRejectedInsteadOfInventingAnEmptyResult(string operation, object value)
        {
            var native = new NativeRoot { FilePath = value, Folder = value, Error = value };
            using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Read(wrapper.Root, operation));
            Assert.That(native.Observations, Is.EqualTo(new[] { operation }));
        }

        [TestCase("GetStaadFileFullPath")]
        [TestCase("GetStaadFileFolder")]
        public void UnwrittenBstrIsRejectedButExplicitEmptyOutputRemainsValid(string operation)
        {
            var native = new NativeRoot { WriteOutput = false, FilePath = "", Folder = "" };
            using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Read(wrapper.Root, operation));
            native.WriteOutput = true;
            Assert.That(Read(wrapper.Root, operation), Is.EqualTo(""));
            Assert.That(native.Observations, Has.Count.EqualTo(2));
        }

        private static IEnumerable<TestCaseData> FlagCases => new object[] {
            false, true, (sbyte)0, (sbyte)1, (byte)0, (byte)1, (short)0, (short)1,
            (ushort)0, (ushort)1, 0, 1, 0u, 1u, 0L, 1L, 0UL, 1UL
        }.Select(value => new TestCaseData(value, value.Equals(true) || Convert.ToInt64(value) == 1));

        [TestCaseSource(nameof(FlagCases))]
        public void PhysicalClassificationAcceptsOnlyBooleanOrIntegralZeroAndOne(object value, bool expected)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { Physical = value });
            Assert.That(wrapper.Root.IsPhysicalModel(), Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> InvalidFlagCases => new object[] {
            null, DBNull.Value, -1, 2, uint.MaxValue, ulong.MaxValue, "true", "1", 1.0, 1f, 1m,
            '1', new IntPtr(1), new UIntPtr(1), DayOfWeek.Monday
        }.Select(value => new TestCaseData(new object[] { value }));

        [TestCaseSource(nameof(InvalidFlagCases))]
        public void PhysicalClassificationRejectsAmbiguousOrCoercibleValues(object value)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { Physical = value });
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.IsPhysicalModel());
        }

        private static IEnumerable<TestCaseData> ProcessIdCases => new object[] {
            (sbyte)1, byte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue,
            (long)uint.MaxValue, (ulong)uint.MaxValue
        }.Select(value => new TestCaseData(value, Convert.ToUInt32(value)));

        [TestCaseSource(nameof(ProcessIdCases))]
        public void ProcessIdentifierPreservesPositiveUInt32RangeIntegralValues(object value, uint expected)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { ProcessId = value });
            Assert.That(wrapper.Root.GetProcessId(), Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> InvalidProcessIdCases => new object[] {
            null, DBNull.Value, 0, -1, (long)uint.MaxValue + 1, (ulong)uint.MaxValue + 1, ulong.MaxValue,
            false, true, "123", 123.0, 123f, 123m, 'x', new IntPtr(123), new UIntPtr(123), DayOfWeek.Monday
        }.Select(value => new TestCaseData(new object[] { value }));

        [TestCaseSource(nameof(InvalidProcessIdCases))]
        public void ProcessIdentifierRejectsInvalidRangesAndNonIntegralValues(object value)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { ProcessId = value });
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetProcessId());
        }

        [TestCaseSource(nameof(Operations))]
        public void OriginalComFailureIsPreservedWithoutRetryOrErrorMessageProbeAndLeaseIsReleased(string operation)
        {
            var failure = new COMException("Native busy", unchecked((int)0x80010001));
            var native = new NativeRoot { Failure = failure }; using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<COMException>(() => Read(wrapper.Root, operation)), Is.SameAs(failure));
            Assert.That(native.Observations, Is.EqualTo(new[] { operation }));
            native.Failure = null;
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingMemberIsReportedWithoutFallbackOrSilentDefault(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new MetadataRoot());
            Assert.Throws<RuntimeBinderException>(() => Read(wrapper.Root, operation));
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void DisposedBindingRejectsNativeDispatch(string operation)
        {
            var native = new NativeRoot(); var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Read(root, operation));
            Assert.That(native.Observations, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void RefreshUnitsKeepsBindingWhileRefreshComObjectsInvalidatesIt(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            wrapper.RefreshUnits(); Assert.DoesNotThrow(() => Read(root, operation)); native.Observations.Clear();
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Read(root, operation));
            Assert.That(native.Observations, Is.Empty);
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void AnotherThreadIsRejectedBeforeDispatchAndOwnerCanContinue(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); Exception observed = null;
            var worker = new Thread(() => { try { Read(wrapper.Root, operation); } catch (Exception error) { observed = error; } });
            worker.SetApartmentState(ApartmentState.STA); worker.Start();
            Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(observed, Is.TypeOf<InvalidOperationException>());
            Assert.That(native.Observations, Is.Empty);
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        private static IEnumerable<TestCaseData> NestedCases => Operations.SelectMany(operation =>
            Operations.Concat(new[] { "GetBaseUnit", "RefreshUnits", "RefreshComObjects", "Dispose" })
                .Select(nested => new TestCaseData(operation, nested)));

        private static IEnumerable<TestCaseData> RefreshCases => Operations.SelectMany(operation =>
            new[] { new TestCaseData(operation, false), new TestCaseData(operation, true) });

        [TestCaseSource(nameof(RefreshCases))]
        public void CallbackDuringRefreshCannotDispatchLiveObservations(string operation, bool rebind)
        {
            var native = new Models.OpenStaadWrapperLifetimeTests.NativeRoot();
            using var wrapper = new OpenStaadWrapper(native);
            native.BeforeBaseUnit = () => Read(wrapper.Root, operation);
            if (rebind) Assert.Throws<InvalidOperationException>(wrapper.RefreshComObjects);
            else Assert.Throws<InvalidOperationException>(wrapper.RefreshUnits);
            native.BeforeBaseUnit = null;
            Assert.DoesNotThrow(wrapper.RefreshUnits);
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(NestedCases))]
        public void NativeCallbackCannotReenterRootOrLifecycleWork(string operation, string nested)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.BeforeRead = () => {
                switch (nested)
                {
                    case "GetBaseUnit": wrapper.Root.GetBaseUnit(); break;
                    case "RefreshUnits": wrapper.RefreshUnits(); break;
                    case "RefreshComObjects": wrapper.RefreshComObjects(); break;
                    case "Dispose": wrapper.Dispose(); break;
                    default: Read(wrapper.Root, nested); break;
                }
            };
            Assert.Throws<InvalidOperationException>(() => Read(wrapper.Root, operation));
            Assert.That(native.Observations, Is.EqualTo(new[] { operation }));
            Assert.That(wrapper.IsConnected, Is.True);
            native.BeforeRead = null;
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void ManuallyConstructedAdapterCannotBypassCurrentBindingValidation(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var other = new OSRootAdapter(wrapper);
            Assert.Throws<InvalidOperationException>(() => Read(other, operation));
            Assert.That(native.Observations, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void InterfaceAndConcreteIntelliSenseIsCompleteExpandedAndIdentical(string operation)
        {
            var members = XDocument.Load(Path.ChangeExtension(typeof(OpenStaadWrapper).Assembly.Location, ".xml"))
                .Descendants("member").ToArray();
            var contract = members.Single(m => (string)m.Attribute("name") == "M:StaadPro.Interop.Core.Adapters.Interfaces.IOSRoot." + operation);
            var implementation = members.Single(m => (string)m.Attribute("name") == "M:StaadPro.Interop.Core.Adapters.Models.OSRootAdapter." + operation);
            foreach (string element in new[] { "summary", "returns", "remarks", "exception", "example" })
                Assert.That(contract.Element(element), Is.Not.Null, operation + ": " + element);
            Assert.That(contract.Elements().Select(e => e.ToString()), Is.EqualTo(implementation.Elements().Select(e => e.ToString())));
            Assert.That(contract.Descendants("include"), Is.Empty); Assert.That(contract.Descendants("inheritdoc"), Is.Empty);
        }
    }
}

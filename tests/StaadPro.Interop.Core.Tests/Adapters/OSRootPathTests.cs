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
    public sealed class OSRootPathTests
    {
        private static readonly string[] Operations = {
            "GetStaadFileNameOnly", "GetAnlFileFullPath", "GetOpenedStaadFilename", "IsTarget", "ModelIsOpen"
        };
        private static readonly string[] Predicates = { "IsTarget", "ModelIsOpen" };

        public sealed class NativeRoot : MetadataRoot
        {
            public object NativePath = @"C:\Models\Frame.std";
            public bool WriteOutput = true;
            public int Calls;
            public Action BeforeRead;
            public Exception Failure;
            public Queue<string> Paths;
            public void GetSTAADFile(ref object output, object includeFullPath)
            {
                Calls++;
                Assert.That(output, Is.TypeOf<string>().And.Not.EqualTo(""), "Native VARIANT must start as a nonempty BSTR sentinel.");
                Assert.That(includeFullPath, Is.TypeOf<bool>().And.EqualTo(true), "Never pass a model path or string TRUE as the flag.");
                BeforeRead?.Invoke();
                if (Failure != null) throw Failure;
                if (WriteOutput) output = Paths == null ? NativePath : Paths.Dequeue();
            }
        }

        private static object Read(IOSRoot root, string operation, string target = @"C:\Models\Frame.std") => operation switch
        {
            "GetStaadFileNameOnly" => root.GetStaadFileNameOnly(),
            "GetAnlFileFullPath" => root.GetAnlFileFullPath(),
            "GetOpenedStaadFilename" => root.GetOpenedStaadFilename(),
            "IsTarget" => root.IsTarget(target),
            "ModelIsOpen" => root.ModelIsOpen(target),
            _ => throw new ArgumentException(nameof(operation))
        };

        [TestCase(@"C:\Models\雪 frame.v2.STD", "雪 frame.v2.STD", @"C:\Models\雪 frame.v2.ANL")]
        [TestCase(@"\\server\share\dir\A.B.std", "A.B.std", @"\\server\share\dir\A.B.ANL")]
        [TestCase("relative.std", "relative.std", "relative.ANL")]
        [TestCase(@"sub\model", "model", @"sub\model.ANL")]
        [TestCase(@"C:\No Such Folder\model.other", "model.other", @"C:\No Such Folder\model.ANL")]
        [TestCase(@"C:\Models\a.b.c", "a.b.c", @"C:\Models\a.b.ANL")]
        [TestCase(@"C:\Models\.std", ".std", @"C:\Models\.ANL")]
        [TestCase("", "", "")]
        public void FilenameAndAnalysisPathUseOneSnapshotWithExpectedWindowsSemantics(string path, string filename, string anl)
        {
            var native = new NativeRoot { NativePath = path };
            using var wrapper = new OpenStaadWrapper(native); int metadataCalls = native.Queries.Count;
            Assert.That(wrapper.Root.GetStaadFileNameOnly(), Is.EqualTo(filename));
            Assert.That(native.Calls, Is.EqualTo(1));
            Assert.That(wrapper.Root.GetAnlFileFullPath(), Is.EqualTo(anl));
            Assert.That(native.Calls, Is.EqualTo(2));
            Assert.That(native.Queries.Count, Is.EqualTo(metadataCalls), "No unit/version refresh belongs in a path helper.");
        }

        [Test]
        public void AnalysisDerivationNeverCombinesTwoDifferentModels()
        {
            var native = new NativeRoot { Paths = new Queue<string>(new[] { @"C:\First\one.v2.std", @"D:\Other\two.std" }) };
            using var wrapper = new OpenStaadWrapper(native);
            Assert.That(wrapper.Root.GetAnlFileFullPath(), Is.EqualTo(@"C:\First\one.v2.ANL"));
            Assert.That(native.Calls, Is.EqualTo(1));
            Assert.That(wrapper.Root.GetAnlFileFullPath(), Is.EqualTo(@"D:\Other\two.ANL"));
            Assert.That(native.Calls, Is.EqualTo(2));
        }

        [TestCase(@"C:/Models/sub/../Frame.v2.STD", @"C:\Models\Frame.v2.STD")]
        [TestCase(@"\\server\share\dir\..\Frame.std", @"\\server\share\Frame.std")]
        [TestCase(@"Z:\NeverCreated\missing.other", @"Z:\NeverCreated\missing.other")]
        [TestCase("", "")]
        public void OpenedFilenameNormalizesNamedSnapshotsAndPreservesExplicitBlank(string nativePath, string expected)
        {
            var native = new NativeRoot { NativePath = nativePath };
            using var wrapper = new OpenStaadWrapper(native);
            Assert.That(wrapper.Root.GetOpenedStaadFilename(), Is.EqualTo(expected));
            Assert.That(native.Calls, Is.EqualTo(1));
        }

        [Test]
        public void RelativeOpenedFilenameUsesWorkingDirectoryWithoutChangingIt()
        {
            string before = Environment.CurrentDirectory;
            var native = new NativeRoot { NativePath = @"sub\..\Frame.std" };
            using var wrapper = new OpenStaadWrapper(native);
            Assert.That(wrapper.Root.GetOpenedStaadFilename(), Is.EqualTo(Path.Combine(before, "Frame.std")));
            Assert.That(Environment.CurrentDirectory, Is.EqualTo(before));
            Assert.That(native.Calls, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Operations))]
        public void HelpersUseTheirOwnBoundSessionAndObserveModelChangesWithoutRefresh(string operation)
        {
            var native = new NativeRoot(); var decoy = new NativeRoot { NativePath = @"D:\Other\Other.std" };
            using var wrapper = new OpenStaadWrapper(native); using var other = new OpenStaadWrapper(decoy);
            object first = Read(wrapper.Root, operation);
            native.NativePath = @"C:\Changed\New.std";
            object second = Read(wrapper.Root, operation);
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(native.Calls, Is.EqualTo(2)); Assert.That(decoy.Calls, Is.Zero);
        }

        private static IEnumerable<TestCaseData> MatchCases => Predicates.SelectMany(operation => new[] {
            new TestCaseData(operation, @"C:\Models\Frame.std", @"c:/models/./FRAME.STD", true),
            new TestCaseData(operation, @"C:\Models\Frame.std", @"C:\Other\Frame.std", false),
            new TestCaseData(operation, @"C:\Models\Frame.std", @"C:\Models\Frame.std.extra", false),
            new TestCaseData(operation, @"C:\Models\Frame.std", @"C:\Models\Other.std", false),
            new TestCaseData(operation, @"\\server\share\Frame.std", @"\\SERVER\SHARE\dir\..\frame.std", true),
            new TestCaseData(operation, @"\\server\share\Frame.std", @"\\other\share\Frame.std", false),
            new TestCaseData(operation, @"Z:\Missing\NoFile.other", @"z:\missing\nofile.OTHER", true),
            new TestCaseData(operation, "", @"C:\Models\Frame.std", false)
        });

        [TestCaseSource(nameof(MatchCases))]
        public void PredicatesCompareCompleteNormalizedPathsWithoutExistenceOrExtensionRequirements(string operation, string nativePath, string requested, bool expected)
        {
            var native = new NativeRoot { NativePath = nativePath }; using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Read(wrapper.Root, operation, requested), Is.EqualTo(expected));
            Assert.That(native.Calls, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Predicates))]
        public void RelativeComparisonUsesOneBaseEvenWhenNativeCallbackChangesTheWorkingDirectory(string operation)
        {
            string before = Environment.CurrentDirectory;
            var native = new NativeRoot { NativePath = "Frame.std", BeforeRead = () => Environment.CurrentDirectory = Path.GetTempPath() };
            try
            {
                using var wrapper = new OpenStaadWrapper(native);
                Assert.That(Read(wrapper.Root, operation, @"sub\..\Frame.std"), Is.True);
                Assert.That(native.Calls, Is.EqualTo(1));
            }
            finally { Environment.CurrentDirectory = before; }
        }

        private static IEnumerable<TestCaseData> InvalidRequests => Predicates.SelectMany(operation =>
            new string[] { null, "", " \t ", "bad\0name.std", "bad|name.std", "bad<name.std", "bad\"name.std" }
                .Select(value => new TestCaseData(operation, value)));

        [TestCaseSource(nameof(InvalidRequests))]
        public void InvalidRequestedPathsAreRejectedBeforeNativeDispatchWithOriginalParameterNames(string operation, string value)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var error = Assert.Catch<ArgumentException>(() => Read(wrapper.Root, operation, value));
            Assert.That(error, value == null ? Is.TypeOf<ArgumentNullException>() : Is.TypeOf<ArgumentException>());
            Assert.That(error.ParamName, Is.EqualTo(operation == "IsTarget" ? "staadFileFullPath" : "fileFullPath"));
            Assert.That(native.Calls, Is.Zero);
        }

        private static IEnumerable<TestCaseData> MalformedOutputCases => Operations.SelectMany(operation =>
            new object[] { null, DBNull.Value, 17, true, new[] { "Frame.std" } }.Select(value => new TestCaseData(operation, value)));

        [TestCaseSource(nameof(MalformedOutputCases))]
        public void MalformedNativeOutputsAreNotCoercedIntoBlankOrFalse(string operation, object value)
        {
            var native = new NativeRoot { NativePath = value }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Read(wrapper.Root, operation));
            Assert.That(native.Calls, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Operations))]
        public void UnwrittenOutputIsDistinctFromExplicitBlank(string operation)
        {
            var native = new NativeRoot { WriteOutput = false }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Read(wrapper.Root, operation));
            Assert.That(native.Calls, Is.EqualTo(1));
        }

        private static IEnumerable<TestCaseData> InvalidNativeCharacters => Operations.SelectMany(operation =>
            new[] { "bad\0name.std", "bad|name.std", "bad>name.std", "bad\nname.std" }.Select(path => new TestCaseData(operation, path)));

        [TestCaseSource(nameof(InvalidNativeCharacters))]
        public void ModernPathExtractionRetainsTheFrameworkInvalidCharacterContract(string operation, string nativePath)
        {
            var native = new NativeRoot { NativePath = nativePath }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<ArgumentException>(() => Read(wrapper.Root, operation));
            Assert.That(native.Calls, Is.EqualTo(1));
        }

        [TestCase("GetOpenedStaadFilename")]
        [TestCase("IsTarget")]
        [TestCase("ModelIsOpen")]
        public void NonemptyWhitespaceNativePathIsInvalidRatherThanBlank(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { NativePath = "   " });
            Assert.Throws<ArgumentException>(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeFailuresPropagateWithoutRetriesAndReleaseTheGuard(string operation)
        {
            var failure = new COMException("Disconnected", unchecked((int)0x80010108));
            var native = new NativeRoot { Failure = failure }; using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<COMException>(() => Read(wrapper.Root, operation)), Is.SameAs(failure));
            Assert.That(native.Calls, Is.EqualTo(1));
            native.Failure = null;
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingNativeSignatureDoesNotBecomeBlankOrFalse(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new MetadataRoot());
            Assert.Throws<RuntimeBinderException>(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void DisposedRootRejectsDispatch(string operation)
        {
            var native = new NativeRoot(); var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            wrapper.Dispose(); Assert.Throws<ObjectDisposedException>(() => Read(root, operation));
            Assert.That(native.Calls, Is.Zero);
        }

        [TestCaseSource(nameof(Operations))]
        public void RefreshUnitsPreservesTheBindingWhileRefreshComObjectsReplacesIt(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); var root = wrapper.Root;
            wrapper.RefreshUnits(); Assert.DoesNotThrow(() => Read(root, operation));
            wrapper.RefreshComObjects(); int calls = native.Calls;
            Assert.Throws<InvalidOperationException>(() => Read(root, operation));
            Assert.That(native.Calls, Is.EqualTo(calls));
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void AnotherThreadIsRejectedBeforeDispatch(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); Exception observed = null;
            var thread = new Thread(() => { try { Read(wrapper.Root, operation); } catch (Exception ex) { observed = ex; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start();
            Assert.That(thread.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(observed, Is.TypeOf<InvalidOperationException>());
            Assert.That(native.Calls, Is.Zero);
        }

        private static IEnumerable<TestCaseData> DuringRefreshCases => Operations.SelectMany(operation =>
            new[] { new TestCaseData(operation, false), new TestCaseData(operation, true) });

        [TestCaseSource(nameof(DuringRefreshCases))]
        public void CallbackDuringRefreshCannotObservePathHelpers(string operation, bool rebind)
        {
            var native = new Models.OpenStaadWrapperLifetimeTests.NativeRoot();
            using var wrapper = new OpenStaadWrapper(native);
            native.BeforeBaseUnit = () => Read(wrapper.Root, operation);
            if (rebind) Assert.Throws<InvalidOperationException>(wrapper.RefreshComObjects);
            else Assert.Throws<InvalidOperationException>(wrapper.RefreshUnits);
            native.BeforeBaseUnit = null;
            Assert.DoesNotThrow(wrapper.RefreshUnits);
        }

        private static IEnumerable<TestCaseData> NestedCases => Operations.SelectMany(operation =>
            Operations.Concat(new[] { "GetStaadFileFullPath", "GetBaseUnit", "RefreshUnits", "RefreshComObjects", "Dispose" })
                .Select(nested => new TestCaseData(operation, nested)));

        [TestCaseSource(nameof(NestedCases))]
        public void NativeCallbackCannotReenterPathObservationOrLifecycle(string operation, string nested)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.BeforeRead = () => {
                switch (nested)
                {
                    case "GetStaadFileFullPath": wrapper.Root.GetStaadFileFullPath(); break;
                    case "GetBaseUnit": wrapper.Root.GetBaseUnit(); break;
                    case "RefreshUnits": wrapper.RefreshUnits(); break;
                    case "RefreshComObjects": wrapper.RefreshComObjects(); break;
                    case "Dispose": wrapper.Dispose(); break;
                    default: Read(wrapper.Root, nested); break;
                }
            };
            Assert.Throws<InvalidOperationException>(() => Read(wrapper.Root, operation));
            Assert.That(native.Calls, Is.EqualTo(1));
            native.BeforeRead = null;
            Assert.DoesNotThrow(() => Read(wrapper.Root, operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void NoncurrentManuallyConstructedBindingCannotDispatch(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Read(new OSRootAdapter(wrapper), operation));
            Assert.That(native.Calls, Is.Zero);
        }

        [TestCaseSource(nameof(Operations))]
        public void PublicSignaturesAndExpandedInterfaceConcreteDocumentationMatch(string operation)
        {
            string suffix = operation == "IsTarget" || operation == "ModelIsOpen" ? "(System.String)" : "";
            var members = XDocument.Load(Path.ChangeExtension(typeof(OpenStaadWrapper).Assembly.Location, ".xml")).Descendants("member").ToArray();
            var contract = members.Single(m => (string)m.Attribute("name") == "M:StaadPro.Interop.Core.Adapters.Interfaces.IOSRoot." + operation + suffix);
            var implementation = members.Single(m => (string)m.Attribute("name") == "M:StaadPro.Interop.Core.Adapters.Models.OSRootAdapter." + operation + suffix);
            foreach (string element in new[] { "summary", "returns", "remarks", "exception", "example" }) Assert.That(contract.Element(element), Is.Not.Null);
            Assert.That(contract.Elements().Select(e => e.ToString()), Is.EqualTo(implementation.Elements().Select(e => e.ToString())));
            Assert.That(contract.Descendants("include"), Is.Empty); Assert.That(contract.Descendants("inheritdoc"), Is.Empty);
            var method = typeof(IOSRoot).GetMethod(operation);
            if (suffix.Length != 0)
            {
                string parameter = operation == "IsTarget" ? "staadFileFullPath" : "fileFullPath";
                Assert.That(method.GetParameters().Single().Name, Is.EqualTo(parameter));
                Assert.That(contract.Element("param").Attribute("name").Value, Is.EqualTo(parameter));
            }
            else Assert.That(method.GetParameters(), Is.Empty);
        }
    }
}

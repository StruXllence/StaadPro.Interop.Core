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
    public sealed class OSRootModelEditTests
    {
        private static readonly string[] Operations = ["length", "force", "job", "jobObject", "update"];
        public sealed class NativeRoot : MetadataRoot
        {
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public int LengthCode = 4, ForceCode = 5;
            public string[] JobFields;
            public Exception Failure;
            public Action<string> OnCall;
            private void Step(string operation)
            {
                Calls.Add(operation); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(operation);
                if (Failure != null) throw Failure;
            }
            public void SetInputUnitForLength(object code)
            {
                Assert.That(code, Is.TypeOf<int>());
                LengthCode = (int)code; LengthText = ""; LengthReturn = LengthCode;
                Step("length");
            }
            public void SetInputUnitForForce(object code)
            {
                Assert.That(code, Is.TypeOf<int>());
                ForceCode = (int)code; ForceText = ""; ForceReturn = ForceCode;
                Step("force");
            }
            public void SetShortJobInfo(object name, object client, object engineer)
            {
                Assert.That(new[] { name, client, engineer }, Is.All.TypeOf<string>());
                JobFields = [(string)name, (string)client, (string)engineer];
                Step("job");
            }
            public void UpdateStructure() => Step("update");
        }

        private static void Request(IOSRoot root, string operation)
        {
            switch (operation)
            {
                case "length": root.SetInputUnitForLength(LengthInputUnit.MilliMeter); break;
                case "force": root.SetInputUnitForForce(ForceInputUnit.Newton); break;
                case "job": root.SetShortJobInfo("name", "client", "engineer"); break;
                case "jobObject": root.SetShortJobInfo(ShortJobInfo.From("name", "client", "engineer")); break;
                case "update": root.UpdateStructure(); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        [TestCaseSource(nameof(Operations))]
        public void EachOperationUsesOnlyItsMatchingCapturedNativeMemberOnTheOwnerThread(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; object geometry = wrapper.RawGeometry; native.Queries.Clear();
            Request(root, operation);
            Assert.That(native.Calls, Is.EqualTo(new[] { operation == "jobObject" ? "job" : operation }));
            Assert.That(native.Threads, Is.EqualTo(new[] { Environment.CurrentManagedThreadId }));
            Assert.That(native.Queries, Is.Empty); Assert.That(wrapper.Root, Is.SameAs(root));
            Assert.That((object)wrapper.RawGeometry, Is.SameAs(geometry));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void EveryDocumentedLengthCodeIncludingFeetAliasIsSentUnchangedWithoutTouchingForce(int code)
        {
            var native = new NativeRoot { ForceCode = 7 }; using var wrapper = new OpenStaadWrapper(native);
            native.Queries.Clear(); wrapper.Root.SetInputUnitForLength((LengthInputUnit)code);
            Assert.That(native.LengthCode, Is.EqualTo(code)); Assert.That(native.ForceCode, Is.EqualTo(7));
            Assert.That(native.Calls, Is.EqualTo(new[] { "length" })); Assert.That(native.Queries, Is.Empty);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void EveryForceUnitIsSentUnchangedWithoutTouchingLength(int code)
        {
            var native = new NativeRoot { LengthCode = 7 }; using var wrapper = new OpenStaadWrapper(native);
            native.Queries.Clear(); wrapper.Root.SetInputUnitForForce((ForceInputUnit)code);
            Assert.That(native.ForceCode, Is.EqualTo(code)); Assert.That(native.LengthCode, Is.EqualTo(7));
            Assert.That(native.Calls, Is.EqualTo(new[] { "force" })); Assert.That(native.Queries, Is.Empty);
        }

        [TestCase(true, -1)] [TestCase(true, 8)] [TestCase(true, int.MinValue)] [TestCase(true, int.MaxValue)]
        [TestCase(false, -1)] [TestCase(false, 8)] [TestCase(false, int.MinValue)] [TestCase(false, int.MaxValue)]
        public void InvalidUnitCodesFailBeforeAnyNativeWork(bool length, int code)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); native.Queries.Clear();
            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                if (length) wrapper.Root.SetInputUnitForLength((LengthInputUnit)code);
                else wrapper.Root.SetInputUnitForForce((ForceInputUnit)code);
            });
            Assert.That(error.ParamName, Is.EqualTo(length ? "lengthUnit" : "forceUnit"));
            Assert.That(native.Calls, Is.Empty); Assert.That(native.Queries, Is.Empty);
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [Test]
        public void IndividualFeetAliasDoesNotBroadenTheExistingCombinedSetter()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            wrapper.Root.SetInputUnitForLength((LengthInputUnit)1);
            Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Root.SetInputUnitsEx((LengthInputUnit)1, ForceInputUnit.Newton));
            Assert.That(native.Calls, Is.EqualTo(new[] { "length" }));
        }

        [Test]
        public void UnitsStayCachedUntilExplicitRefreshAndTheRootBindingIsRetained()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; native.Queries.Clear();
            root.SetInputUnitForLength((LengthInputUnit)1); root.SetInputUnitForForce(ForceInputUnit.Newton);
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Meter));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.KiloNewton));
            Assert.That(native.Queries, Is.Empty); wrapper.RefreshUnits();
            Assert.That(root.GetInputUnitForLength(), Is.EqualTo(LengthInputUnit.Feet));
            Assert.That(root.GetInputUnitForForce(), Is.EqualTo(ForceInputUnit.Newton));
            Assert.That(wrapper.Root, Is.SameAs(root));
            Assert.That(native.Queries, Is.EqualTo(new[] { "base", "force", "length" }));
        }

        [TestCase("JOB", "CLIENT", "ENGINEER")]
        [TestCase("", "", "")]
        [TestCase("  \u96ea job  ", "Client\r\nName", " Engineer ")]
        [TestCase("\t", " ", "\u03b1\u03b2\u03b3")]
        public void BothJobOverloadsPreserveExactTextAndNativeArgumentOrder(string name, string client, string engineer)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); native.Queries.Clear();
            wrapper.Root.SetShortJobInfo(name, client, engineer);
            Assert.That(native.JobFields, Is.EqualTo(new[] { name, client, engineer }));
            wrapper.Root.SetShortJobInfo(ShortJobInfo.From(name, client, engineer));
            Assert.That(native.JobFields, Is.EqualTo(new[] { name, client, engineer }));
            Assert.That(native.Calls, Is.EqualTo(new[] { "job", "job" })); Assert.That(native.Queries, Is.Empty);
        }

        [TestCase("jobName")] [TestCase("jobClient")] [TestCase("engineerName")]
        public void NullTextNamesTheExactPublicArgumentAndNeverPartiallyDispatches(string parameter)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var error = Assert.Throws<ArgumentNullException>(() => wrapper.Root.SetShortJobInfo(
                parameter == "jobName" ? null : "name", parameter == "jobClient" ? null : "client", parameter == "engineerName" ? null : "engineer"));
            Assert.That(error.ParamName, Is.EqualTo(parameter)); Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void JobObjectIsValidatedAndSnapshottedBeforeOneNativeCall()
        {
            var record = ShortJobInfo.From("name", "client", "engineer");
            var native = new NativeRoot { OnCall = _ => { record.Name = "later"; record.Client = "later"; record.Originator = "later"; } };
            using var wrapper = new OpenStaadWrapper(native);
            wrapper.Root.SetShortJobInfo(record);
            Assert.That(native.JobFields, Is.EqualTo(new[] { "name", "client", "engineer" }));
            Assert.That(native.Calls, Is.EqualTo(new[] { "job" })); Assert.That(record.Name, Is.EqualTo("later"));
        }

        [Test]
        public void ApplyingJobInfoDoesNotEditTheObjectOrSubmitAdditionalFullJobFields()
        {
            var record = new FullJobInfo { Name = "", Client = "", Originator = "", Number = "not submitted", Comments = "not submitted" };
            int notifications = 0; record.PropertyChanged += (_, _) => notifications++;
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            wrapper.Root.SetShortJobInfo(record);
            Assert.That(native.JobFields, Is.EqualTo(new[] { "", "", "" })); Assert.That(notifications, Is.Zero);
            Assert.That(record.Number, Is.EqualTo("not submitted")); Assert.That(record.Comments, Is.EqualTo("not submitted"));
        }

        [Test]
        public void NullJobObjectFailsBeforeNativeAccess()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentNullException>(() => wrapper.Root.SetShortJobInfo((ShortJobInfo)null)).ParamName, Is.EqualTo("jobInfo"));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase("Name")] [TestCase("Client")] [TestCase("Originator")]
        public void EachUninitializedJobFieldFailsAsJobInfoBeforeDispatch(string property)
        {
            var record = ShortJobInfo.From("name", "client", "engineer"); typeof(ShortJobInfo).GetProperty(property).SetValue(record, null);
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentException>(() => wrapper.Root.SetShortJobInfo(record)).ParamName, Is.EqualTo("jobInfo"));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeErrorsPropagateAfterPossibleEffectsWithoutRetryAndReleaseTheLease(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var failure = new COMException("native edit response failed", unchecked((int)0x80010108)); native.Failure = failure;
            Assert.That(Assert.Throws<COMException>(() => Request(wrapper.Root, operation)), Is.SameAs(failure));
            Assert.That(native.Calls.Count, Is.EqualTo(1));
            if (operation == "length") Assert.That(native.LengthCode, Is.EqualTo(5));
            if (operation == "force") Assert.That(native.ForceCode, Is.EqualTo(4));
            native.Failure = null; Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingMembersPropagateInsteadOfSubstitutingAnotherNativeRequest(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new MetadataRoot());
            Assert.Throws<RuntimeBinderException>(() => Request(wrapper.Root, operation));
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void DisposedStaleAndManualNoncurrentAdaptersFailBeforeNativeAccess(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; var manual = new OSRootAdapter(wrapper);
            Assert.Throws<InvalidOperationException>(() => Request(manual, operation));
            wrapper.RefreshComObjects(); Assert.Throws<InvalidOperationException>(() => Request(root, operation));
            var current = wrapper.Root; wrapper.Dispose(); Assert.Throws<ObjectDisposedException>(() => Request(current, operation));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void OtherThreadsAreRejectedWithoutNativeWork(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); Exception failure = null;
            var worker = new Thread(() => { try { Request(wrapper.Root, operation); } catch (Exception e) { failure = e; } });
            worker.Start(); Assert.That(worker.Join(5000), Is.True); Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void OneLeaseRejectsNestedRootRefreshAndDisposalWork(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native); int callbacks = 0;
            native.OnCall = _ =>
            {
                callbacks++;
                foreach (string nested in Operations) Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root, nested));
                Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetShortJobInfo());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Request(wrapper.Root, operation); Assert.That(callbacks, Is.EqualTo(1));
            native.OnCall = null; Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void PublicContractsAndMatchingExpandedIntelliSenseAreComplete(string operation)
        {
            string name = operation == "length" ? "SetInputUnitForLength" : operation == "force" ? "SetInputUnitForForce" : operation.StartsWith("job") ? "SetShortJobInfo" : "UpdateStructure";
            Type[] types = operation == "length" ? [typeof(LengthInputUnit)] : operation == "force" ? [typeof(ForceInputUnit)] : operation == "job" ? [typeof(string), typeof(string), typeof(string)] : operation == "jobObject" ? [typeof(ShortJobInfo)] : [];
            string[] names = operation == "length" ? ["lengthUnit"] : operation == "force" ? ["forceUnit"] : operation == "job" ? ["jobName", "jobClient", "engineerName"] : operation == "jobObject" ? ["jobInfo"] : [];
            foreach (Type owner in new[] { typeof(IOSRoot), typeof(OSRootAdapter) })
            {
                var method = owner.GetMethod(name, types); Assert.That(method, Is.Not.Null);
                Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
                Assert.That(method.GetParameters().Select(p => p.Name), Is.EqualTo(names));
                Assert.That(method.GetParameters().Any(p => p.IsOptional || p.IsOut || p.ParameterType.IsByRef), Is.False);
            }
            string suffix = name + (types.Length == 0 ? "" : "(" + string.Join(",", types.Select(t => t.FullName)) + ")");
            var docs = XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location, ".xml"));
            var contract = docs.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(IOSRoot).FullName + "." + suffix);
            var concrete = docs.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + typeof(OSRootAdapter).FullName + "." + suffix);
            Assert.That(contract.Elements().Select(e => e.ToString()), Is.EqualTo(concrete.Elements().Select(e => e.ToString())));
            foreach (string tag in new[] { "summary", "remarks", "exception", "example" }) Assert.That(contract.Element(tag)?.Value, Is.Not.Null.And.Not.Empty);
            Assert.That(contract.Elements("param").Select(e => (string)e.Attribute("name")), Is.EqualTo(names));
            Assert.That(contract.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"), Is.False);
        }
    }
}

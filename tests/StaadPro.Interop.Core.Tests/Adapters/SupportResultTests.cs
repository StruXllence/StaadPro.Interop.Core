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
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class SupportResultTests
    {
        private static readonly string[] Operations = ["reaction", "force", "reactionTask", "forceTask", "staticTask"];

        public sealed class Native
        {
            public object Status = true;
            public object Loads = new[] { 11d, -12d, 13d, -14d, 15d, -16d };
            public object Reactions = new[] { 1d, -2d, 3d, -4d, 5d, -6d };
            public bool Write = true, WriteLoads = true, WriteReactions = true, InPlace;
            public Action Callback;
            public Exception Failure;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];

            private void Step(string name)
            {
                Calls.Add(name); Threads.Add(Environment.CurrentManagedThreadId);
                Callback?.Invoke(); if (Failure != null) throw Failure;
            }
            private static void Check(object buffer)
            {
                Assert.That(buffer, Is.TypeOf<double[]>());
                Assert.That((double[])buffer, Has.Length.EqualTo(6));
                Assert.That(((double[])buffer).All(double.IsNaN), Is.True);
            }
            public object GetSupportReactions(int node, int load, ref object values)
            {
                Check(values); Step($"support:{node}:{load}");
                if (Write)
                {
                    if (InPlace) Array.Copy((double[])Reactions, (double[])values, 6);
                    else values = Reactions;
                }
                return Status;
            }
            public object GetStaticCheckResult(int load, ref object forces, ref object reactions)
            {
                Check(forces); Check(reactions); Assert.That(forces, Is.Not.SameAs(reactions));
                Step($"static:{load}");
                if (Write && WriteLoads)
                {
                    if (InPlace) Array.Copy((double[])Loads, (double[])forces, 6);
                    else forces = Loads;
                }
                if (Write && WriteReactions)
                {
                    if (InPlace) Array.Copy((double[])Reactions, (double[])reactions, 6);
                    else reactions = Reactions;
                }
                return Status;
            }
        }

        private static OpenStaadWrapper Wrap(object output) => new(new NativeRoot { Output = output });
        private static object Invoke(IOSOutput output, string operation, int node = 2, int load = 1) => operation switch
        {
            "reaction" => output.GetSupportReactions(node, load),
            "force" => output.GetSupportForces(node, load),
            "reactionTask" => output.GetSupportReactionsAsync(node, load).GetAwaiter().GetResult(),
            "forceTask" => output.GetSupportForcesAsync(node, load).GetAwaiter().GetResult(),
            "staticTask" => output.GetStaticCheckResultAsync(load).GetAwaiter().GetResult(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        private static Task Start(IOSOutput output, string operation) => operation switch
        {
            "reactionTask" => output.GetSupportReactionsAsync(2, 1),
            "forceTask" => output.GetSupportForcesAsync(2, 1),
            "staticTask" => output.GetStaticCheckResultAsync(1),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        private static double[] Values(Forces f) => [f.Fx, f.Fy, f.Fz, f.Mx, f.My, f.Mz];
        private static IEnumerable<TestCaseData> BadStatuses()
        {
            foreach (string op in Operations)
                foreach (object flag in new object[] { null, false, 0, -1, 2, "1", 1d, 1m, DayOfWeek.Monday, ulong.MaxValue })
                    yield return new TestCaseData(op, flag);
        }
        private static IEnumerable<TestCaseData> BadBuffers()
        {
            foreach (string op in Operations)
                foreach (object buffer in new object[] { null, 0d, Array.Empty<double>(), new double[5], new double[7],
                    new double[2, 3], new int[6], new object[] { 1d, 2d, 3d, 4d, 5d, 6d },
                    new[] { 1d, 2d, 3d, 4d, 5d, double.NaN }, new[] { 1d, 2d, 3d, 4d, 5d, double.PositiveInfinity },
                    new[] { double.NegativeInfinity, 2d, 3d, 4d, 5d, 6d } })
                    yield return new TestCaseData(op, buffer);
        }

        [TestCaseSource(nameof(Operations))]
        public void QualifiedStatusIssuesOneCallWithExactIdsSignsAndOwningThread(string op)
        {
            foreach (object status in new object[] { true, (sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL })
            {
                var native = new Native { Status = status }; using var wrapper = Wrap(native);
                object result = Invoke(wrapper.Output, op);
                Assert.That(native.Calls, Is.EqualTo(new[] { op == "staticTask" ? "static:1" : "support:2:1" }));
                Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
                if (result is StaticCheckResult check)
                {
                    Assert.That(check.LoadCaseId, Is.EqualTo(1));
                    Assert.That(Values(check.Forces), Is.EqualTo(native.Loads));
                    Assert.That(Values(check.Reactions), Is.EqualTo(native.Reactions));
                    Assert.That(Values(check.Differentials), Is.EqualTo(new[] { 10d, -10d, 10d, -10d, 10d, -10d }));
                }
                else
                {
                    var support = (LoadCaseForces)result;
                    Assert.That(support.Id, Is.EqualTo(1));
                    double sign = op.StartsWith("force", StringComparison.Ordinal) ? -1 : 1;
                    Assert.That(Values(support.Forces), Is.EqualTo(((double[])native.Reactions).Select(v => sign * v)));
                }
            }
        }

        [TestCaseSource(nameof(BadStatuses))]
        public void UnsuccessfulOrMalformedStatusNeverReturnsDefaultForces(string op, object status)
        {
            var native = new Native { Status = status, Loads = null, Reactions = null }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
            Assert.That(native.Calls, Has.Count.EqualTo(1));
            native.Status = true; native.Loads = native.Reactions = new double[6];
            Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }

        [TestCaseSource(nameof(BadBuffers))]
        public void SuccessfulReactionsMustBeExactSixFiniteDoubles(string op, object buffer)
        {
            var native = new Native { Reactions = buffer }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeWritesIntoOriginalBuffersAreAccepted(string op)
        {
            var native = new Native { InPlace = true }; using var wrapper = Wrap(native);
            object result = Invoke(wrapper.Output, op);
            Forces values = result is StaticCheckResult check ? check.Reactions : ((LoadCaseForces)result).Forces;
            Assert.That(values.Mz, Is.EqualTo(op.StartsWith("force", StringComparison.Ordinal) ? 6 : -6));
        }

        [TestCaseSource(nameof(Operations))]
        public void UnwrittenBuffersAreRejectedAndLeaseIsReleased(string op)
        {
            var native = new Native { Write = false }; using var wrapper = Wrap(native);
            Assert.Throws<InvalidOperationException>(() => Invoke(wrapper.Output, op));
            native.Write = true; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }

        [TestCase(false, true)] [TestCase(true, false)]
        public void BothStaticArraysMustBeWritten(bool loads, bool reactions)
        {
            var native = new Native { WriteLoads = loads, WriteReactions = reactions }; using var wrapper = Wrap(native);
            var task = wrapper.Output.GetStaticCheckResultAsync(1);
            Assert.That(task.IsFaulted, Is.True);
            Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult());
        }

        [Test]
        public void MalformedStaticLoadsCannotEscapeWithValidReactions()
        {
            foreach (object value in new object[] { null, new int[6], new double[5], new double[2, 3], new[] { 1d, 2d, 3d, 4d, 5d, double.NaN } })
            {
                var native = new Native { Loads = value }; using var wrapper = Wrap(native);
                Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetStaticCheckResultAsync(1).GetAwaiter().GetResult());
            }
        }

        [TestCaseSource(nameof(Operations))]
        public void ReplacementArraysWithNegativeLowerBoundsAreCopiedWithoutRetainingStorage(string op)
        {
            Array array = Array.CreateInstance(typeof(double), [6], [-4]);
            for (int i = 0; i < 6; i++) array.SetValue(i - 3d, -4 + i);
            var native = new Native { Reactions = array, Loads = array }; using var wrapper = Wrap(native);
            object first = Invoke(wrapper.Output, op); object second = Invoke(wrapper.Output, op);
            array.SetValue(900d, -4);
            Forces a = first is StaticCheckResult check ? check.Forces : ((LoadCaseForces)first).Forces;
            Forces b = second is StaticCheckResult other ? other.Forces : ((LoadCaseForces)second).Forces;
            Assert.That(a.Fx, Is.EqualTo(op.StartsWith("force", StringComparison.Ordinal) ? 3 : -3));
            Assert.That(a, Is.Not.SameAs(b)); a.Fx = 55;
            Assert.That(b.Fx, Is.Not.EqualTo(55));
            if (first is StaticCheckResult stat)
            {
                Assert.That(stat.Forces, Is.Not.SameAs(stat.Reactions));
                Assert.That(stat.Reactions.Fx, Is.EqualTo(-3));
            }
        }

        [TestCaseSource(nameof(Operations))]
        public void AllWrittenZerosAndExtremeFiniteSignsRemainValid(string op)
        {
            var native = new Native { Loads = new double[6], Reactions = new double[6] }; using var wrapper = Wrap(native);
            Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
            native.Reactions = new[] { double.MaxValue, -double.MaxValue, 0d, -0d, 1d, -1d };
            Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }

        [TestCaseSource(nameof(Operations))]
        public void InvalidCaseAndSupportIdsFailBeforeNativeDispatch(string op)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            foreach (int id in new[] { 0, -1, int.MinValue })
            {
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Invoke(wrapper.Output, op, 2, id)).ParamName, Is.EqualTo("loadCaseId"));
                if (op != "staticTask")
                    Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Invoke(wrapper.Output, op, id)).ParamName, Is.EqualTo("supportId"));
            }
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCase("reactionTask")] [TestCase("forceTask")] [TestCase("staticTask")]
        public void TaskApiPerformsNativeWorkBeforeReturningAndCapturesFailures(string op)
        {
            var native = new Native(); using var wrapper = Wrap(native);
            Task success = Start(wrapper.Output, op);
            Assert.That(success.IsCompletedSuccessfully, Is.True); Assert.That(native.Calls, Has.Count.EqualTo(1));
            var failure = new COMException("native", unchecked((int)0x80004005)); native.Failure = failure;
            Task fault = null; Assert.DoesNotThrow(() => fault = Start(wrapper.Output, op));
            Assert.That(fault.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => fault.GetAwaiter().GetResult()), Is.SameAs(failure));
        }

        [TestCaseSource(nameof(Operations))]
        public void NativeFailuresAndMissingSignaturesPreserveFailureAndReleaseLease(string op)
        {
            var failure = new COMException("original", unchecked((int)0x80004005)); var native = new Native { Failure = failure };
            using var wrapper = Wrap(native);
            Assert.That(Assert.Throws<COMException>(() => Invoke(wrapper.Output, op)), Is.SameAs(failure));
            native.Failure = null; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
            using var missing = Wrap(new object()); Assert.Throws<RuntimeBinderException>(() => Invoke(missing.Output, op));
        }

        [TestCaseSource(nameof(Operations))]
        public void LifetimeBindingAndThreadGuardsRejectCallsWithoutNativeWork(string op)
        {
            var native = new Native(); var wrapper = Wrap(native); var stale = wrapper.Output;
            var manual = new OSOutputAdapter(wrapper);
            Assert.Throws<InvalidOperationException>(() => Invoke(manual, op));
            wrapper.RefreshComObjects(); Assert.Throws<InvalidOperationException>(() => Invoke(stale, op));
            Exception otherThread = Task.Run(() => { try { Invoke(wrapper.Output, op); return null; } catch (Exception error) { return error; } }).GetAwaiter().GetResult();
            Assert.That(otherThread, Is.TypeOf<InvalidOperationException>());
            var current = wrapper.Output; wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Invoke(current, op)); Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void SharedLeaseRejectsReentrantCallsAndRecoversAfterCallbackFailure(string op)
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
            native.Callback = () => throw new ApplicationException("callback");
            Assert.Throws<ApplicationException>(() => Invoke(wrapper.Output, op));
            native.Callback = null; Assert.DoesNotThrow(() => Invoke(wrapper.Output, op));
        }

        [Test]
        public void ScalarModelsPreserveEditableValuesNotificationsAndLiteralDifferences()
        {
            var f = new Forces(1, 2, 3, 4, 5, 6); var names = new List<string>();
            f.PropertyChanged += (_, e) => names.Add(e.PropertyName); f.Scale(-1);
            Assert.That(names, Is.EqualTo(new[] { "Fx", "Fy", "Fz", "Mx", "My", "Mz" })); f.Fx = -1; Assert.That(names, Has.Count.EqualTo(6));
            var check = new StaticCheckResult(0) { Forces = new Forces(1, 2, 3, 4, 5, 6), Reactions = new Forces(-1, -2, -3, -4, -5, -6) };
            Assert.That(Values(check.Differentials), Is.EqualTo(new[] { 2d, 4d, 6d, 8d, 10d, 12d }));
            Assert.That(check.Differentials, Is.Not.SameAs(check.Differentials));
            check.Forces.Fx = double.MaxValue; check.Reactions.Fx = -double.MaxValue;
            Assert.That(check.Differentials.Fx, Is.EqualTo(double.PositiveInfinity));
            check.Forces = null; Assert.Throws<InvalidOperationException>(() => _ = check.Differentials);
            Assert.Throws<ArgumentNullException>(() => Forces.ParseFromArray(null));
            Assert.Throws<ArgumentException>(() => Forces.ParseFromArray(new double[7]));
            double[] source = [1, 2, 3, 4, 5, 6]; var copy = Forces.ParseFromArray(source); source[0] = 90; Assert.That(copy.Fx, Is.EqualTo(1));
            var draft = new LoadCaseForces(); Assert.That(draft.Id, Is.Zero); Assert.That(draft.Forces, Is.Null);
        }

        [Test]
        public void ModelNotificationsPropagateAfterWritesWithoutInventingRollback()
        {
            var f = new Forces(1, 2, 3, 4, 5, 6); var failure = new ApplicationException("handler");
            f.PropertyChanged += (_, _) => throw failure;
            Assert.That(Assert.Throws<ApplicationException>(() => f.Scale(-1)), Is.SameAs(failure));
            Assert.That(f.Fx, Is.EqualTo(-1)); Assert.That(f.Fy, Is.EqualTo(2));
            var check = new StaticCheckResult(1); check.PropertyChanged += (_, _) => throw failure;
            Assert.That(Assert.Throws<ApplicationException>(() => check.Forces = f), Is.SameAs(failure)); Assert.That(check.Forces, Is.SameAs(f));
        }

        [Test]
        public void InterfaceAndAdapterExposeExactSourceParametersAndMatchingCompiledXml()
        {
            var xml = XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location, ".xml"));
            foreach (string name in new[] { "GetSupportReactions", "GetSupportForces", "GetSupportReactionsAsync", "GetSupportForcesAsync", "GetStaticCheckResultAsync" })
            {
                var method = typeof(IOSOutput).GetMethod(name); var parameters = method.GetParameters();
                Assert.That(parameters.Select(p => p.Name), Is.EqualTo(name == "GetStaticCheckResultAsync" ? new[] { "loadCaseId" } : new[] { "supportId", "loadCaseId" }));
                Assert.That(parameters.All(p => p.ParameterType == typeof(int) && !p.IsOptional), Is.True);
                string suffix = "." + name + "(" + string.Join(",", parameters.Select(p => p.ParameterType.FullName)) + ")";
                var contract = xml.Descendants("member").Single(x => (string)x.Attribute("name") == "M:" + typeof(IOSOutput).FullName + suffix);
                var adapter = xml.Descendants("member").Single(x => (string)x.Attribute("name") == "M:" + typeof(OSOutputAdapter).FullName + suffix);
                Assert.That(string.Concat(contract.Elements()), Is.EqualTo(string.Concat(adapter.Elements())));
                foreach (string element in new[] { "summary", "returns", "remarks", "exception", "example" }) Assert.That(contract.Elements(element), Is.Not.Empty);
                Assert.That(contract.ToString(), Does.Not.Contain("<include").And.Not.Contain("<inheritdoc"));
            }
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Models
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class OpenStaadWrapperLifetimeTests
    {
        public sealed class NativeRoot
        {
            public object Geometry { get; } = new object();
            public object Load { get; } = new object();
            public int Queries;
            public Action BeforeBaseUnit;
            public int GetBaseUnit() { Queries++; BeforeBaseUnit?.Invoke(); return 2; }
            public object GetInputUnitForForce(ref object value) { Queries++; value = "kN"; return null; }
            public object GetInputUnitForLength(ref object value) { Queries++; value = "m"; return null; }
        }

        private static void Invoke(OpenStaadWrapper wrapper, string operation)
        {
            if (operation == "RefreshUnits") wrapper.RefreshUnits();
            else if (operation == "RefreshComObjects") wrapper.RefreshComObjects();
            else if (operation == "Dispose") wrapper.Dispose();
            else throw new ArgumentException(nameof(operation));
        }

        private static Exception OnAnotherThread(Action action)
        {
            Exception observed = null;
            var thread = new Thread(() => { try { action(); } catch (Exception ex) { observed = ex; } }) { IsBackground = true };
            thread.Start();
            Assert.That(thread.Join(TimeSpan.FromSeconds(5)), Is.True, "Worker did not finish.");
            return observed;
        }

        [TestCase("RefreshUnits")]
        [TestCase("RefreshComObjects")]
        [TestCase("Dispose")]
        public void AnotherThreadCannotRefreshOrDetachTheWrapper(string operation)
        {
            var native = new NativeRoot();
            using (var wrapper = new OpenStaadWrapper(native))
            {
                int before = native.Queries;
                object geometry = wrapper.Geometry;
                Exception error = OnAnotherThread(() => Invoke(wrapper, operation));
                Assert.That(error, Is.TypeOf<InvalidOperationException>());
                Assert.That(native.Queries, Is.EqualTo(before), "No native call is permitted on the wrong thread.");
                Assert.That(wrapper.RawOpenStaad, Is.SameAs(native));
                Assert.That(wrapper.Geometry, Is.SameAs(geometry));
                Assert.That(wrapper.IsConnected, Is.True);
                wrapper.RefreshUnits();
                Assert.That(native.Queries, Is.GreaterThan(before), "Owner remains able to use the wrapper.");
            }
        }

        [TestCase("RefreshUnits", "RefreshUnits")]
        [TestCase("RefreshUnits", "RefreshComObjects")]
        [TestCase("RefreshUnits", "Dispose")]
        [TestCase("RefreshComObjects", "RefreshUnits")]
        [TestCase("RefreshComObjects", "RefreshComObjects")]
        [TestCase("RefreshComObjects", "Dispose")]
        public void NativeCallbackCannotReenterRefreshOrDispose(string outerOperation, string nestedOperation)
        {
            var native = new NativeRoot();
            using (var wrapper = new OpenStaadWrapper(native))
            {
                native.BeforeBaseUnit = () => Invoke(wrapper, nestedOperation);
                Assert.Throws<InvalidOperationException>(() => Invoke(wrapper, outerOperation));
                Assert.That(wrapper.RawOpenStaad, Is.SameAs(native), "Rejected disposal must not detach the live root.");
                Assert.That(wrapper.IsConnected, Is.True);
                native.BeforeBaseUnit = null;
                Assert.DoesNotThrow(() => wrapper.RefreshUnits(), "A failed refresh must release its operation guard.");
            }
        }

        [Test]
        public void NativeFailureKeepsItsExceptionAndDoesNotPermanentlyBlockTheWrapper()
        {
            var native = new NativeRoot();
            using (var wrapper = new OpenStaadWrapper(native))
            {
                var failure = new COMException("Disconnected during refresh", unchecked((int)0x80010108));
                native.BeforeBaseUnit = () => throw failure;
                Assert.That(Assert.Throws<COMException>(() => wrapper.RefreshUnits()), Is.SameAs(failure));
                native.BeforeBaseUnit = null;
                Assert.DoesNotThrow(() => wrapper.RefreshComObjects());
            }
        }

        [Test]
        public void RepeatedCompletedDisposalIsHarmlessFromAnotherThread()
        {
            var wrapper = new OpenStaadWrapper(new NativeRoot());
            wrapper.Dispose();
            Assert.That(OnAnotherThread(wrapper.Dispose), Is.Null);
            Assert.That(wrapper.IsConnected, Is.False);
        }
    }
}

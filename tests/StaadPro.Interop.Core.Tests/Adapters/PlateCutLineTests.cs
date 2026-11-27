using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCutLineTests
    {
        private static readonly CartesianVector3 Start = new(1, 2, 3);
        private static readonly CartesianVector3 End = new(4, 5, 6);

        private static Plate PlateWithId(int id) => new(null, null, null) { Id = id };
        private static OpenStaadWrapper Create(PlateCutLineNative native) =>
            new(new PlateCornerHydrationRoot(new PlateCornerHydrationNative()) { Output = native });

        [TestCase(true)]
        [TestCase(false)]
        public void CapturesCutArgumentsAndDuplicatePlateIdsOnce(bool useGlobal)
        {
            var native = new PlateCutLineNative();
            using var wrapper = Create(native);
            var plate = PlateWithId(17);
            int enumerations = 0;
            bool disposed = false;
            IEnumerable<Plate> Plates()
            {
                enumerations++;
                try { yield return plate; yield return null; yield return PlateWithId(6); yield return PlateWithId(17); }
                finally { disposed = true; }
            }
            native.Callback = () => plate.Id = 40;

            var result = wrapper.Output.QueryPlateCutLineResultant(Plates(), 8, Start, End, useGlobal);

            Assert.Multiple(() =>
            {
                Assert.That(native.Calls, Is.EqualTo(1));
                Assert.That(enumerations, Is.EqualTo(1));
                Assert.That(disposed, Is.True);
                Assert.That(result.PlateIds, Is.EqualTo(new[] { 17, 6 }));
                Assert.That(native.Plates, Is.EqualTo(new[] { 17, 6 }));
                Assert.That(native.Case, Is.EqualTo(new[] { 8 }));
                Assert.That(native.Start, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(native.End, Is.EqualTo(new[] { 4, 5, 6 }));
                Assert.That(result.StartPoint, Is.EqualTo(Start));
                Assert.That(result.EndPoint, Is.EqualTo(End));
                Assert.That(result.LoadCaseId, Is.EqualTo(8));
                Assert.That(native.GlobalFlag, Is.EqualTo(useGlobal ? 1 : 0));
                Assert.That(result.UsesGlobalAxes, Is.EqualTo(useGlobal));
                Assert.That(result.IsSuccessful, Is.True);
            });
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CopiesSixDoubleValuesWithNonzeroNativeLowerBounds(int rank)
        {
            var values = new[] { 1d, -2d, 3d, -4d, 5d, -6d };
            var array = rank == 1 ? Array.CreateInstance(typeof(double), [6], [-5]) :
                Array.CreateInstance(typeof(double), [1, 6], [3, -5]);
            for (int i = 0; i < values.Length; i++)
            {
                if (rank == 1) array.SetValue(values[i], -5 + i);
                else array.SetValue(values[i], 3, -5 + i);
            }
            var native = new PlateCutLineNative { Buffer = array, Status = 1 };
            using var wrapper = Create(native);

            var result = wrapper.Output.GetPlateCutLineResultant([PlateWithId(1)], 8, Start, End, false);
            if (rank == 1) array.SetValue(100d, -5); else array.SetValue(100d, 3, -5);

            Assert.That(new[] { result.Fx, result.Fy, result.Fz, result.Mx, result.My, result.Mz }, Is.EqualTo(values));
        }

        [TestCase(false)]
        [TestCase(0)]
        public void NativeRejectionDiscardsOutputAndRequiredVariantsFail(object status)
        {
            var native = new PlateCutLineNative { Status = status, Buffer = "ignored" };
            using var wrapper = Create(native);
            Plate[] plates = [PlateWithId(1)];
            var query = wrapper.Output.QueryPlateCutLineResultant(plates, 8, Start, End, true);
            Assert.That(query.IsSuccessful, Is.False);
            Assert.That(query.Forces, Is.Null);
            Assert.That(query.PlateIds, Is.EqualTo(new[] { 1 }));
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetPlateCutLineResultant(plates, 8, Start, End, true));
            var task = wrapper.Output.GetPlateCutLineResultantAsync(plates, 8, Start, End, true);
            Assert.That(task.IsFaulted, Is.True);
            Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult());
            Assert.That(native.Calls, Is.EqualTo(3));
        }

        [Test]
        public void ChecksCallStatusAndRejectsMalformedOrUnwrittenSuccess()
        {
            var native = new PlateCutLineNative();
            using var wrapper = Create(native);
            void Read() => wrapper.Output.QueryPlateCutLineResultant([PlateWithId(1)], 8, Start, End, true);
            foreach (object malformed in new object[] { "true", null, 2 })
            {
                native.Status = malformed;
                Assert.Throws<InvalidOperationException>(Read);
            }
            native.Status = true;
            foreach (object malformed in new object[] { new double[2, 6], new double[6, 1], new double[5],
                new object[6], null, new double[] { 0, 0, 0, 0, 0, double.PositiveInfinity } })
            {
                native.Buffer = malformed;
                Assert.Throws<InvalidOperationException>(Read);
            }
            native.WriteResult = false;
            Assert.Throws<InvalidOperationException>(Read);
        }

        [Test]
        public void InvalidArgumentsNeverDispatchAndOverLimitEnumerationIsDisposed()
        {
            var native = new PlateCutLineNative();
            using var wrapper = Create(native);
            var output = wrapper.Output;
            Plate[] plates = [PlateWithId(1)];
            Assert.Throws<ArgumentNullException>(() => output.QueryPlateCutLineResultant(null, 8, Start, End, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => output.QueryPlateCutLineResultant(plates, 0, Start, End, true));
            Assert.Throws<ArgumentException>(() => output.QueryPlateCutLineResultant([], 8, Start, End, true));
            Assert.Throws<ArgumentException>(() => output.QueryPlateCutLineResultant([PlateWithId(0)], 8, Start, End, true));
            Assert.Throws<ArgumentException>(() => output.QueryPlateCutLineResultant(plates, 8, Start, Start, true));
            Assert.Throws<ArgumentException>(() => output.QueryPlateCutLineResultant(plates, 8, Start, new(double.NaN, 2, 3), true));

            bool disposed = false;
            IEnumerable<Plate> TooMany()
            {
                try { foreach (var plate in Enumerable.Repeat(plates[0], 100_001)) yield return plate; }
                finally { disposed = true; }
            }
            Assert.Throws<ArgumentException>(() => output.QueryPlateCutLineResultant(TooMany(), 8, Start, End, true));
            Assert.That(disposed, Is.True);
            Assert.That(native.Calls, Is.Zero);
        }

        [Test]
        public void TaskUsesOwningThreadAndPreservesTransportFailureAndBindingGuards()
        {
            var error = new COMException("Dispatch failed", unchecked((int)0x800706BA));
            var native = new PlateCutLineNative { Failure = error };
            using var wrapper = Create(native);
            var output = wrapper.Output;
            Plate[] plates = [PlateWithId(1)];
            native.Callback = () => Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());

            var failed = output.GetPlateCutLineResultantAsync(plates, 8, Start, End, false);
            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => failed.GetAwaiter().GetResult()), Is.SameAs(error));
            Assert.That(native.Thread, Is.EqualTo(Environment.CurrentManagedThreadId));
            native.Failure = null;

            var available = output.GetPlateCutLineResultantAsync(plates, 8, Start, End, false);
            Assert.That(available.IsCompletedSuccessfully, Is.True);
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => output.GetPlateCutLineResultant(plates, 8, Start, End, false));
            Assert.That(native.Calls, Is.EqualTo(2));
        }
    }
}

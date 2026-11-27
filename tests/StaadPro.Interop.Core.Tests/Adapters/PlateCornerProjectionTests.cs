using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class PlateCornerProjectionTests
    {
        private static Plate CreatePlate() =>
            new(new Node(11, 0, 0, 0), new Node(12, 2, 0, 0), new Node(13, 0, 0, 1)) { Id = 17 };

        private static double[] Components(MemberForces value) =>
            [value.Fx, value.Fy, value.Fz, value.Mx, value.My, value.Mz];

        [TestCase(true)]
        [TestCase(1)]
        public void GlobalResultCopiesReportedComponentsAndKeepsTheQueriedKey(object status)
        {
            var native = new PlateCornerHydrationNative { ForceStatus = status };
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = CreatePlate();

            var result = wrapper.Output.GetPlateCornerForcesGlobal(plate, new Node { Id = 11 }, 8);

            Assert.That(native.Calls, Is.EqualTo(new[] { "force:17:11:8" }));
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Key, Is.EqualTo(new PlateCornerForcesKey(8, 17, 11)));
            Assert.That(result.Forces.CornerNode, Is.SameAs(plate.A));
            Assert.That(result.Forces.LoadCase.Title, Is.Null);
            var data = result.Forces;
            Assert.That(new[] { data.Fx, data.Fy, data.Fz, data.Mx, data.My, data.Mz },
                Is.EqualTo(new[] { 1d, -2d, 3d, -4d, 5d, -6d }));
            ((double[])native.ForceValues)[0] = 90;
            plate.Id = 23;
            Assert.That(data.Fx, Is.EqualTo(1));
            Assert.That(result.Key.PlateId, Is.EqualTo(17));
        }

        [TestCase(false)]
        [TestCase(0)]
        public void GlobalResultKeepsTheRequestAndDiscardsEvenAWrittenBufferOnNativeFailure(object status)
        {
            var native = new PlateCornerHydrationNative { ForceStatus = status, ForceValues = "ignored" };
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = CreatePlate();

            var result = wrapper.Output.GetPlateCornerForcesGlobal(plate, plate.A, 8);

            Assert.That(result.Key, Is.EqualTo(new PlateCornerForcesKey(8, 17, 11)));
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Forces, Is.Null);
            Assert.That(native.Calls, Has.Count.EqualTo(1));
        }

        [Test]
        public void GlobalResultPropagatesInvalidInputsAndMalformedSuccessWithoutReturningFailureData()
        {
            var native = new PlateCornerHydrationNative();
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = CreatePlate();
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(
                () => wrapper.Output.GetPlateCornerForcesGlobal(plate, plate.A, 0)).ParamName, Is.EqualTo("loadCaseId"));
            Assert.Throws<ArgumentException>(() => wrapper.Output.GetPlateCornerForcesGlobal(plate, new Node { Id = 99 }, 1));
            Assert.That(native.Calls, Is.Empty);

            native.WriteForces = false;
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetPlateCornerForcesGlobal(plate, plate.A, 1));
            native.ForceStatus = 2;
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetPlateCornerForcesGlobal(plate, plate.A, 1));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ProjectionReadsOnePacketAndUsesTheCapturedFrameAndCase(bool beam, bool task)
        {
            var native = new PlateCornerHydrationNative();
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = CreatePlate();
            var load = new LoadCase(8, "Captured");
            var other = new Node(14, 0, 0, 2);
            var member = new Beam(94, plate.B, plate.A) { BetaAngle = 90 };
            var corner = new Node(11, 900, 900, 900);
            native.Callback = _ =>
            {
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshUnits());
                load.Id = 81;
                load.Title = "Changed";
                member.BetaAngle = 0;
                other.X = 600;
                plate.A.X = 900;
            };

            var projected = Read(wrapper.Output, load, plate, corner, other, member, beam, task);

            Assert.That(Components(projected), Is.EqualTo(beam
                ? new[] { -1d, -3d, 2d, 4d, 6d, -5d }
                : new[] { 3d, -2d, -1d, -6d, 5d, 4d }).Within(1e-12));
            Assert.That(projected.BeamId, Is.EqualTo(beam ? 94 : 0));
            Assert.That(projected.End, Is.EqualTo(beam ? MemberEnd.End : MemberEnd.Start));
            Assert.That(projected.LoadCaseId, Is.EqualTo(8));
            Assert.That(projected.LoadCaseTitle, Is.EqualTo("Captured"));
            Assert.That(native.Calls, Is.EqualTo(new[] { "force:17:11:8" }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [Test]
        public void SparseInputsHydrateTheCanonicalCornerBeforeProjecting()
        {
            var native = new PlateCornerHydrationNative();
            native.Coordinates = id => id == 11 ? [0d, 0d, 0d] : [(double)id, 0d, 0d];
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = new Plate(null, null, null) { Id = 17 };
            var node = new Node { Id = 11 };

            var result = wrapper.Output.GetPlateCornerForces(new LoadCase(8), plate, node, new Node(14, 0, 0, 2));

            Assert.That(result.Fx, Is.EqualTo(3));
            Assert.That(native.Calls, Is.EqualTo(new[]
                { "incidence:17", "coordinate:11", "coordinate:12", "coordinate:13", "force:17:11:8" }));
            Assert.That(plate.A, Is.Null);
        }

        [Test]
        public void InvalidFrameAndStaleBindingFailWithoutForceDispatch()
        {
            var native = new PlateCornerHydrationNative();
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = CreatePlate();
            var load = new LoadCase(8);
            Assert.Throws<ArgumentException>(() => wrapper.Output.GetPlateCornerForces(load, plate, plate.A, plate.A));
            var badMember = new Beam(94, plate.B, plate.C);
            var invalid = wrapper.Output.GetPlateCornerForcesAsync(load, plate, plate.A, badMember);
            Assert.That(invalid.IsFaulted, Is.True);
            Assert.Throws<ArgumentException>(() => invalid.GetAwaiter().GetResult());
            var previous = wrapper.Output;
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => previous.GetPlateCornerForcesGlobal(plate, plate.A, 8));
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void ProjectionTaskRetainsOriginalTransportErrorAndReleasesTheLease()
        {
            var error = new COMException("Transport failure", unchecked((int)0x800706BA));
            var native = new PlateCornerHydrationNative { Failure = error, FailureAt = "force:" };
            using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
            var plate = CreatePlate();
            var load = new LoadCase(8);

            var failed = wrapper.Output.GetPlateCornerForcesAsync(load, plate, plate.A, plate.B);

            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => failed.GetAwaiter().GetResult()), Is.SameAs(error));
            native.Failure = null;
            native.ForceStatus = false;
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetPlateCornerForces(load, plate, plate.A, plate.B));
            native.ForceStatus = true;
            Assert.That(wrapper.Output.GetPlateCornerForces(load, plate, plate.A, plate.B).Fx, Is.EqualTo(1));
        }

        private static MemberForces Read(IOSOutput output, LoadCase load, Plate plate, Node corner, Node other,
            Beam member, bool beam, bool task)
        {
            if (!task)
                return beam ? output.GetPlateCornerForces(load, plate, corner, member) :
                    output.GetPlateCornerForces(load, plate, corner, other);
            Task<MemberForces> result = beam ? output.GetPlateCornerForcesAsync(load, plate, corner, member) :
                output.GetPlateCornerForcesAsync(load, plate, corner, other);
            Assert.That(result.IsCompletedSuccessfully, Is.True);
            return result.GetAwaiter().GetResult();
        }
    }
}

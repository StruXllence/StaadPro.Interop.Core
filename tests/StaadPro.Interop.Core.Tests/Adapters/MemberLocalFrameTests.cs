using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class MemberLocalFrameTests
    {
        private const double Precision = 1e-12;

        private static PlateCornerForces Corner(Node corner) =>
            new(new LoadCase(7, "Dead"), new Plate(corner, new Node { Id = 11 }, new Node { Id = 12 }) { Id = 23 },
                corner, 1, 2, 3, 4, 5, 6);

        private static double[] Components(MemberForces result) =>
            [result.Fx, result.Fy, result.Fz, result.Mx, result.My, result.Mz];

        private static MemberForces NodeTransform(IOSOutput output, PlateCornerForces forces, Node other, bool task)
        {
            if (!task) return output.TransformPlateCornerForcesToMemberLocal(forces, other);
            var completed = output.TransformPlateCornerForcesToMemberLocalAsync(forces, other);
            Assert.That(completed.IsCompletedSuccessfully, Is.True);
            return completed.GetAwaiter().GetResult();
        }

        private static MemberForces BeamTransform(IOSOutput output, PlateCornerForces forces, Beam member, bool task)
        {
            if (!task) return output.TransformPlateCornerForcesToMemberLocal(forces, member);
            var completed = output.TransformPlateCornerForcesToMemberLocalAsync(forces, member);
            Assert.That(completed.IsCompletedSuccessfully, Is.True);
            return completed.GetAwaiter().GetResult();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NodeOverloadsRotateForceAndMomentAboutTheCornerWithoutTranslatingEither(bool task)
        {
            // +global Z is local U, +global Y is V and -global X is W.
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var forces = Corner(new Node(3, 20, -30, 40));
            var result = NodeTransform(wrapper.Output, forces, new Node(4, 20, -30, 52), task);

            Assert.That(Components(result), Is.EqualTo(new[] { 3d, 2d, -1d, 6d, 5d, -4d }).Within(Precision));
            Assert.That(result.BeamId, Is.Zero);
            Assert.That(result.End, Is.EqualTo(MemberEnd.Start));
            Assert.That(result.LoadCaseId, Is.EqualTo(7));
            Assert.That(result.LoadCaseTitle, Is.EqualTo("Dead"));
            Assert.That(forces.Fx, Is.EqualTo(1));
            Assert.That(forces.Mx, Is.EqualTo(4));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void BeamOverloadsUseTheSameBetaRotatedStartToEndFrameAtEitherEnd(bool task, bool atEnd)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var member = new Beam(97, new Node(3, 10, 20, 30), new Node(4, 14, 20, 30)) { BetaAngle = 90 };
            // Match by positive ID even though this detached corner is a separate object with different coordinates.
            var forces = Corner(new Node(atEnd ? 4 : 3, -2, 5, 7));
            var result = BeamTransform(wrapper.Output, forces, member, task);

            Assert.That(Components(result), Is.EqualTo(new[] { 1d, 3d, -2d, 4d, 6d, -5d }).Within(Precision));
            Assert.That(result.End, Is.EqualTo(atEnd ? MemberEnd.End : MemberEnd.Start));
            Assert.That(result.BeamId, Is.EqualTo(97));
        }

        [Test]
        public void DraftCornerCanMatchEndpointByDistanceWhileDifferentPositiveIdsStayDistinct()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var member = new Beam(97, new Node(3, 10, 20, 30), new Node(4, 14, 20, 30));
            var draft = Corner(new Node(0, 14, 20, 30));
            var numbered = Corner(new Node(99, 14, 20, 30));

            var result = wrapper.Output.TransformPlateCornerForcesToMemberLocal(draft, member);
            Assert.That(result.End, Is.EqualTo(MemberEnd.End));
            Assert.That(Assert.Throws<ArgumentException>(() =>
                wrapper.Output.TransformPlateCornerForcesToMemberLocal(numbered, member)).ParamName, Is.EqualTo("member"));
        }

        [TestCase(1, -1)]
        [TestCase(-1, 1)]
        public void VerticalFrameHasTheSourceSignedVAxisAndPositiveGlobalZ(int direction, int expectedVx)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var frame = wrapper.Output.GetMemberLocalCoordinateSystem(
                new Beam(new Node(5, 2, 4, 8), new Node(6, 2, 4 + direction, 8)));

            AssertVector(frame.U, 0, direction, 0);
            AssertVector(frame.V, expectedVx, 0, 0);
            AssertVector(frame.W, 0, 0, 1);
        }

        [Test]
        public void NearVerticalFrameStaysOrthonormalAndOwnsItsOriginSnapshot()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var start = new Node(9, 2, 4, 8);
            var member = new Beam(start, new Node(10, 2.00001, 9, 8.00002)) { BetaAngle = -25 };
            var frame = wrapper.Output.GetMemberLocalCoordinateSystem(member);

            Assert.That(Dot(frame.U, frame.V), Is.EqualTo(0).Within(Precision));
            Assert.That(Dot(frame.V, frame.W), Is.EqualTo(0).Within(Precision));
            Assert.That(Dot(frame.W, frame.U), Is.EqualTo(0).Within(Precision));
            Assert.That(Dot(frame.U, frame.U), Is.EqualTo(1).Within(Precision));
            Assert.That(Dot(frame.V, frame.V), Is.EqualTo(1).Within(Precision));
            Assert.That(Dot(frame.W, frame.W), Is.EqualTo(1).Within(Precision));
            AssertVector(frame.W, frame.U.Y * frame.V.Z - frame.U.Z * frame.V.Y,
                frame.U.Z * frame.V.X - frame.U.X * frame.V.Z, frame.U.X * frame.V.Y - frame.U.Y * frame.V.X);
            Assert.That(frame.U.Y, Is.GreaterThan(0.999999999));
            Assert.That(frame.U.X, Is.GreaterThan(0));
            Assert.That(frame.U.Z, Is.GreaterThan(0));

            start.Id = 42;
            start.X = -17;
            member.BetaAngle = 90;
            Assert.That(frame.OriginNodeId, Is.EqualTo(9));
            AssertVector(frame.Origin, 2, 4, 8);
        }

        private static double Dot(CartesianVector3 a, CartesianVector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        private static void AssertVector(CartesianVector3 actual, double x, double y, double z) =>
            Assert.That(new[] { actual.X, actual.Y, actual.Z }, Is.EqualTo(new[] { x, y, z }).Within(Precision));

        [Test]
        public void FrameRejectsCoincidentNodesEvenWithDifferentIdsAndNonfiniteMetadata()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var start = new Node(3, 10, 20, 30);
            Assert.Catch<ArgumentException>(() =>
                wrapper.Output.GetMemberLocalCoordinateSystem(new Beam(start, new Node(4, 10, 20, 30))));
            Assert.Catch<ArgumentException>(() =>
                wrapper.Output.GetMemberLocalCoordinateSystem(new Beam(start, new Node(4, double.NaN, 20, 30))));
            Assert.Catch<ArgumentException>(() =>
                wrapper.Output.GetMemberLocalCoordinateSystem(new Beam(start, new Node(4, 14, 20, 30)) { BetaAngle = double.PositiveInfinity }));
        }

        [Test]
        public void TransformRejectsUnrelatedCornersAndNonfiniteActions()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var forces = Corner(new Node(3, 10, 20, 30));
            var other = new Node(4, 14, 20, 30);
            var plate = forces.Plate;
            forces.Plate = new Plate(new Node(13, 10, 20, 30), null, null);
            Assert.Catch<ArgumentException>(() => wrapper.Output.TransformPlateCornerForcesToMemberLocal(forces, other));
            forces.Plate = plate;
            forces.Mz = double.NaN;
            Assert.Catch<ArgumentException>(() => wrapper.Output.TransformPlateCornerForcesToMemberLocal(forces, other));
        }

        [Test]
        public void InvalidTaskArgumentsReturnSettledFaultsAndReleaseTheLease()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var forces = Corner(new Node(3, 10, 20, 30));
            var other = new Node(4, 14, 20, 30);
            var missing = wrapper.Output.TransformPlateCornerForcesToMemberLocalAsync(forces, (Node)null);
            Assert.That(missing.IsFaulted, Is.True);
            Assert.That(Assert.Throws<ArgumentNullException>(() => missing.GetAwaiter().GetResult()).ParamName, Is.EqualTo("otherMemberNode"));

            var invalid = wrapper.Output.TransformPlateCornerForcesToMemberLocalAsync(forces, new Beam(0, forces.CornerNode, other));
            Assert.That(invalid.IsFaulted, Is.True);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => invalid.GetAwaiter().GetResult()).ParamName, Is.EqualTo("member"));
            Assert.That(wrapper.Output.TransformPlateCornerForcesToMemberLocal(forces, other).Fx, Is.EqualTo(1).Within(Precision));
        }

        public sealed class AvailabilityCallback
        {
            public Action OnRead { get; set; }

            public bool AreResultsAvailable()
            {
                OnRead?.Invoke();
                return true;
            }
        }

        [Test]
        public void SharedOutputLeaseRejectsFramesAndTaskTransformsDuringAnExistingRead()
        {
            var native = new AvailabilityCallback();
            using var wrapper = new OpenStaadWrapper(new NativeRoot { Output = native });
            var forces = Corner(new Node(3, 10, 20, 30));
            var member = new Beam(7, forces.CornerNode, new Node(4, 14, 20, 30));
            native.OnRead = () =>
            {
                Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetMemberLocalCoordinateSystem(member));
                var task = wrapper.Output.TransformPlateCornerForcesToMemberLocalAsync(forces, member);
                Assert.That(task.IsFaulted, Is.True);
                Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult());
            };

            Assert.That(wrapper.Output.AreResultsAvailable(), Is.True);
            AssertVector(wrapper.Output.GetMemberLocalCoordinateSystem(member).U, 1, 0, 0);
        }

        [TestCase("node")]
        [TestCase("nodeTask")]
        [TestCase("beam")]
        [TestCase("beamTask")]
        [TestCase("frame")]
        public void DetachedOperationsStillUseTheCurrentOwningThreadBinding(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            var forces = Corner(new Node(3, 10, 20, 30));
            var member = new Beam(7, forces.CornerNode, new Node(4, 14, 20, 30));
            void Invoke(IOSOutput output)
            {
                switch (operation)
                {
                    case "node": output.TransformPlateCornerForcesToMemberLocal(forces, member.EndNode); break;
                    case "nodeTask": output.TransformPlateCornerForcesToMemberLocalAsync(forces, member.EndNode).GetAwaiter().GetResult(); break;
                    case "beam": output.TransformPlateCornerForcesToMemberLocal(forces, member); break;
                    case "beamTask": output.TransformPlateCornerForcesToMemberLocalAsync(forces, member).GetAwaiter().GetResult(); break;
                    case "frame": output.GetMemberLocalCoordinateSystem(member); break;
                    default: throw new AssertionException("Unknown operation.");
                }
            }

            var old = wrapper.Output;
            Assert.Throws<InvalidOperationException>(() => Invoke(new OSOutputAdapter(wrapper)));
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => Invoke(old));
            var current = wrapper.Output;
            var fault = Task.Run(() => { try { Invoke(current); return null; } catch (Exception error) { return error; } })
                .GetAwaiter().GetResult();
            Assert.That(fault, Is.TypeOf<InvalidOperationException>());
            Assert.DoesNotThrow(() => Invoke(current));
            wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Invoke(current));
        }
    }
}

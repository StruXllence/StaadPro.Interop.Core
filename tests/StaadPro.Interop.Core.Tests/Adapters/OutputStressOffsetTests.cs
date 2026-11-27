using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OutputStressOffsetTests
    {
        [TestCase(0)]
        [TestCase(9)]
        public void PointStressCopiesEveryNamedComponentAndRetainsTheRequest(int lowerBound)
        {
            var native = new OutputStressOffsetNative { StressStatus = 1 };
            var buffer = Array.CreateInstance(typeof(double), [32], [lowerBound]);
            for (int i = 0; i < 32; i++) buffer.SetValue(i + 0.25, i + lowerBound);
            native.StressValues = buffer;
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            var point = new Node(99, 1, 2, 3);
            var facing = new Node(0, 4, 5, 6);

            var result = wrapper.Output.GetPlateStressAtPoint(17, 3, point, facing);
            Assert.That(native.Calls, Is.EqualTo(new[] { "stress:17:3" }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(native.StressInput, Is.EqualTo(new[] { 1d, 2d, 3d }));
            Assert.That(native.FacingInput, Is.EqualTo(new[] { 4d, 5d, 6d }));
            Assert.That(result.PlateId, Is.EqualTo(17));
            Assert.That(result.LoadCaseId, Is.EqualTo(3));
            var names = new[] { "None", "MaxAbs", "TopMax", "TopMin", "TopTauMax", "BotMax", "BotMin", "BotTauMax", "MaxVM", "VMTopMax", "VMBotMax", "MaxTresca", "TopTresca", "BotTresca", "FX", "FY", "FXY", "MX", "MY", "MZ", "QX", "QY", "Global", "GlobalMembraneStresses", "GlobalShearStresses", "BasePres", "CombXTop", "CombYTop", "CombXYTop", "CombXBot", "CombYBot", "CombXYBot" };
            foreach (var index in Enumerable.Range(0, names.Length))
                Assert.That(typeof(PlateStressResult).GetProperty(names[index])!.GetValue(result), Is.EqualTo(index + 0.25));

            buffer.SetValue(300d, lowerBound + 2);
            result.Raw[2] = 901;
            point.X = 700;
            facing.Z = 800;
            Assert.That(result.TopMax, Is.EqualTo(2.25));
            Assert.That(result.StressPoint.X, Is.EqualTo(1));
            Assert.That(result.FacingPoint.Z, Is.EqualTo(6));
        }

        [Test]
        public void PointStressRejectsMalformedOrUnwrittenResults()
        {
            var native = new OutputStressOffsetNative();
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            var point = new Node(0, 1, 2, 3);
            var facing = new Node(0, 4, 5, 6);
            foreach (object invalid in new object[] { new float[32], new double[2, 16], new double[31],
                Enumerable.Range(0, 32).Select(i => i == 7 ? double.PositiveInfinity : i).ToArray() })
            {
                native.StressValues = invalid;
                Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetPlateStressAtPoint(17, 3, point, facing));
            }
            native.WriteStress = false;
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetPlateStressAtPoint(17, 3, point, facing));
            native.StressStatus = false;
            Assert.That(Assert.Throws<InvalidOperationException>(
                () => wrapper.Output.GetPlateStressAtPoint(17, 3, point, facing)).Message,
                Does.Contain("could not retrieve"));
        }

        [TestCase(false, MemberEnd.Start)]
        [TestCase(false, MemberEnd.End)]
        [TestCase(true, MemberEnd.Start)]
        [TestCase(true, MemberEnd.End)]
        public void BothOffsetPathsReadGlobalForceAndTransferAboutTheSelectedJoint(bool byId, MemberEnd end)
        {
            var native = new OutputStressOffsetNative();
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            var beam = new Beam(17, new Node(11, 4, 5, 6), new Node(12, 4, 5, 6));
            var jointNode = end == MemberEnd.Start ? beam.StartNode : beam.EndNode;
            int jointId = jointNode.Id;
            native.OnCall = call =>
            {
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                if (call.StartsWith("offset:")) jointNode.X = 900;
            };
            var result = byId ? wrapper.Output.GetMemberEndForcesConsideringOffsets("  Dead  ", 17, 3, end) :
                wrapper.Output.GetMemberEndForcesConsideringOffsets("  Dead  ", beam, 3, end);
            var expected = (byId ? new[] { "incidence:17", $"coordinate:{jointId}" } : Array.Empty<string>())
                .Concat(new[] { $"offset:17:{(int)end}", $"forces:17:{(int)end}:3:1" });
            Assert.That(native.Calls, Is.EqualTo(expected));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(result.PhysicalJoint.Id, Is.EqualTo(jointId));
            Assert.That(result.PhysicalJoint.X, Is.EqualTo(4));
            Assert.That(result.MemberFace.X, Is.EqualTo(5));
            Assert.That(result.MemberFace.Y, Is.EqualTo(7));
            Assert.That(result.MemberFace.Z, Is.EqualTo(9));
            Assert.That(result.MemberFaceForces.Mx, Is.EqualTo(13));
            Assert.That(result.PhysicalJointForces.Fy, Is.EqualTo(-7));
            Assert.That(result.PhysicalJointForces.Mx, Is.EqualTo(56));
            Assert.That(result.PhysicalJointForces.My, Is.EqualTo(21));
            Assert.That(result.PhysicalJointForces.Mz, Is.EqualTo(2));
            Assert.That(result.PhysicalJointForces.End, Is.EqualTo(end));
            Assert.That(result.PhysicalJointForces.LoadCaseTitle, Is.EqualTo("  Dead  "));
            result.PhysicalJoint.X = 100;
            Assert.That(result.PhysicalJoint.X, Is.EqualTo(4));
            result.MemberFaceForces.Fx = 99;
            Assert.That(result.PhysicalJointForces.Fx, Is.EqualTo(5));
        }

        [TestCase(0)]
        [TestCase(-6027)]
        public void AbsentOffsetDiffersFromAnExplicitZeroAndPreservesExistingMoments(int offsetStatus)
        {
            var native = new OutputStressOffsetNative { OffsetStatus = offsetStatus,
                Offset = offsetStatus == 0 ? [0d, 0d, 0d] : ["ignored", null, double.NaN] };
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            var result = wrapper.Output.GetMemberEndForcesConsideringOffsets("Dead", 17, 3, MemberEnd.End);
            Assert.That(result.OffsetNativeStatus, Is.EqualTo(offsetStatus));
            Assert.That(result.HasOffsetSpecification, Is.EqualTo(offsetStatus == 0));
            Assert.That(result.HasOffset, Is.False);
            Assert.That(result.MemberFace.Z, Is.EqualTo(result.PhysicalJoint.Z));
            Assert.That(result.PhysicalJointForces.Mx, Is.EqualTo(result.MemberFaceForces.Mx));
            Assert.That(result.PhysicalJointForces.My, Is.EqualTo(result.MemberFaceForces.My));
            Assert.That(result.PhysicalJointForces.Mz, Is.EqualTo(result.MemberFaceForces.Mz));
        }

        [TestCase(-3001)]
        [TestCase(-6026)]
        [TestCase(42)]
        public void UnsuccessfulOffsetStatusDoesNotTurnIntoZeroOrDispatchOutput(int status)
        {
            var native = new OutputStressOffsetNative { OffsetStatus = status };
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                wrapper.Output.GetMemberEndForcesAtPhysicalJoint("Dead", 17, 3, MemberEnd.End)).Message,
                Does.Contain(status.ToString()));
            Assert.That(native.Calls, Is.EqualTo(new[] { "incidence:17", "coordinate:12", "offset:17:1" }));
        }

        [Test]
        public void NativeFalseReturnsNullInBothJointOverloadsButTransportPropagates()
        {
            var native = new OutputStressOffsetNative { ForceStatus = 0, ForceValues = "unused" };
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            var beam = new Beam(17, new Node(11, 1, 2, 3), new Node(12, 4, 5, 6));
            Assert.That(wrapper.Output.GetMemberEndForcesAtPhysicalJoint("Dead", beam, 3, MemberEnd.End), Is.Null);
            Assert.That(wrapper.Output.GetMemberEndForcesAtPhysicalJoint("Dead", 17, 3, MemberEnd.End), Is.Null);
            var error = new COMException("Model disconnected", unchecked((int)0x80010108));
            native.OnCall = call => { if (call.StartsWith("forces:")) throw error; };
            Assert.That(Assert.Throws<COMException>(() =>
                wrapper.Output.GetMemberEndForcesAtPhysicalJoint("Dead", 17, 3, MemberEnd.End)), Is.SameAs(error));
        }

        [Test]
        public void InvalidArgumentsAndStaleBindingFailBeforeDispatch()
        {
            var native = new OutputStressOffsetNative();
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            var output = wrapper.Output;
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                output.GetMemberEndForcesAtPhysicalJoint("Dead", 0, 3, MemberEnd.Start)).ParamName, Is.EqualTo("beamId"));
            Assert.That(Assert.Throws<ArgumentException>(() =>
                output.GetPlateStressAtPoint(17, 3, new Node(0, 1, 2, 3), new Node(0, 0, double.NaN, 0))).ParamName,
                Is.EqualTo("facingPoint"));
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => output.GetMemberEndForcesConsideringOffsets("Dead", 17, 3, MemberEnd.Start));
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void IncompleteIncidenceCoordinatesAndOffsetFailBeforeForceRead()
        {
            var native = new OutputStressOffsetNative { StartId = int.MinValue };
            using var wrapper = OutputStressOffsetFixture.Wrap(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetMemberEndForcesConsideringOffsets("Dead", 17, 3, MemberEnd.Start));
            native.StartId = 11;
            native.Joint[0] = "4";
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetMemberEndForcesConsideringOffsets("Dead", 17, 3, MemberEnd.End));
            native.Joint[0] = 4d;
            native.Offset[0] = double.NaN;
            Assert.Throws<InvalidOperationException>(() => wrapper.Output.GetMemberEndForcesConsideringOffsets("Dead", 17, 3, MemberEnd.End));
            Assert.That(native.Calls.Any(call => call.StartsWith("forces:")), Is.False);
        }
    }
}

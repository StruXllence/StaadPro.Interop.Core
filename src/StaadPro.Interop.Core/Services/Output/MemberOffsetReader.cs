using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class MemberOffsetReader
    {
        // This is the documented Property code for absence at the requested end.
        private const int NoOffsetSpecification = -6027;

        internal static OffsetAwareMemberEndForces Read(OpenStaadWrapper wrapper, string loadCaseTitle,
            Beam beam, int loadCaseId, MemberEnd memberEnd)
        {
            ArgumentNullException.ThrowIfNull(beam);
            int beamId = beam.Id;
            Validate(loadCaseTitle, beamId, loadCaseId, memberEnd, nameof(beam));
            var joint = memberEnd == MemberEnd.Start ? beam.StartNode : beam.EndNode;
            if (joint is null || joint.Id <= 0)
                throw new ArgumentException("The requested member end must have a joint with a positive ID.", nameof(beam));
            int jointId = joint.Id;
            var position = new CartesianVector3(joint.X, joint.Y, joint.Z);
            if (!IsFinite(position))
                throw new ArgumentException("The supplied joint coordinates must be finite.", nameof(beam));
            return Transfer(wrapper, loadCaseTitle, beamId, loadCaseId, memberEnd, jointId, position);
        }

        internal static OffsetAwareMemberEndForces Read(OpenStaadWrapper wrapper, string loadCaseTitle,
            int beamId, int loadCaseId, MemberEnd memberEnd)
        {
            Validate(loadCaseTitle, beamId, loadCaseId, memberEnd, nameof(beamId));
            object geometry = wrapper.RawGeometry;
            if (geometry == null)
                throw new InvalidOperationException("Member incidence requires the current Geometry binding.");

            object start = int.MinValue, end = int.MinValue;
            object status = ((dynamic)geometry).GetMemberIncidence(beamId, ref start, ref end);
            int code = NativeValues.ReadInt32(status, "member-incidence status");
            if (code != 0)
                throw new InvalidOperationException($"OpenSTAAD GetMemberIncidence failed with status {code} for member {beamId}.");

            int startId = NativeValues.ReadInt32(start, "member start node ID");
            int endId = NativeValues.ReadInt32(end, "member end node ID");
            if (startId <= 0 || endId <= 0 || startId == endId)
                throw new InvalidOperationException("Native member incidence must supply two distinct positive node IDs.");

            int jointId = memberEnd == MemberEnd.Start ? startId : endId;
            object x = double.NaN, y = double.NaN, z = double.NaN;
            ((dynamic)geometry).GetNodeCoordinates(jointId, ref x, ref y, ref z);
            var position = ReadVector(x, y, z, "GetNodeCoordinates");
            return Transfer(wrapper, loadCaseTitle, beamId, loadCaseId, memberEnd, jointId, position);
        }

        private static OffsetAwareMemberEndForces Transfer(OpenStaadWrapper wrapper, string loadCaseTitle,
            int beamId, int loadCaseId, MemberEnd memberEnd, int jointId, CartesianVector3 joint)
        {
            object property = wrapper.RawProperty;
            if (property == null)
                throw new InvalidOperationException("Member offsets require the current Property binding.");

            object x = double.NaN, y = double.NaN, z = double.NaN;
            object status = ((dynamic)property).GetMemberGlobalOffSet(beamId, (int)memberEnd, ref x, ref y, ref z);
            int code = NativeValues.ReadInt32(status, "global member offset status");
            if (code != 0 && code != NoOffsetSpecification)
                throw new InvalidOperationException($"OpenSTAAD GetMemberGlobalOffSet failed with status {code} for member {beamId}.");

            var offset = code == 0 ? ReadVector(x, y, z, "GetMemberGlobalOffSet") : default;
            var face = new CartesianVector3(joint.X + offset.X, joint.Y + offset.Y, joint.Z + offset.Z);
            if (!IsFinite(face))
                throw new OverflowException("The member face lies outside the finite Double coordinate range.");

            var query = MemberForceReader.Read((object)wrapper.RawOutput, loadCaseTitle, beamId, loadCaseId, memberEnd, LocalOrGlobal.Global);
            if (!query.IsSuccessful)
                return null;

            var forces = query.Forces;
            // The lever arm runs from the target joint to the original face.
            var moment = new CartesianVector3(
                forces.Mx + (offset.Y * forces.Fz - offset.Z * forces.Fy),
                forces.My + (offset.Z * forces.Fx - offset.X * forces.Fz),
                forces.Mz + (offset.X * forces.Fy - offset.Y * forces.Fx));
            if (!IsFinite(moment))
                throw new OverflowException("The transferred member moment lies outside the finite Double range.");

            var atJoint = new MemberForces
            {
                BeamId = beamId,
                LoadCaseId = loadCaseId,
                LoadCaseTitle = loadCaseTitle,
                End = memberEnd,
                Fx = forces.Fx,
                Fy = forces.Fy,
                Fz = forces.Fz,
                Mx = moment.X,
                My = moment.Y,
                Mz = moment.Z
            };
            return new OffsetAwareMemberEndForces(forces, atJoint, jointId, joint, face, offset, code);
        }

        private static void Validate(string loadCaseTitle, int beamId, int loadCaseId, MemberEnd memberEnd, string memberParameter)
        {
            ArgumentNullException.ThrowIfNull(loadCaseTitle);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(beamId, memberParameter);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(loadCaseId);
            if (!Enum.IsDefined(memberEnd))
                throw new ArgumentOutOfRangeException(nameof(memberEnd));
        }

        private static CartesianVector3 ReadVector(object x, object y, object z, string operation)
        {
            if (x is double dx && y is double dy && z is double dz &&
                double.IsFinite(dx) && double.IsFinite(dy) && double.IsFinite(dz))
                return new CartesianVector3(dx, dy, dz);

            throw new InvalidOperationException(operation + " must write three finite Double components.");
        }

        private static bool IsFinite(CartesianVector3 vector) =>
            double.IsFinite(vector.X) && double.IsFinite(vector.Y) && double.IsFinite(vector.Z);
    }
}

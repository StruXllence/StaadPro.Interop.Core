using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Contains one global member-face force couple and its transfer to the incidence joint.</summary>
    /// <remarks>All records are detached. The two editable force records are independent. Editing one does
    /// not recompute the other. The joint and face node properties return fresh copies of the captured points.</remarks>
    public sealed class OffsetAwareMemberEndForces
    {
        private readonly int jointId;
        private readonly CartesianVector3 joint;
        private readonly CartesianVector3 face;

        internal OffsetAwareMemberEndForces(MemberForces memberFaceForces, MemberForces physicalJointForces,
            int jointId, CartesianVector3 joint, CartesianVector3 face, CartesianVector3 globalOffset, int offsetNativeStatus)
        {
            MemberFaceForces = memberFaceForces;
            PhysicalJointForces = physicalJointForces;
            this.jointId = jointId;
            this.joint = joint;
            this.face = face;
            GlobalOffset = globalOffset;
            OffsetNativeStatus = offsetNativeStatus;
        }

        /// <summary>Gets the detached native force and moment components at the member face in global axes.</summary>
        public MemberForces MemberFaceForces { get; }

        /// <summary>Gets the detached force and moment components transferred to the incidence joint in global axes.</summary>
        public MemberForces PhysicalJointForces { get; }

        /// <summary>Gets a fresh copy of the physical joint with the captured incidence ID and coordinates.</summary>
        public Node PhysicalJoint => new(jointId, joint.X, joint.Y, joint.Z);

        /// <summary>Gets a fresh copy of the face point, with the joint ID retained for correlation.</summary>
        /// <remarks>The face point is computed from the joint and global offset. It does not identify a second native node.</remarks>
        public Node MemberFace => new(jointId, face.X, face.Y, face.Z);

        /// <summary>Gets the joint-to-face offset vector in global axes and native length units.</summary>
        public CartesianVector3 GlobalOffset { get; }

        /// <summary>Gets the native Property status. Zero indicates an offset was read. -6027 indicates no specification.</summary>
        public int OffsetNativeStatus { get; }

        /// <summary>Indicates whether an offset was reported, including an explicitly specified zero offset.</summary>
        public bool HasOffsetSpecification => OffsetNativeStatus == 0;

        /// <summary>Indicates that the captured offset length exceeds the source tolerance of 1e-9 native length units.</summary>
        public bool HasOffset => GlobalOffset.X * GlobalOffset.X + GlobalOffset.Y * GlobalOffset.Y +
            GlobalOffset.Z * GlobalOffset.Z > 1e-18;
    }
}

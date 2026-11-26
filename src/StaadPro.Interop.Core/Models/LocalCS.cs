namespace StaadPro.Interop.Core.Models
{
    /// <summary>Contains a captured member origin and its right-handed orthonormal U, V and W axes.</summary>
    /// <remarks>Axes and origin are expressed in global XYZ. Each vector is a value snapshot. Later node edits and beta changes leave this frame unchanged.</remarks>
    public sealed class LocalCS
    {
        internal LocalCS(int originNodeId, CartesianVector3 origin, CartesianVector3 u, CartesianVector3 v, CartesianVector3 w)
        {
            OriginNodeId = originNodeId;
            Origin = origin;
            U = u;
            V = v;
            W = w;
        }

        /// <summary>Gets the start-node ID captured when the frame was created.</summary>
        public int OriginNodeId { get; }
        /// <summary>Gets the captured global start-node position in the supplied coordinate units.</summary>
        public CartesianVector3 Origin { get; }
        /// <summary>Gets the local X unit direction from member start toward member end.</summary>
        public CartesianVector3 U { get; }
        /// <summary>Gets the local Y unit direction after applying the member beta angle.</summary>
        public CartesianVector3 V { get; }
        /// <summary>Gets the local Z unit direction given by the cross product of U and V, within floating-point precision.</summary>
        public CartesianVector3 W { get; }
    }
}

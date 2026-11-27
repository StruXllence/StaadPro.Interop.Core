namespace StaadPro.Interop.Core.Models.Output
{
    /// <summary>Contains the captured identifiers and qualified result of one plate-corner force query.</summary>
    /// <remarks>The optional force record is editable managed data and may retain supplied geometry references.
    /// Later edits do not change the captured key.</remarks>
    public sealed class PlateCornerForcesQueryResult
    {
        internal PlateCornerForcesQueryResult(PlateCornerForcesKey key, PlateCornerForces forces)
        {
            Key = key;
            Forces = forces;
        }

        /// <summary>Gets the captured case, plate and corner node identifiers sent to OpenSTAAD.</summary>
        /// <value>The positive numeric identifiers, preserved even when the native function returns false.</value>
        public PlateCornerForcesKey Key { get; }

        /// <summary>Indicates whether native retrieval succeeded and all six components were qualified.</summary>
        /// <value>True when <see cref="Forces"/> contains a complete record.</value>
        public bool IsSuccessful => Forces != null;

        /// <summary>Gets the qualified force and moment record.</summary>
        /// <value>Copied Fx/Fy/Fz/Mx/My/Mz with case and plate context on success, or null on native false.</value>
        public PlateCornerForces Forces { get; }
    }
}

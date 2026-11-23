using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Interfaces;

namespace StaadPro.Interop.Core.Base
{
    /// <summary>Stores source-compatible references shared by detached plate stress results.</summary>
    /// <remarks>References are retained without copying or native access. Later metadata edits can change the IDs
    /// visible through them without changing previously read stresses. Manual construction performs no validation.</remarks>
    public abstract class PlateStress : IPlateStress
    {
        /// <summary>Stores the supplied detached context references.</summary>
        /// <param name="loadCase">Caller-owned case metadata, stored unchanged, including null for manual records.</param>
        /// <param name="plate">Caller-owned plate metadata, stored unchanged, including null for manual records.</param>
        protected PlateStress(ILoadCase loadCase, Plate plate) { LoadCase = loadCase; Plate = plate; }
        /// <summary>Gets the supplied load-case metadata reference.</summary>
        /// <value>The retained reference; changing its fields does not update result components or native state.</value>
        public ILoadCase LoadCase { get; }
        /// <summary>Gets the supplied plate metadata reference.</summary>
        /// <value>The retained reference; its mutable Id need not remain equal to the ID captured by a prior query.</value>
        public Plate Plate { get; }
    }
}

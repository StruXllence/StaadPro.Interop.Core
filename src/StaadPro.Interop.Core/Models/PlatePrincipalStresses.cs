using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores four detached native in-plane normal principal stresses at plate-center surfaces.</summary>
    /// <remarks>Values retain native force/length-squared units, signs and top/bottom labels. The query copies named
    /// native outputs without sorting or recomputing principal values, directions or angles. Scalar setters and manual
    /// construction are unvalidated with no notifications, transformation, unit conversion or native effects.
    /// This scalar/context subset does not include the original unit-service API.</remarks>
    public class PlatePrincipalStresses : PlateStress
    {
        /// <summary>Creates a manual zero-component record retaining supplied context references.</summary>
        /// <param name="loadCase">Caller-owned case metadata stored unchanged, including manual null.</param>
        /// <param name="plate">Caller-owned plate metadata stored unchanged, including manual null.</param>
        public PlatePrincipalStresses(ILoadCase loadCase, Plate plate) : base(loadCase, plate) { }

        /// <summary>Gets or sets the native maximum in-plane normal principal stress at the top surface.</summary>
        /// <value>The SMAXTop output, with native sign/units retained; manual values are unvalidated.</value>
        public double SmaxTop { get; set; }
        /// <summary>Gets or sets the native minimum in-plane normal principal stress at the top surface.</summary>
        /// <value>The SMINTop output; not reordered relative to other components by managed code.</value>
        public double SminTop { get; set; }
        /// <summary>Gets or sets the native maximum in-plane normal principal stress at the bottom surface.</summary>
        /// <value>The SMAXBottom output, independently copied without a global-surface inference.</value>
        public double SmaxBottom { get; set; }
        /// <summary>Gets or sets the native minimum in-plane normal principal stress at the bottom surface.</summary>
        /// <value>The SMINBottom output; no principal angle or allowable-stress criterion is implied.</value>
        public double SminBottom { get; set; }
    }
}

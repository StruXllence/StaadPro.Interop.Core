using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores detached local plate-center bending and twisting moments per unit width.</summary>
    /// <remarks>Retains the scalar/context subset of the source model. Query values use native moment-per-width
    /// units, for example kN-m/m, with native signs and local-face conventions. No unit conversion, Wood-Armer
    /// design calculation, geometry transformation, UI dependency or engineering acceptance is introduced.
    /// Constructors/setters store unvalidated manual data without notifications or native changes.</remarks>
    public class PlateCenterMoments : PlateStress
    {
        /// <summary>Creates a manual zero-component result retaining supplied context references.</summary>
        /// <param name="loadCase">Caller-owned case metadata stored unchanged, including manual null.</param>
        /// <param name="plate">Caller-owned plate metadata stored unchanged, including manual null.</param>
        public PlateCenterMoments(ILoadCase loadCase, Plate plate) : base(loadCase, plate) { }

        /// <summary>Stores three literal detached components without validation or conversion.</summary>
        /// <param name="loadCase">Caller-owned case metadata retained by reference.</param>
        /// <param name="plate">Caller-owned plate metadata retained by reference.</param>
        /// <param name="mx">Moment per unit width about the local X face.</param>
        /// <param name="my">Moment per unit width about the local Y face.</param>
        /// <param name="mxy">Twisting moment per unit width in the local X-Y plane.</param>
        public PlateCenterMoments(ILoadCase loadCase, Plate plate, double mx, double my, double mxy)
            : this(loadCase, plate) { Mx = mx; My = my; Mxy = mxy; }

        /// <summary>Gets or sets moment per unit width about the local X face.</summary>
        /// <value>Native MX on qualified queries, preserving units/sign; manual values are unvalidated.</value>
        public double Mx { get; set; }
        /// <summary>Gets or sets moment per unit width about the local Y face.</summary>
        /// <value>Native MY, independently copied without conversion to a nodal or member-end moment.</value>
        public double My { get; set; }
        /// <summary>Gets or sets twisting moment per unit width in the local X-Y plane.</summary>
        /// <value>Native MXY, retaining native plate-local sign and width convention.</value>
        public double Mxy { get; set; }
    }
}

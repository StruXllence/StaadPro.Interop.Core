using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores five detached local plate-center shear and membrane stress components.</summary>
    /// <remarks>Despite the retained Forces method/type name, these are stresses in native force/length-squared units,
    /// not nodal forces or plate bending moments. Qualified queries copy all five finite outputs. Scalar setters and
    /// manual constructors preserve editable source semantics without validation, notifications, native changes,
    /// unit conversion or coordinate transformation. This ports the scalar/context subset of the original model.</remarks>
    public class PlateCenterForces : PlateStress
    {
        /// <summary>Creates a manual record with zero components and the supplied context references.</summary>
        /// <param name="loadCase">Caller-owned metadata retained unchanged; manual null is allowed.</param>
        /// <param name="plate">Caller-owned plate retained unchanged; manual null is allowed.</param>
        public PlateCenterForces(ILoadCase loadCase, Plate plate) : base(loadCase, plate) { }
        /// <summary>Stores five supplied components without validation or native access.</summary>
        /// <param name="loadCase">Caller-owned concrete case metadata retained unchanged.</param>
        /// <param name="plate">Caller-owned plate metadata retained unchanged.</param>
        /// <param name="sqx">Local-X-face shear stress in the local Z direction.</param>
        /// <param name="sqy">Local-Y-face shear stress in the local Z direction.</param>
        /// <param name="sx">Normal membrane stress along local X.</param>
        /// <param name="sy">Normal membrane stress along local Y.</param>
        /// <param name="sxy">In-plane local XY shear stress.</param>
        public PlateCenterForces(LoadCase loadCase, Plate plate, double sqx, double sqy, double sx, double sy, double sxy)
            : this(loadCase, plate) { Sqx = sqx; Sqy = sqy; Sx = sx; Sy = sy; Sxy = sxy; }
        /// <summary>Gets or sets shear stress on the local X face in the local Z direction.</summary>
        /// <value>Native SQX in force/length-squared units on a qualified query; manual values are unvalidated.</value>
        public double Sqx { get; set; }
        /// <summary>Gets or sets shear stress on the local Y face in the local Z direction.</summary>
        /// <value>Native SQY, with native local axes and sign retained.</value>
        public double Sqy { get; set; }
        /// <summary>Gets or sets normal membrane stress in local X.</summary>
        /// <value>Native SX, with no managed stress-to-force conversion.</value>
        public double Sx { get; set; }
        /// <summary>Gets or sets normal membrane stress in local Y.</summary>
        /// <value>Native SY, not a principal stress or global force.</value>
        public double Sy { get; set; }
        /// <summary>Gets or sets in-plane local XY shear stress.</summary>
        /// <value>Native SXY, not a twisting moment or resultant shear force.</value>
        public double Sxy { get; set; }
    }
}

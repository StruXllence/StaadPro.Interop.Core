using System;
using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores detached native von Mises stresses at the top and bottom plate-center surfaces.</summary>
    /// <remarks>Values retain native force/length-squared units and native surface labels; no principal direction,
    /// global top/bottom orientation, stress calculation, unit conversion or acceptance criterion is inferred.
    /// This is the scalar/context subset; manual constructors/setters are unvalidated, without notifications or native effects.</remarks>
    public sealed class PlateCenterVonMises : PlateStress
    {
        /// <summary>Creates a manual zero-component record retaining supplied context references.</summary>
        /// <param name="loadCase">Caller-owned case metadata stored unchanged, including manual null.</param>
        /// <param name="plate">Caller-owned plate metadata stored unchanged, including manual null.</param>
        public PlateCenterVonMises(ILoadCase loadCase, Plate plate) : base(loadCase, plate) { }

        /// <summary>Stores two literal detached stresses without validation or conversion.</summary>
        /// <param name="loadCase">Caller-owned concrete case metadata retained by reference.</param>
        /// <param name="plate">Caller-owned plate metadata retained by reference.</param>
        /// <param name="top">Native top-surface von Mises stress in force/length-squared units.</param>
        /// <param name="bottom">Native bottom-surface von Mises stress in the same units.</param>
        public PlateCenterVonMises(LoadCase loadCase, Plate plate, double top, double bottom)
            : this(loadCase, plate) { Top = top; Bottom = bottom; }

        /// <summary>Gets the larger absolute value of the current top and bottom components.</summary>
        /// <value>Math.Max(Math.Abs(Top), Math.Abs(Bottom)), recomputed on access; manual NaN/infinity propagates.</value>
        /// <remarks>Preserves the source scalar convenience; does not change either component or compare with an allowable stress.</remarks>
        public double AbsoluteMaximum => Math.Max(Math.Abs(Top), Math.Abs(Bottom));
        /// <summary>Gets or sets the plate-center top-surface von Mises stress.</summary>
        /// <value>The qualified native top output with its literal finite value preserved; manual values are unvalidated.</value>
        public double Top { get; set; }
        /// <summary>Gets or sets the plate-center bottom-surface von Mises stress.</summary>
        /// <value>The qualified native bottom output without reordering, sign normalization or unit conversion.</value>
        public double Bottom { get; set; }
    }
}

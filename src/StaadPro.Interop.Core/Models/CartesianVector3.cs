namespace StaadPro.Interop.Core.Models
{
    /// <summary>Holds three immutable Cartesian components in double precision.</summary>
    /// <remarks>Interpret the components according to the enclosing result. A member-frame origin uses model length units and its axes are dimensionless unit directions.</remarks>
    public readonly struct CartesianVector3
    {
        /// <summary>Stores three supplied double-precision components without validation or conversion.</summary>
        /// <param name="x">X component.</param>
        /// <param name="y">Y component.</param>
        /// <param name="z">Z component.</param>
        public CartesianVector3(double x, double y, double z) { X = x; Y = y; Z = z; }

        /// <summary>Gets the global X component.</summary>
        public double X { get; }
        /// <summary>Gets the global Y component.</summary>
        public double Y { get; }
        /// <summary>Gets the global Z component.</summary>
        public double Z { get; }
    }
}

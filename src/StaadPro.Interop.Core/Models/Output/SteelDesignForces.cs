namespace StaadPro.Interop.Core.Models.Output
{
    /// <summary>Contains the three critical-section force components actually returned by steel-design retrieval.</summary>
    /// <remarks>The native vector supplies Fx, My and Mz in current base units. Other force/moment components are
    /// unreported and are not synthesized as zero. Axes, signs and native code conventions are preserved without
    /// transformation. This named scalar model avoids UI/unit dependencies and does not implement the full source force API.</remarks>
    public sealed class SteelDesignForces
    {
        /// <summary>Stores three detached critical-section components without native access.</summary>
        /// <param name="fx">Axial component in native base force units; manual input is not validated.</param>
        /// <param name="my">First reported moment in native base force-times-length units.</param>
        /// <param name="mz">Second reported moment in native base force-times-length units.</param>
        public SteelDesignForces(double fx, double my, double mz) { Fx = fx; My = my; Mz = mz; }
        /// <summary>Gets the reported axial force.</summary>
        /// <value>The unchanged native Fx observation.</value>
        public double Fx { get; }
        /// <summary>Gets the first reported bending moment.</summary>
        /// <value>The unchanged native My observation.</value>
        public double My { get; }
        /// <summary>Gets the second reported bending moment.</summary>
        /// <value>The unchanged native Mz observation.</value>
        public double Mz { get; }
    }
}

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Contains the qualified 32-component result reported for one plate stress and facing point.</summary>
    /// <remarks>Components retain their native meanings, coordinate conventions and units. This packet mixes stress
    /// and resultant fields. Native category names are exposed as literal indexed values. Each Raw access returns
    /// a separate array. No unit conversion or stress recovery calculation runs in managed code.</remarks>
    public sealed class PlateStressResult
    {
        internal const int ComponentCount = 32;
        private readonly double[] values;

        internal PlateStressResult(int plateId, int loadCaseId, CartesianVector3 stressPoint,
            CartesianVector3 facingPoint, double[] values)
        {
            PlateId = plateId;
            LoadCaseId = loadCaseId;
            StressPoint = stressPoint;
            FacingPoint = facingPoint;
            this.values = values;
        }

        /// <summary>Gets the plate identifier used in the native request.</summary>
        public int PlateId { get; }

        /// <summary>Gets the case or combination identifier used in the native request.</summary>
        public int LoadCaseId { get; }

        /// <summary>Gets the coordinates of the captured stress evaluation point in global axes.</summary>
        public CartesianVector3 StressPoint { get; }

        /// <summary>Gets the captured facing point supplied to OpenSTAAD in global axes.</summary>
        public CartesianVector3 FacingPoint { get; }

        /// <summary>Gets an independent copy of the thirty-two reported Double components in native index order.</summary>
        public double[] Raw => (double[])values.Clone();

        /// <summary>Gets the native None slot. This index has no documented engineering component.</summary>
        /// <value>Reported component at zero-based index 0.</value>
        public double None => values[0];

        /// <summary>Gets the native MaxAbs stress value.</summary>
        /// <value>Reported component at zero-based index 1.</value>
        public double MaxAbs => values[1];

        /// <summary>Gets the reported maximum top-surface stress.</summary>
        /// <value>Reported component at zero-based index 2.</value>
        public double TopMax => values[2];

        /// <summary>Gets the reported minimum top-surface stress.</summary>
        /// <value>Reported component at zero-based index 3.</value>
        public double TopMin => values[3];

        /// <summary>Gets the reported top-surface maximum shear stress.</summary>
        /// <value>Reported component at zero-based index 4.</value>
        public double TopTauMax => values[4];

        /// <summary>Gets the reported maximum bottom-surface stress.</summary>
        /// <value>Reported component at zero-based index 5.</value>
        public double BotMax => values[5];

        /// <summary>Gets the reported minimum bottom-surface stress.</summary>
        /// <value>Reported component at zero-based index 6.</value>
        public double BotMin => values[6];

        /// <summary>Gets the reported bottom-surface maximum shear stress.</summary>
        /// <value>Reported component at zero-based index 7.</value>
        public double BotTauMax => values[7];

        /// <summary>Gets the native maximum von Mises value.</summary>
        /// <value>Reported component at zero-based index 8.</value>
        public double MaxVM => values[8];

        /// <summary>Gets the top-surface maximum von Mises value.</summary>
        /// <value>Reported component at zero-based index 9.</value>
        public double VMTopMax => values[9];

        /// <summary>Gets the bottom-surface maximum von Mises value.</summary>
        /// <value>Reported component at zero-based index 10.</value>
        public double VMBotMax => values[10];

        /// <summary>Gets the native maximum Tresca value.</summary>
        /// <value>Reported component at zero-based index 11.</value>
        public double MaxTresca => values[11];

        /// <summary>Gets the top-surface Tresca value.</summary>
        /// <value>Reported component at zero-based index 12.</value>
        public double TopTresca => values[12];

        /// <summary>Gets the bottom-surface Tresca value.</summary>
        /// <value>Reported component at zero-based index 13.</value>
        public double BotTresca => values[13];

        /// <summary>Gets the native FX entry.</summary>
        /// <value>Reported component at zero-based index 14.</value>
        public double FX => values[14];

        /// <summary>Gets the native FY entry.</summary>
        /// <value>Reported component at zero-based index 15.</value>
        public double FY => values[15];

        /// <summary>Gets the native FXY entry.</summary>
        /// <value>Reported component at zero-based index 16.</value>
        public double FXY => values[16];

        /// <summary>Gets the native MX entry.</summary>
        /// <value>Reported component at zero-based index 17.</value>
        public double MX => values[17];

        /// <summary>Gets the native MY entry.</summary>
        /// <value>Reported component at zero-based index 18.</value>
        public double MY => values[18];

        /// <summary>Gets the native MZ entry.</summary>
        /// <value>Reported component at zero-based index 19.</value>
        public double MZ => values[19];

        /// <summary>Gets the native QX entry.</summary>
        /// <value>Reported component at zero-based index 20.</value>
        public double QX => values[20];

        /// <summary>Gets the native QY entry.</summary>
        /// <value>Reported component at zero-based index 21.</value>
        public double QY => values[21];

        /// <summary>Gets the native Global slot.</summary>
        /// <value>Reported component at zero-based index 22.</value>
        public double Global => values[22];

        /// <summary>Gets the native GlobalMembraneStresses slot.</summary>
        /// <value>Reported component at zero-based index 23.</value>
        public double GlobalMembraneStresses => values[23];

        /// <summary>Gets the native GlobalShearStresses slot.</summary>
        /// <value>Reported component at zero-based index 24.</value>
        public double GlobalShearStresses => values[24];

        /// <summary>Gets the native BasePres slot.</summary>
        /// <value>Reported component at zero-based index 25.</value>
        public double BasePres => values[25];

        /// <summary>Gets the top-surface combined X stress entry.</summary>
        /// <value>Reported component at zero-based index 26.</value>
        public double CombXTop => values[26];

        /// <summary>Gets the top-surface combined Y stress entry.</summary>
        /// <value>Reported component at zero-based index 27.</value>
        public double CombYTop => values[27];

        /// <summary>Gets the top-surface combined XY stress entry.</summary>
        /// <value>Reported component at zero-based index 28.</value>
        public double CombXYTop => values[28];

        /// <summary>Gets the bottom-surface combined X stress entry.</summary>
        /// <value>Reported component at zero-based index 29.</value>
        public double CombXBot => values[29];

        /// <summary>Gets the bottom-surface combined Y stress entry.</summary>
        /// <value>Reported component at zero-based index 30.</value>
        public double CombYBot => values[30];

        /// <summary>Gets the bottom-surface combined XY stress entry.</summary>
        /// <value>Reported component at zero-based index 31.</value>
        public double CombXYBot => values[31];
    }
}

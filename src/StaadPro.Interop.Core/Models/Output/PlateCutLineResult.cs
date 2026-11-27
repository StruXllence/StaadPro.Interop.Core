using System.Collections.Generic;

namespace StaadPro.Interop.Core.Models.Output
{
    /// <summary>Contains the captured request and optional force and moment resultant for one plate cut line and load case.</summary>
    public sealed class PlateCutLineResult
    {
        internal PlateCutLineResult(List<int> plateIds, int loadCaseId, CartesianVector3 startPoint,
            CartesianVector3 endPoint, bool usesGlobalAxes, Forces forces)
        {
            PlateIds = plateIds.AsReadOnly();
            LoadCaseId = loadCaseId;
            StartPoint = startPoint;
            EndPoint = endPoint;
            UsesGlobalAxes = usesGlobalAxes;
            Forces = forces;
        }

        /// <summary>Gets the distinct plate IDs submitted to OpenSTAAD in first-occurrence order.</summary>
        public IReadOnlyList<int> PlateIds { get; }

        /// <summary>Gets the single positive load-case or combination ID submitted to OpenSTAAD.</summary>
        public int LoadCaseId { get; }

        /// <summary>Gets the captured global start coordinates in active base length units.</summary>
        public CartesianVector3 StartPoint { get; }

        /// <summary>Gets the captured global end coordinates in active base length units.</summary>
        public CartesianVector3 EndPoint { get; }

        /// <summary>Gets the requested output axes. True requests global XYZ and false requests native cut-line local axes.</summary>
        public bool UsesGlobalAxes { get; }

        /// <summary>Indicates that OpenSTAAD reported success and supplied all six finite components.</summary>
        public bool IsSuccessful => Forces != null;

        /// <summary>Gets the editable force and moment record in native units, or null when OpenSTAAD reports failure.</summary>
        public Forces Forces { get; }
    }
}

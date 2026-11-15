using System;
using System.Collections.Generic;

namespace StaadPro.Interop.Core.Models.Output
{
    /// <summary>Contains a qualified six-component native buckling-mode displacement observation.</summary>
    /// <remarks>Owns immutable query identity and read-only component storage. Values preserve native order, signs,
    /// axes, units and eigenmode normalization without reinterpretation. They are not a scalar displacement or a
    /// physical response to a load case. Consult the installed STAAD build's result conventions before engineering use.</remarks>
    public sealed class BucklingModeDisplacementResult
    {
        internal BucklingModeDisplacementResult(int bucklingMode, int nodeId, double[] components)
        {
            BucklingMode = bucklingMode; NodeId = nodeId;
            Components = Array.AsReadOnly((double[])components.Clone());
        }
        /// <summary>Gets the positive native buckling-mode number submitted to the query.</summary>
        /// <value>The captured mode identifier, independent of later model changes.</value>
        public int BucklingMode { get; }
        /// <summary>Gets the positive native node number submitted to the query.</summary>
        /// <value>The captured node identifier, independent of edits to a supplied Node record.</value>
        public int NodeId { get; }
        /// <summary>Gets all six written finite native displacement components.</summary>
        /// <value>Read-only owned storage in unchanged native array order, indexed zero through five.</value>
        /// <remarks>No component is discarded, scaled, transformed or assigned an inferred axis/normalization.</remarks>
        public IReadOnlyList<double> Components { get; }
    }
}

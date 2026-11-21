using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Output;
namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Contains detached steel-design metadata for one supplied analytical member.</summary>
    /// <remarks>Query factories qualify native status and every output before construction. Public constructor/setters
    /// preserve editable source data semantics without validation or native access. Member retains its supplied reference;
    /// queried case and force data are independently owned. Later edits do not recompute results or update STAAD.
    /// DesignForces deliberately uses a named three-component model instead of UI-dependent six-component IForces.</remarks>
    public class MemberSteelDesignResults
    {
        /// <summary>Stores detached steel-design metadata without validation or native dispatch.</summary>
        /// <param name="beam">Original supplied Beam reference; later edits remain visible without recomputing this result. Manual values are stored unchanged.</param>
        /// <param name="code">Literal native design-code text. Manual values are stored unchanged.</param>
        /// <param name="designStatus">Qualified PASS or FAIL enum; not inferred from utilization. Manual values are stored unchanged.</param>
        /// <param name="utilizationRatio">Finite nonnegative critical ratio on a qualified query. Manual values are stored unchanged.</param>
        /// <param name="allowableRatio">Finite nonnegative native allowable ratio. Manual values are stored unchanged.</param>
        /// <param name="criticalLoadCase">Independent captured case metadata, resolved by unique native case ID. Manual values are stored unchanged.</param>
        /// <param name="criticalSection">Finite nonnegative location from member start in native base length units. Manual values are stored unchanged.</param>
        /// <param name="criticalClause">Literal native governing clause text. Manual values are stored unchanged.</param>
        /// <param name="designSection">Literal native design-section text. Manual values are stored unchanged.</param>
        /// <param name="designForces">The three reported Fx/My/Mz components; no unreported components are invented. Manual values are stored unchanged.</param>
        /// <param name="slendernessRatio">Finite nonnegative native KL/r observation. Manual values are stored unchanged.</param>
        public MemberSteelDesignResults(Beam beam, string code, DesignStatus designStatus, double utilizationRatio, double allowableRatio, ILoadCase criticalLoadCase, double criticalSection, string criticalClause, string designSection, SteelDesignForces designForces, double slendernessRatio)
        {
            Member = beam;
            Code = code;
            DesignStatus = designStatus;
            UtilizationRatio = utilizationRatio;
            AllowableRatio = allowableRatio;
            CriticalLoadCase = criticalLoadCase;
            CriticalSection = criticalSection;
            CriticalClause = criticalClause;
            DesignSection = designSection;
            DesignForces = designForces;
            SlendernessRatio = slendernessRatio;
        }
        /// <summary>Gets or sets the detached Member value.</summary>
        /// <value>Original supplied Beam reference; later edits remain visible without recomputing this result. Edits do not update STAAD.</value>
        public Beam Member { get; set; }
        /// <summary>Gets or sets the detached Code value.</summary>
        /// <value>Literal native design-code text. Edits do not update STAAD.</value>
        public string Code { get; set; }
        /// <summary>Gets or sets the detached DesignStatus value.</summary>
        /// <value>Qualified PASS or FAIL enum; not inferred from utilization. Edits do not update STAAD.</value>
        public DesignStatus DesignStatus { get; set; }
        /// <summary>Gets or sets the detached UtilizationRatio value.</summary>
        /// <value>Finite nonnegative critical ratio on a qualified query. Edits do not update STAAD.</value>
        public double UtilizationRatio { get; set; }
        /// <summary>Gets or sets the detached AllowableRatio value.</summary>
        /// <value>Finite nonnegative native allowable ratio. Edits do not update STAAD.</value>
        public double AllowableRatio { get; set; }
        /// <summary>Gets or sets the detached CriticalLoadCase value.</summary>
        /// <value>Independent captured case metadata, resolved by unique native case ID. Edits do not update STAAD.</value>
        public ILoadCase CriticalLoadCase { get; set; }
        /// <summary>Gets or sets the detached CriticalSection value.</summary>
        /// <value>Finite nonnegative location from member start in native base length units. Edits do not update STAAD.</value>
        public double CriticalSection { get; set; }
        /// <summary>Gets or sets the detached CriticalClause value.</summary>
        /// <value>Literal native governing clause text. Edits do not update STAAD.</value>
        public string CriticalClause { get; set; }
        /// <summary>Gets or sets the detached DesignSection value.</summary>
        /// <value>Literal native design-section text. Edits do not update STAAD.</value>
        public string DesignSection { get; set; }
        /// <summary>Gets or sets the detached DesignForces value.</summary>
        /// <value>The three reported Fx/My/Mz components; no unreported components are invented. Edits do not update STAAD.</value>
        public SteelDesignForces DesignForces { get; set; }
        /// <summary>Gets or sets the detached SlendernessRatio value.</summary>
        /// <value>Finite nonnegative native KL/r observation. Edits do not update STAAD.</value>
        public double SlendernessRatio { get; set; }
    }
}

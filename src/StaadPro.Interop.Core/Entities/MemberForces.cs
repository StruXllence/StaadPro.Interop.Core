using StaadPro.Interop.Core.Enums;


namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Contains an editable six-component member-end force record.</summary>
    /// <remarks>Values preserve the enclosing query coordinate selector and native result units, without managed conversion or offset transfer.
    /// Editing this managed model does not update STAAD. This Core data model has no WPF vector, JSON, unit-conversion or equality dependency.
    /// Record identity is retained independently in each query result; reference equality is used.</remarks>
    public class MemberForces
    {
        /// <summary>Gets or sets native Fx; no unit conversion is applied.</summary>
        /// <value>The qualified force component in the enclosing query axes and native result units.</value>
        public double Fx { get; set; }
        /// <summary>Gets or sets native Fy; no unit conversion is applied.</summary>
        /// <value>The qualified force component in the enclosing query axes and native result units.</value>
        public double Fy { get; set; }
        /// <summary>Gets or sets native Fz; no unit conversion is applied.</summary>
        /// <value>The qualified force component in the enclosing query axes and native result units.</value>
        public double Fz { get; set; }
        /// <summary>Gets or sets native Mx; no unit conversion is applied.</summary>
        /// <value>The qualified moment component in the enclosing query axes and native result units.</value>
        public double Mx { get; set; }
        /// <summary>Gets or sets native My; no unit conversion is applied.</summary>
        /// <value>The qualified moment component in the enclosing query axes and native result units.</value>
        public double My { get; set; }
        /// <summary>Gets or sets native Mz; no unit conversion is applied.</summary>
        /// <value>The qualified moment component in the enclosing query axes and native result units.</value>
        public double Mz { get; set; }

        /// <summary>Gets or sets the native load-case identifier.</summary>
        /// <value>The load case associated with this record.</value>
        public int LoadCaseId { get; set; }
        /// <summary>Gets or sets the native load-case title.</summary>
        /// <value>Exact native text, including an explicitly empty title.</value>
        public string LoadCaseTitle { get; set; }
        /// <summary>Gets or sets the member identifier.</summary>
        /// <value>The native beam/member number.</value>
        public int BeamId { get; set; }
        /// <summary>Gets or sets the member end.</summary>
        /// <value>Start or End for a qualified native query.</value>
        public MemberEnd End { get; set; }
        /// <summary>Describes the member, end, load case and six force/moment components.</summary>
        /// <returns>A culture-formatted description using G6 component formatting and the optional load title.</returns>
        /// <remarks>Matches the source model's formatting and performs no native access or unit conversion.</remarks>
        /// <example><code>
        /// var forces = new StaadPro.Interop.Core.Entities.MemberForces { BeamId = 1, LoadCaseId = 2 };
        /// Console.WriteLine(forces.ToString());
        /// </code></example>
        public override string ToString() =>
            $"Member {BeamId}, End {End}, Load Case {LoadCaseId}" +
            (string.IsNullOrWhiteSpace(LoadCaseTitle) ? string.Empty : $" ({LoadCaseTitle})") +
            $": Fx={Fx:G6}, Fy={Fy:G6}, Fz={Fz:G6}, Mx={Mx:G6}, My={My:G6}, Mz={Mz:G6}";
    }
}

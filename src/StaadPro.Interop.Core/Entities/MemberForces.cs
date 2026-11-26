using StaadPro.Interop.Core.Enums;


namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Contains an editable six-component member-end force record.</summary>
    /// <remarks>Values use the enclosing operation's axes and retain the supplied force and moment units. Member-local
    /// transformations rotate the supplied vectors at their application point. They leave the moment reference point unchanged.
    /// Editing this managed model does not update STAAD. This Core data model has no WPF vector, JSON, unit-conversion or equality dependency.
    /// Record identity is retained independently in each query result; reference equality is used.</remarks>
    public class MemberForces
    {
        /// <summary>Gets or sets the force's X component.</summary>
        /// <value>The force component in the enclosing operation's axes and original force units.</value>
        public double Fx { get; set; }
        /// <summary>Gets or sets the force's Y component.</summary>
        /// <value>The force component in the enclosing operation's axes and original force units.</value>
        public double Fy { get; set; }
        /// <summary>Gets or sets the force's Z component.</summary>
        /// <value>The force component in the enclosing operation's axes and original force units.</value>
        public double Fz { get; set; }
        /// <summary>Gets or sets the moment's X component.</summary>
        /// <value>The moment component in the enclosing operation's axes and original moment units.</value>
        public double Mx { get; set; }
        /// <summary>Gets or sets the moment's Y component.</summary>
        /// <value>The moment component in the enclosing operation's axes and original moment units.</value>
        public double My { get; set; }
        /// <summary>Gets or sets the moment's Z component.</summary>
        /// <value>The moment component in the enclosing operation's axes and original moment units.</value>
        public double Mz { get; set; }

        /// <summary>Gets or sets the load-case identifier.</summary>
        /// <value>The load case associated with this record.</value>
        public int LoadCaseId { get; set; }
        /// <summary>Gets or sets the load-case title.</summary>
        /// <value>Literal title text from the query or supplied load case.</value>
        public string LoadCaseTitle { get; set; }
        /// <summary>Gets or sets the member identifier.</summary>
        /// <value>The analytical member number, or zero for a transformation defined by two nodes alone.</value>
        public int BeamId { get; set; }
        /// <summary>Gets or sets the member end.</summary>
        /// <value>The associated member end. A transformation defined by two nodes uses Start.</value>
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

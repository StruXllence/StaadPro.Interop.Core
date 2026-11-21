using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;
namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores supplied physical-member groups and discarded-beam metadata.</summary>
    /// <remarks>Construction creates independent empty lists. This data container does not identify/group geometry;
    /// no physical-member identification operation is implied. Governing selection reads Members only.</remarks>
    public class PhysicalMemberIdentificationResult
    {
        /// <summary>Creates independent empty member and discarded-beam lists.</summary>
        public PhysicalMemberIdentificationResult() { }
        /// <summary>Gets or sets supplied physical-member groups.</summary>
        /// <value>The original editable List reference; null is allowed and yields no governing member.</value>
        public List<Member> Members { get; set; } = new List<Member>();
        /// <summary>Gets or sets optional discarded-beam metadata.</summary>
        /// <value>An independent empty list initially; governing selection does not inspect or modify it.</value>
        public List<Beam> DiscardedBeams { get; set; } = new List<Beam>();
    }
}

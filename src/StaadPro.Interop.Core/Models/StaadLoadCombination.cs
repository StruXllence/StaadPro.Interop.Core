using System.Collections.Generic;
using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;
namespace StaadPro.Interop.Core.Models
{
    /// <summary>Detached ordinary load-combination metadata and primary-case coefficients.</summary>
    /// <remarks>Factors are signed dimensionless coefficients. The model has no SRSS overall multiplier.
    /// Managed edits do not update STAAD. Source case key equality uses Id, CaseType and Type; remove/reinsert
    /// keys before editing those fields to preserve dictionary lookup consistency. Title is excluded from key equality.</remarks>
    public class StaadLoadCombination : LoadCombination
    {
        /// <summary>Creates metadata with an independent empty coefficient dictionary.</summary>
        /// <param name="id">Combination ID stored unchanged.</param>
        /// <param name="title">Literal title stored unchanged.</param>
        public StaadLoadCombination(int id, string title) : base(id, title)
        { Factors = new Dictionary<ILoadCase, double>(); }
        /// <summary>Gets or replaces the primary-case metadata to coefficient mapping.</summary>
        /// <value>Queries supply owned case objects and finite coefficients. Manual callers may assign null or caller-owned storage.</value>
        public IDictionary<ILoadCase, double> Factors { get; set; }
    }
}

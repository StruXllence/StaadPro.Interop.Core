using System.Collections.Generic;
using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Enums;
namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Detached editable load-envelope metadata backed by the managed property store.</summary>
    /// <remarks>Uses reference identity. Edits notify managed observers and do not change STAAD.
    /// Membership set edits neither raise notifications nor synchronize LoadCasesCount.</remarks>
    public class LoadEnvelopeDetail : ValidatablePropertyStore, ILoadEnvelopeDetail
    {
        /// <summary>Creates default metadata with Id/count zero, None classification and a fresh empty set.</summary>
        public LoadEnvelopeDetail() { LoadCaseIds = new HashSet<int>(); }
        /// <summary>Stores supplied metadata and initializes a fresh empty membership set.</summary>
        /// <param name="id">Envelope identifier stored unchanged without native validation.</param>
        /// <param name="envelopType">Classification stored unchanged.</param>
        /// <param name="nLoadCases">Reported count stored independently of membership.</param>
        /// <remarks>Construction performs no native access or validation. Query methods qualify these values.</remarks>
        public LoadEnvelopeDetail(int id, LoadEnvelopType envelopType, int nLoadCases) : this()
        { Id = id; EnvelopType = envelopType; LoadCasesCount = nLoadCases; }
        /// <summary>Stores supplied metadata and the supplied membership set reference.</summary>
        /// <param name="id">Envelope identifier stored unchanged.</param>
        /// <param name="envelopType">Classification stored unchanged.</param>
        /// <param name="nLoadCases">Reported membership count stored unchanged.</param>
        /// <param name="loadCaseList">Set reference stored directly; null is allowed for manually constructed records.</param>
        /// <remarks>Native readers supply a new validated set. Manual callers manage ownership and count consistency.</remarks>
        public LoadEnvelopeDetail(int id, LoadEnvelopType envelopType, int nLoadCases, HashSet<int> loadCaseList)
            : this(id, envelopType, nLoadCases) { LoadCaseIds = loadCaseList; }
        /// <summary>Gets or sets the managed envelope identifier.</summary>
        /// <value>The stored ID. Native queries supply a positive value.</value>
        public int Id { get => Get<int>(); set => Set(value); }
        /// <summary>Gets or sets the managed classification.</summary>
        /// <value>The stored enum. Native queries validate codes 0 through 6.</value>
        public LoadEnvelopType EnvelopType { get => Get<LoadEnvelopType>(); set => Set(value); }
        /// <summary>Gets or sets the separately stored reported membership count.</summary>
        /// <value>The count is not recomputed when the set changes.</value>
        public int LoadCasesCount { get => Get<int>(); set => Set(value); }
        /// <summary>Gets or replaces the managed membership set reference.</summary>
        /// <value>Query readers provide independent validated positive IDs. Manual setters store values unchanged.</value>
        public HashSet<int> LoadCaseIds { get => Get<HashSet<int>>(); set => Set(value); }
    }
}

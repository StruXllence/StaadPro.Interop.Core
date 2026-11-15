using System.Collections.Generic;
using System.ComponentModel;
using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Enums;
namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Editable detached load-envelope identity, classification and membership metadata.</summary>
    /// <remarks>Property edits raise notifications but do not submit native changes. Mutating the membership set
    /// does not update LoadCasesCount or raise property notifications. Query methods return owned, validated snapshots.</remarks>
    public interface ILoadEnvelopeDetail : IEntity, INotifyPropertyChanged
    {
        /// <summary>Gets or sets the stored envelope classification.</summary>
        /// <value>The native classification code, validated by query methods.</value>
        LoadEnvelopType EnvelopType { get; set; }
        /// <summary>Gets or sets the separately stored reported membership count.</summary>
        /// <value>A managed count independent of subsequent changes to LoadCaseIds.</value>
        int LoadCasesCount { get; set; }
        /// <summary>Gets or replaces the stored set of member load-case IDs.</summary>
        /// <value>An owned non-null set from native queries. Manual setters may store null or caller-owned data.</value>
        HashSet<int> LoadCaseIds { get; set; }
    }
}

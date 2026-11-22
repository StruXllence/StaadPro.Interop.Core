using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Contains editable detached six-component results for one load case.</summary>
    /// <remarks>Query methods set Id to the requested case number and copy all six qualified native components.
    /// This container does not retain the support number; callers must retain their query identity.
    /// Manual construction and edits are unvalidated metadata and never update STAAD.</remarks>
    public class LoadCaseForces
    {
        /// <summary>Creates an unpopulated detached result.</summary>
        /// <remarks>Id starts at zero and Forces at null; construction does not perform a native query.</remarks>
        public LoadCaseForces() { }

        /// <summary>Gets or sets the load-case number associated with the components.</summary>
        /// <value>The positive requested case number on query results; manual values are stored unchanged.</value>
        public int Id { get; set; }

        /// <summary>Gets or sets the independently owned force and moment components.</summary>
        /// <value>A non-null qualified Forces record on successful queries; manual values, including null, are allowed.</value>
        public Forces Forces { get; set; }
    }
}

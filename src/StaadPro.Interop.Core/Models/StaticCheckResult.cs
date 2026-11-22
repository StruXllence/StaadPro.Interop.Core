using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Contains detached native static-check loads and reactions for one requested case.</summary>
    /// <remarks>Native queries qualify both six-component arrays before returning this model. Quantities retain
    /// native units, axes, reference point and signs. No balance tolerance or acceptance criterion is inferred.
    /// Public setters preserve editable data semantics; edits do not update STAAD or qualify manual data.</remarks>
    public sealed class StaticCheckResult : INotifyPropertyChanged
    {
        private Forces forces;
        private Forces reactions;

        /// <summary>Creates an unpopulated detached static-check record.</summary>
        /// <param name="loadCaseId">Case number stored unchanged; manual construction performs no validation or native call.</param>
        public StaticCheckResult(int loadCaseId) { LoadCaseId = loadCaseId; }

        /// <summary>Occurs synchronously when a Forces or Reactions reference changes.</summary>
        /// <remarks>Nested component edits are not forwarded. A handler exception propagates after assignment.</remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets the case number supplied at construction.</summary>
        /// <value>The positive requested load-case number on qualified queries; manual values remain unchanged.</value>
        public int LoadCaseId { get; }

        /// <summary>Gets or sets the native static-check load totals.</summary>
        /// <value>Independent Fx,Fy,Fz,Mx,My,Mz values on successful queries; manually assignable, including null.</value>
        public Forces Forces { get => forces; set => Set(ref forces, value); }

        /// <summary>Gets or sets the native static-check reaction totals.</summary>
        /// <value>Independent Fx,Fy,Fz,Mx,My,Mz values on successful queries; manually assignable, including null.</value>
        public Forces Reactions { get => reactions; set => Set(ref reactions, value); }

        /// <summary>Computes a fresh component-wise Forces minus Reactions record.</summary>
        /// <value>A new detached record for each access, using current editable values and ordinary Double arithmetic.</value>
        /// <remarks>Preserves the source subtraction convention; does not change native signs, take absolute values,
        /// sum for equilibrium or certify balance. Extreme/manual values can produce nonfinite differences.
        /// Nested edits do not raise a Differentials notification.</remarks>
        /// <exception cref="InvalidOperationException">Either input reference is null.</exception>
        public Forces Differentials
        {
            get
            {
                if (Forces == null || Reactions == null)
                    throw new InvalidOperationException("Both loads and reactions are required for differences.");
                return new Forces(Forces.Fx - Reactions.Fx, Forces.Fy - Reactions.Fy,
                    Forces.Fz - Reactions.Fz, Forces.Mx - Reactions.Mx,
                    Forces.My - Reactions.My, Forces.Mz - Reactions.Mz);
            }
        }

        private void Set(ref Forces field, Forces value, [CallerMemberName] string name = null)
        {
            if (ReferenceEquals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

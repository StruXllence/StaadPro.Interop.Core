using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StaadPro.Interop.Core.Common
{
    /// <summary>Provides managed property storage and change notifications without a UI-framework dependency.</summary>
    /// <remarks>Individual reads and writes are locked; multi-property operations are not atomic.
    /// Notifications run synchronously on the writing thread after releasing the storage lock.
    /// Stored reference values are not cloned and no native STAAD updates occur.</remarks>
    public abstract class ValidatablePropertyStore : INotifyPropertyChanged
    {
        private readonly object gate = new();
        private readonly Dictionary<string, object> properties = new(StringComparer.Ordinal);

        /// <summary>Initializes empty independent property storage.</summary>
        protected ValidatablePropertyStore() { }

        /// <summary>Occurs when a property is first stored or changes according to its default equality comparer.</summary>
        /// <remarks>Handler exceptions propagate after the stored value changes; no rollback or UI dispatch occurs.</remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Reads a stored value, or the type's default when the property has not been initialized.</summary>
        /// <typeparam name="T">The stored property's expected managed type.</typeparam>
        /// <param name="propertyName">A nonempty property name; normally supplied by CallerMemberName.</param>
        /// <returns>The stored value or default(T). References remain shared with their stored value.</returns>
        /// <exception cref="ArgumentNullException">propertyName is null or empty.</exception>
        /// <exception cref="InvalidCastException">The named value was stored using an incompatible type.</exception>
        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            lock (gate)
                return properties.TryGetValue(propertyName, out object value) ? (T)value : default;
        }

        /// <summary>Stores a changed value and raises a property notification outside the storage lock.</summary>
        /// <typeparam name="T">The managed property's type.</typeparam>
        /// <param name="value">The value to store, including null for a reference type.</param>
        /// <param name="propertyName">A nonempty property name; normally supplied by CallerMemberName.</param>
        /// <returns>True for the first stored value or a change; false for an equal existing value.</returns>
        /// <remarks>A first null value is an initialization and raises a notification. Subscriber exceptions
        /// propagate after the write. No validation, native synchronization or rollback is performed.</remarks>
        /// <exception cref="ArgumentNullException">propertyName is null or empty.</exception>
        /// <exception cref="InvalidCastException">An existing value has an incompatible type.</exception>
        protected bool Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            lock (gate)
            {
                if (properties.TryGetValue(propertyName, out object existing) && EqualityComparer<T>.Default.Equals((T)existing, value))
                    return false;
                properties[propertyName] = value;
            }
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>Raises PropertyChanged synchronously on the calling thread.</summary>
        /// <param name="propertyName">The changed property, normally supplied by CallerMemberName; null is forwarded unchanged.</param>
        /// <remarks>Overrides may extend notification behavior. Subscriber exceptions propagate unchanged.</remarks>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

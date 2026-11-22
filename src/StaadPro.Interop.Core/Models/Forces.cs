using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Contains editable detached force Fx,Fy,Fz and moment Mx,My,Mz components.</summary>
    /// <remarks>Query factories validate and copy native data before construction. Public constructors/setters
    /// store manual values without validation. The scalar subset has no WPF vectors, UI, serialization or unit
    /// conversion dependency. Components carry no axes/unit metadata: use the originating query contract.</remarks>
    public class Forces : INotifyPropertyChanged
    {
        private double fx, fy, fz, mx, my, mz;

        /// <summary>Creates zero-valued detached components without native access.</summary>
        public Forces() { }

        /// <summary>Stores six detached components without numeric validation or conversion.</summary>
        /// <param name="fx">Literal force component along/about the X axis, in the caller's units and sign convention.</param>
        /// <param name="fy">Literal force component along/about the Y axis, in the caller's units and sign convention.</param>
        /// <param name="fz">Literal force component along/about the Z axis, in the caller's units and sign convention.</param>
        /// <param name="mx">Literal moment component along/about the X axis, in the caller's units and sign convention.</param>
        /// <param name="my">Literal moment component along/about the Y axis, in the caller's units and sign convention.</param>
        /// <param name="mz">Literal moment component along/about the Z axis, in the caller's units and sign convention.</param>
        public Forces(double fx, double fy, double fz, double mx, double my, double mz)
        {
            this.fx = fx; this.fy = fy; this.fz = fz;
            this.mx = mx; this.my = my; this.mz = mz;
        }

        /// <summary>Occurs synchronously after a component changes.</summary>
        /// <remarks>Equal Double values suppress notification. Handler failures propagate after the assignment;
        /// multi-component edits are not atomic and are not rolled back.</remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets or sets the detached Fx component.</summary>
        /// <value>Literal force along/about X; manual nonfinite values are permitted. Edits do not update STAAD.</value>
        public double Fx { get => fx; set => Set(ref fx, value); }

        /// <summary>Gets or sets the detached Fy component.</summary>
        /// <value>Literal force along/about Y; manual nonfinite values are permitted. Edits do not update STAAD.</value>
        public double Fy { get => fy; set => Set(ref fy, value); }

        /// <summary>Gets or sets the detached Fz component.</summary>
        /// <value>Literal force along/about Z; manual nonfinite values are permitted. Edits do not update STAAD.</value>
        public double Fz { get => fz; set => Set(ref fz, value); }

        /// <summary>Gets or sets the detached Mx component.</summary>
        /// <value>Literal moment along/about X; manual nonfinite values are permitted. Edits do not update STAAD.</value>
        public double Mx { get => mx; set => Set(ref mx, value); }

        /// <summary>Gets or sets the detached My component.</summary>
        /// <value>Literal moment along/about Y; manual nonfinite values are permitted. Edits do not update STAAD.</value>
        public double My { get => my; set => Set(ref my, value); }

        /// <summary>Gets or sets the detached Mz component.</summary>
        /// <value>Literal moment along/about Z; manual nonfinite values are permitted. Edits do not update STAAD.</value>
        public double Mz { get => mz; set => Set(ref mz, value); }

        /// <summary>Multiplies all six current components in place by a supplied factor.</summary>
        /// <param name="factor">Literal multiplier, including zero or negative values; no numeric validation or conversion.</param>
        /// <remarks>Uses Fx,Fy,Fz,Mx,My,Mz order and ordinary Double arithmetic. Property notifications run after
        /// each assignment; exceptions can leave partial edits. Overflow/nonfinite manual inputs are not normalized.</remarks>
        public void Scale(double factor)
        {
            Fx *= factor; Fy *= factor; Fz *= factor;
            Mx *= factor; My *= factor; Mz *= factor;
        }

        /// <summary>Copies six components from a managed array in Fx,Fy,Fz,Mx,My,Mz order.</summary>
        /// <param name="forcesArray">Non-null, exact-length six-element zero-based Double array; values are copied literally.</param>
        /// <returns>A new independently owned editable scalar record; manual nonfinite values are not validated.</returns>
        /// <exception cref="ArgumentNullException">forcesArray is null.</exception>
        /// <exception cref="ArgumentException">forcesArray does not have exactly six entries.</exception>
        public static Forces ParseFromArray(double[] forcesArray)
        {
            if (forcesArray == null) throw new ArgumentNullException(nameof(forcesArray));
            if (forcesArray.Length != 6) throw new ArgumentException("Exactly six components are required.", nameof(forcesArray));
            return new Forces(forcesArray[0], forcesArray[1], forcesArray[2], forcesArray[3], forcesArray[4], forcesArray[5]);
        }

        private void Set(ref double field, double value, [CallerMemberName] string name = null)
        {
            if (field.Equals(value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

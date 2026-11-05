using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Table binding without implementing domain operations.</summary>
    public sealed class OSTableAdapter : OSBaseAdapter, IOSTable
    {
        /// <summary>Captures the native Table object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Table binding is unavailable.</exception>
        public OSTableAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawTable ?? throw new ArgumentException("The Table binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this placeholder was constructed.</summary>
        public dynamic ComObject { get; }
    }
}

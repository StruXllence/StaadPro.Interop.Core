using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Support binding without implementing domain operations.</summary>
    public sealed class OSSupportAdapter : OSBaseAdapter, IOSSupport
    {
        /// <summary>Captures the native Support object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Support binding is unavailable.</exception>
        public OSSupportAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawSupport ?? throw new ArgumentException("The Support binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this placeholder was constructed.</summary>
        public dynamic ComObject { get; }
    }
}

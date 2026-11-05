using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Design binding without implementing domain operations.</summary>
    public sealed class OSDesignAdapter : OSBaseAdapter, IOSDesign
    {
        /// <summary>Captures the native Design object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Design binding is unavailable.</exception>
        public OSDesignAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawDesign ?? throw new ArgumentException("The Design binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this placeholder was constructed.</summary>
        public dynamic ComObject { get; }
    }
}

using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Output binding without implementing domain operations.</summary>
    public sealed class OSOutputAdapter : OSBaseAdapter, IOSOutput
    {
        /// <summary>Captures the native Output object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Output binding is unavailable.</exception>
        public OSOutputAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawOutput ?? throw new ArgumentException("The Output binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this placeholder was constructed.</summary>
        public dynamic ComObject { get; }
    }
}

using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Geometry binding without implementing domain operations.</summary>
    public sealed class OSGeometryAdapter : OSBaseAdapter, IOSGeometry
    {
        /// <summary>Captures the native Geometry object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Geometry binding is unavailable.</exception>
        public OSGeometryAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawGeometry ?? throw new ArgumentException("The Geometry binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this placeholder was constructed.</summary>
        public dynamic ComObject { get; }
    }
}

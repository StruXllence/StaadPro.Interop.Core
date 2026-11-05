using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Provides the wrapper relationship shared by domain bindings.</summary>
    public abstract class OSBaseAdapter : IOSBase
    {
        /// <summary>Captures the wrapper without acquiring another native application.</summary>
        /// <param name="wrapper">The non-null wrapper for this binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        protected OSBaseAdapter(OpenStaadWrapper wrapper) { Wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper)); }
        /// <summary>Gets the wrapper supplied at construction.</summary>
        public OpenStaadWrapper Wrapper { get; }
    }
}

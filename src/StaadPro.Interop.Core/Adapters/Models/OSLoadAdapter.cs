using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Output;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Load binding with guarded member-force foundation operations.</summary>
    public sealed class OSLoadAdapter : OSBaseAdapter, IOSLoad
    {
        /// <summary>Captures the native Load object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Load binding is unavailable.</exception>
        public OSLoadAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawLoad ?? throw new ArgumentException("The Load binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this adapter was constructed.</summary>
        public dynamic ComObject { get; }

        /// <summary>Reads the title of one native load case without activating it.</summary>
        /// <param name="lcId">Positive native load-case ID; not a collection index.</param>
        /// <returns>The exact native string, including an explicitly empty title. Non-string/null outputs throw.</returns>
        /// <remarks>Calls GetLoadCaseTitle once. Preserves Unicode, whitespace and line breaks; does not infer case family
        /// or whether results exist. Native errors and missing cases are not converted to an empty title.<para>Runs on the wrapper's constructing COM thread under one current-binding operation lease, including
        /// validation, enumeration, native dispatch and output qualification. RefreshComObjects invalidates captured
        /// adapters; reacquire them. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output,
        /// refresh or disposal calls are rejected before native access. The guard cannot coordinate raw calls,
        /// other wrappers or external UI. No implicit analysis, case activation, selection, save, unit conversion,
        /// background COM, retry or rollback is added. Native failures preserve original exceptions/HRESULTs.
        /// Calls can block. Snapshots do not guarantee result freshness or engineering validity.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, another thread/reentrant operation is active,
        /// a native flag/title is malformed or successful outputs are unwritten, nonfinite or incorrectly shaped.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A compatible native member is unavailable.</exception>

        /// <exception cref="ArgumentOutOfRangeException">lcId is not positive; no native call is made.</exception>
        /// <example><code>Console.WriteLine(wrapper.Load.GetLoadCaseTitle(1));</code></example>
        public string GetLoadCaseTitle(int lcId) => Wrapper.UseLoadBinding(this, wrapper => MemberForceReader.GetLoadCaseTitle(wrapper.RawLoad, lcId));
    }
}

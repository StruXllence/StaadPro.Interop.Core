using System;
using StaadPro.Interop.Core.Adapters.Interfaces.Capabilities;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Root.Capabilities;

namespace StaadPro.Interop.Core.Adapters.Models.Capabilities
{
    internal sealed class OSRootCapabilitiesAdapter : IOSRootCapabilities
    {
        private readonly OSRootAdapter owner;
        internal OSRootCapabilitiesAdapter(OSRootAdapter owner) { this.owner = owner; }

        /// <summary>Attempts to acquire IOSRootAdinaExport for the captured native root.</summary>
        /// <returns>A typed availability result; only an Available outcome contains a non-null adapter.</returns>
        /// <remarks>Uses the owning wrapper's cached native StaadVersion. Exact candidate profiles are 25.0.1.424 (export only) and 26.0.0.340 (export and messages).
        /// Requires compatible non-mutating native member metadata; message grants also inspect GetAnalysisStatus.
        /// The profiles are backed by installed help/managed declarations, not certified native runtime tests.
        /// Inspection never exports, retrieves messages, mutates models or guesses from installed directories.
        /// No grant is cached globally. Reacquire after a version change; calls through a grant validate its snapshot.
        /// Use the session's COM thread. Busy, disconnected, wrong-thread and locale failures remain exceptions.<para>Uses the wrapper's constructing COM thread and current Root operation lease throughout input
        /// validation, enumeration, native dispatch and output qualification. Disposal, stale/manual-noncurrent
        /// adapters, other threads and reentrant Root/refresh/disposal operations are rejected before native work.
        /// Reacquire wrapper.Root and its capabilities after RefreshComObjects, even when the native version
        /// is unchanged. No automatic unit or domain refresh occurs. Raw calls, other wrappers and external UI
        /// are outside this guard.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper was disposed.</exception>
        /// <exception cref="InvalidOperationException">Native version information is malformed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Version or metadata inspection fails; native failures are not converted to unsupported results.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The Root binding is stale, another thread is calling,
        /// or a Root/lifecycle operation is already in progress.</exception>
        /// <example><code>
        /// var grant = wrapper.Root.Capabilities.GetAdinaExport();
        /// Console.WriteLine(grant.Availability);
        /// </code></example>
        public RootCapabilityResult<IOSRootAdinaExport> GetAdinaExport() =>
            Acquire<IOSRootAdinaExport>(RootNativeContract.Export, true, b => new OSRootAdinaExportAdapter(b));
        /// <summary>Attempts to acquire IOSRootAnalysisErrorMessages for the captured native root.</summary>
        /// <returns>A typed availability result; only an Available outcome contains a non-null adapter.</returns>
        /// <remarks>Uses the owning wrapper's cached native StaadVersion. Exact candidate profiles are 25.0.1.424 (export only) and 26.0.0.340 (export and messages).
        /// Requires compatible non-mutating native member metadata; message grants also inspect GetAnalysisStatus.
        /// The profiles are backed by installed help/managed declarations, not certified native runtime tests.
        /// Inspection never exports, retrieves messages, mutates models or guesses from installed directories.
        /// No grant is cached globally. Reacquire after a version change; calls through a grant validate its snapshot.
        /// Use the session's COM thread. Busy, disconnected, wrong-thread and locale failures remain exceptions.<para>Uses the wrapper's constructing COM thread and current Root operation lease throughout input
        /// validation, enumeration, native dispatch and output qualification. Disposal, stale/manual-noncurrent
        /// adapters, other threads and reentrant Root/refresh/disposal operations are rejected before native work.
        /// Reacquire wrapper.Root and its capabilities after RefreshComObjects, even when the native version
        /// is unchanged. No automatic unit or domain refresh occurs. Raw calls, other wrappers and external UI
        /// are outside this guard.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper was disposed.</exception>
        /// <exception cref="InvalidOperationException">Native version information is malformed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Version or metadata inspection fails; native failures are not converted to unsupported results.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The Root binding is stale, another thread is calling,
        /// or a Root/lifecycle operation is already in progress.</exception>
        /// <example><code>
        /// var grant = wrapper.Root.Capabilities.GetAnalysisErrorMessages();
        /// Console.WriteLine(grant.Availability);
        /// </code></example>
        public RootCapabilityResult<IOSRootAnalysisErrorMessages> GetAnalysisErrorMessages() =>
            Acquire<IOSRootAnalysisErrorMessages>(RootNativeContract.Errors, false, b => new OSRootAnalysisErrorMessagesAdapter(b));
        /// <summary>Attempts to acquire IOSRootAnalysisWarningMessages for the captured native root.</summary>
        /// <returns>A typed availability result; only an Available outcome contains a non-null adapter.</returns>
        /// <remarks>Uses the owning wrapper's cached native StaadVersion. Exact candidate profiles are 25.0.1.424 (export only) and 26.0.0.340 (export and messages).
        /// Requires compatible non-mutating native member metadata; message grants also inspect GetAnalysisStatus.
        /// The profiles are backed by installed help/managed declarations, not certified native runtime tests.
        /// Inspection never exports, retrieves messages, mutates models or guesses from installed directories.
        /// No grant is cached globally. Reacquire after a version change; calls through a grant validate its snapshot.
        /// Use the session's COM thread. Busy, disconnected, wrong-thread and locale failures remain exceptions.<para>Uses the wrapper's constructing COM thread and current Root operation lease throughout input
        /// validation, enumeration, native dispatch and output qualification. Disposal, stale/manual-noncurrent
        /// adapters, other threads and reentrant Root/refresh/disposal operations are rejected before native work.
        /// Reacquire wrapper.Root and its capabilities after RefreshComObjects, even when the native version
        /// is unchanged. No automatic unit or domain refresh occurs. Raw calls, other wrappers and external UI
        /// are outside this guard.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper was disposed.</exception>
        /// <exception cref="InvalidOperationException">Native version information is malformed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Version or metadata inspection fails; native failures are not converted to unsupported results.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The Root binding is stale, another thread is calling,
        /// or a Root/lifecycle operation is already in progress.</exception>
        /// <example><code>
        /// var grant = wrapper.Root.Capabilities.GetAnalysisWarningMessages();
        /// Console.WriteLine(grant.Availability);
        /// </code></example>
        public RootCapabilityResult<IOSRootAnalysisWarningMessages> GetAnalysisWarningMessages() =>
            Acquire<IOSRootAnalysisWarningMessages>(RootNativeContract.Warnings, false, b => new OSRootAnalysisWarningMessagesAdapter(b));

        private RootCapabilityResult<T> Acquire<T>(RootNativeContract contract, bool export, Func<RootCapabilityBinding, T> factory)
            where T : class
        {
            return owner.UseCapability((root, nativeVersion) =>
            {
                var observed = RootCapabilityBinding.Freeze(nativeVersion);
                if (observed == null) return Unavailable<T>(RootCapabilityAvailability.UnknownVersion, null, "The acquired native version is unavailable or incomplete.");
                if (!RootCapabilityProfiles.IsCandidateYear(observed, export))
                    return Unavailable<T>(RootCapabilityAvailability.DisabledByVersionPolicy, observed, "This product family is outside the reviewed feature policy.");
                if (!RootCapabilityProfiles.IsCoveredBuild(observed))
                    return Unavailable<T>(RootCapabilityAvailability.ContractUnverified, observed, "This servicing build has no exact candidate contract profile.");
                var evidence = owner.CapabilityInspector.Inspect(root, contract);
                if (evidence != RootMemberEvidence.Compatible) return Missing<T>(evidence, observed, contract.Name);
                if (!export)
                {
                    evidence = owner.CapabilityInspector.Inspect(root, RootNativeContract.AnalysisStatus);
                    if (evidence != RootMemberEvidence.Compatible) return Missing<T>(evidence, observed, RootNativeContract.AnalysisStatus.Name);
                }
                var capability = factory(new RootCapabilityBinding(owner, observed));
                return new RootCapabilityResult<T>(RootCapabilityAvailability.Available, capability, observed,
                    "The exact candidate build and compatible member metadata permit acquisition; native runtime behavior remains unverified.");
            });
        }

        private static RootCapabilityResult<T> Missing<T>(RootMemberEvidence evidence, Version version, string member) where T : class =>
            Unavailable<T>(evidence == RootMemberEvidence.Absent ? RootCapabilityAvailability.NativeMemberAbsent :
                RootCapabilityAvailability.ContractUnverified, version, member + (evidence == RootMemberEvidence.Absent ?
                " is absent." : " has unavailable or incompatible signature metadata."));
        private static RootCapabilityResult<T> Unavailable<T>(RootCapabilityAvailability status, Version version, string reason) where T : class =>
            new RootCapabilityResult<T>(status, null, version, reason);
    }
}

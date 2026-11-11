using System;
using StaadPro.Interop.Core.Models.Root;
namespace StaadPro.Interop.Core.Adapters.Interfaces.Capabilities
{
    /// <summary>Acquires optional Root interfaces after native-version and member-metadata checks.</summary>
    /// <remarks>Each method checks independently without executing an optional operation. Unknown/future versions,
    /// uncovered servicing builds, absent members and incompatible metadata grant no interface. Public declarations
    /// remain inspectable; this boundary does not restrict deliberate raw COM access.<para>Uses the wrapper's constructing COM thread and current Root operation lease throughout input
        /// validation, enumeration, native dispatch and output qualification. Disposal, stale/manual-noncurrent
        /// adapters, other threads and reentrant Root/refresh/disposal operations are rejected before native work.
        /// Reacquire wrapper.Root and its capabilities after RefreshComObjects, even when the native version
        /// is unchanged. No automatic unit or domain refresh occurs. Raw calls, other wrappers and external UI
        /// are outside this guard.</para></remarks>
    public interface IOSRootCapabilities
    {
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
        RootCapabilityResult<IOSRootAdinaExport> GetAdinaExport();

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
        RootCapabilityResult<IOSRootAnalysisErrorMessages> GetAnalysisErrorMessages();

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
        RootCapabilityResult<IOSRootAnalysisWarningMessages> GetAnalysisWarningMessages();
    }
}

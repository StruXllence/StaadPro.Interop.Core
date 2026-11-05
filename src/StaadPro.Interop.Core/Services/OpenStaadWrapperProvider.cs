using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using StaadPro.Interop.Core.Interfaces;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Acquisition;
namespace StaadPro.Interop.Core.Services
{
    /// <summary>Obtains a verified wrapper through the Core acquisition engine.</summary>
    /// <remarks>Create/use/dispose the returned wrapper on the calling Windows STA thread. Existing named models are attached without reopening. A requested path can yield a verified blank; use Acquire to distinguish that outcome. Get without a path first attaches and, only when no supported application exists, can create a new temporary model using an installed STAAD application.</remarks>
    public static class OpenStaadWrapperProvider
    {
        private static IOpenStaadWrapperResolver resolver = new DefaultResolver();
        /// <summary>Gets or sets the extension used by the single-argument Get convenience.</summary>
        /// <remarks>A custom resolver is responsible for identity, readiness and apartment correctness. Acquire and GetRunning always use the built-in engine.</remarks>
        /// <exception cref="ArgumentNullException">The assigned resolver is null.</exception>
        public static IOpenStaadWrapperResolver Resolver { get => Volatile.Read(ref resolver); set => Interlocked.Exchange(ref resolver, value ?? throw new ArgumentNullException(nameof(value))); }

        /// <summary>Returns a wrapper or throws a diagnostic acquisition exception.</summary>
        /// <param name="fileFullPath">An absolute .std target, or null to prefer an existing session and permit new-session fallback.</param>
        /// <returns>The acquired wrapper; never a silent null result.</returns>
        /// <exception cref="OpenStaadAcquisitionException">No verified wrapper could be obtained.</exception>
        /// <exception cref="ArgumentException">The path is invalid.</exception>
        /// <exception cref="InvalidOperationException">The caller is not on the required STA/context.</exception>
        public static OpenStaadWrapper Get(string fileFullPath = null)
        {
            OpenStaadWrapper wrapper = Resolver.Get(fileFullPath);
            if (wrapper == null) throw new OpenStaadAcquisitionException(Failure(OpenStaadAcquisitionFailure.DiscoveryFailed, "The configured resolver returned no wrapper."));
            return wrapper;
        }

        /// <summary>Uses explicit acquisition options and returns a wrapper or diagnostic exception.</summary>
        /// <param name="fileFullPath">An absolute .std target, or null for existing-session preference.</param>
        /// <param name="options">Options copied for the request; an explicit executable supports nonstandard installations.</param>
        /// <returns>The verified wrapper.</returns>
        /// <exception cref="OpenStaadAcquisitionException">The request failed operationally.</exception>
        /// <exception cref="ArgumentException">An option or target is invalid.</exception>
        /// <exception cref="InvalidOperationException">The caller violates the STA/context contract.</exception>
        public static OpenStaadWrapper Get(string fileFullPath, OpenStaadAcquisitionOptions options) => Require(Acquire(fileFullPath, options));

        /// <summary>Obtains a wrapper with process identity, actual outcome and failure diagnostics.</summary>
        /// <param name="fileFullPath">An absolute .std target, or null to attach first and permit temporary-model fallback only when no application is running.</param>
        /// <param name="options">Optional copied policy; null uses complete-discovery policy and installed-executable lookup.</param>
        /// <param name="cancellationToken">Cancellation observed between native operations and before side effects.</param>
        /// <returns>A non-null result. ExistingBlank means the requested file was not opened.</returns>
        /// <remarks>Automatic lookup checks the registered App Paths executable and conventional 2026/2025/2024 locations. A missing registration/install does not prevent attachment. A generated model is retained after later failure. Logical timeout cannot interrupt a hung COM call; no process is automatically terminated. This method bypasses custom Resolver.</remarks>
        /// <exception cref="ArgumentException">The target/options are invalid.</exception>
        /// <exception cref="InvalidOperationException">The caller is not on a Windows STA thread.</exception>
        public static OpenStaadAcquisitionResult Acquire(string fileFullPath = null, OpenStaadAcquisitionOptions options = null, CancellationToken cancellationToken = default)
        {
            var copy = Copy(options);
            copy.StaadExecutablePath ??= OpenStaadInstallationLocator.FindExecutable();
            return AcquireRequest(fileFullPath, copy, o => new OpenStaadSessionProvider(o),
                () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StaadPro.Interop.Core", "Models", "Session-" + Guid.NewGuid().ToString("N") + ".std"), cancellationToken);
        }

        /// <summary>Attaches to a running session without creating files or launching STAAD.</summary>
        /// <returns>A verified wrapper, or null when attachment fails. Use the instance provider's Acquire for diagnostics.</returns>
        /// <remarks>Always uses the built-in attachment-only engine, independently of custom Resolver.</remarks>
        /// <exception cref="InvalidOperationException">The caller is not on a Windows STA thread.</exception>
        public static OpenStaadWrapper GetRunning()
        {
            using (var provider = new OpenStaadSessionProvider(new OpenStaadAcquisitionOptions { DiscoveryPolicy = OpenStaadDiscoveryPolicy.RequireCompleteDiscovery })) return provider.Get();
        }

        internal static OpenStaadAcquisitionResult AcquireRequest(string path, OpenStaadAcquisitionOptions options,
            Func<OpenStaadAcquisitionOptions, OpenStaadSessionProvider> createProvider, Func<string> createTemporaryPath, CancellationToken cancellation)
        {
            var clock = Stopwatch.StartNew();
            using (var provider = createProvider(options))
            {
                var result = provider.Acquire(path, cancellation);
                if (path != null || result.Succeeded || result.Failure != OpenStaadAcquisitionFailure.NoRunningSession) return result;
            }
            if (cancellation.IsCancellationRequested) return Failure(OpenStaadAcquisitionFailure.Cancelled, "Acquisition was cancelled before new-session fallback.");
            if (options.StaadExecutablePath == null) return Failure(OpenStaadAcquisitionFailure.LaunchFailed, "No existing session or installed launcher was found. Configure StaadExecutablePath for a nonstandard installation.");
            TimeSpan remaining = options.Timeout - clock.Elapsed;
            if (remaining <= TimeSpan.Zero) return Failure(OpenStaadAcquisitionFailure.Timeout, "The acquisition budget expired before startup.");
            var fallback = Copy(options);
            fallback.Timeout = remaining;
            if (fallback.PollInterval > remaining) fallback.PollInterval = remaining;
            if (fallback.DiscoveryGracePeriod > remaining) fallback.DiscoveryGracePeriod = remaining;
            using (var provider = createProvider(fallback)) return provider.Acquire(createTemporaryPath(), cancellation);
        }
        private static OpenStaadAcquisitionOptions Copy(OpenStaadAcquisitionOptions options) => new OpenStaadAcquisitionOptions
        {
            StaadExecutablePath = options?.StaadExecutablePath,
            Timeout = options?.Timeout ?? TimeSpan.FromMinutes(2),
            PollInterval = options?.PollInterval ?? TimeSpan.FromMilliseconds(250),
            DiscoveryGracePeriod = options?.DiscoveryGracePeriod ?? TimeSpan.FromSeconds(1),
            DiscoveryPolicy = options?.DiscoveryPolicy ?? OpenStaadDiscoveryPolicy.RequireCompleteDiscovery,
            CreateMissingTarget = options?.CreateMissingTarget ?? true
        };
        private static OpenStaadWrapper Require(OpenStaadAcquisitionResult result) => result.Wrapper ?? throw new OpenStaadAcquisitionException(result);
        internal static OpenStaadAcquisitionResult Failure(OpenStaadAcquisitionFailure failure, string message) => new OpenStaadAcquisitionResult(null, OpenStaadAcquisitionOutcome.Failure, failure, null, null, null, null, false, new[] { message });
        private sealed class DefaultResolver : IOpenStaadWrapperResolver
        {
            public OpenStaadWrapper Get(string fileFullPath = null) => Require(Acquire(fileFullPath));
        }
    }
}

using System;
namespace StaadPro.Interop.Core.Models
{
    /// <summary>Reports an operational acquisition failure from a direct wrapper request.</summary>
    /// <remarks>The result preserves diagnostics and any observed startup identity. A failed request may have published a file or started STAAD; no rollback or process termination is implied.</remarks>
    public sealed class OpenStaadAcquisitionException : InvalidOperationException
    {
        /// <summary>Creates an exception for a failed acquisition result.</summary>
        /// <param name="result">A non-null failed result.</param>
        /// <exception cref="ArgumentNullException">The result is null.</exception>
        /// <exception cref="ArgumentException">The result represents success.</exception>
        public OpenStaadAcquisitionException(OpenStaadAcquisitionResult result) : base(CreateMessage(result)) { Result = result; }
        /// <summary>Gets the failed acquisition evidence.</summary>
        public OpenStaadAcquisitionResult Result { get; }
        private static string CreateMessage(OpenStaadAcquisitionResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Succeeded) throw new ArgumentException("A successful acquisition is not an error.", nameof(result));
            return "Could not acquire OpenSTAAD (" + result.Failure + "). " + (result.Diagnostics.Count == 0 ? string.Empty : result.Diagnostics[result.Diagnostics.Count - 1]);
        }
    }
}

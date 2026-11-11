using System;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Services.Root.Capabilities
{
    internal sealed class RootCapabilityBinding
    {
        private readonly OSRootAdapter owner;
        private readonly Version version;
        internal RootCapabilityBinding(OSRootAdapter owner, Version version) { this.owner = owner; this.version = version; }
        internal T Use<T>(Func<object, T> operation) => owner.UseCapability((root, observed) =>
        {
            if (!Equals(version, Freeze(observed)))
                throw new InvalidOperationException("The native version snapshot changed. Acquire this capability again.");
            return operation(root);
        });
        internal static Version Freeze(ApplicationVersion value)
        {
            if (value == null || value.Major < 0 || value.Update < 0 || value.Revision < 0 || value.Build < 0) return null;
            return new Version(value.Major, value.Update, value.Revision, value.Build);
        }
    }

    internal static class RootCapabilityProfiles
    {
        internal static bool IsCandidateYear(Version version, bool export) =>
            export ? version.Major == 25 || version.Major == 26 : version.Major == 26;
        internal static bool IsCoveredBuild(Version version) =>
            version.Equals(new Version(25, 0, 1, 424)) || version.Equals(new Version(26, 0, 0, 340));
    }
}

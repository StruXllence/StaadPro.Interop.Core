![StaadPro.Interop.Core](https://raw.githubusercontent.com/StruXllence/StaadPro.Interop.Core/staadpro-interop-core/docs/images/logo-with-text-400.png)

# StaadPro.Interop.Core

A strongly-typed Windows .NET wrapper for Bentley STAAD.Pro OpenSTAAD.

By **Srinivasa Rao Masanam**. Published by **StruXllence**.

[![YouTube](https://img.shields.io/badge/YouTube-%40StruXllence-FF0000?style=flat-square&logo=youtube&logoColor=white)](https://www.youtube.com/@StruXllence) [![Website](https://img.shields.io/badge/Website-Live-16A34A?style=flat-square)](https://struxllence.com) [![Forum](https://img.shields.io/badge/Community-Forum-0866C6?style=flat-square)](https://forum.struxllence.com) [![Forum preview](https://img.shields.io/badge/Revamp%20forum-Preview-7657B5?style=flat-square)](https://new-forum.struxllence.com)

## Version 1.0.0-preview.20261105

This first Core preview contains the **wrapper and acquisition foundation only**. Root, Geometry, Load, Output, Property, Support, Design, Commands, Table, View and IDEA StatiCa are binding-only placeholders. They expose no domain automation methods. **OSRoot is the first domain planned for implementation.**

The package targets **net9.0-windows**. It has no dependency on the Framework StaadPro.Interop package, WPF or Newtonsoft.Json. Live use requires a separately installed, licensed STAAD.Pro application with compatible OpenSTAAD members. Build/package checks do not certify every native build or licensing state.

The default factory verifies the hosting process, native model identity and Geometry/Load readiness. It prefers an already-open requested model, then a reachable verified blank, then a permitted newly opened target. **A returned blank remains blank; the requested target is not opened into it.** Use Acquire when the outcome matters.

~~~csharp
using System;
using StaadPro.Interop.Core.Services;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var result = OpenStaadWrapperProvider.Acquire(@"C:/Models/Structure.std");
        if (!result.Succeeded)
        {
            Console.WriteLine(result.Failure);
            foreach (string message in result.Diagnostics) Console.WriteLine(message);
            return;
        }
        using var wrapper = result.Wrapper;
        Console.WriteLine($"{result.Outcome}: '{result.ActualPath}'");
        Console.WriteLine(wrapper.StaadVersion?.Display);
        // Domain automation methods will be introduced in later previews.
    }
}
~~~

Get(path) returns a wrapper or throws OpenStaadAcquisitionException with diagnostic evidence. GetRunning() attaches only and never starts STAAD. The static Acquire()/Get() without a path attaches first; when no supported application exists, installed-executable lookup can enable creation of a temporary STD model. The advanced OpenStaadSessionProvider.Acquire() without a path remains attachment only.

Create, use and dispose connections on the appropriate Windows STA thread. Wrapper refresh/live disposal enforce their constructing thread and reject reentrancy. Raw native objects remain outside managed thread/operation validation. Synchronous COM calls cannot be forcibly interrupted by the logical timeout. A missing license, inaccessible root or hung application can prevent acquisition; failures are reported rather than disguised as successful wrappers.

Passed **132 offline foundation tests**, including identity/readiness, busy and blank states, atomic file races, startup verification, wrapper parity and lifecycle guards. Read-only named-session and same-process target-reuse checks succeeded on STAAD.Pro 26.00.01.491 without startup. Incomplete and complete discovery snapshots were observed. This limited evidence is not full cross-version/startup qualification.

[Roadmap](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/ROADMAP.md) · [Installation](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Installation.md) · [Wrapper and acquisition design](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Wrapper-Foundation.md) · [Publishing](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Publishing.md)

This project is independent and is neither affiliated with, sponsored by nor endorsed by Bentley Systems. Bentley, STAAD, STAAD.Pro and OpenSTAAD names and marks belong to their respective owners. Apache-2.0 applies with the retained MIT-origin notice; see [LICENSE](LICENSE), [NOTICE](NOTICE.md) and [trademark guidance](TRADEMARKS.md). Copyright (c) 2026 StruXllence.

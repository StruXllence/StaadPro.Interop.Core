![StaadPro.Interop.Core](https://raw.githubusercontent.com/StruXllence/StaadPro.Interop.Core/staadpro-interop-core/docs/images/logo-with-text-core-400.png)

# StaadPro.Interop.Core: OpenSTAAD for C# and .NET 9

[![Build and Test Core](https://github.com/StruXllence/StaadPro.Interop.Core/actions/workflows/build-and-test.yml/badge.svg?branch=staadpro-interop-core)](https://github.com/StruXllence/StaadPro.Interop.Core/actions/workflows/build-and-test.yml?query=branch%3Astaadpro-interop-core) [![NuGet latest preview](https://img.shields.io/nuget/vpre/StaadPro.Interop.Core?style=flat-square&label=nuget&color=F78C1E)](https://www.nuget.org/packages/StaadPro.Interop.Core) [![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-007EC6?style=flat-square)](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/LICENSE) [![.NET 9.0 Windows](https://img.shields.io/badge/.NET-9.0%20Windows-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Installation.md)

StaadPro.Interop.Core is an open-source, strongly typed **C# OpenSTAAD wrapper for Bentley STAAD.Pro** on **Windows .NET 9 (.NET Core)**. Attach to a STAAD.Pro session, check process/model identity, and read cached native version, model units and application-window metadata with detailed XML IntelliSense.

By **Srinivasa Rao Masanam**. Published by **StruXllence**.

[![YouTube](https://img.shields.io/badge/YouTube-%40StruXllence-FF0000?style=flat-square&logo=youtube&logoColor=white)](https://www.youtube.com/@StruXllence) [![Website](https://img.shields.io/badge/Website-Live-16A34A?style=flat-square)](https://struxllence.com) [![Forum](https://img.shields.io/badge/Community-Forum-0866C6?style=flat-square)](https://forum.struxllence.com) [![Forum preview](https://img.shields.io/badge/Revamp%20forum-Preview-7657B5?style=flat-square)](https://new-forum.struxllence.com)

## Install the OpenSTAAD C# package

Install the latest published preview from NuGet.org:

~~~powershell
dotnet add ./YourApp.csproj package StaadPro.Interop.Core --prerelease --source https://api.nuget.org/v3/index.json
~~~
In Visual Studio's NuGet Package Manager, enable **Include prerelease** and search for **StaadPro.Interop.Core**. Target `net9.0-windows` and use an STA entry point for native automation. The NuGet badge tracks the latest public preview; see the [installation guide](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Installation.md) for exact-version and local-feed instructions.

## Version 1.0.0-preview.20261108.1

This Core preview contains wrapper/acquisition infrastructure and **twenty-five Root operations**: metadata, model/process and path observations, model/application requests, job-information snapshots and main-window controls. New functions are `GetShortJobInfo()`, `GetFullJobInfo()`, `MinimizeStaadMainWindow()`, `MaximizeStaadMainWindow()` and `RestoreStaadMainWindow()`. See [job information and window contracts](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Root-Job-Information-and-Windows.md) and [model lifecycle contracts](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Root-Model-Lifecycle.md). Geometry, Load, Output, Property, Support, Design, Commands, Table, View and IDEA StatiCa remain binding-only placeholders.

The package targets **net9.0-windows**. It has no dependency on the Framework StaadPro.Interop package, WPF or Newtonsoft.Json. Live use requires a separately installed, licensed STAAD.Pro application with compatible OpenSTAAD members. Build/package checks do not certify every native build or licensing state.

## Connect to STAAD.Pro from C#

The default factory verifies the hosting process, native model identity and Geometry/Load readiness. It prefers an already-open requested model, then a reachable verified blank, then a permitted newly opened target. **A returned blank remains blank; the requested target is not opened into it.** Use `Acquire()` when the outcome matters.

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
        var root = wrapper.Root;
        Console.WriteLine(root.GetApplicationVersion()?.Display);
        Console.WriteLine(root.GetBaseUnit());
        Console.WriteLine(root.GetInputUnitForForce());
        Console.WriteLine(root.GetInputUnitForLength());
        Console.WriteLine(root.GetMainWindowHandle());
        Console.WriteLine(root.GetStaadFileFullPath());
        Console.WriteLine(root.GetStaadFileFolder());
        Console.WriteLine(root.IsPhysicalModel());
        Console.WriteLine(root.GetErrorMessage());
        Console.WriteLine(root.GetProcessId());
    }
}
~~~
Get(path) returns a wrapper or throws OpenStaadAcquisitionException with diagnostic evidence. GetRunning() attaches only and never starts STAAD. The static Acquire()/Get() without a path attaches first; when no supported application exists, installed-executable lookup can enable creation of a temporary STD model. The advanced OpenStaadSessionProvider.Acquire() without a path remains attachment only.

Create, use and dispose connections on the appropriate Windows STA thread. Wrapper refresh/live disposal enforce their constructing thread and reject reentrancy. Raw native objects remain outside managed thread/operation validation. Synchronous COM calls cannot be forcibly interrupted by the logical timeout. A missing license, inaccessible root or hung application can prevent acquisition; failures are reported rather than disguised as successful wrappers.

## Root metadata and IntelliSense

The five metadata methods reuse wrapper snapshots without COM dispatch. `GetApplicationVersion()` returns an independent native version, distinct from the host assembly version. Input units can be compatibility fallbacks; the handle is borrowed and may be zero or stale. `RefreshUnits()` updates units while retaining Root. After `RefreshComObjects()`, reacquire `wrapper.Root`: retained adapters are rejected. Root reads also reject disposal, another thread and reads during refresh. See the [Root metadata contracts](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Root-Metadata.md).

The five [live Root observations](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Root-Observations.md) each issue one native query. Path/folder/error text is preserved exactly; unwritten or malformed text is rejected rather than treated as a blank. Physical-model flags and process IDs are validated without coercing arbitrary values. All managed Root reads share binding, thread and reentrancy guards. A PID is a live identifier; the cached window handle is a different value. Error-message reads retain native state semantics.

Passed **555 offline tests**, including 199 path/identity cases, 181 live-observation cases, 43 cached-metadata cases and 132 foundation cases. Installed-package verification compiles 34 XML examples, README usage and all three Root guides, and exercises all fifteen observations using a managed native stand-in. Earlier read-only named-session and same-process target-reuse checks succeeded on STAAD.Pro 26.00.01.491 without startup; this increment adds no new live native qualification. The limited evidence does not establish full cross-version/startup support.

## Model paths and identity

Use `GetStaadFileNameOnly()` for the current filename, `GetAnlFileFullPath()` for the expected `.ANL` path and `GetOpenedStaadFilename()` for an absolute normalized path. `IsTarget(string staadFileFullPath)` and `ModelIsOpen(string fileFullPath)` compare complete paths, including directories. Each helper makes one native filename query. An explicitly blank session returns empty/false; malformed output throws. The analysis path is a prediction and does not prove results exist. See the [path-helper contracts and C# examples](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Root-Path-Helpers.md).

## Common questions

### Can I use OpenSTAAD with .NET Core?

This package targets Windows .NET 9 (`net9.0-windows`), using late-bound COM access without a Bentley interop-assembly dependency. It requires the Windows .NET runtime and compatible installed STAAD.Pro; it does not run on Linux or macOS. For the separate .NET Framework package, see [StaadPro.Interop](https://www.nuget.org/packages/StaadPro.Interop).

### Does this preview automate geometry, loads or analysis?

### Where can I find examples or report an issue?

Start with the [installation guide](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Installation.md), [C# Root examples](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Root-Metadata.md) and [wrapper/acquisition design](https://github.com/StruXllence/StaadPro.Interop.Core/blob/staadpro-interop-core/docs/Wrapper-Foundation.md). Report reproducible defects through [GitHub Issues](https://github.com/StruXllence/StaadPro.Interop.Core/issues), or join the [StruXllence community forum](https://forum.struxllence.com) for discussion.

This project is independent and is neither affiliated with, sponsored by nor endorsed by Bentley Systems. Bentley, STAAD, STAAD.Pro and OpenSTAAD names and marks belong to their respective owners. Apache-2.0 applies with the retained MIT-origin notice; see [LICENSE](LICENSE), [NOTICE](NOTICE.md) and [trademark guidance](TRADEMARKS.md). Copyright (c) 2026 StruXllence.

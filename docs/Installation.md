# Installing StaadPro.Interop.Core

This preview contains wrapper/acquisition infrastructure and five cached Root metadata observations. Other domain operations remain pending. It requires Windows and the .NET 9 runtime. No WPF or Framework-package dependency is required. STAAD.Pro must be installed and licensed for live use. See [Root metadata](Root-Metadata.md) for snapshot, unit fallback and lifetime rules.

~~~xml
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0-windows</TargetFramework></PropertyGroup>
<ItemGroup><PackageReference Include="StaadPro.Interop.Core" Version="1.0.0-preview.20261105.1" /></ItemGroup>
~~~

Use the .NET 9.0.3xx SDK with Visual Studio 2022 17.14. The repository global.json selects that feature band. A modern console application needs a conventional entry point with STAThread; an asynchronous entry point does not itself establish a pumping STA context.

The archive is built locally; public package availability requires manual upload and indexing. For a local archive, place it in a local feed and restore with dotnet add package StaadPro.Interop.Core --version 1.0.0-preview.20261105.1 --source C:/Path/To/Feed. After publication use https://api.nuget.org/v3/index.json.

See the [README example](../README.md), [acquisition behavior](Wrapper-Foundation.md) and [roadmap](../ROADMAP.md). The Windows target-platform suffix is a framework identifier, not a Windows 7 runtime-support claim.

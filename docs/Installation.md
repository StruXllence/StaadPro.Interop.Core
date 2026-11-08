# Installing StaadPro.Interop.Core

This preview contains wrapper/acquisition infrastructure and thirty Root operations: metadata/model/path observations, model/application operations, job-information snapshots, window-state requests, input-unit changes and model coordination. Other domain adapters are binding-only placeholders. It requires Windows and the .NET 9 runtime, without WPF or a Framework-package dependency. STAAD.Pro must be installed and licensed for live use. See [Root metadata](Root-Metadata.md), [live observations](Root-Observations.md), [path helpers](Root-Path-Helpers.md), [model lifecycle](Root-Model-Lifecycle.md), [job information/windows](Root-Job-Information-and-Windows.md) and [model coordination](Root-Model-Coordination.md) for contracts and lifetime rules.

~~~xml
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0-windows</TargetFramework></PropertyGroup>
<ItemGroup><PackageReference Include="StaadPro.Interop.Core" Version="1.0.0-preview.20261108.2" /></ItemGroup>
~~~
Use the .NET 9.0.3xx SDK with Visual Studio 2022 17.14. The repository global.json selects that feature band. A modern console application needs a conventional entry point with STAThread; an asynchronous entry point does not itself establish a pumping STA context.

The archive is built locally; public package availability requires manual upload and indexing. For a local archive, place it in a local feed and restore with dotnet add package StaadPro.Interop.Core --version 1.0.0-preview.20261108.2 --source C:/Path/To/Feed. After publication use https://api.nuget.org/v3/index.json.

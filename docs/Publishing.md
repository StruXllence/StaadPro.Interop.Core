# Building and publishing the Core preview

Package ID: **StaadPro.Interop.Core**. Target: **net9.0-windows**. Author: **Srinivasa Rao Masanam**. Company/publisher: **StruXllence**. Copyright: **Copyright (c) 2026 StruXllence**. License: **Apache-2.0**, preserving the MIT-origin notice. Repository: https://github.com/StruXllence/StaadPro.Interop.Core.

~~~powershell
dotnet build StaadPro.Interop.Core.sln -c Release
dotnet test StaadPro.Interop.Core.sln -c Release --no-build
dotnet pack src/StaadPro.Interop.Core/StaadPro.Interop.Core.csproj -c Release --no-build -o artifacts/release
./eng/Verify-CorePackage.ps1 -PackagePath artifacts/release/StaadPro.Interop.Core.1.0.0-preview.20261110.nupkg
~~~
Update README and release notes whenever the documented version/scope changes. Distribution builds must match ReleaseDocumentationVersion. CI archives receive their actual preview.ci version in packaged documentation. Verify a fresh package consumer, XML documentation and package identity before upload; retain its checksum.

Sign in to the branded NuGet.org owner account, select Upload and choose the verified archive. Confirm the separate Core ID, author, framework, license, icon, README and release notes. The Authors field and registry owner are distinct. No upload is performed by the build.

After submission/indexing, confirm the exact version at https://api.nuget.org/v3-flatcontainer/staadpro.interop.core/index.json and restore from the public feed in a new Windows .NET 9 project. A local archive is not proof of public availability. Do not overwrite or unlist the Framework package as part of publishing Core. Keep publishing keys out of source control and chat.

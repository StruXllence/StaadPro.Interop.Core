param([Parameter(Mandatory = $true)][string] $PackagePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$archive = [IO.Compression.ZipFile]::OpenRead($package)
function Read-Entry([string] $name) {
    $entry = $archive.GetEntry($name)
    if ($null -eq $entry) { throw "Missing package entry: $name" }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
try {
    $asset = 'lib/net9.0-windows7.0/StaadPro.Interop.Core.dll'
    if ($null -eq $archive.GetEntry($asset) -or @($archive.Entries | Where-Object FullName -like 'lib/*/*.dll').Count -ne 1) { throw 'Expected one Windows .NET 9 Core asset.' }
    [xml] $documentation = Read-Entry ($asset.Replace('.dll','.xml'))
    $readme = Read-Entry 'README.md'
    [xml] $nuspec = Read-Entry (@($archive.Entries | Where-Object FullName -like '*.nuspec')[0].FullName)
    $metadata = $nuspec.package.metadata
    if ($metadata.id -ne 'StaadPro.Interop.Core' -or $metadata.authors -ne 'Srinivasa Rao Masanam' -or $metadata.copyright -ne 'Copyright (c) 2026 StruXllence') { throw 'Unexpected package identity.' }
    if ($metadata.license.InnerText -ne 'Apache-2.0' -or $metadata.repository.url -ne 'https://github.com/StruXllence/StaadPro.Interop.Core') { throw 'Unexpected license/repository.' }
    if (@($metadata.dependencies.group.dependency).Where({ $null -ne $_ }).Count -ne 0 -or @($metadata.frameworkReferences.group.frameworkReference).Where({ $null -ne $_ }).Count -ne 0) { throw 'Unexpected runtime dependency.' }
    foreach ($name in @('LICENSE','NOTICE.md','LICENSES/MIT-legacy.txt','TRADEMARKS.md','CITATION.cff','ROADMAP.md','docs/Licensing.md','docs/Wrapper-Foundation.md','docs/Installation.md','docs/Publishing.md','docs/Release-Notes.md','images/symbol-only-logo.png','images/logo-with-text.png','images/logo-with-text-400.png')) { if ($null -eq $archive.GetEntry($name)) { throw "Missing entry: $name" } }
    if ((Read-Entry 'LICENSE') -notmatch 'Apache License' -or (Read-Entry 'LICENSES/MIT-legacy.txt') -notmatch 'MIT License') { throw 'Missing licensing terms.' }
    if ($readme -notmatch 'binding-only placeholders' -or $readme -notmatch 'net9.0-windows' -or $readme -match '<img') { throw 'Inaccurate README scope/image syntax.' }
    if (!$readme.StartsWith('![StaadPro.Interop.Core](https://raw.githubusercontent.com/StruXllence/StaadPro.Interop.Core/staadpro-interop-core/docs/images/logo-with-text-400.png)')) { throw 'Unexpected banner.' }
    if ($metadata.icon -ne 'images/symbol-only-logo.png' -or $archive.GetEntry([string]$metadata.icon).Length -gt 1MB) { throw 'Invalid icon.' }
    $version = [string] $metadata.version
    $examples = @($documentation.SelectNodes('//example/code'))
}
finally { $archive.Dispose() }
$consumer = Join-Path (Split-Path -Parent $package) ('consumer-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($consumer)
$feed = [Security.SecurityElement]::Escape((Split-Path -Parent $package))
[IO.File]::WriteAllText((Join-Path $consumer 'NuGet.Config'), "<configuration><packageSources><clear/><add key='local' value='$feed'/></packageSources></configuration>")
$project = "<Project Sdk='Microsoft.NET.Sdk'><PropertyGroup><TargetFramework>net9.0-windows</TargetFramework><OutputType>Exe</OutputType><StartupObject>PackageSmoke</StartupObject><LangVersion>13.0</LangVersion><TreatWarningsAsErrors>true</TreatWarningsAsErrors><RestorePackagesPath>packages</RestorePackagesPath></PropertyGroup><ItemGroup><PackageReference Include='StaadPro.Interop.Core' Version='$version'/></ItemGroup></Project>"
[IO.File]::WriteAllText((Join-Path $consumer 'Consumer.csproj'), $project)
$smoke = @'
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using StaadPro.Interop.Core.Models;
public sealed class NativeRoot
{
    public object Geometry { get; } = new object(); public object Load { get; } = new object();
    public int GetBaseUnit() => 2;
    public object GetInputUnitForForce(ref object value) { value = "kN"; return null; }
    public object GetInputUnitForLength(ref object value) { value = "m"; return null; }
}
internal static class PackageSmoke
{
    [STAThread] private static void Main(string[] arguments)
    {
        var assembly = typeof(OpenStaadWrapper).Assembly;
        if (assembly.GetName().Name != "StaadPro.Interop.Core" || assembly.GetType("StaadPro.Interop.Models.OpenStaadWrapper") != null) throw new Exception("Assembly/namespace collision.");
        if (assembly.GetReferencedAssemblies().Any(a => a.Name == "Newtonsoft.Json" || a.Name == "WindowsBase" || a.Name == "PresentationCore" || a.Name == "StaadPro.Interop")) throw new Exception("Unexpected dependency.");
        var docs = XDocument.Load(arguments[0]);
        using (var wrapper = new OpenStaadWrapper(new NativeRoot()))
        {
            if (!wrapper.IsConnected || wrapper.Root == null || wrapper.Geometry == null || wrapper.Load == null) throw new Exception("Missing wrapper binding.");
            if (typeof(OpenStaadWrapper).GetProperties().Length != 31) throw new Exception("Wrapper parity changed.");
            foreach (var property in typeof(OpenStaadWrapper).GetProperties()) if (!docs.Descendants("member").Any(e => (string)e.Attribute("name") == "P:StaadPro.Interop.Core.Models.OpenStaadWrapper."+property.Name && e.Element("summary") != null)) throw new Exception("Missing IntelliSense: "+property.Name);
            wrapper.RefreshUnits(); wrapper.RefreshComObjects();
        }
        foreach (var contract in assembly.GetTypes().Where(t=>t.IsInterface && t.Namespace=="StaadPro.Interop.Core.Adapters.Interfaces")) if (contract.GetMethods().Any(m=>!m.IsSpecialName)) throw new Exception("Unexpected domain operation: "+contract.Name);
        Console.WriteLine("PASS: installed foundation, wrapper parity, XML and dependency isolation.");
    }
}
'@
[IO.File]::WriteAllText((Join-Path $consumer 'Program.cs'), $smoke)
$readmeCode = [regex]::Match($readme, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($readmeCode)) { throw 'Missing README usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'ReadmeExample.cs'), "namespace ReadmeExample {" + [Environment]::NewLine + $readmeCode + [Environment]::NewLine + "}")
$exampleCode = 'using System; using StaadPro.Interop.Core.Models; using StaadPro.Interop.Core.Services; internal static class XmlExamples {'
for ($index = 0; $index -lt $examples.Count; $index++) {
    $snippet = $examples[$index].InnerText
    $context = 'object rawOpenStaad = new NativeRoot();'
    if ($snippet -notmatch '\bvar\s+wrapper\b') { $context += 'using var wrapper = new OpenStaadWrapper(rawOpenStaad);' }
    $exampleCode += "static void Example$index() {" + $context + [Environment]::NewLine + $snippet + [Environment]::NewLine + '}'
}
[IO.File]::WriteAllText((Join-Path $consumer 'XmlExamples.cs'), $exampleCode + '}')
& dotnet restore (Join-Path $consumer 'Consumer.csproj') --configfile (Join-Path $consumer 'NuGet.Config') --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Consumer restore failed.' }
& dotnet build (Join-Path $consumer 'Consumer.csproj') -c Release --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Consumer compilation failed.' }
$installedXml = Join-Path $consumer "packages/staadpro.interop.core/$version/lib/net9.0-windows7.0/StaadPro.Interop.Core.xml"
if (!(Test-Path -LiteralPath $installedXml)) { throw 'Missing installed IntelliSense XML.' }
[xml] $installedDocumentation = [IO.File]::ReadAllText($installedXml)
if ($installedDocumentation.OuterXml -cne $documentation.OuterXml) { throw 'Installed IntelliSense differs from archive.' }
& (Join-Path $consumer 'bin/Release/net9.0-windows/Consumer.exe') $installedXml
if ($LASTEXITCODE -ne 0) { throw 'Consumer runtime failed.' }
$restored = Join-Path $consumer "packages/staadpro.interop.core/$version/staadpro.interop.core.$version.nupkg"
if ((Get-FileHash -LiteralPath $restored -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash) { throw 'Restored archive differs.' }
Write-Output ("PASS: " + $examples.Count + ' XML examples and README usage compiled without executing native examples.')

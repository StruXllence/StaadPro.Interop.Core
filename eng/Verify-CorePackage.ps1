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
    $rootGuide = Read-Entry 'docs/Root-Metadata.md'
    [xml] $nuspec = Read-Entry (@($archive.Entries | Where-Object FullName -like '*.nuspec')[0].FullName)
    $metadata = $nuspec.package.metadata
    if ($metadata.id -ne 'StaadPro.Interop.Core' -or $metadata.authors -ne 'Srinivasa Rao Masanam' -or $metadata.copyright -ne 'Copyright (c) 2026 StruXllence') { throw 'Unexpected package identity.' }
    if ([string]$metadata.title -ne 'StaadPro.Interop.Core - OpenSTAAD C# API for STAAD.Pro (.NET 9)') { throw 'Unexpected package title.' }
    foreach ($term in @('C#','OpenSTAAD','STAAD.Pro','Windows .NET 9','five Root observations')) {
        if (([string]$metadata.description).IndexOf($term,[StringComparison]::OrdinalIgnoreCase) -lt 0) { throw "Incomplete package description: $term" }
    }
    $packageTags = @(([string]$metadata.tags) -split '\s+' | Where-Object { $_ })
    foreach ($tag in @('openstaad','staadpro','csharp','dotnet-core','dotnet9','windows','com-interop','structural-engineering')) {
        if ($packageTags -notcontains $tag) { throw "Missing package tag: $tag" }
    }
    if ($metadata.license.InnerText -ne 'Apache-2.0' -or $metadata.repository.url -ne 'https://github.com/StruXllence/StaadPro.Interop.Core') { throw 'Unexpected license/repository.' }
    if (@($metadata.dependencies.group.dependency).Where({ $null -ne $_ }).Count -ne 0 -or @($metadata.frameworkReferences.group.frameworkReference).Where({ $null -ne $_ }).Count -ne 0) { throw 'Unexpected runtime dependency.' }
    foreach ($name in @('LICENSE','NOTICE.md','LICENSES/MIT-legacy.txt','TRADEMARKS.md','CITATION.cff','ROADMAP.md','docs/Licensing.md','docs/Wrapper-Foundation.md','docs/Root-Metadata.md','docs/Installation.md','docs/Publishing.md','docs/Release-Notes.md','images/symbol-only-logo.png','images/logo-with-text-core.png','images/logo-with-text-core-400.png')) { if ($null -eq $archive.GetEntry($name)) { throw "Missing entry: $name" } }
    if ((Read-Entry 'LICENSE') -notmatch 'Apache License' -or (Read-Entry 'LICENSES/MIT-legacy.txt') -notmatch 'MIT License') { throw 'Missing licensing terms.' }
    if ($readme -notmatch 'binding-only placeholders' -or $readme -notmatch 'five.*Root' -or $readme -notmatch 'net9.0-windows' -or $readme -match '<img') { throw 'Inaccurate README scope/image syntax.' }
    if (!$readme.StartsWith('![StaadPro.Interop.Core](https://raw.githubusercontent.com/StruXllence/StaadPro.Interop.Core/staadpro-interop-core/docs/images/logo-with-text-core-400.png)')) { throw 'Unexpected banner.' }
    foreach ($badge in @(
        'https://github.com/StruXllence/StaadPro.Interop.Core/actions/workflows/build-and-test.yml/badge.svg?branch=staadpro-interop-core',
        'https://img.shields.io/nuget/vpre/StaadPro.Interop.Core?style=flat-square&label=nuget&color=F78C1E',
        'https://img.shields.io/badge/License-Apache%202.0-007EC6?style=flat-square',
        'https://img.shields.io/badge/.NET-9.0%20Windows-512BD4?style=flat-square&logo=dotnet&logoColor=white'
    )) { if (!$readme.Contains($badge)) { throw "Missing or incorrect Core badge: $badge" } }
    if ($readme.Contains('img.shields.io/badge/.NET_Framework') -or $readme.Contains('img.shields.io/nuget/vpre/StaadPro.Interop?')) { throw 'Badges must describe the Core package.' }
    if ($metadata.icon -ne 'images/logo-with-text-core-400.png' -or $archive.GetEntry([string]$metadata.icon).Length -gt 1MB) { throw 'Invalid icon.' }
    $version = [string] $metadata.version
    if ($readme -notmatch ('(?m)^## Version ' + [regex]::Escape($version) + '\r?$') -or (Read-Entry 'docs/Release-Notes.md') -notmatch ('(?m)^## ' + [regex]::Escape($version) + ' - ')) { throw 'Package version and current documentation differ.' }
    if ($documentation.SelectNodes('//include|//inheritdoc').Count -ne 0) { throw 'Shipped IntelliSense must expand source documentation.' }
    foreach ($operation in @('GetApplicationVersion','GetBaseUnit','GetInputUnitForForce','GetInputUnitForLength','GetMainWindowHandle')) {
        $contract = $documentation.SelectSingleNode("//member[@name='M:StaadPro.Interop.Core.Adapters.Interfaces.IOSRoot.$operation']")
        $implementation = $documentation.SelectSingleNode("//member[@name='M:StaadPro.Interop.Core.Adapters.Models.OSRootAdapter.$operation']")
        if ($null -eq $contract -or $null -eq $implementation -or $contract.InnerXml -cne $implementation.InnerXml) { throw "Missing/mismatched Root documentation: $operation" }
        foreach ($section in @('summary','returns','remarks','exception','example/code')) { if ($null -eq $contract.SelectSingleNode($section)) { throw "Incomplete Root documentation: $operation/$section" } }
    }
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
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
public sealed class NativeRoot
{
    public int Calls;
    public object Geometry { get; } = new object(); public object Load { get; } = new object();
    public int GetBaseUnit() { Calls++; return 2; }
    public object GetInputUnitForForce(ref object value) { Calls++; value = "kN"; return null; }
    public object GetInputUnitForLength(ref object value) { Calls++; value = "m"; return null; }
    public string GetApplicationVersion(ref object major, ref object update, ref object revision, ref object build)
    { Calls++; major = 26; update = 0; revision = 1; build = 491; return "26.00.01.491"; }
    public int GetMainWindowHandle() { Calls++; return 12345; }
}
internal static class PackageSmoke
{
    [STAThread] private static void Main(string[] arguments)
    {
        var assembly = typeof(OpenStaadWrapper).Assembly;
        if (assembly.GetName().Name != "StaadPro.Interop.Core" || assembly.GetType("StaadPro.Interop.Models.OpenStaadWrapper") != null) throw new Exception("Assembly/namespace collision.");
        if (assembly.GetReferencedAssemblies().Any(a => a.Name == "Newtonsoft.Json" || a.Name == "WindowsBase" || a.Name == "PresentationCore" || a.Name == "StaadPro.Interop")) throw new Exception("Unexpected dependency.");
        var docs = XDocument.Load(arguments[0]);
        var native = new NativeRoot();
        using (var wrapper = new OpenStaadWrapper(native))
        {
            if (!wrapper.IsConnected || wrapper.Root == null || wrapper.Geometry == null || wrapper.Load == null) throw new Exception("Missing wrapper binding.");
            if (typeof(OpenStaadWrapper).GetProperties().Length != 31) throw new Exception("Wrapper parity changed.");
            foreach (var property in typeof(OpenStaadWrapper).GetProperties()) if (!docs.Descendants("member").Any(e => (string)e.Attribute("name") == "P:StaadPro.Interop.Core.Models.OpenStaadWrapper."+property.Name && e.Element("summary") != null)) throw new Exception("Missing IntelliSense: "+property.Name);
            var root = wrapper.Root; int calls = native.Calls;
            var version = root.GetApplicationVersion();
            if (version.Display != "v26.00.01.491" || root.GetBaseUnit() != BaseUnitSystem.Metric || root.GetInputUnitForForce() != ForceInputUnit.KiloNewton || root.GetInputUnitForLength() != LengthInputUnit.Meter || root.GetMainWindowHandle() != new IntPtr(12345)) throw new Exception("Incorrect Root projection.");
            version.Major = 999;
            if (root.GetApplicationVersion().Major != 26 || native.Calls != calls) throw new Exception("Cache isolation or zero-dispatch contract failed.");
            wrapper.RefreshUnits();
            if (!ReferenceEquals(wrapper.Root, root) || root.GetBaseUnit() != BaseUnitSystem.Metric) throw new Exception("Unit refresh replaced Root.");
            wrapper.RefreshComObjects();
            bool staleRejected = false;
            try { root.GetBaseUnit(); } catch (InvalidOperationException) { staleRejected = true; }
            if (!staleRejected) throw new Exception("Stale Root remained usable.");
            var current = wrapper.Root;
            if (current.GetApplicationVersion().Major != 26) throw new Exception("Reacquired Root failed.");
            wrapper.Dispose();
            bool disposedRejected = false;
            try { current.GetMainWindowHandle(); } catch (ObjectDisposedException) { disposedRejected = true; }
            if (!disposedRejected) throw new Exception("Disposed Root remained usable.");
        }
        var expected = new[] { "GetApplicationVersion", "GetBaseUnit", "GetInputUnitForForce", "GetInputUnitForLength", "GetMainWindowHandle" };
        foreach (var contract in assembly.GetTypes().Where(t=>t.IsInterface && t.Namespace=="StaadPro.Interop.Core.Adapters.Interfaces"))
        {
            var methods = contract.GetMethods().Where(m=>!m.IsSpecialName).ToArray();
            if (contract == typeof(IOSRoot))
            {
                if (!methods.Select(m=>m.Name).OrderBy(n=>n).SequenceEqual(expected.OrderBy(n=>n)) || methods.Any(m=>m.GetParameters().Length!=0)) throw new Exception("Unexpected Root contracts.");
            }
            else if (methods.Length != 0) throw new Exception("Unexpected domain operation: "+contract.Name);
        }
        Console.WriteLine("PASS: installed foundation, five guarded Root observations, XML and dependency isolation.");
    }
}
'@
[IO.File]::WriteAllText((Join-Path $consumer 'Program.cs'), $smoke)
$readmeCode = [regex]::Match($readme, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($readmeCode)) { throw 'Missing README usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'ReadmeExample.cs'), "namespace ReadmeExample {" + [Environment]::NewLine + $readmeCode + [Environment]::NewLine + "}")
$guideCode = [regex]::Match($rootGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($guideCode)) { throw 'Missing Root guide usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootGuideExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootGuideExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $guideCode + [Environment]::NewLine + '}}')
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
Write-Output ("PASS: " + $examples.Count + ' XML examples, README and Root guide usage compiled without executing native examples.')

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
    foreach ($entry in $archive.Entries) {
        if ($entry.FullName -match '(?i)(^|/)ROADMAP\.md$|Implementation-Plan|(^|/)reports/|(^|/)artifacts/') { throw "Unexpected package entry: $($entry.FullName)" }
    }
    $asset = 'lib/net9.0-windows7.0/StaadPro.Interop.Core.dll'
    if ($null -eq $archive.GetEntry($asset) -or @($archive.Entries | Where-Object FullName -like 'lib/*/*.dll').Count -ne 1) { throw 'Expected one Windows .NET 9 Core asset.' }
    [xml] $documentation = Read-Entry ($asset.Replace('.dll','.xml'))
    $readme = Read-Entry 'README.md'
    if (!$readme.Contains('https://img.shields.io/badge/StruXllence%20Themes-Install-007ACC?style=flat-square') -or $readme.Contains('img.shields.io/visual-studio-marketplace/')) { throw 'The packaged Marketplace installation badge is missing or uses a retired endpoint.' }
    $rootGuide = Read-Entry 'docs/Root-Metadata.md'
    $observationGuide = Read-Entry 'docs/Root-Observations.md'
    $pathGuide = Read-Entry 'docs/Root-Path-Helpers.md'
    $lifecycleGuide = Read-Entry 'docs/Root-Model-Lifecycle.md'
    $jobWindowGuide = Read-Entry 'docs/Root-Job-Information-and-Windows.md'
    $coordinationGuide = Read-Entry 'docs/Root-Model-Coordination.md'
    $analysisGuide = Read-Entry 'docs/Root-Analysis.md'
    $editGuide = Read-Entry 'docs/Root-Model-Edits.md'
    $projectGuide = Read-Entry 'docs/Root-Job-Analysis-and-Projects.md'
    $creationGuide = Read-Entry 'docs/Root-Creation-Views-and-Shutdown.md'
    $optionalGuide = Read-Entry 'docs/Root-View-Changes-and-Optional-Operations.md'
    $forceGuide = Read-Entry 'docs/Member-Force-Queries.md'
    $loadGuide = Read-Entry 'docs/Load-Case-Metadata.md'
    [xml] $nuspec = Read-Entry (@($archive.Entries | Where-Object FullName -like '*.nuspec')[0].FullName)
    $metadata = $nuspec.package.metadata
    if ($metadata.id -ne 'StaadPro.Interop.Core' -or $metadata.authors -ne 'Srinivasa Rao Masanam' -or $metadata.copyright -ne 'Copyright (c) 2026 StruXllence') { throw 'Unexpected package identity.' }
    if ([string]$metadata.title -ne 'StaadPro.Interop.Core - OpenSTAAD C# API for STAAD.Pro (.NET 9)') { throw 'Unexpected package title.' }
    foreach ($term in @('C#','OpenSTAAD','STAAD.Pro','Windows .NET 9','53 common Root contracts')) {
        if (([string]$metadata.description).IndexOf($term,[StringComparison]::OrdinalIgnoreCase) -lt 0) { throw "Incomplete package description: $term" }
    }
    $packageTags = @(([string]$metadata.tags) -split '\s+' | Where-Object { $_ })
    foreach ($tag in @('openstaad','staadpro','csharp','dotnet-core','dotnet9','windows','com-interop','structural-engineering')) {
        if ($packageTags -notcontains $tag) { throw "Missing package tag: $tag" }
    }
    if ($metadata.license.InnerText -ne 'Apache-2.0' -or $metadata.repository.url -ne 'https://github.com/StruXllence/StaadPro.Interop.Core') { throw 'Unexpected license/repository.' }
    if (@($metadata.dependencies.group.dependency).Where({ $null -ne $_ }).Count -ne 0 -or @($metadata.frameworkReferences.group.frameworkReference).Where({ $null -ne $_ }).Count -ne 0) { throw 'Unexpected runtime dependency.' }
    foreach ($name in @('LICENSE','NOTICE.md','LICENSES/MIT-legacy.txt','TRADEMARKS.md','CITATION.cff','docs/Licensing.md','docs/Wrapper-Foundation.md','docs/Root-Creation-Views-and-Shutdown.md','docs/Root-View-Changes-and-Optional-Operations.md','docs/Member-Force-Queries.md','docs/Load-Case-Metadata.md','docs/Root-Metadata.md','docs/Root-Observations.md','docs/Root-Path-Helpers.md','docs/Root-Model-Lifecycle.md','docs/Root-Job-Information-and-Windows.md','docs/Root-Model-Coordination.md','docs/Root-Analysis.md','docs/Root-Model-Edits.md','docs/Root-Job-Analysis-and-Projects.md','docs/Installation.md','docs/Publishing.md','docs/Release-Notes.md','images/symbol-only-logo.png','images/symbol-only-logo-core.png','images/logo-with-text-core.png','images/logo-with-text-core-400.png')) { if ($null -eq $archive.GetEntry($name)) { throw "Missing entry: $name" } }
    if ((Read-Entry 'LICENSE') -notmatch 'Apache License' -or (Read-Entry 'LICENSES/MIT-legacy.txt') -notmatch 'MIT License') { throw 'Missing licensing terms.' }
    if ($readme -notmatch 'binding-only placeholders' -or $readme -notmatch '53 common Root' -or $readme -notmatch 'net9.0-windows' -or $readme -match '<img') { throw 'Inaccurate README scope/image syntax.' }
    if (!$readme.StartsWith('![StaadPro.Interop.Core](https://raw.githubusercontent.com/StruXllence/StaadPro.Interop.Core/staadpro-interop-core/docs/images/logo-with-text-core-400.png)')) { throw 'Unexpected banner.' }
    foreach ($badge in @(
        'https://github.com/StruXllence/StaadPro.Interop.Core/actions/workflows/build-and-test.yml/badge.svg?branch=staadpro-interop-core',
        'https://img.shields.io/nuget/vpre/StaadPro.Interop.Core?style=flat-square&label=nuget&color=F78C1E',
        'https://img.shields.io/badge/License-Apache%202.0-007EC6?style=flat-square',
        'https://img.shields.io/badge/.NET-9.0%20Windows-512BD4?style=flat-square&logo=dotnet&logoColor=white'
    )) { if (!$readme.Contains($badge)) { throw "Missing or incorrect Core badge: $badge" } }
    if ($readme.Contains('img.shields.io/badge/.NET_Framework') -or $readme.Contains('img.shields.io/nuget/vpre/StaadPro.Interop?')) { throw 'Badges must describe the Core package.' }
    if ($metadata.icon -ne 'images/symbol-only-logo-core.png' -or $archive.GetEntry([string]$metadata.icon).Length -gt 1MB) { throw 'Invalid icon.' }
    Add-Type -AssemblyName System.Drawing
    $iconStream = $archive.GetEntry([string]$metadata.icon).Open()
    $iconBitmap = $null
    try {
        $iconBitmap = [Drawing.Bitmap]::new($iconStream)
        if ($iconBitmap.Width -ne $iconBitmap.Height -or $iconBitmap.GetPixel(0,0).A -ne 0) { throw 'The Core icon must be square with a transparent background.' }
    } finally { if ($null -ne $iconBitmap) { $iconBitmap.Dispose() }; $iconStream.Dispose() }
    if(!$readme.Contains('https://marketplace.visualstudio.com/items?itemName=StruXllence.struxllence-themes') -or !$readme.Contains('code --install-extension StruXllence.struxllence-themes')){throw 'Missing theme installation resource in packaged README.'}
    $version = [string] $metadata.version
    if ($readme -notmatch ('(?m)^## Version ' + [regex]::Escape($version) + '\r?$') -or (Read-Entry 'docs/Release-Notes.md') -notmatch ('(?m)^## ' + [regex]::Escape($version) + ' - ')) { throw 'Package version and current documentation differ.' }
    if ($documentation.SelectNodes('//include|//inheritdoc').Count -ne 0) { throw 'Shipped IntelliSense must expand source documentation.' }
    foreach ($operation in @('GetApplicationVersion','GetBaseUnit','GetInputUnitForForce','GetInputUnitForLength','GetMainWindowHandle','GetStaadFileFullPath','GetStaadFileFolder','IsPhysicalModel','GetErrorMessage','GetProcessId','GetStaadFileNameOnly','GetAnlFileFullPath','GetOpenedStaadFilename','IsTarget','ModelIsOpen','SetSilentMode','SaveModel','OpenSTAADFile','CloseSTAADFile','ShowApplication','GetShortJobInfo','GetFullJobInfo','MinimizeStaadMainWindow','MaximizeStaadMainWindow','RestoreStaadMainWindow','SetInputUnitsEx','AwaitModelHandle','OpenStaadModel','CloseCurrentStaadModel','CloseWriteReopenStaadFile','AnalyzeEx','Analyze','IsAnalyzing','GetAnalysisStatus','GetAnalysisStatus(System.String)','SetInputUnitForLength','SetInputUnitForForce','SetShortJobInfo(System.String,System.String,System.String)','SetShortJobInfo(StaadPro.Interop.Core.Models.ShortJobInfo)','UpdateStructure','SetFullJobInfo(System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String)','AnalyzeModel(StaadPro.Interop.Core.Enums.Root.AnalysisEngine)','GetConnectedProjectInfo','SetConnectedProjectInfo(System.String,System.String)','SetConnectedProjectInfo(StaadPro.Interop.Core.Models.Root.ConnectedProjectInfo)','GetProcessHandle','NewSTAADFile(System.String,StaadPro.Interop.Core.Enums.LengthInputUnit,StaadPro.Interop.Core.Enums.ForceInputUnit)','CreateNamedView(System.String,System.Int32)','SaveNamedView(System.String)','Quit','RemoveNamedView(System.String)','ModifyNamedView(System.String,System.Collections.Generic.IEnumerable{System.Int32},StaadPro.Interop.Core.Enums.Root.NamedViewEntityType,System.Int32)','RetrieveMemberForces(System.Collections.Generic.IEnumerable{System.Int32},System.Collections.Generic.IEnumerable{System.Int32})')) {
        $memberId = if ($operation -in @('IsTarget','ModelIsOpen','OpenSTAADFile')) { $operation + '(System.String)' } elseif ($operation -in @('SetSilentMode','SaveModel')) { $operation + '(StaadPro.Interop.Core.Enums.SilentMode)' } elseif ($operation -eq 'SetInputUnitsEx') { $operation + '(StaadPro.Interop.Core.Enums.LengthInputUnit,StaadPro.Interop.Core.Enums.ForceInputUnit)' } elseif ($operation -eq 'AwaitModelHandle') { $operation + '(System.String,System.Double,System.Double)' } elseif ($operation -eq 'OpenStaadModel') { $operation + '(System.String,System.Int32,System.Int32)' } elseif ($operation -eq 'CloseWriteReopenStaadFile') { $operation + '(System.String,System.Collections.Generic.List{System.String},System.Int32,System.Int32)' } elseif ($operation -eq 'AnalyzeEx') { $operation + '(StaadPro.Interop.Core.Enums.SilentMode,StaadPro.Interop.Core.Enums.HiddenMode,StaadPro.Interop.Core.Enums.WaitAnalysisMode)' } elseif ($operation -eq 'SetInputUnitForLength') { $operation + '(StaadPro.Interop.Core.Enums.LengthInputUnit)' } elseif ($operation -eq 'SetInputUnitForForce') { $operation + '(StaadPro.Interop.Core.Enums.ForceInputUnit)' } else { $operation }
        $contract = $documentation.SelectSingleNode("//member[@name='M:StaadPro.Interop.Core.Adapters.Interfaces.IOSRoot.$memberId']")
        $implementation = $documentation.SelectSingleNode("//member[@name='M:StaadPro.Interop.Core.Adapters.Models.OSRootAdapter.$memberId']")
        if ($null -eq $contract -or $null -eq $implementation -or $contract.InnerXml -cne $implementation.InnerXml) { throw "Missing/mismatched Root documentation: $operation" }
        foreach ($section in @('summary','returns','remarks','exception','example/code')) { if ($section -eq 'returns' -and $operation -in @('RemoveNamedView(System.String)','ModifyNamedView(System.String,System.Collections.Generic.IEnumerable{System.Int32},StaadPro.Interop.Core.Enums.Root.NamedViewEntityType,System.Int32)','NewSTAADFile(System.String,StaadPro.Interop.Core.Enums.LengthInputUnit,StaadPro.Interop.Core.Enums.ForceInputUnit)','CreateNamedView(System.String,System.Int32)','SaveNamedView(System.String)','Quit','MinimizeStaadMainWindow','MaximizeStaadMainWindow','RestoreStaadMainWindow','CloseCurrentStaadModel','Analyze','SetInputUnitForLength','SetInputUnitForForce','SetShortJobInfo(System.String,System.String,System.String)','SetShortJobInfo(StaadPro.Interop.Core.Models.ShortJobInfo)','UpdateStructure','SetFullJobInfo(System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String)','AnalyzeModel(StaadPro.Interop.Core.Enums.Root.AnalysisEngine)')) { continue }; if ($null -eq $contract.SelectSingleNode($section)) { throw "Incomplete Root documentation: $operation/$section" } }
    }
    foreach ($pair in @(
        @('IOSRootCapabilities','OSRootCapabilitiesAdapter'),
        @('IOSRootAdinaExport','OSRootAdinaExportAdapter'),
        @('IOSRootAnalysisErrorMessages','OSRootAnalysisErrorMessagesAdapter'),
        @('IOSRootAnalysisWarningMessages','OSRootAnalysisWarningMessagesAdapter')
    )) {
        $prefix = 'M:StaadPro.Interop.Core.Adapters.Interfaces.Capabilities.' + $pair[0] + '.'
        $members = @($documentation.SelectNodes('//member') | Where-Object { $_.name.StartsWith($prefix) })
        if ($members.Count -ne $(if ($pair[0] -eq 'IOSRootCapabilities') { 3 } else { 1 })) { throw "Unexpected optional contract count: $($pair[0])" }
        foreach ($contract in $members) {
            $concreteId = 'M:StaadPro.Interop.Core.Adapters.Models.Capabilities.' + $pair[1] + '.' + $contract.name.Substring($prefix.Length)
            $concrete = $documentation.SelectSingleNode("//member[@name='$concreteId']")
            if ($null -eq $concrete -or $concrete.InnerXml -cne $contract.InnerXml) { throw "Mismatched optional IntelliSense: $($contract.name)" }
            foreach ($section in @('summary','returns','remarks','exception','example/code')) {
                if ($null -eq $contract.SelectSingleNode($section)) { throw "Incomplete optional IntelliSense: $($contract.name)/$section" }
            }
        }
    }
    $gatewayContract = $documentation.SelectSingleNode("//member[@name='P:StaadPro.Interop.Core.Adapters.Interfaces.IOSRoot.Capabilities']")
    $gatewayConcrete = $documentation.SelectSingleNode("//member[@name='P:StaadPro.Interop.Core.Adapters.Models.OSRootAdapter.Capabilities']")
    if ($null -eq $gatewayContract -or $null -eq $gatewayConcrete -or $gatewayContract.InnerXml -cne $gatewayConcrete.InnerXml) { throw 'Mismatched capability gateway IntelliSense.' }
    foreach($domain in @('Load','Output')) {
        $prefix='M:StaadPro.Interop.Core.Adapters.Interfaces.IOS'+$domain+'.'
        $contracts=@($documentation.SelectNodes('//member') | Where-Object {$_.name.StartsWith($prefix)})
        if($contracts.Count -ne $(if($domain -eq 'Load'){16}else{3})){throw 'Unexpected force domain documentation count.'}
        foreach($contract in $contracts){$id='M:StaadPro.Interop.Core.Adapters.Models.OS'+$domain+'Adapter.'+$contract.name.Substring($prefix.Length);$concrete=$documentation.SelectSingleNode("//member[@name='$id']");if($null -eq $concrete -or $concrete.InnerXml -cne $contract.InnerXml){throw 'Mismatched force domain IntelliSense.'};foreach($tag in @('summary','returns','remarks','exception','example/code')){if($null -eq $contract.SelectSingleNode($tag)){throw 'Incomplete force domain IntelliSense.'}}}
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Interfaces.Capabilities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Models;
public class NativeRoot
{
    public int Calls;
    public string InputForceText="kN",InputLengthText="m";
    public object Geometry { get; } = new object(); public object Load { get; } = new object();
    public int GetBaseUnit() { Calls++; return 2; }
    public object GetInputUnitForForce(ref object value) { Calls++; value = InputForceText; return null; }
    public object GetInputUnitForLength(ref object value) { Calls++; value = InputLengthText; return null; }
    public string GetApplicationVersion(ref object major, ref object update, ref object revision, ref object build)
    { Calls++; major = 26; update = 0; revision = 1; build = 491; return "26.00.01.491"; }
    public object WindowHandleValue = 12345;
    public object GetMainWindowHandle() { Calls++; return WindowHandleValue; }
    public object[] JobFields = { " Name ? ", "Client", "Engineer", "", "N-17", "R2", "Part", "Ref", "Checker", "", "Approver", "", "first\r\nsecond" };
    public int UnwrittenJobField = -1;
    private void JobOutput(ref object value, int index) { if (value is not string) throw new Exception("Job output must be BSTR-shaped."); if (index != UnwrittenJobField) value = JobFields[index]; }
    public void GetShortJobInfo(ref object name, ref object client, ref object originator) { Calls++; JobOutput(ref name,0); JobOutput(ref client,1); JobOutput(ref originator,2); }
    public void GetFullJobInfo(ref object name,ref object client,ref object originator,ref object originated,ref object number,ref object revision,ref object part,ref object reference,ref object checker,ref object dateChecked,ref object approver,ref object approved,ref object comments)
    { Calls++;JobOutput(ref name,0);JobOutput(ref client,1);JobOutput(ref originator,2);JobOutput(ref originated,3);JobOutput(ref number,4);JobOutput(ref revision,5);JobOutput(ref part,6);JobOutput(ref reference,7);JobOutput(ref checker,8);JobOutput(ref dateChecked,9);JobOutput(ref approver,10);JobOutput(ref approved,11);JobOutput(ref comments,12); }
    public object PathValue = @"C:\Models\Structure.std", FolderValue = @"C:\Models\", PhysicalValue = true, ErrorValue = "", ProcessIdValue = 321u;
    public readonly List<string> LifecycleCalls = new List<string>();
    public Exception MutationFailure;
    public bool TransitionPaths;
    public object PreviousMode = false;
    private void Mutation(string name) { Calls++; LifecycleCalls.Add(name); if (MutationFailure != null) throw MutationFailure; }
    public object SetSilentMode(object mode) { if (mode is not int) throw new Exception("Mode must be Int32."); Mutation("SetSilentMode"); return PreviousMode; }
    public void SetInputUnits(object length, object force) { if (length is not int || force is not int) throw new Exception("Units require Int32."); Mutation("SetInputUnits"); }
    public void SaveModel(object mode) { if (mode is not int) throw new Exception("Save mode must be Int32."); Mutation("SaveModel"); }
    public void OpenSTAADFile(object path) { if (path is not string text || !System.IO.Path.IsPathFullyQualified(text)) throw new Exception("Open requires a full path."); Mutation("OpenSTAADFile"); if (TransitionPaths) PathValue = text; }
    public void CloseSTAADFile() { Mutation("CloseSTAADFile"); if (TransitionPaths) PathValue = ""; }
    public object ShowApplication() { Mutation("ShowApplication"); return false; }
    public object AnalysisResultValue = 2, AnalysisStatusValue = 3, RunningValue = true;
    public int? AnalysisWarnings = 1, AnalysisErrors = 0;
    public double? AnalysisCpu = .25;
    public string LastAnalysisPath;
    public void Analyze() => Mutation("Analyze");
    public object AnalyzeEx(int silent,int hidden,int wait) { if(silent!=1||hidden!=1||wait!=0)throw new Exception("Analysis flag order changed.");Mutation("AnalyzeEx");return AnalysisResultValue; }
    public object IsAnalyzing() { Calls++;return RunningValue; }
    public object GetAnalysisStatus(string path,ref int warnings,ref int errors,ref double seconds)
    {
        Calls++;LastAnalysisPath=path;
        if(warnings!=int.MinValue||errors!=int.MinValue||!double.IsNaN(seconds))throw new Exception("Analysis buffers require Int32/Double sentinels.");
        if(AnalysisWarnings.HasValue)warnings=AnalysisWarnings.Value;if(AnalysisErrors.HasValue)errors=AnalysisErrors.Value;if(AnalysisCpu.HasValue)seconds=AnalysisCpu.Value;
        return AnalysisStatusValue;
    }
    public int? ChangedLength,ChangedForce;
    public string[] WrittenShortInfo;
    public Action BeforeShortWrite;
    public void SetInputUnitForLength(object code) { if(code is not int n)throw new Exception("Length requires Int32.");ChangedLength=n;InputLengthText=n==1?"ft":"mm";Mutation("SetInputUnitForLength"); }
    public void SetInputUnitForForce(object code) { if(code is not int n)throw new Exception("Force requires Int32.");ChangedForce=n;InputForceText="N";Mutation("SetInputUnitForForce"); }
    public void SetShortJobInfo(object name,object client,object engineer) { if(name is not string || client is not string || engineer is not string)throw new Exception("Short info requires strings.");BeforeShortWrite?.Invoke();WrittenShortInfo=new[]{(string)name,(string)client,(string)engineer};Mutation("SetShortJobInfo"); }
    public void UpdateStructure()=>Mutation("UpdateStructure");
    public string LastRemovedView, LastModifiedView;
    public int[] ModifiedIds;
    public void RemoveNamedView(object name,object error) { if(name is not string s||error is not int n||n!=0)throw new Exception("Removal argument shape changed.");LastRemovedView=s;Mutation("RemoveNamedView"); }
    public void ModifyNamedView(object name,object ids,object entity,object flag) { if(name is not string s||ids is not int[] a||entity is not int q||flag is not int f||q!=1||f!=-7)throw new Exception("Modification argument shape changed.");LastModifiedView=s;ModifiedIds=a;Mutation("ModifyNamedView"); }

    public string[] WrittenFullInfo, AssignedProject;
    public int? Engine;
    public object ProjectFlag=true, ProjectSetFlag=true, ProjectId=" ID ", ProjectName=" Name ";
    public bool WriteProjectId=true, WriteProjectName=true;
    public void SetFullJobInfo(string a,string b,string c,string d,string e,string f,string g,string h,string i,string j,string k,string l,string m)
    { WrittenFullInfo=new[]{a,b,c,d,e,f,g,h,i,j,k,l,m};Mutation("SetFullJobInfo"); }
    public void AnalyzeModel(object engine) { if(engine is not int n)throw new Exception("Engine requires Int32.");Engine=n;Mutation("AnalyzeModel"); }
    public object GetCONNECTEDProjectInfo(ref object id,ref object name)
    { Calls++;if(id is not string||name is not string||Equals(id,name))throw new Exception("Distinct string output markers required.");if(WriteProjectId)id=ProjectId;if(WriteProjectName)name=ProjectName;return ProjectFlag; }
    public object SetCONNECTEDProjectInfo(object id,object name)
    { if(id is not string||name is not string)throw new Exception("Project inputs require strings.");AssignedProject=new[]{(string)id,(string)name};Mutation("SetCONNECTEDProjectInfo");return ProjectSetFlag; }
    public object ProcessHandleValue = ulong.MaxValue;
    public object[] CreatedModel, CreatedView;
    public string SavedView;
    public object GetProcessHandle() { Calls++; return ProcessHandleValue; }
    public void NewSTAADFile(object path,object length,object force)
    { if(path is not string||length is not int||force is not int)throw new Exception("Creation argument types changed.");CreatedModel=new[]{path,length,force};Mutation("NewSTAADFile"); }
    public void CreateNamedView(object name,object flag)
    { if(name is not string||flag is not int)throw new Exception("View argument types changed.");CreatedView=new[]{name,flag};Mutation("CreateNamedView"); }
    public void SaveNamedView(object name)
    { if(name is not string text)throw new Exception("View name type changed.");SavedView=text;Mutation("SaveNamedView"); }
    public void Quit()=>Mutation("Quit");
    public bool WriteOutput = true;
    public void GetSTAADFile(ref object value, object fullPath)
    { Calls++; if (value is not string || fullPath is not true) throw new Exception("Incorrect native filename arguments."); if (WriteOutput) value = PathValue; }
    public void GetSTAADFileFolder(ref object value)
    { Calls++; if (value is not string) throw new Exception("Incorrect native folder output."); if (WriteOutput) value = FolderValue; }
    public object IsPhysicalModel() { Calls++; return PhysicalValue; }
    public object GetErrorMessage() { Calls++; return ErrorValue; }
    public object GetProcessId() { Calls++; return ProcessIdValue; }
}
public sealed class OptionalNativeRoot : NativeRoot
{
    public int MessageCode=1,ExportCode=-14501,Count=1;
    public int MessageCalls,StatusCalls,ExportCalls;
    public bool WriteMessages=true;
    public new string GetApplicationVersion(ref object major,ref object update,ref object revision,ref object build)
    {major=26;update=0;revision=0;build=340;return "26.0.0.340";}
    public new int GetAnalysisStatus(string path,ref int warnings,ref int errors,ref double cpu)
    {StatusCalls++;warnings=Count;errors=Count;cpu=0;return 2;}
    public int GetAnalysisErrorMessages(string path,ref object buffer)=>Messages(ref buffer);
    public int GetAnalysisWarningMessages(string path,ref object buffer)=>Messages(ref buffer);
    private int Messages(ref object buffer)
    {MessageCalls++;if(WriteMessages){var a=(string[])buffer;for(int i=0;i<a.Length;i++)a[i]="message "+i;}return MessageCode;}
    public int ExportModelToADINA(int axis,int loads)
    {if(axis!=0||loads!=0)throw new Exception("Export options changed.");ExportCalls++;return ExportCode;}
}
public sealed class ForceOutput
{
    public bool Available=true,Success=true;public readonly List<string> Calls=new();
    public object AreResultsAvailable(){Calls.Add("available");return Available;}
    public object GetMemberEndForces(int beam,int end,int load,ref object buffer,int axes){Calls.Add($"{beam}:{end}:{load}:{axes}");buffer=new double[]{1,2,3,4,5,6};return Success;}
}
public sealed class ForceLoad
{public int Titles;public string GetLoadCaseTitle(int id){Titles++;return "Case "+id;}}
public sealed class ForceRoot : NativeRoot
{public ForceOutput Output {get;}=new ForceOutput();public new ForceLoad Load {get;}=new ForceLoad();}
public sealed class MetadataLoad
{
    public int Count=2;public object PrimaryStatus=true,CombinationStatus=true;public int TitleCalls,NumberCalls,TypeCalls;
    public object EnvelopeStatus=1,EnvelopeIds=new[]{9,2};public int EnvelopeFetches,EnvelopeCounts;
    public int GetEnvelopeCount(){EnvelopeCounts++;return Count;}
    public object GetEnvelopeIDs(ref object buffer){EnvelopeFetches++;buffer=EnvelopeIds;return EnvelopeStatus;}
    public int GetPrimaryLoadCaseCount()=>Count;public int GetReferenceLoadCaseCount()=>Count;
    public int GetLoadCombinationCaseCount()=>Count;
    public object GetLoadCombinationCaseNumbers(ref object buffer){NumberCalls++;buffer=new[]{201,301};return CombinationStatus;}
    public object GetPrimaryLoadCaseNumbers(ref object buffer){NumberCalls++;buffer=new[]{7,3};return PrimaryStatus;}
    public int GetReferenceLoadCaseNumbers(ref object buffer){NumberCalls++;buffer=new[]{7,3};return 2;}
    public string GetLoadCaseTitle(int id){TitleCalls++;return " Primary "+id;}
    public int GetLoadType(int id){TypeCalls++;return 23;}
    public string GetReferenceLoadCaseTitle(int id){TitleCalls++;return " Reference "+id;}
    public int GetReferenceLoadType(int id)=>0;
}
public sealed class MetadataRoot : NativeRoot {public new MetadataLoad Load {get;}=new MetadataLoad();}
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
            if (root.GetStaadFileFullPath() != @"C:\Models\Structure.std" || root.GetStaadFileFolder() != @"C:\Models\" || !root.IsPhysicalModel() || root.GetErrorMessage() != "" || root.GetProcessId() != 321u || native.Calls != calls + 5) throw new Exception("Incorrect live Root observations or dispatch count.");
            int pathCalls = native.Calls;
            if (root.GetStaadFileNameOnly() != "Structure.std" || root.GetAnlFileFullPath() != @"C:\Models\Structure.ANL" || root.GetOpenedStaadFilename() != @"C:\Models\Structure.std" || !root.IsTarget(@"c:/models/./STRUCTURE.std") || root.ModelIsOpen(@"C:\Other\Structure.std") || native.Calls != pathCalls + 5) throw new Exception("Incorrect path helpers, full-path comparison or dispatch count.");
            int beforeInvalid = native.Calls; bool invalidRequestedPath = false;
            try { root.IsTarget(null); } catch (ArgumentNullException error) { invalidRequestedPath = error.ParamName == "staadFileFullPath"; }
            if (!invalidRequestedPath || native.Calls != beforeInvalid) throw new Exception("Invalid path was dispatched or lost its public parameter name.");
            native.PathValue = ""; native.FolderValue = "";
            if (root.GetStaadFileFullPath() != "" || root.GetStaadFileFolder() != "") throw new Exception("Written empty output was lost.");
            if (root.GetStaadFileNameOnly() != "" || root.GetAnlFileFullPath() != "" || root.GetOpenedStaadFilename() != "" || root.IsTarget(@"C:\Models\Structure.std") || root.ModelIsOpen(@"C:\Models\Structure.std")) throw new Exception("Explicit blank path behavior changed.");
            native.WriteOutput = false;
            bool unwrittenPathRejected = false, unwrittenFolderRejected = false;
            try { root.GetStaadFileFullPath(); } catch (InvalidOperationException) { unwrittenPathRejected = true; }
            try { root.GetStaadFileFolder(); } catch (InvalidOperationException) { unwrittenFolderRejected = true; }
            if (!unwrittenPathRejected || !unwrittenFolderRejected) throw new Exception("Unwritten native output was accepted.");
            foreach (Action read in new Action[] { () => root.GetStaadFileNameOnly(), () => root.GetAnlFileFullPath(), () => root.GetOpenedStaadFilename(), () => root.IsTarget(@"C:\Models\Structure.std"), () => root.ModelIsOpen(@"C:\Models\Structure.std") })
            { bool rejected = false; try { read(); } catch (InvalidOperationException) { rejected = true; } if (!rejected) throw new Exception("Unwritten path helper output was accepted."); }
            native.WriteOutput = true;
            native.ProcessIdValue = true;
            bool invalidPidRejected = false;
            try { root.GetProcessId(); } catch (InvalidOperationException) { invalidPidRejected = true; }
            if (!invalidPidRejected) throw new Exception("Malformed process ID was accepted.");
            native.ProcessIdValue = 321u;
            wrapper.RefreshUnits();
            if (!ReferenceEquals(wrapper.Root, root) || root.GetBaseUnit() != BaseUnitSystem.Metric) throw new Exception("Unit refresh replaced Root.");
            wrapper.RefreshComObjects();
            bool staleRejected = false;
            try { root.GetBaseUnit(); } catch (InvalidOperationException) { staleRejected = true; }
            if (!staleRejected) throw new Exception("Stale Root remained usable.");
            foreach (Action read in new Action[] { () => root.GetStaadFileFullPath(), () => root.GetStaadFileFolder(), () => root.IsPhysicalModel(), () => root.GetErrorMessage(), () => root.GetProcessId(), () => root.GetStaadFileNameOnly(), () => root.GetAnlFileFullPath(), () => root.GetOpenedStaadFilename(), () => root.IsTarget(@"C:\Models\Structure.std"), () => root.ModelIsOpen(@"C:\Models\Structure.std") })
            { int before = native.Calls; bool rejected = false; try { read(); } catch (InvalidOperationException) { rejected = true; } if (!rejected || native.Calls != before) throw new Exception("Stale live observation dispatched."); }
            var current = wrapper.Root;
            if (current.GetApplicationVersion().Major != 26) throw new Exception("Reacquired Root failed.");
            wrapper.Dispose();
            bool disposedRejected = false;
            try { current.GetMainWindowHandle(); } catch (ObjectDisposedException) { disposedRejected = true; }
            if (!disposedRejected) throw new Exception("Disposed Root remained usable.");
            foreach (Action read in new Action[] { () => current.GetStaadFileFullPath(), () => current.GetStaadFileFolder(), () => current.IsPhysicalModel(), () => current.GetErrorMessage(), () => current.GetProcessId(), () => current.GetStaadFileNameOnly(), () => current.GetAnlFileFullPath(), () => current.GetOpenedStaadFilename(), () => current.IsTarget(@"C:\Models\Structure.std"), () => current.ModelIsOpen(@"C:\Models\Structure.std") })
            { int before = native.Calls; bool rejected = false; try { read(); } catch (ObjectDisposedException) { rejected = true; } if (!rejected || native.Calls != before) throw new Exception("Disposed live observation dispatched."); }
        }
        var mutableNative = new NativeRoot();
        using (var wrapper = new OpenStaadWrapper(mutableNative))
        {
            var root = wrapper.Root;
            if (!root.SetSilentMode(SilentMode.Enable)) throw new Exception("Previous false mode was misread as failure.");
            if (!ReferenceEquals(root.SaveModel(SilentMode.Disable), root) || !ReferenceEquals(root.OpenSTAADFile(@"relative\Frame.std"), root) || !ReferenceEquals(root.CloseSTAADFile(), root) || !ReferenceEquals(root.ShowApplication(), root)) throw new Exception("Fluent adapter identity changed.");
            if (!mutableNative.LifecycleCalls.SequenceEqual(new[] { "SetSilentMode", "SaveModel", "OpenSTAADFile", "CloseSTAADFile", "ShowApplication" })) throw new Exception("Incorrect lifecycle dispatch order.");
            int before = mutableNative.Calls;
            bool invalidMode = false, invalidPath = false;
            try { root.SetSilentMode((SilentMode)2); } catch (ArgumentOutOfRangeException e) { invalidMode = e.ParamName == "silentMode"; }
            try { root.OpenSTAADFile(null); } catch (ArgumentNullException e) { invalidPath = e.ParamName == "filePath"; }
            if (!invalidMode || !invalidPath || mutableNative.Calls != before) throw new Exception("Invalid lifecycle input was dispatched.");
            var nativeError = new System.Runtime.InteropServices.COMException("native mutation failure");
            mutableNative.MutationFailure = nativeError;
            if (root.SetSilentMode(SilentMode.Enable)) throw new Exception("Silent native failure was accepted.");
            bool propagated = false;
            try { root.SaveModel(SilentMode.Enable); } catch (System.Runtime.InteropServices.COMException e) { propagated = ReferenceEquals(e, nativeError); }
            if (!propagated) throw new Exception("Save error identity changed.");
            mutableNative.MutationFailure = null;
            Action<IOSRoot>[] writes = { r => r.SetSilentMode(SilentMode.Enable), r => r.SaveModel(SilentMode.Enable), r => r.OpenSTAADFile(@"C:\Models\Frame.std"), r => r.CloseSTAADFile(), r => r.ShowApplication() };
            wrapper.RefreshComObjects(); before = mutableNative.Calls;
            foreach (var write in writes) { bool rejected = false; try { write(root); } catch (InvalidOperationException) { rejected = true; } if (!rejected || mutableNative.Calls != before) throw new Exception("Stale lifecycle binding dispatched."); }
            var current = wrapper.Root; wrapper.Dispose();
            foreach (var write in writes) { bool rejected = false; try { write(current); } catch (ObjectDisposedException) { rejected = true; } if (!rejected || mutableNative.Calls != before) throw new Exception("Disposed lifecycle binding dispatched."); }
        }
        var jobNative = new NativeRoot();
        using (var wrapper = new OpenStaadWrapper(jobNative))
        {
            var root = wrapper.Root; int before = jobNative.Calls;
            var shortInfo = root.GetShortJobInfo(); var full = root.GetFullJobInfo();
            if (jobNative.Calls != before + 2 || shortInfo.Name != " Name ? " || full.Client != "Client" || full.Originator != "Engineer" || full.Number != "N-17" || full.Revision != "R2" || full.Part != "Part" || full.Reference != "Ref" || full.Checker != "Checker" || full.Approver != "Approver" || full.Comments != "first\r\nsecond") throw new Exception("Job output mapping changed.");
            if (full.Originated != DateTime.MinValue || full.Checked != DateTime.MinValue || full.Approved != DateTime.MinValue) throw new Exception("Blank date contract changed.");
            int events = 0; full.PropertyChanged += (sender,e) => events++;
            full.Name = "edited"; full.Name = "edited";
            if (events != 1 || root.GetFullJobInfo().Name != " Name ? " || shortInfo.Name != " Name ? ") throw new Exception("Snapshot storage or notifications changed.");
            jobNative.UnwrittenJobField = 12; bool rejected = false;
            try { root.GetFullJobInfo(); } catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new Exception("Unwritten full job output was accepted."); jobNative.UnwrittenJobField = -1;
            jobNative.JobFields[3] = "invalid date"; rejected = false;
            try { root.GetFullJobInfo(); } catch (FormatException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid job date was accepted."); jobNative.JobFields[3] = "";
            jobNative.JobFields[0] = null; rejected = false;
            try { root.GetShortJobInfo(); } catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new Exception("Null short job output was accepted."); jobNative.JobFields[0] = "Name";
            var manual = FullJobInfo.From(null,"client","originator",null,"checker"," ","approver","","1","2","3","4","comments");
            if (manual.Name != null || manual.Originated != DateTime.MinValue || manual.Comments != "comments") throw new Exception("Job factory contract changed.");
            jobNative.WindowHandleValue = 0;
            Action<IOSRoot>[] newOperations = { r => r.GetShortJobInfo(),r => r.GetFullJobInfo(),r => r.MinimizeStaadMainWindow(),r => r.MaximizeStaadMainWindow(),r => r.RestoreStaadMainWindow() };
            foreach (var changeWindow in newOperations.Skip(2))
            { before=jobNative.Calls; rejected=false; try { changeWindow(root); } catch (InvalidOperationException) { rejected=true; } if (!rejected || jobNative.Calls!=before+1) throw new Exception("Invalid window handle was posted or read incorrectly."); }
            wrapper.RefreshComObjects();before=jobNative.Calls;
            foreach (var operation in newOperations) { rejected=false;try{operation(root);}catch(InvalidOperationException){rejected=true;}if(!rejected||jobNative.Calls!=before)throw new Exception("Stale job/window binding dispatched."); }
            var current=wrapper.Root;wrapper.Dispose();
            foreach (var operation in newOperations) { rejected=false;try{operation(current);}catch(ObjectDisposedException){rejected=true;}if(!rejected||jobNative.Calls!=before)throw new Exception("Disposed job/window binding dispatched."); }
        }
        var workflowNative = new NativeRoot { TransitionPaths = true };
        using (var wrapper = new OpenStaadWrapper(workflowNative))
        {
            var root = wrapper.Root;
            if (!ReferenceEquals(root.SetInputUnitsEx(LengthInputUnit.Meter,ForceInputUnit.KiloNewton),root)) throw new Exception("Unit adapter identity changed.");
            if (!root.AwaitModelHandle(@"c:/models/./STRUCTURE.std",1,.01)) throw new Exception("Exact model wait failed.");
            if (!root.OpenStaadModel(@"C:\Models\Other.std",1,1)) throw new Exception("Open and identity wait failed.");
            root.CloseCurrentStaadModel();
            if (root.GetStaadFileFullPath()!="") throw new Exception("Conditional close failed.");
            string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"StaadCoreConsumer-"+Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            string target=System.IO.Path.Combine(directory,"Owned.std");
            try
            {
                System.IO.File.WriteAllText(target,"original");workflowNative.PathValue=target;
                if (!root.CloseWriteReopenStaadFile(target,new List<string>{"STAAD SPACE",null,"FINISH"},1,0)) throw new Exception("File workflow failed.");
                if (System.IO.File.ReadAllText(target)!="STAAD SPACE"+Environment.NewLine+Environment.NewLine+"FINISH"+Environment.NewLine || !root.ModelIsOpen(target) || System.IO.Directory.GetFiles(directory).Length!=1) throw new Exception("Replacement/cleanup/reopen contract failed.");
            }
            finally { foreach(string owned in System.IO.Directory.GetFiles(directory)) System.IO.File.Delete(owned);System.IO.Directory.Delete(directory,false); }
            Action<IOSRoot>[] operations={r=>r.SetInputUnitsEx(LengthInputUnit.Meter,ForceInputUnit.KiloNewton),r=>r.AwaitModelHandle(@"C:\Models\Other.std",1,.01),r=>r.OpenStaadModel(@"C:\Models\Other.std",1,1),r=>r.CloseCurrentStaadModel(),r=>r.CloseWriteReopenStaadFile(@"C:\Models\Other.std",new List<string>())};
            wrapper.RefreshComObjects();int before=workflowNative.Calls;
            foreach(var operation in operations){bool rejected=false;try{operation(root);}catch(InvalidOperationException){rejected=true;}if(!rejected||workflowNative.Calls!=before)throw new Exception("Stale workflow binding dispatched.");}
            var current=wrapper.Root;wrapper.Dispose();
            foreach(var operation in operations){bool rejected=false;try{operation(current);}catch(ObjectDisposedException){rejected=true;}if(!rejected||workflowNative.Calls!=before)throw new Exception("Disposed workflow binding dispatched.");}
        }
        var analysisNative=new NativeRoot();
        using(var wrapper=new OpenStaadWrapper(analysisNative))
        {
            var root=wrapper.Root;int before=analysisNative.Calls;
            if(root.AnalyzeEx(SilentMode.Enable,HiddenMode.Enable,WaitAnalysisMode.Return)!=AnalysisResult.CompletedSuccessfully)throw new Exception("Analysis request status changed.");
            root.Analyze();
            if(!root.IsAnalyzing()||!analysisNative.LifecycleCalls.SequenceEqual(new[]{"AnalyzeEx","Analyze"})||analysisNative.Calls!=before+3)throw new Exception("Unexpected analysis dispatch.");
            var status=root.GetAnalysisStatus(@"C:\Models\Other.std");
            if(status.ModelFilePath!=@"C:\Models\Other.std"||status.NativeStatus!=3||status.Status!=AnalysisStatus.CompletedWithWarnings||!status.IsSuccessful||!status.IsCompleted||status.WarningCount!=1||status.ErrorCount!=0||status.CpuTimeSeconds!=.25)throw new Exception("Analysis snapshot semantics changed.");
            before=analysisNative.Calls;var current=root.GetAnalysisStatus();
            if(current.ModelFilePath!=@"C:\Models\Structure.std"||analysisNative.LastAnalysisPath!=current.ModelFilePath||analysisNative.Calls!=before+2)throw new Exception("Current status path/read count changed.");
            analysisNative.AnalysisStatusValue=5;analysisNative.AnalysisWarnings=null;analysisNative.AnalysisErrors=null;analysisNative.AnalysisCpu=null;
            var unavailable=root.GetAnalysisStatus(@"C:\Models\Other.std");
            if(!unavailable.IsSuccessful||unavailable.IsCompleted||unavailable.WarningCount!=null||unavailable.ErrorCount!=null||unavailable.CpuTimeSeconds!=null)throw new Exception("Unavailable outputs fabricated data.");
            analysisNative.AnalysisStatusValue=4;
            bool rejected=false;try{root.GetAnalysisStatus(@"C:\Models\Other.std");}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Missing completed outputs accepted.");
            analysisNative.AnalysisStatusValue=999;var unknown=root.GetAnalysisStatus(@"C:\Models\Other.std");
            if(unknown.NativeStatus!=999||unknown.Status!=null||unknown.IsSuccessful||unknown.IsCompleted||unknown.WarningCount!=null)throw new Exception("Unknown status lost or treated as success.");
            if(status.WarningCount!=1||status.CpuTimeSeconds!=.25)throw new Exception("Snapshot changed after later native queries.");
            analysisNative.AnalysisResultValue=true;rejected=false;before=analysisNative.Calls;
            try{root.AnalyzeEx(SilentMode.Enable,HiddenMode.Enable,WaitAnalysisMode.Return);}catch(InvalidOperationException){rejected=true;}
            if(!rejected||analysisNative.Calls!=before+1)throw new Exception("Boolean analysis status accepted or retried.");
            before=analysisNative.Calls;rejected=false;
            try{root.AnalyzeEx((SilentMode)2,HiddenMode.Enable,WaitAnalysisMode.Return);}catch(ArgumentOutOfRangeException e){rejected=e.ParamName=="silentMode";}
            if(!rejected||analysisNative.Calls!=before)throw new Exception("Invalid analysis flag dispatched.");
            Action<IOSRoot>[] operations={r=>r.AnalyzeEx(SilentMode.Enable,HiddenMode.Enable,WaitAnalysisMode.Return),r=>r.Analyze(),r=>r.IsAnalyzing(),r=>r.GetAnalysisStatus(),r=>r.GetAnalysisStatus(@"C:\Models\Other.std")};
            wrapper.RefreshComObjects();before=analysisNative.Calls;
            foreach(var operation in operations){rejected=false;try{operation(root);}catch(InvalidOperationException){rejected=true;}if(!rejected||analysisNative.Calls!=before)throw new Exception("Stale analysis binding dispatched.");}
            var live=wrapper.Root;wrapper.Dispose();
            foreach(var operation in operations){rejected=false;try{operation(live);}catch(ObjectDisposedException){rejected=true;}if(!rejected||analysisNative.Calls!=before)throw new Exception("Disposed analysis binding dispatched.");}
        }
        var editNative=new NativeRoot();
        using(var wrapper=new OpenStaadWrapper(editNative))
        {
            var root=wrapper.Root;int before=editNative.Calls;
            root.SetInputUnitForLength((LengthInputUnit)1);root.SetInputUnitForForce(ForceInputUnit.Newton);
            if(editNative.ChangedLength!=1||editNative.ChangedForce!=4||editNative.Calls!=before+2||root.GetInputUnitForLength()!=LengthInputUnit.Meter||root.GetInputUnitForForce()!=ForceInputUnit.KiloNewton)throw new Exception("Individual unit/code/cache contract failed.");
            wrapper.RefreshUnits();
            if(!ReferenceEquals(wrapper.Root,root)||root.GetInputUnitForLength()!=LengthInputUnit.Feet||root.GetInputUnitForForce()!=ForceInputUnit.Newton)throw new Exception("Explicit unit refresh failed.");
            root.SetShortJobInfo(" Name ","Client\r\nText","");
            if(!editNative.WrittenShortInfo.SequenceEqual(new[]{" Name ","Client\r\nText",""}))throw new Exception("Short text mapping changed.");
            var job=ShortJobInfo.From("first","client","engineer");
            editNative.BeforeShortWrite=()=>{job.Name="later";job.Client="later";job.Originator="later";};
            root.SetShortJobInfo(job);editNative.BeforeShortWrite=null;
            if(!editNative.WrittenShortInfo.SequenceEqual(new[]{"first","client","engineer"}))throw new Exception("Short job snapshot was not detached before dispatch.");
            root.UpdateStructure();
            if(!editNative.LifecycleCalls.SequenceEqual(new[]{"SetInputUnitForLength","SetInputUnitForForce","SetShortJobInfo","SetShortJobInfo","UpdateStructure"}))throw new Exception("Unexpected model-edit dispatch or implicit mutation.");
            before=editNative.Calls;
            Action[] invalid={()=>root.SetInputUnitForLength((LengthInputUnit)8),()=>root.SetInputUnitForForce((ForceInputUnit)(-1)),()=>root.SetShortJobInfo(null,"c","e"),()=>root.SetShortJobInfo((ShortJobInfo)null),()=>root.SetShortJobInfo(new ShortJobInfo())};
            foreach(var call in invalid){bool rejected=false;try{call();}catch(ArgumentException){rejected=true;}if(!rejected||editNative.Calls!=before)throw new Exception("Invalid edit input dispatched.");}
            Action<IOSRoot>[] operations={r=>r.SetInputUnitForLength(LengthInputUnit.Meter),r=>r.SetInputUnitForForce(ForceInputUnit.Newton),r=>r.SetShortJobInfo("n","c","e"),r=>r.SetShortJobInfo(ShortJobInfo.From("n","c","e")),r=>r.UpdateStructure()};
            wrapper.RefreshComObjects();before=editNative.Calls;
            foreach(var operation in operations){bool rejected=false;try{operation(root);}catch(InvalidOperationException){rejected=true;}if(!rejected||editNative.Calls!=before)throw new Exception("Stale model-edit binding dispatched.");}
            var current=wrapper.Root;wrapper.Dispose();
            foreach(var operation in operations){bool rejected=false;try{operation(current);}catch(ObjectDisposedException){rejected=true;}if(!rejected||editNative.Calls!=before)throw new Exception("Disposed model-edit binding dispatched.");}
        }
        var projectNative=new NativeRoot();
        using(var wrapper=new OpenStaadWrapper(projectNative))
        {
            var root=wrapper.Root;
            root.SetFullJobInfo("n","c","e","literal-date","num","rev","part","ref","checker","","approver","native-date","notes\r\n");
            if(!projectNative.WrittenFullInfo.SequenceEqual(new[]{"n","c","e","literal-date","num","rev","part","ref","checker","","approver","native-date","notes\r\n"}))throw new Exception("Full job mapping changed.");
            root.AnalyzeModel();if(projectNative.Engine!=-1)throw new Exception("Default analysis engine changed.");
            int before=projectNative.Calls;
            bool invalid=false;try{root.AnalyzeModel((AnalysisEngine)0);}catch(ArgumentOutOfRangeException e){invalid=e.ParamName=="engine";}
            if(!invalid||projectNative.Calls!=before)throw new Exception("Invalid engine dispatched.");
            var result=root.GetConnectedProjectInfo();
            if(!result.IsSuccessful||result.Project.ProjectId!=" ID "||result.Project.Name!=" Name ")throw new Exception("Project output changed.");
            if(!root.SetConnectedProjectInfo(result.Project)||!projectNative.AssignedProject.SequenceEqual(new[]{" ID "," Name "}))throw new Exception("Project object assignment changed.");
            projectNative.ProjectSetFlag=0;if(root.SetConnectedProjectInfo("i","n"))throw new Exception("Native false assignment lost.");
            projectNative.ProjectFlag=false;projectNative.ProjectId=123;projectNative.WriteProjectName=false;
            var failed=root.GetConnectedProjectInfo();if(failed.IsSuccessful||failed.Project!=null)throw new Exception("False query leaked outputs.");
            projectNative.ProjectFlag=true;projectNative.ProjectId="id";
            invalid=false;try{root.GetConnectedProjectInfo();}catch(InvalidOperationException){invalid=true;}
            if(!invalid)throw new Exception("Unwritten successful output accepted.");
            projectNative.WriteProjectName=true;projectNative.ProjectSetFlag=2;
            invalid=false;try{root.SetConnectedProjectInfo("i","n");}catch(InvalidOperationException){invalid=true;}
            if(!invalid)throw new Exception("Malformed assignment flag accepted.");
            Action<IOSRoot>[] operations={r=>r.SetFullJobInfo("n","c","e","","num","rev","p","ref","ch","","ap","","comments"),r=>r.AnalyzeModel(),r=>r.GetConnectedProjectInfo(),r=>r.SetConnectedProjectInfo("i","n"),r=>r.SetConnectedProjectInfo(new ConnectedProjectInfo("i","n"))};
            before=projectNative.Calls;wrapper.RefreshComObjects();
            foreach(var action in operations){invalid=false;try{action(root);}catch(InvalidOperationException){invalid=true;}if(!invalid)throw new Exception("Stale project/edit binding dispatched.");}
            if(projectNative.Calls==before)throw new Exception("Expected explicit refresh.");
            var current=wrapper.Root;before=projectNative.Calls;wrapper.Dispose();
            foreach(var action in operations){invalid=false;try{action(current);}catch(ObjectDisposedException){invalid=true;}if(!invalid||projectNative.Calls!=before)throw new Exception("Disposed project/edit binding dispatched.");}
        }
        var creationNative=new NativeRoot { PathValue="" };
        using(var wrapper=new OpenStaadWrapper(creationNative))
        {
            var root=wrapper.Root;
            var handle=root.GetProcessHandle();
            if(handle.RawValue!=ulong.MaxValue||ReferenceEquals(handle,root.GetProcessHandle()))throw new Exception("Opaque handle snapshot changed.");
            creationNative.ProcessHandleValue=true;bool rejected=false;
            try{root.GetProcessHandle();}catch(InvalidOperationException){rejected=true;}
            if(!rejected)throw new Exception("Boolean handle accepted.");
            string directory=Path.Combine(Path.GetTempPath(),"StaadCoreCreation-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string target=Path.Combine(directory,"NewFrame.std");
            try
            {
                root.NewSTAADFile(target,LengthInputUnit.Feet,ForceInputUnit.KiloNewton);
                if(!creationNative.CreatedModel.SequenceEqual(new object[]{target,2,5})||File.Exists(target))throw new Exception("Creation contract changed.");
                File.WriteAllText(target,"existing");int before=creationNative.Calls;rejected=false;
                try{root.NewSTAADFile(target,LengthInputUnit.Meter,ForceInputUnit.KiloNewton);}catch(IOException){rejected=true;}
                if(!rejected||creationNative.Calls!=before||File.ReadAllText(target)!="existing")throw new Exception("Creation overwrote an existing target.");
            }
            finally { foreach(string file in Directory.GetFiles(directory))File.Delete(file);Directory.Delete(directory,false); }
            root.CreateNamedView(" Frame Ω ",int.MinValue);root.SaveNamedView(" Frame Ω ");root.Quit();
            if(!creationNative.CreatedView.SequenceEqual(new object[]{" Frame Ω ",int.MinValue})||creationNative.SavedView!=" Frame Ω "||!ReferenceEquals(root,wrapper.Root)||!wrapper.IsConnected)throw new Exception("View/shutdown semantics changed.");
            if(!creationNative.LifecycleCalls.SequenceEqual(new[]{"NewSTAADFile","CreateNamedView","SaveNamedView","Quit"}))throw new Exception("Unexpected implicit request.");
            Action<IOSRoot>[] operations={r=>r.GetProcessHandle(),r=>r.NewSTAADFile(target,LengthInputUnit.Meter,ForceInputUnit.KiloNewton),r=>r.CreateNamedView("v",0),r=>r.SaveNamedView("v"),r=>r.Quit()};
            wrapper.RefreshComObjects();int count=creationNative.Calls;
            foreach(var operation in operations){rejected=false;try{operation(root);}catch(InvalidOperationException){rejected=true;}if(!rejected||creationNative.Calls!=count)throw new Exception("Stale application request dispatched.");}
            var current=wrapper.Root;wrapper.Dispose();
            foreach(var operation in operations){rejected=false;try{operation(current);}catch(ObjectDisposedException){rejected=true;}if(!rejected||creationNative.Calls!=count)throw new Exception("Disposed application request dispatched.");}
            if(!typeof(NativeProcessHandleValue).IsSealed||typeof(NativeProcessHandleValue).GetConstructors().Length!=0||typeof(NativeProcessHandleValue).GetProperties().Any(p=>p.GetSetMethod(true)!=null)||typeof(IDisposable).IsAssignableFrom(typeof(NativeProcessHandleValue)))throw new Exception("Handle model ownership or mutability changed.");
        }
        using(var wrapper=new OpenStaadWrapper(new NativeRoot()))
        {
            var root=wrapper.Root;int[] ids={3,1,3};root.RemoveNamedView(" V ");root.ModifyNamedView(" V ",ids,NamedViewEntityType.Beam,-7);
            var viewNative=(NativeRoot)root.ComObject;
            if(viewNative.LastRemovedView!=" V "||viewNative.LastModifiedView!=" V "||!viewNative.ModifiedIds.SequenceEqual(ids)||ReferenceEquals(viewNative.ModifiedIds,ids))throw new Exception("View text or private array contract changed.");
            var denied=root.Capabilities.GetAdinaExport();if(denied.IsAvailable||denied.Capability!=null||denied.Availability!=RootCapabilityAvailability.ContractUnverified)throw new Exception("Uncovered build granted export.");
        }
        var optionalNative=new OptionalNativeRoot();
        using(var wrapper=new OpenStaadWrapper(optionalNative))
        {
            var root=wrapper.Root;var gateway=root.Capabilities;
            var export=gateway.GetAdinaExport();var errors=gateway.GetAnalysisErrorMessages();var warnings=gateway.GetAnalysisWarningMessages();
            if(!export.IsAvailable||!errors.IsAvailable||!warnings.IsAvailable||optionalNative.MessageCalls!=0||optionalNative.ExportCalls!=0||optionalNative.StatusCalls!=0)throw new Exception("Acquisition shape or side effects changed.");
            export.NativeVersion.Build=999;if(export.NativeVersion.Build!=340)throw new Exception("Capability version is mutable.");
            var exported=export.Capability.ExportModelToAdina(AdinaAxisSystem.Preserve,AdinaLoadCaseHandling.MultipleLoadCases);
            if(exported.NativeStatus!=-14501||!exported.IsSuccessful||!exported.HasWarnings)throw new Exception("Export warning-success semantics changed.");
            var messages=errors.Capability.GetAnalysisErrorMessages(@"C:\Models\Frame.std");
            if(!messages.IsSuccessful||messages.NativeStatus!=1||!messages.Messages.SequenceEqual(new[]{"message 0"})||messages.RetrievalAttempts!=1)throw new Exception("Message retrieval changed.");
            bool rejected=false;try{((IList<string>)messages.Messages)[0]="changed";}catch(NotSupportedException){rejected=true;}if(!rejected)throw new Exception("Messages are mutable.");
            optionalNative.MessageCode=0;var empty=warnings.Capability.GetAnalysisWarningMessages(@"C:\Models\Frame.std");if(!empty.IsSuccessful||empty.Messages.Count!=0)throw new Exception("Successful absence lost.");
            optionalNative.MessageCode=-108;int before=optionalNative.MessageCalls;var limited=errors.Capability.GetAnalysisErrorMessages(@"C:\Models\Frame.std");if(limited.RetrievalAttempts!=2||limited.NativeStatus!=-108||optionalNative.MessageCalls!=before+2)throw new Exception("Capacity retry is not bounded.");
            optionalNative.MessageCode=1;optionalNative.WriteMessages=false;rejected=false;try{errors.Capability.GetAnalysisErrorMessages(@"C:\Models\Frame.std");}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Unwritten successful message accepted.");
            Action[] captured={ ()=>gateway.GetAdinaExport(),()=>gateway.GetAnalysisErrorMessages(),()=>gateway.GetAnalysisWarningMessages(),()=>export.Capability.ExportModelToAdina(AdinaAxisSystem.Preserve,AdinaLoadCaseHandling.MultipleLoadCases),()=>errors.Capability.GetAnalysisErrorMessages(@"C:\Models\Frame.std"),()=>warnings.Capability.GetAnalysisWarningMessages(@"C:\Models\Frame.std") };
            wrapper.RefreshComObjects();
            foreach(var action in captured){rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Stale gateway/grant remained usable.");}
            var current=root=wrapper.Root;var newGrant=current.Capabilities.GetAdinaExport();wrapper.Dispose();
            rejected=false;try{newGrant.Capability.ExportModelToAdina(AdinaAxisSystem.Preserve,AdinaLoadCaseHandling.MultipleLoadCases);}catch(ObjectDisposedException){rejected=true;}if(!rejected)throw new Exception("Disposed export grant remained usable.");
        }
        var forceNative=new ForceRoot();
        using(var wrapper=new OpenStaadWrapper(forceNative))
        {
            var root=wrapper.Root;var load=wrapper.Load;var output=wrapper.Output;
            if(!output.AreResultsAvailable()||load.GetLoadCaseTitle(2)!="Case 2")throw new Exception("Force foundation reads changed.");
            var local=output.GetMemberEndForces("Caller",1,2,MemberEnd.Start);var global=output.GetMemberEndForces("Caller",1,2,MemberEnd.End,LocalOrGlobal.Global);
            if(!local.IsSuccessful||local.Forces.Fx!=1||local.Forces.Mz!=6||local.CoordinateSystem!=LocalOrGlobal.Local||global.CoordinateSystem!=LocalOrGlobal.Global)throw new Exception("Force components/axes changed.");
            var batch=root.RetrieveMemberForces(new[]{2,1,2},new[]{4,4});if(!batch.IsSuccessful||batch.Queries.Count!=4||batch.Forces.Count!=4)throw new Exception("Force batch identity/deduplication changed.");
            forceNative.Output.Success=false;if(output.GetMemberEndForces("",1,1,MemberEnd.Start).Forces!=null)throw new Exception("Failed force query leaked data.");
            var empty=root.RetrieveMemberForces(Array.Empty<int>(),new[]{1});if(empty.AreResultsAvailable!=null||!empty.IsSuccessful)throw new Exception("Empty force request asserted availability.");
            wrapper.RefreshComObjects();bool rejected=false;try{output.AreResultsAvailable();}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Stale Output remained usable.");
            var current=wrapper.Load;wrapper.Dispose();rejected=false;try{current.GetLoadCaseTitle(1);}catch(ObjectDisposedException){rejected=true;}if(!rejected)throw new Exception("Disposed Load remained usable.");
        }
        var metadataNative=new MetadataRoot();
        using(var wrapper=new OpenStaadWrapper(metadataNative))
        {
            var load=wrapper.Load;var primary=load.GetAllPrimaryLoadCases();var reference=load.GetAllReferenceLoadCases();
            if(primary.Count!=2||reference.Count!=2||primary.Any(x=>x.Type!=LoadType.None||x.CaseType!=LoadCaseType.PrimaryLoad)||reference.Any(x=>x.CaseType!=LoadCaseType.ReferenceLoad))throw new Exception("Load metadata family/type mapping changed.");
            if(load.GetPrimaryLoadCaseFromId(7).Title!=" Primary 7"||load.GetReferenceLoadCaseFromId(7).Title!=" Reference 7")throw new Exception("Load metadata title changed.");
            var cases=load.GetPrimaryLoadCasesFromIds(new[]{7,3,7});if(!cases.Select(x=>x.Id).SequenceEqual(new[]{7,3,7})||ReferenceEquals(cases[0],cases[2]))throw new Exception("Ordered duplicate case snapshots changed.");
            metadataNative.Load.PrimaryStatus=1;bool rejected=false;try{load.GetAllPrimaryLoadCases();}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Boolean primary success was confused with integer one.");
            metadataNative.Load.Count=0;int before=metadataNative.Load.NumberCalls;if(load.GetAllPrimaryLoadCases().Count!=0||metadataNative.Load.NumberCalls!=before)throw new Exception("Zero inventory made number queries.");
        }
        var combinationNative=new MetadataRoot();
        using(var wrapper=new OpenStaadWrapper(combinationNative))
        {
            var load=wrapper.Load;
            var references=load.GetReferenceLoadCasesFromIds(new[]{7,3,7});
            if(!references.Select(x=>x.Id).SequenceEqual(new[]{7,3,7})||ReferenceEquals(references[0],references[2])||references.Any(x=>x.CaseType!=LoadCaseType.ReferenceLoad))throw new Exception("Reference-list duplicate/family contract changed.");
            var single=load.GetLoadCombinationCaseFromId(201);
            var requested=load.GetLoadCombinationCasesFromIds(new[]{301,201,301}).ToArray();
            var combinations=load.GetAllLoadCombinationCases();
            if(single.Id!=201||single.Type!=LoadType.None||single.CaseType!=LoadCaseType.LoadCombination||requested.Length!=2||combinations.Count!=2||combinations.Any(c=>c.CaseType!=LoadCaseType.LoadCombination||c.Type!=LoadType.None)||combinationNative.Load.TypeCalls!=0)throw new Exception("Combination metadata contract changed.");
            int titleCalls=combinationNative.Load.TitleCalls;bool rejected=false;
            try{load.GetLoadCombinationCasesFromIds(new[]{201,999});}catch(InvalidOperationException){rejected=true;}
            if(!rejected||combinationNative.Load.TitleCalls!=titleCalls)throw new Exception("Missing combination membership was ignored or read partial titles.");
            foreach(var family in new[]{LoadCaseType.PrimaryLoad,LoadCaseType.ReferenceLoad,LoadCaseType.LoadCombination})
                if(load.GetAllLoadCasesOfType(family).Any(c=>c.CaseType!=family))throw new Exception("Load-family routing changed.");
            if(load.GetAllLoadCasesOfType().Any(c=>c.CaseType!=LoadCaseType.PrimaryLoad))throw new Exception("Default load family changed.");
            foreach(var family in new[]{LoadCaseType.RepeatLoad,(LoadCaseType)0})
            {rejected=false;try{load.GetAllLoadCasesOfType(family);}catch(NotSupportedException){rejected=true;}if(!rejected)throw new Exception("Unsupported family accepted.");}
            Action<IOSLoad>[] operations={l=>l.GetReferenceLoadCasesFromIds(new[]{7}),l=>l.GetLoadCombinationCaseFromId(201),l=>l.GetLoadCombinationCasesFromIds(new[]{201}),l=>l.GetAllLoadCombinationCases(),l=>l.GetAllLoadCasesOfType()};
            wrapper.RefreshComObjects();int before=combinationNative.Load.TitleCalls;
            foreach(var action in operations){rejected=false;try{action(load);}catch(InvalidOperationException){rejected=true;}if(!rejected||combinationNative.Load.TitleCalls!=before)throw new Exception("Stale new Load query dispatched.");}
            var current=wrapper.Load;wrapper.Dispose();
            foreach(var action in operations){rejected=false;try{action(current);}catch(ObjectDisposedException){rejected=true;}if(!rejected||combinationNative.Load.TitleCalls!=before)throw new Exception("Disposed new Load query dispatched.");}
        }
        foreach(var name in new[]{"GetReferenceLoadCasesFromIds","GetLoadCombinationCaseFromId","GetLoadCombinationCasesFromIds","GetAllLoadCombinationCases","GetAllLoadCasesOfType"})
        {
            bool reference=name=="GetReferenceLoadCasesFromIds",single=name=="GetLoadCombinationCaseFromId",family=name=="GetAllLoadCasesOfType",all=name=="GetAllLoadCombinationCases";
            Type[] types=all?Array.Empty<Type>():new[]{family?typeof(LoadCaseType):single?typeof(int):typeof(IEnumerable<int>)};
            string[] names=all?Array.Empty<string>():new[]{family?"loadCaseType":single?"lcId":reference?"loadCasesIds":"lcIds"};
            Type result=reference?typeof(List<ILoadCase>):single?typeof(ILoadCase):all?typeof(HashSet<ILoadCase>):typeof(IEnumerable<ILoadCase>);
            System.Xml.Linq.XElement previous=null;
            foreach(Type owner in new[]{typeof(IOSLoad),typeof(StaadPro.Interop.Core.Adapters.Models.OSLoadAdapter)})
            {
                var method=owner.GetMethod(name,types);if(method==null||method.ReturnType!=result||!method.GetParameters().Select(p=>p.Name).SequenceEqual(names)||method.GetParameters().Any(p=>p.IsOut||p.ParameterType.IsByRef))throw new Exception("New Load signature changed.");
                if(family){if(!method.GetParameters()[0].IsOptional||!Equals(method.GetParameters()[0].DefaultValue,LoadCaseType.PrimaryLoad))throw new Exception("Default load family changed.");}
                else if(method.GetParameters().Any(p=>p.IsOptional))throw new Exception("Unexpected new Load default.");
                string prefix="M:"+owner.FullName+"."+name;
                var entry=docs.Descendants("member").Single(e=>((string)e.Attribute("name")).StartsWith(prefix+"(")||(string)e.Attribute("name")==prefix);
                foreach(string tag in new[]{"summary","returns","remarks","exception","example"})if(entry.Element(tag)==null)throw new Exception("Missing new Load IntelliSense.");
                if(previous!=null&&!entry.Elements().Select(e=>e.ToString()).SequenceEqual(previous.Elements().Select(e=>e.ToString())))throw new Exception("Load interface/adapter IntelliSense differ.");
                if(!entry.Elements("param").Select(e=>(string)e.Attribute("name")).SequenceEqual(names))throw new Exception("New Load parameter docs differ.");
                previous=entry;
            }
        }
        foreach(var name in new[]{"GetAllPrimaryLoadCases","GetAllReferenceLoadCases","GetPrimaryLoadCaseFromId","GetReferenceLoadCaseFromId","GetPrimaryLoadCasesFromIds"})
        {
            var method=typeof(IOSLoad).GetMethod(name);var ps=method.GetParameters();Type expectedLoadReturn=name.StartsWith("GetAll")?typeof(HashSet<ILoadCase>):name.EndsWith("FromIds")?typeof(List<ILoadCase>):typeof(ILoadCase);
            if(method.ReturnType!=expectedLoadReturn||ps.Any(p=>p.IsOptional||p.ParameterType.IsByRef)||ps.Length!=(name.StartsWith("GetAll")?0:1))throw new Exception("Load metadata signature changed.");
            if(ps.Length==1&&(ps[0].Name!=(name.EndsWith("FromIds")?"loadCasesIds":"lcId")||ps[0].ParameterType!=(name.EndsWith("FromIds")?typeof(IEnumerable<int>):typeof(int))))throw new Exception("Load metadata parameter changed.");
        }
        foreach(Type type in new[]{typeof(LoadCase),typeof(ILoadCase),typeof(LoadCaseType),typeof(LoadType)})
        {if(!docs.Descendants("member").Any(e=>(string)e.Attribute("name")=="T:"+type.FullName&&e.Element("summary")!=null))throw new Exception("Missing load metadata type XML.");}
        var lookupNative = new MetadataRoot();
        using(var lookupWrapper = new OpenStaadWrapper(lookupNative))
        {
            var lookupLoad = lookupWrapper.Load;
            if(lookupLoad.GetLoadCase(7,LoadCaseType.PrimaryLoad).Id!=7||lookupLoad.GetLoadCase(999,LoadCaseType.PrimaryLoad)!=null)throw new Exception("Generic family lookup changed.");
            if(lookupLoad.GetLoadCases(7,7,LoadCaseType.PrimaryLoad).Single().Id!=7||lookupLoad.GetLoadCases(1,int.MaxValue,LoadCaseType.PrimaryLoad).Count()!=2)throw new Exception("Inclusive family range changed.");
            var lookupFiltered = lookupLoad.GetLoadCasesFromIds(new[]{999,7,7},LoadCaseType.PrimaryLoad).ToList();
            if(lookupFiltered.Count!=1||lookupFiltered[0].Id!=7)throw new Exception("Family filter missing/duplicate semantics changed.");
            var lookupListed = lookupLoad.GetLoadCasesOfType(LoadCaseType.ReferenceLoad);
            if(lookupListed.Count!=2||lookupListed.Any(item=>item.CaseType!=LoadCaseType.ReferenceLoad))throw new Exception("List family routing changed.");
            var lookupEnvelopes = lookupLoad.GetEnvelopeIds();
            if(!lookupEnvelopes.SetEquals(new[]{9,2})||lookupNative.Load.EnvelopeFetches!=1||lookupNative.Load.EnvelopeCounts!=2)throw new Exception("Envelope ID qualification or count recheck changed.");
            lookupNative.Load.EnvelopeStatus=true;
            bool lookupRejected=false;
            try{lookupLoad.GetEnvelopeIds();}catch(InvalidOperationException){lookupRejected=true;}
            if(!lookupRejected)throw new Exception("Boolean envelope status was accepted as integer one.");
            lookupNative.Load.EnvelopeStatus=1;lookupNative.Load.EnvelopeIds=new[]{9,9};lookupRejected=false;
            try{lookupLoad.GetEnvelopeIds();}catch(InvalidOperationException){lookupRejected=true;}
            if(!lookupRejected)throw new Exception("Duplicate envelope IDs were accepted.");
            lookupNative.Load.Count=0;int lookupFetches=lookupNative.Load.EnvelopeFetches;
            if(lookupLoad.GetEnvelopeIds().Count!=0||lookupNative.Load.EnvelopeFetches!=lookupFetches)throw new Exception("Zero envelope inventory fetched numbers.");
            foreach(var lookupFamily in new[]{LoadCaseType.RepeatLoad,(LoadCaseType)999})
            {
                lookupRejected=false;
                try{lookupLoad.GetLoadCasesFromIds(Array.Empty<int>(),lookupFamily);}catch(NotSupportedException){lookupRejected=true;}
                if(!lookupRejected)throw new Exception("Unsupported empty filter family accepted.");
            }
        }
        foreach(string lookupName in new[]{"GetLoadCase","GetLoadCases","GetLoadCasesFromIds","GetLoadCasesOfType","GetEnvelopeIds"})
        {
            var lookupMethod=typeof(IOSLoad).GetMethod(lookupName);
            Type lookupReturn=lookupName=="GetLoadCase"?typeof(ILoadCase):lookupName=="GetLoadCasesOfType"?typeof(List<ILoadCase>):lookupName=="GetEnvelopeIds"?typeof(HashSet<int>):typeof(IEnumerable<ILoadCase>);
            string[] lookupNames=lookupName=="GetLoadCase"?new[]{"lcId","loadCaseType"}:lookupName=="GetLoadCases"?new[]{"fromId","toId","loadCaseType"}:lookupName=="GetLoadCasesFromIds"?new[]{"loadCasesIds","loadCaseType"}:lookupName=="GetLoadCasesOfType"?new[]{"loadCaseType"}:Array.Empty<string>();
            Type[] lookupTypes=lookupName=="GetLoadCase"?new[]{typeof(int),typeof(LoadCaseType)}:lookupName=="GetLoadCases"?new[]{typeof(int),typeof(int),typeof(LoadCaseType)}:lookupName=="GetLoadCasesFromIds"?new[]{typeof(IEnumerable<int>),typeof(LoadCaseType)}:lookupName=="GetLoadCasesOfType"?new[]{typeof(LoadCaseType)}:Array.Empty<Type>();
            var lookupParameters=lookupMethod.GetParameters();
            if(lookupMethod.ReturnType!=lookupReturn||!lookupParameters.Select(item=>item.Name).SequenceEqual(lookupNames)||!lookupParameters.Select(item=>item.ParameterType).SequenceEqual(lookupTypes)||lookupParameters.Any(item=>item.IsOptional||item.ParameterType.IsByRef))throw new Exception("Load lookup/envelope public signature changed.");
        }
        var expected = new[] { "GetApplicationVersion", "GetBaseUnit", "GetInputUnitForForce", "GetInputUnitForLength", "GetMainWindowHandle", "GetStaadFileFullPath", "GetStaadFileFolder", "IsPhysicalModel", "GetErrorMessage", "GetProcessId", "GetStaadFileNameOnly", "GetAnlFileFullPath", "GetOpenedStaadFilename", "IsTarget", "ModelIsOpen", "SetSilentMode", "SaveModel", "OpenSTAADFile", "CloseSTAADFile", "ShowApplication", "GetShortJobInfo", "GetFullJobInfo", "MinimizeStaadMainWindow", "MaximizeStaadMainWindow", "RestoreStaadMainWindow", "SetInputUnitsEx", "AwaitModelHandle", "OpenStaadModel", "CloseCurrentStaadModel", "CloseWriteReopenStaadFile", "AnalyzeEx", "Analyze", "IsAnalyzing", "GetAnalysisStatus", "GetAnalysisStatus", "SetInputUnitForLength", "SetInputUnitForForce", "SetShortJobInfo", "SetShortJobInfo", "UpdateStructure", "SetFullJobInfo", "AnalyzeModel", "GetConnectedProjectInfo", "SetConnectedProjectInfo", "SetConnectedProjectInfo", "GetProcessHandle", "NewSTAADFile", "CreateNamedView", "SaveNamedView", "Quit", "RemoveNamedView", "ModifyNamedView", "RetrieveMemberForces" };
        foreach (var contract in assembly.GetTypes().Where(t=>t.IsInterface && t.Namespace=="StaadPro.Interop.Core.Adapters.Interfaces"))
        {
            var methods = contract.GetMethods().Where(m=>!m.IsSpecialName).ToArray();
            if (contract == typeof(IOSRoot))
            {
                if (!methods.Select(m=>m.Name).OrderBy(n=>n).SequenceEqual(expected.OrderBy(n=>n))) throw new Exception("Unexpected Root contracts.");
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (method.Name == "IsTarget" || method.Name == "ModelIsOpen")
                    { if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string) || parameters[0].Name != (method.Name == "IsTarget" ? "staadFileFullPath" : "fileFullPath") || parameters[0].IsOptional) throw new Exception("Path predicate signature changed."); }
                    else if (method.Name == "SetSilentMode" || method.Name == "SaveModel")
                    { if (parameters.Length != 1 || parameters[0].ParameterType != typeof(SilentMode) || parameters[0].Name != "silentMode" || parameters[0].IsOptional || method.ReturnType != (method.Name == "SetSilentMode" ? typeof(bool) : typeof(IOSRoot))) throw new Exception("Mode signature changed."); }
                    else if (method.Name == "OpenSTAADFile")
                    { if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string) || parameters[0].Name != "filePath" || parameters[0].IsOptional || method.ReturnType != typeof(IOSRoot)) throw new Exception("Open signature changed."); }
                    else if (method.Name == "SetInputUnitsEx")
                    { if (!parameters.Select(p=>p.ParameterType).SequenceEqual(new[] { typeof(LengthInputUnit),typeof(ForceInputUnit) }) || !parameters.Select(p=>p.Name).SequenceEqual(new[] { "lengthUnit","forceUnit" }) || parameters.Any(p=>p.IsOptional) || method.ReturnType!=typeof(IOSRoot)) throw new Exception("Unit signature changed."); }
                    else if (method.Name == "AwaitModelHandle")
                    { if (!parameters.Select(p=>p.ParameterType).SequenceEqual(new[] { typeof(string),typeof(double),typeof(double) }) || !parameters.Select(p=>p.Name).SequenceEqual(new[] { "fileFullPath","timeoutDurationInSeconds","sleepIntervalInSeconds" }) || parameters[0].IsOptional || !parameters.Skip(1).All(p=>p.IsOptional) || !parameters.Skip(1).Select(p=>p.DefaultValue).SequenceEqual(new object[] { 60d,.1d }) || method.ReturnType!=typeof(bool)) throw new Exception("Wait signature changed."); }
                    else if (method.Name == "OpenStaadModel")
                    { if (!parameters.Select(p=>p.ParameterType).SequenceEqual(new[] { typeof(string),typeof(int),typeof(int) }) || !parameters.Select(p=>p.Name).SequenceEqual(new[] { "staadFileFullPath","sleepInterval","maxWaitTime" }) || parameters.Take(2).Any(p=>p.IsOptional) || !parameters[2].IsOptional || !Equals(parameters[2].DefaultValue,600) || method.ReturnType!=typeof(bool)) throw new Exception("Open/wait signature changed."); }
                    else if (method.Name == "CloseWriteReopenStaadFile")
                    { if (!parameters.Select(p=>p.ParameterType).SequenceEqual(new[] { typeof(string),typeof(List<string>),typeof(int),typeof(int) }) || !parameters.Select(p=>p.Name).SequenceEqual(new[] { "filePath","fileContents","sleepTimeMilliseconds","maxTimeMilliseconds" }) || parameters.Take(2).Any(p=>p.IsOptional) || !parameters.Skip(2).All(p=>p.IsOptional) || !parameters.Skip(2).Select(p=>p.DefaultValue).SequenceEqual(new object[] { 200,5000 }) || method.ReturnType!=typeof(bool)) throw new Exception("Rewrite signature changed."); }
                    else if(method.Name=="AnalyzeEx")
                    { if(!parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(SilentMode),typeof(HiddenMode),typeof(WaitAnalysisMode)})||!parameters.Select(p=>p.Name).SequenceEqual(new[]{"silentMode","hiddenMode","waitMode"})||parameters.Any(p=>p.IsOptional)||method.ReturnType!=typeof(AnalysisResult))throw new Exception("AnalyzeEx signature changed."); }
                    else if(method.Name=="GetAnalysisStatus")
                    { if(parameters.Length>1||(parameters.Length==1&&(parameters[0].ParameterType!=typeof(string)||parameters[0].Name!="modelFilePath"||parameters[0].IsOptional))||method.ReturnType!=typeof(AnalysisStatusResult))throw new Exception("Analysis status signature changed."); }
                    else if(method.Name=="SetInputUnitForLength"||method.Name=="SetInputUnitForForce")
                    { bool length=method.Name=="SetInputUnitForLength";if(parameters.Length!=1||parameters[0].ParameterType!=(length?typeof(LengthInputUnit):typeof(ForceInputUnit))||parameters[0].Name!=(length?"lengthUnit":"forceUnit")||parameters[0].IsOptional||method.ReturnType!=typeof(void))throw new Exception("Individual unit signature changed."); }
                    else if(method.Name=="SetShortJobInfo")
                    { if(method.ReturnType!=typeof(void)||parameters.Any(p=>p.IsOptional)||!(parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(ShortJobInfo)})&&parameters[0].Name=="jobInfo"||parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(string),typeof(string),typeof(string)})&&parameters.Select(p=>p.Name).SequenceEqual(new[]{"jobName","jobClient","engineerName"})))throw new Exception("Short job signature changed."); }
                    else if(method.Name=="SetFullJobInfo")
                    { if(method.ReturnType!=typeof(void)||parameters.Length!=13||parameters.Any(p=>p.ParameterType!=typeof(string)||p.IsOptional)||!parameters.Select(p=>p.Name).SequenceEqual(new[]{"jobName","jobClient","engineerName","engineerDate","jobNumber","revision","part","reference","checkerName","checkerDate","approverName","approverDate","comments"}))throw new Exception("Full job signature changed."); }
                    else if(method.Name=="AnalyzeModel")
                    { if(method.ReturnType!=typeof(void)||parameters.Length!=1||parameters[0].ParameterType!=typeof(AnalysisEngine)||parameters[0].Name!="engine"||!parameters[0].IsOptional||!Equals(parameters[0].DefaultValue,AnalysisEngine.StaadPro))throw new Exception("Analysis engine signature changed."); }
                    else if(method.Name=="GetConnectedProjectInfo")
                    { if(parameters.Length!=0||method.ReturnType!=typeof(ConnectedProjectResult))throw new Exception("Project query signature changed."); }
                    else if(method.Name=="SetConnectedProjectInfo")
                    { if(method.ReturnType!=typeof(bool)||parameters.Any(p=>p.IsOptional||p.IsOut||p.ParameterType.IsByRef)||!(parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(ConnectedProjectInfo)})&&parameters[0].Name=="projectInfo"||parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(string),typeof(string)})&&parameters.Select(p=>p.Name).SequenceEqual(new[]{"projectId","projectName"})))throw new Exception("Project assignment signature changed."); }
                    else if(method.Name=="NewSTAADFile")
                    { if(method.ReturnType!=typeof(void)||!parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(string),typeof(LengthInputUnit),typeof(ForceInputUnit)})||!parameters.Select(p=>p.Name).SequenceEqual(new[]{"filePath","lengthUnit","forceUnit"})||parameters.Any(p=>p.IsOptional))throw new Exception("Creation signature changed."); }
                    else if(method.Name=="CreateNamedView"||method.Name=="SaveNamedView")
                    { bool create=method.Name=="CreateNamedView";if(method.ReturnType!=typeof(void)||!parameters.Select(p=>p.ParameterType).SequenceEqual(create?new[]{typeof(string),typeof(int)}:new[]{typeof(string)})||!parameters.Select(p=>p.Name).SequenceEqual(create?new[]{"viewName","creationFlag"}:new[]{"viewName"})||parameters.Any(p=>p.IsOptional))throw new Exception("Named-view signature changed."); }
                    else if(method.Name=="GetProcessHandle"||method.Name=="Quit")
                    { if(parameters.Length!=0||method.ReturnType!=(method.Name=="Quit"?typeof(void):typeof(NativeProcessHandleValue)))throw new Exception("Application signature changed."); }
                    else if(method.Name=="RemoveNamedView")
                    { if(method.ReturnType!=typeof(void)||parameters.Length!=1||parameters[0].ParameterType!=typeof(string)||parameters[0].Name!="viewName"||parameters[0].IsOptional)throw new Exception("Removal signature changed."); }
                    else if(method.Name=="ModifyNamedView")
                    { if(method.ReturnType!=typeof(void)||!parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(string),typeof(IEnumerable<int>),typeof(NamedViewEntityType),typeof(int)})||!parameters.Select(p=>p.Name).SequenceEqual(new[]{"viewName","entityIds","entityType","modificationFlag"})||parameters.Any(p=>p.IsOptional))throw new Exception("Modification signature changed."); }
                    else if(method.Name=="RetrieveMemberForces")
                    {if(method.ReturnType!=typeof(MemberForcesResult)||!parameters.Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(IEnumerable<int>),typeof(IEnumerable<int>)})||!parameters.Select(p=>p.Name).SequenceEqual(new[]{"beams","loadCases"})||parameters.Any(p=>p.IsOptional))throw new Exception("Member-force batch signature changed.");}
                    else if (parameters.Length != 0) throw new Exception("Unexpected Root parameters.");
                    if(method.Name=="UpdateStructure"&&method.ReturnType!=typeof(void))throw new Exception("Structure update must be void.");
                    if(method.Name=="Analyze"&&method.ReturnType!=typeof(void))throw new Exception("Analyze must be void.");
                    if(method.Name=="IsAnalyzing"&&method.ReturnType!=typeof(bool))throw new Exception("Running predicate result changed.");
                    if (method.Name == "GetShortJobInfo" && method.ReturnType != typeof(ShortJobInfo)) throw new Exception("Short job result type changed.");
                    if (method.Name == "GetFullJobInfo" && method.ReturnType != typeof(FullJobInfo)) throw new Exception("Full job result type changed.");
                    if ((method.Name == "MinimizeStaadMainWindow" || method.Name == "MaximizeStaadMainWindow" || method.Name == "RestoreStaadMainWindow") && method.ReturnType != typeof(void)) throw new Exception("Window return contract changed.");
                    if (method.Name == "CloseCurrentStaadModel" && method.ReturnType != typeof(void)) throw new Exception("Close current result changed.");
                    if ((method.Name == "CloseSTAADFile" || method.Name == "ShowApplication") && method.ReturnType != typeof(IOSRoot)) throw new Exception("Fluent return changed.");
                }
            }
            else if(contract==typeof(IOSLoad)) {if(!methods.Select(m=>m.Name).OrderBy(n=>n).SequenceEqual(new[]{"GetLoadCaseTitle","GetAllPrimaryLoadCases","GetAllReferenceLoadCases","GetPrimaryLoadCaseFromId","GetReferenceLoadCaseFromId","GetPrimaryLoadCasesFromIds","GetReferenceLoadCasesFromIds","GetLoadCombinationCaseFromId","GetLoadCombinationCasesFromIds","GetAllLoadCombinationCases","GetAllLoadCasesOfType","GetLoadCase","GetLoadCases","GetLoadCasesFromIds","GetLoadCasesOfType","GetEnvelopeIds"}.OrderBy(n=>n)))throw new Exception("Unexpected Load operations.");}
            else if(contract==typeof(IOSOutput)) {if(!methods.Select(m=>m.Name).OrderBy(n=>n).SequenceEqual(new[]{"AreResultsAvailable","GetMemberEndForces","GetMemberEndForces"}.OrderBy(n=>n)))throw new Exception("Unexpected Output operations.");}
            else if (methods.Length != 0) throw new Exception("Unexpected domain operation: "+contract.Name);
        }
        foreach (Type model in new[] { typeof(ShortJobInfo), typeof(FullJobInfo) })
        {
            if (!docs.Descendants("member").Any(e => (string)e.Attribute("name") == "T:"+model.FullName && e.Element("summary") != null)) throw new Exception("Missing job model IntelliSense.");
            foreach (var property in model.GetProperties())
            { string id="P:"+property.DeclaringType.FullName+"."+property.Name; if (!docs.Descendants("member").Any(e => (string)e.Attribute("name")==id && e.Element("summary")!=null)) throw new Exception("Missing job field IntelliSense: "+id); }
            var factory=model.GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.DeclaredOnly).Single(m=>m.Name=="From");
            string methodId="M:"+model.FullName+".From("+string.Join(",",factory.GetParameters().Select(p=>p.ParameterType.FullName))+")";
            var entry=docs.Descendants("member").Single(e=>(string)e.Attribute("name")==methodId);
            if (entry.Element("returns")==null || entry.Elements("param").Count()!=factory.GetParameters().Length) throw new Exception("Incomplete job factory IntelliSense.");
        }
        foreach(var type in new[]{typeof(AnalysisResult),typeof(HiddenMode),typeof(WaitAnalysisMode),typeof(AnalysisStatus),typeof(AnalysisStatusResult),typeof(AnalysisEngine),typeof(ConnectedProjectInfo),typeof(ConnectedProjectResult),typeof(NativeProcessHandleValue)})
        {
            var typeDoc=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="T:"+type.FullName);
            if(typeDoc.Element("summary")==null)throw new Exception("Missing analysis type IntelliSense.");
            foreach(var field in type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static))
                if(!docs.Descendants("member").Any(e=>(string)e.Attribute("name")=="F:"+type.FullName+"."+field.Name&&e.Element("summary")!=null))throw new Exception("Missing analysis enum IntelliSense.");
            foreach(var property in type.GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))
                if(!docs.Descendants("member").Any(e=>(string)e.Attribute("name")=="P:"+type.FullName+"."+property.Name&&e.Element("summary")!=null&&e.Element("value")!=null))throw new Exception("Missing analysis result property IntelliSense.");
        }
        if(!typeof(AnalysisStatusResult).IsSealed||typeof(AnalysisStatusResult).GetConstructors().Length!=0||typeof(AnalysisStatusResult).GetProperties().Any(p=>p.GetSetMethod(true)!=null))throw new Exception("Status snapshot is not immutable.");
        foreach(var type in new[]{typeof(ConnectedProjectInfo),typeof(ConnectedProjectResult)})
            if(!type.IsSealed||type.GetProperties().Any(p=>p.GetSetMethod(true)!=null))throw new Exception("Project models must be immutable.");
        if(typeof(ConnectedProjectResult).GetConstructors().Length!=0)throw new Exception("Query result must not have a public constructor.");
        foreach(Type contract in new[]{typeof(IOSRootAdinaExport),typeof(IOSRootAnalysisErrorMessages),typeof(IOSRootAnalysisWarningMessages)})
        {
            if(contract.IsAssignableFrom(typeof(StaadPro.Interop.Core.Adapters.Models.OSRootAdapter)))throw new Exception("Common adapter exposes optional operations by cast.");
            var concrete=assembly.GetTypes().Where(t=>t.IsClass&&contract.IsAssignableFrom(t)).ToArray();if(concrete.Length!=1||concrete[0].IsVisible)throw new Exception("Optional adapter visibility changed.");
            foreach(var method in contract.GetMethods())if(method.GetParameters().Any(p=>p.ParameterType.IsByRef||p.IsOut||p.IsOptional))throw new Exception("Optional public references/defaults introduced.");
        }
        foreach(Type type in new[]{typeof(AdinaExportResult),typeof(AnalysisMessagesResult),typeof(RootCapabilityResult<IOSRootAdinaExport>),typeof(AdinaAxisSystem),typeof(AdinaLoadCaseHandling),typeof(AdinaExportStatus),typeof(AnalysisMessageQueryOutcome),typeof(AnalysisMessageRetrievalStatus),typeof(RootCapabilityAvailability),typeof(NamedViewEntityType)})
        {
            string name=(type.IsGenericType?type.GetGenericTypeDefinition():type).FullName;
            if(!docs.Descendants("member").Any(e=>(string)e.Attribute("name")=="T:"+name&&e.Element("summary")!=null))throw new Exception("Missing optional type IntelliSense.");
            foreach(var property in type.GetProperties())
            {if(property.GetSetMethod(true)!=null)throw new Exception("Optional result is mutable.");if(!docs.Descendants("member").Any(e=>(string)e.Attribute("name")=="P:"+name+"."+property.Name&&e.Element("summary")!=null&&e.Element("value")!=null))throw new Exception("Missing optional result field IntelliSense.");}
            foreach(var field in type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static))
                if(!docs.Descendants("member").Any(e=>(string)e.Attribute("name")=="F:"+name+"."+field.Name&&e.Element("summary")!=null))throw new Exception("Missing optional enum IntelliSense.");
        }
        Console.WriteLine("PASS: installed foundation, 53 common and three optional guarded Root operations, XML and dependency isolation.");
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
$observationCode = [regex]::Match($observationGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($observationCode)) { throw 'Missing live Root guide usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootObservationExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootObservationExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $observationCode + [Environment]::NewLine + '}}')
$pathCode = [regex]::Match($pathGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($pathCode)) { throw 'Missing Root path guide usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootPathExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootPathExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $pathCode + [Environment]::NewLine + '}}')
$lifecycleCode = [regex]::Match($lifecycleGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($lifecycleCode)) { throw 'Missing lifecycle guide usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootLifecycleExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootLifecycleExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $lifecycleCode + [Environment]::NewLine + '}}')
$jobCode = [regex]::Match($jobWindowGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($jobCode)) { throw 'Missing job/window guide usage example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootJobWindowExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootJobWindowExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $jobCode + [Environment]::NewLine + '}}')
$coordinationCode = [regex]::Match($coordinationGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($coordinationCode)) { throw 'Missing model coordination guide example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootCoordinationExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootCoordinationExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $coordinationCode + [Environment]::NewLine + '}}')
$analysisCode = [regex]::Match($analysisGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($analysisCode)) { throw 'Missing analysis guide example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootAnalysisExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootAnalysisExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $analysisCode + [Environment]::NewLine + '}}')
$editCode = [regex]::Match($editGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($editCode)) { throw 'Missing model edits guide example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootEditExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootEditExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $editCode + [Environment]::NewLine + '}}')
$projectCode = [regex]::Match($projectGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($projectCode)) { throw 'Missing job/analysis/projects guide example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootProjectExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootProjectExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $projectCode + [Environment]::NewLine + '}}')
$creationCode = [regex]::Match($creationGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($creationCode)) { throw 'Missing creation/views/shutdown guide example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootCreationExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootCreationExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $creationCode + [Environment]::NewLine + '}}')
$optionalCode = [regex]::Match($optionalGuide, '(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($optionalCode)) { throw 'Missing view/optional guide example.' }
[IO.File]::WriteAllText((Join-Path $consumer 'RootOptionalExample.cs'), 'using System; using StaadPro.Interop.Core.Models; internal static class RootOptionalExample { static void Example() { using var wrapper = new OpenStaadWrapper(new NativeRoot());' + [Environment]::NewLine + $optionalCode + [Environment]::NewLine + '}}')
$forceCode=[regex]::Match($forceGuide,'(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if([string]::IsNullOrWhiteSpace($forceCode)){throw 'Missing force guide example.'}
[IO.File]::WriteAllText((Join-Path $consumer 'ForceExample.cs'),'using System; using StaadPro.Interop.Core.Models; internal static class ForceExample { static void Example(){using var wrapper=new OpenStaadWrapper(new ForceRoot());'+$forceCode+'}}')
$loadCode=[regex]::Match($loadGuide,'(?s)~~~csharp\s*\r?\n(.*?)~~~').Groups[1].Value
if([string]::IsNullOrWhiteSpace($loadCode)){throw 'Missing load metadata guide example.'}
[IO.File]::WriteAllText((Join-Path $consumer 'LoadMetadataExample.cs'),'using System; using StaadPro.Interop.Core.Models; internal static class LoadMetadataExample{static void Example(){using var wrapper=new OpenStaadWrapper(new MetadataRoot());'+$loadCode+'}}')
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
Write-Output ("PASS: " + $examples.Count + ' XML examples, README and all thirteen Root/domain guide examples compiled without executing native examples.')

# Named-view changes and optional Root operations

This preview adds two common Root methods and three separately acquired optional operations, with full XML IntelliSense on their interfaces and concrete implementations. The package exposes **52 common Root contracts and three optional Root operations**. Other domain adapters remain binding-only.

| API | Contract |
| --- | --- |
| `RemoveNamedView(string viewName)` | Sends the exact nonblank name and a required by-value Int32 zero; returns void. |
| `ModifyNamedView(string viewName, IEnumerable<int> entityIds, NamedViewEntityType entityType, int modificationFlag)` | Snapshots positive IDs once and sends the four inspected inputs; returns void. |
| `ExportModelToAdina(AdinaAxisSystem axisSystem, AdinaLoadCaseHandling loadCaseHandling)` | Returns a named status result; available through `Capabilities.GetAdinaExport()`. |
| `GetAnalysisErrorMessages(string modelFilePath)` | Returns qualified analysis-log messages through its independently acquired capability. |
| `GetAnalysisWarningMessages(string modelFilePath)` | Returns qualified ANL messages through its independently acquired capability. |

## Named-view changes

Both common methods reject null or blank `viewName` before native work and preserve nonblank text, including surrounding spaces and Unicode. Removal initializes the inspected by-value error slot with zero; it is not an output and supplies no observable success flag. Modification accepts a non-null ID sequence and validates the documented entity qualifier before enumeration: Node=0, Beam=1, Plate=2, Solid=3, Surface=4. Every ID must be positive. The sequence is enumerated once into a private Int32 array, preserving order and duplicates. An empty array is explicitly forwarded; no no-op behavior is inferred. Enumerator errors propagate before mutation.

`modificationFlag` is a required Int32. The reviewed documentation does not establish its meanings/range; the caller must confirm a suitable value and empty-array behavior on the connected build. Every Int32 is forwarded unchanged. The inspected signatures differ from vendor help that includes additional count/error parameters. The library uses the inspected two-input removal and four-input modification declarations, with no alternate-arity retry, invented status or public reference output.

A normal void return establishes dispatch completion only. Native membership, duplicate handling, entity existence and persistence are not inferred. The library adds no selection change, geometry edit, save, read-back or rollback. Native view effects may precede an exception. Array marshaling and mode semantics still require controlled native acceptance.

## Acquiring optional operations

Use `wrapper.Root.Capabilities`. Reading the property captures the existing guarded gateway without running an optional operation. Each acquisition independently checks the wrapper's native version snapshot and member metadata on that exact root. It never selects a session from a global active object, process name, installed directory or host application version.

| Exact observed version | ADINA export | Error/warning messages |
| --- | --- | --- |
| `25.0.1.424` | Candidate | Disabled by version policy |
| `26.0.0.340` | Candidate | Candidate |
| Other builds in a candidate year, including `26.0.1.491` | `ContractUnverified` | `ContractUnverified` where the family permits the feature |
| Missing/incomplete version | `UnknownVersion` | `UnknownVersion` |
| Other product families | `DisabledByVersionPolicy` | `DisabledByVersionPolicy` |

These conservative candidate profiles carry forward the reviewed declarations; they do not certify native runtime behavior. A candidate also needs matching actual function kind, return, parameter count and reference types. Name resolution alone never grants an interface. Message grants additionally require compatible `GetAnalysisStatus` metadata. Absent members return `NativeMemberAbsent`; incompatible/unavailable metadata returns `ContractUnverified`. Busy, disconnected, wrong-thread, locale and other native errors propagate rather than becoming cached absence. No optional operation is executed during discovery.

`RootCapabilityResult<TCapability>` supplies `Availability`, `IsAvailable`, `Reason`, a defensive `NativeVersion` copy and `Capability`. Only an available result contains a non-null interface. The common Root adapter cannot be cast to an optional interface; concrete optional adapters are internal. A grant is a past acquisition result, not a live liveness, licensing, readiness or success guarantee.

The metadata inspection follows Microsoft's [IDispatch name-resolution](https://learn.microsoft.com/en-us/windows/win32/api/oaidl/nf-oaidl-idispatch-getidsofnames), [type-information](https://learn.microsoft.com/en-us/windows/win32/api/oaidl/nf-oaidl-idispatch-gettypeinfo) and [FUNCDESC](https://learn.microsoft.com/en-us/windows/win32/api/oaidl/ns-oaidl-funcdesc) contracts. Inspection-owned descriptors/type information are released; the caller's native root is not released by discovery.

## Analysis messages

Error and warning interfaces are acquired independently. Each operation validates and normalizes its model path once to an absolute Windows path. It does not require a file to exist or parse a log itself. It reads `GetAnalysisStatus` for the selected count, allocates a BSTR array internally and invokes the exact selected message method. An unavailable count produces `AnalysisCountUnavailable` without retrieval; counts above **100,000** produce `MessageLimitExceeded`. A count of zero still invokes native retrieval, preserving the actual native response.

| Native code | Meaning |
| --- | --- |
| `1` | Retrieved messages |
| `0` | Successful absence |
| `-1`, `-2` | General error, invalid path |
| `-107`, `-119` | Not an array, wrong array type |
| `-108` | Insufficient capacity |
| `-1001`, `-10004` | Missing source, analysis running |
| Other Int32 | Preserved unknown native failure |

Only an explicit `-108` allows one fresh count query and one additional retrieval. A second capacity failure stops; exceptions and other failures are not retried. An unsuccessful retry preflight retains the preceding `-108` as `NativeStatus`.

Successful data must be a one-dimensional BSTR array within the allocation limit, with every returned slot written as a non-null string. Replaced arrays and nonzero lower bounds are supported. Text order, empty strings, Unicode, whitespace and line breaks are preserved in an independent read-only collection. Unwritten or malformed successful buffers throw. Native false/failure codes expose no partial messages.

`AnalysisMessagesResult` separates `Outcome` from nullable `NativeStatus`, recognized nullable `Status`, `AnalysisState`, `RetrievalAttempts` and `Messages`. A null native status means retrieval was not invoked. `IsSuccessful` describes message retrieval or successful absence; it does not certify successful analysis or current results. Counts and retrieval are sequential and can race native changes.

## ADINA export

Export validates `AdinaAxisSystem` (Preserve=0, YUp=1, ZUp=2) and `AdinaLoadCaseHandling` (MultipleLoadCases=0, TimeHistory=1), then submits those two Int32 options once. `AdinaExportResult` preserves the raw native code and requested options, a nullable recognized status, `IsSuccessful` and `HasWarnings`. Zero is success; `-14501` is success with warnings, `-14502` failure and `-101` no open model. Unknown signed codes remain inspectable and do not become success.

The server chooses the output path and overwrite behavior for its ADINA `.in` and `.ADINA.log` files. This API provides no output filename, filesystem preflight, atomic overwrite guarantee or rollback. Use a controlled model/destination and inspect the outputs separately. No save, analysis, retry or alternative signature is added. Bentley introduced the export API in the [STAAD.Pro 2025 update](https://docs.bentley.com/LiveContent/web/STAAD.Pro-v2026.0.1/ReadMe/en/topics/ReadMe/c-stpst_Revision_History_250001.html); availability still depends on the exact qualified native object.

## Example

On an owned wrapper's constructing STA thread:

~~~csharp
var root = wrapper.Root;
bool modifyConfirmedView = false;
int confirmedMode = 0; // replace with a mode verified for the connected build
if (modifyConfirmedView)
    root.ModifyNamedView("Frame view", new[] { 1, 2 },
        StaadPro.Interop.Core.Enums.Root.NamedViewEntityType.Beam, confirmedMode);

bool removeView = false;
if (removeView) root.RemoveNamedView("Frame view");

var errors = root.Capabilities.GetAnalysisErrorMessages();
if (errors.IsAvailable)
{
    var result = errors.Capability.GetAnalysisErrorMessages(@"C:\Models\Frame.std");
    Console.WriteLine(result.Outcome);
    foreach (string text in result.Messages) Console.WriteLine(text);
}
else Console.WriteLine(errors.Availability + ": " + errors.Reason);

var export = root.Capabilities.GetAdinaExport();
bool requestExport = false; // enable only for a controlled native export
if (export.IsAvailable && requestExport)
    Console.WriteLine(export.Capability.ExportModelToAdina(
        StaadPro.Interop.Core.Enums.Root.AdinaAxisSystem.Preserve,
        StaadPro.Interop.Core.Enums.Root.AdinaLoadCaseHandling.MultipleLoadCases).NativeStatus);
~~~

## Lifetime and verification

The common methods, gateway property, acquisitions and acquired operations use one current-Root lease per call on the constructing COM thread. Validation, enumeration, metadata checks, dispatch, retries and output qualification remain under that lease. Captured gateways/grants are rejected after `RefreshComObjects()`, including refreshes that report the same native version. Reacquire `wrapper.Root`, the gateway and grants. Disposal throws `ObjectDisposedException`; stale/manual-noncurrent adapters, other threads and reentrant Root/refresh/disposal calls throw `InvalidOperationException` before native work.

The lease cannot coordinate raw native calls, other wrappers or external UI. Native calls may block and exceptions can follow partial effects. Original exceptions/HRESULTs propagate. No thread migration, timeout, session acquisition, implicit unit/domain refresh, blanket retry or rollback is provided.

Offline tests and the installed-package consumer verify exact signatures, defensive inputs/results, metadata profiles and descriptors, malformed native outputs, bounded retry, exception identity and lifecycle/thread/reentrancy guards. XML examples and this guide compile against the installed package. No live view mutation, ADINA export, message retrieval or new native-build runtime acceptance was performed; metadata compatibility is distinct from runtime and engineering qualification.

# Full job information, analysis engine and CONNECTED projects

These five `IOSRoot` contracts operate on the wrapper's currently bound STAAD model. They have matching XML IntelliSense on `IOSRoot` and `OSRootAdapter`.

| Contract | Result | Native request |
| --- | --- | --- |
| `SetFullJobInfo(...)` | `void` | One thirteen-string `SetFullJobInfo` call |
| `AnalyzeModel(AnalysisEngine engine = AnalysisEngine.StaadPro)` | `void` | One `AnalyzeModel` call with Int32 engine code `-1` |
| `GetConnectedProjectInfo()` | `ConnectedProjectResult` | One `GetCONNECTEDProjectInfo` query with two internal VARIANT outputs |
| `SetConnectedProjectInfo(string projectId, string projectName)` | `bool` | One `SetCONNECTEDProjectInfo` request in ID/name order |
| `SetConnectedProjectInfo(ConnectedProjectInfo projectInfo)` | `bool` | The same assignment using an immutable managed value |

## Full job information

`SetFullJobInfo` preserves this parameter order: `jobName`, `jobClient`, `engineerName`, `engineerDate`, `jobNumber`, `revision`, `part`, `reference`, `checkerName`, `checkerDate`, `approverName`, `approverDate`, `comments`. Every input must be non-null. Validation finishes before native dispatch; `ArgumentNullException.ParamName` identifies the null input.

Empty strings, whitespace, Unicode and line breaks are forwarded unchanged. The three dates are literal native text. The library does not parse them, select a culture, validate a date format or serialize the `FullJobInfo` model's `DateTime` fields. Supply date strings accepted by your STAAD version. Native field limits, clearing behavior and persistence still apply. A normal void return reports completion of dispatch; it does not certify storage or save. Read back and save explicitly when required.

## Analysis engine

`AnalysisEngine.StaadPro` is the only documented engine selector and has native value `-1`. The optional default explicitly sends that Int32 argument. Undefined enum values throw `ArgumentOutOfRangeException` naming `engine` before native access.

The installed native declaration returns void, even though one vendor help example assigns its result to an integer. `AnalyzeModel` therefore does not invent a success flag. It does not call `Analyze` or `AnalyzeEx`, save, change silent mode, poll or wait for results. Use [analysis observations](Root-Analysis.md) separately. A synchronous request can block or display UI; returning does not guarantee completed analysis or valid engineering results. A native exception may follow a launched analysis.

## CONNECTED project queries

`GetConnectedProjectInfo` returns an immutable `ConnectedProjectResult`. Its `IsSuccessful` property reflects a native Boolean or integral zero/one. Unsupported types and other values throw `InvalidOperationException` rather than being coerced. False is an ordinary native response: `Project` is null, even if native outputs were partly written or malformed. It does not establish why retrieval failed or prove that no association exists.

On success, both output buffers must be written strings. Unwritten buffers and non-string values throw. The internal reference parameters are not exposed publicly. Successful text is preserved exactly, including explicit empty strings; identifiers are not parsed as GUIDs. Each call produces new immutable result/data objects. A successful query does not verify credentials, membership, cloud synchronization or continued association.

## CONNECTED project assignment

Both assignment overloads reject null and blank identifiers/names before native access. The string overload identifies `projectId` or `projectName`; the value overload identifies `projectInfo`. `ConnectedProjectInfo` itself permits empty query snapshots, but those snapshots cannot be assigned. Nonblank values retain surrounding spaces and Unicode. The object overload uses the shared internal implementation inside the same operation lease; it does not reenter a public Root method.

The returned Boolean represents only the qualified native zero/one response. Unsupported return shapes throw, potentially after native effects. The library adds no project creation, clearing/unassignment, login, membership change, network retry, save or read-back. Coordinate intended assignments with your application and native account state.

## Example

On an owned wrapper's constructing STA thread:

~~~csharp
var root = wrapper.Root;
bool updateFullJob = false;
if (updateFullJob)
    root.SetFullJobInfo("Frame", "Client", "Engineer", "", "J-1", "A", "Part",
        "Reference", "Checker", "", "Approver", "", "Notes");

bool requestAnalysis = false;
if (requestAnalysis)
    root.AnalyzeModel(StaadPro.Interop.Core.Enums.Root.AnalysisEngine.StaadPro);

var projectResult = root.GetConnectedProjectInfo();
if (projectResult.IsSuccessful)
    Console.WriteLine(projectResult.Project.Name);

bool assignProject = false;
if (assignProject)
{
    var projectInfo = new StaadPro.Interop.Core.Models.Root.ConnectedProjectInfo("project-id", "Project name");
    bool assigned = root.SetConnectedProjectInfo(projectInfo);
    Console.WriteLine(assigned);
}
~~~

The flags control independent examples; these calls do not form a transaction. Obtain the actual project identity before assignment.

## Lifetime and verification

One operation lease covers validation, input capture, native dispatch and output qualification. Use the constructing COM thread and current `wrapper.Root`. Disposed wrappers throw `ObjectDisposedException`; stale/manual-noncurrent bindings, another thread and nested Root/refresh/disposal operations throw `InvalidOperationException` before native access. Reacquire Root after `RefreshComObjects()`. This guard cannot coordinate another wrapper, raw native calls or external UI.

Native errors preserve their original exception and HRESULT and may follow partial effects. No automatic retry, rollback, worker-thread COM, session acquisition, unit refresh or domain refresh is added.

Managed tests cover thirteen-field mapping/null validation, engine codes/defaults, native flag shapes, false/partial/unwritten query outputs, immutable snapshots, exact assignment strings, original failures and thread/lifetime/reentrancy guards. Installed-package checks compile this guide and all XML examples and exercise the public package with managed native stand-ins. No live analysis, project assignment, model mutation or new cross-version native qualification was performed.

# Root unit, job-information and structure updates

Preview `1.0.0-preview.20261110` adds five contracts to `IOSRoot` and its concrete `OSRootAdapter` with complete matching XML IntelliSense:

| Operation | Contract | Native dispatch |
| --- | --- | --- |
| `SetInputUnitForLength` | `void SetInputUnitForLength(LengthInputUnit lengthUnit)` | One Int32 length code |
| `SetInputUnitForForce` | `void SetInputUnitForForce(ForceInputUnit forceUnit)` | One Int32 force code |
| `SetShortJobInfo` | `void SetShortJobInfo(string jobName, string jobClient, string engineerName)` | Three strings in job/client/engineer order |
| `SetShortJobInfo` | `void SetShortJobInfo(ShortJobInfo jobInfo)` | One snapshot of the existing model's three short fields |
| `UpdateStructure` | `void UpdateStructure()` | One no-argument native update |

All five native operations return void. A normal return establishes dispatch completion, not a success acknowledgement, read-back, persistence or engineering acceptance. Native failures may follow partial effects. These APIs make explicit changes to the bound model; coordinate session ownership before calling them.

## Changing one input unit

`SetInputUnitForLength` accepts documented native codes 0 through 7: Inch=0, Feet=2, CentiMeter=3, Meter=4, MilliMeter=5, DeciMeter=6 and KiloMeter=7, plus the unnamed feet alias `(LengthInputUnit)1`. The exact Int32 code is passed unchanged. Values outside that range throw `ArgumentOutOfRangeException` naming `lengthUnit` before dispatch. The existing combined `SetInputUnitsEx` continues to accept named enum values only and rejects unnamed code 1.

`SetInputUnitForForce` accepts the defined codes 0 through 7: KiloPound, Pound, KiloGram, MetricTon, Newton, KiloNewton, MegaNewton and DecaNewton. Undefined values throw `ArgumentOutOfRangeException` naming `forceUnit` before dispatch. Each setter calls only its own native member; neither queries or sets the other input unit or substitutes the combined setter.

Changing native input units does not convert existing managed values or results, change the model base-system enum or automatically save. Wrapper unit properties and cached Root unit getters keep their previous snapshots. Call `wrapper.RefreshUnits()` explicitly after the setter returns to reread native units without replacing Root. A returned feet alias is normalized to the named `Feet` value by the existing unit reader. Native read failures can still use the compatibility fallbacks described in [Root metadata](Root-Metadata.md).

## Updating short job information

The three-string overload requires every argument to be non-null. A null string throws `ArgumentNullException` naming `jobName`, `jobClient` or `engineerName`; all validation finishes before native access. Empty strings, whitespace, Unicode and line breaks are forwarded unchanged. Native text limits and clearing/storage behavior still apply and are not inferred by the managed library.

The model overload requires a non-null `ShortJobInfo`. It reads `Name`, `Client` and `Originator` once into local strings and validates them before one native call. A null object throws `ArgumentNullException` naming `jobInfo`; an uninitialized field throws `ArgumentException` naming `jobInfo`. The caller must coordinate concurrent edits: the three property reads are separate and do not form an atomic snapshot of simultaneous writers. Changes after capture do not change the submitted strings.

`FullJobInfo` is accepted through its inherited short fields. Dates, job number, revision, reference, comments and other full-job fields are not submitted. The setter does not modify the managed record or raise its property notifications. Editing a detached record alone still has no native effect; call the setter explicitly to submit it. Use `GetShortJobInfo()` separately for read-back. Neither overload performs a full-job update, date conversion, save, analysis or automatic read-back.

## Updating the structure

`UpdateStructure` invokes its own native no-argument member once. The vendor help describes an update of the current structure but does not provide a returned status, readiness protocol or detailed rebuild/save guarantee. The library does not substitute analysis, save, model reopening or wrapper refresh. Normal return does not certify that edits are persisted, analysis results are current or domain objects remain usable. Refresh and reacquire adapters explicitly when a separate model transition requires it.

## Example

With an owned connected wrapper on its constructing STA thread:

~~~csharp
var root = wrapper.Root;
bool changeInputUnits = false;
if (changeInputUnits)
{
    root.SetInputUnitForLength(StaadPro.Interop.Core.Enums.LengthInputUnit.Meter);
    root.SetInputUnitForForce(StaadPro.Interop.Core.Enums.ForceInputUnit.KiloNewton);
    wrapper.RefreshUnits();
}

bool submitText = false;
if (submitText) root.SetShortJobInfo("Frame study", "Client", "Engineer");

var job = StaadPro.Interop.Core.Models.ShortJobInfo.From("Frame study", "Client", "Engineer");
bool submitRecord = false;
if (submitRecord) root.SetShortJobInfo(job);

bool updateStructure = false;
if (updateStructure) root.UpdateStructure();
~~~

The flags are independent examples. Choose the operations needed by the application; they are not a transaction and a later error does not undo an earlier update.

## Threading, lifetime and errors

One shared wrapper operation lease covers validation, job-field capture and native dispatch on the constructing COM thread. Disposed wrappers throw `ObjectDisposedException`. Stale or manual-noncurrent adapters, another thread and reentrant Root/refresh/disposal work throw `InvalidOperationException` before native mutation. Reacquire `wrapper.Root` after `RefreshComObjects()`. The object overload calls the shared internal implementation under the same lease rather than reentering a public Root method.

The guard does not coordinate raw-object calls, other wrappers or external UI. Native calls are synchronous and may block or display UI. Original native exceptions and HRESULTs propagate. No timeout, cancellation, retry, rollback, worker-thread COM, alternate-session acquisition, implicit save or cache/domain refresh is added.

The installed STAAD.Pro 2025 Root help documents the individual unit-code tables, three-string short job setter and void structure update. Managed tests cover every valid unit code, alias behavior, invalid inputs, exact string mapping, detached records, cache ownership, original failures and binding/thread/reentrancy guards. The installed-package consumer checks all common Root contracts and expanded XML, compiles this guide and the XML examples, and exercises the new requests against a managed native stand-in. No live model mutation or new cross-version native qualification was performed.

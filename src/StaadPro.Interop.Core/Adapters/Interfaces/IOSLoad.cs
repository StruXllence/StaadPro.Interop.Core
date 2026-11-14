using StaadPro.Interop.Core.Entities;
using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Provides guarded load-case title and primary/reference metadata queries.</summary>
    /// <remarks>Other Load operations are not yet implemented.</remarks>
    public interface IOSLoad : IOSBase
    {
        /// <summary>Gets the captured native Load object for advanced, apartment-aware interoperation.</summary>
        /// <remarks>Raw access is outside managed operation validation. Do not use a captured binding after wrapper refresh/disposal.</remarks>
        dynamic ComObject { get; }

        /// <summary>Reads the title of one native load case without activating it.</summary>
        /// <param name="lcId">Positive native load-case ID; not a collection index.</param>
        /// <returns>The exact native string, including an explicitly empty title. Non-string/null outputs throw.</returns>
        /// <remarks>Calls GetLoadCaseTitle once. Preserves Unicode, whitespace and line breaks; does not infer case family
        /// or whether results exist. Native errors and missing cases are not converted to an empty title.<para>Runs on the wrapper's constructing COM thread under one current-binding operation lease, including
        /// validation, enumeration, native dispatch and output qualification. RefreshComObjects invalidates captured
        /// adapters; reacquire them. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output,
        /// refresh or disposal calls are rejected before native access. The guard cannot coordinate raw calls,
        /// other wrappers or external UI. No implicit analysis, case activation, selection, save, unit conversion,
        /// background COM, retry or rollback is added. Native failures preserve original exceptions/HRESULTs.
        /// Calls can block. Snapshots do not guarantee result freshness or engineering validity.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, another thread/reentrant operation is active,
        /// a native flag/title is malformed or successful outputs are unwritten, nonfinite or incorrectly shaped.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A compatible native member is unavailable.</exception>

        /// <exception cref="ArgumentOutOfRangeException">lcId is not positive; no native call is made.</exception>
        /// <example><code>Console.WriteLine(wrapper.Load.GetLoadCaseTitle(1));</code></example>
        string GetLoadCaseTitle(int lcId);

        /// <summary>Reads all existing primary load cases and their titles and engineering load types.</summary>
        /// <returns>A newly allocated, non-null set of new <see cref="LoadCase"/> objects with positive
        /// IDs, preserved titles, CaseType of PrimaryLoad, and validated Type values.
        /// A successful zero count returns a fresh empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads GetPrimaryLoadCaseCount, GetPrimaryLoadCaseNumbers, GetLoadCaseTitle
        /// and GetLoadType on the currently open model. Primary number retrieval accepts Boolean
        /// true (observed on STAAD.Pro 2025) or an integral count matching the preceding count.
        /// Boolean false fails. A Boolean success requires a valid ID array and a matching count
        /// read again before metadata retrieval. True is never converted to integer one; a numeric
        /// zero is not success for a nonempty inventory. Reference enumeration retains its integral-count contract.
        /// Titles (including empty strings, whitespace and Unicode) are preserved without trimming.
        /// Engineering types include None (23); unknown codes are rejected.</para>
        /// <para>No case is activated and no active-case state or assignments are changed.
        /// Each call creates independent case objects; they contain metadata, not load assignments.
        /// Primary and reference IDs can overlap; metadata is read from the corresponding family.</para>
        /// <para>Negative/malformed counts, changed counts, invalid ID arrays, nonpositive or duplicate
        /// IDs, non-string titles and invalid type results throw instead of returning a partial set.
        /// The read is not an atomic model snapshot: serialize model edits and shared-session operations
        /// on the appropriate COM thread. Use a connected, undisposed wrapper.</para>
        /// <para>LoadCase equality and hashing use Id, CaseType and Type. Do not mutate those fields
        /// while a case belongs to a HashSet; remove it first or rebuild the set afterward.</para>
        /// <para>Runs on the wrapper constructing thread under one current Load operation lease spanning input
        /// enumeration, validation, count/ID qualification and metadata calls. RefreshComObjects invalidates this
        /// adapter; reacquire wrapper.Load. Disposed, stale/manual-noncurrent, other-thread and reentrant
        /// Root/Load/Output/refresh/disposal calls are rejected before native work. Raw access, another wrapper
        /// and external UI are not coordinated. Each operation is bounded to 100,000 cases; larger native
        /// inventories or input lists are rejected before allocation/metadata retrieval. Native arrays are
        /// one-dimensional Int32 buffers with every slot written; replacement arrays/nonzero bounds are accepted.
        /// No unit conversion, assignment hydration, retries, analysis, activation, save or native mutation is added.
        /// Concurrent model changes can still invalidate sequential observations.</para></remarks>
        /// <exception cref="InvalidOperationException">OpenSTAAD reports failure or returns inconsistent or malformed metadata.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface does not expose a required signature.</exception>
        /// <example>
        /// With a connected IOSLoad named load:
        /// <code>
        /// var load = wrapper.Load;
        /// var cases = wrapper.Load.GetAllPrimaryLoadCases();
        /// foreach (var lc in cases) Console.WriteLine(lc.Id + ": " + lc.Title + " (" + lc.Type + ")");
        /// </code>
        /// </example>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, a native response is malformed,
        /// or the managed case limit is exceeded.</exception>
        HashSet<ILoadCase> GetAllPrimaryLoadCases();

        /// <summary>Reads all existing reference load cases and their titles and engineering load types.</summary>
        /// <returns>A newly allocated, non-null set of new <see cref="LoadCase"/> objects with positive
        /// IDs, preserved titles, CaseType of ReferenceLoad, and validated Type values.
        /// A successful zero count returns a fresh empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads GetReferenceLoadCaseCount, GetReferenceLoadCaseNumbers, GetReferenceLoadCaseTitle
        /// and GetReferenceLoadType on the currently open model. The case-number method returns a count,
        /// which must match the preceding count; it does not use zero as a success status.
        /// Titles (including empty strings, whitespace and Unicode) are preserved without trimming.
        /// Engineering types include None (23); unknown codes are rejected.</para>
        /// <para>No case is activated and no active-case state or assignments are changed.
        /// Each call creates independent case objects; they contain metadata, not load assignments.
        /// Primary and reference IDs can overlap; metadata is read from the corresponding family.</para>
        /// <para>Negative/malformed counts, changed counts, invalid ID arrays, nonpositive or duplicate
        /// IDs, non-string titles and invalid type results throw instead of returning a partial set.
        /// The read is not an atomic model snapshot: serialize model edits and shared-session operations
        /// on the appropriate COM thread. Use a connected, undisposed wrapper.</para>
        /// <para>LoadCase equality and hashing use Id, CaseType and Type. Do not mutate those fields
        /// while a case belongs to a HashSet; remove it first or rebuild the set afterward.</para>
        /// <para>Runs on the wrapper constructing thread under one current Load operation lease spanning input
        /// enumeration, validation, count/ID qualification and metadata calls. RefreshComObjects invalidates this
        /// adapter; reacquire wrapper.Load. Disposed, stale/manual-noncurrent, other-thread and reentrant
        /// Root/Load/Output/refresh/disposal calls are rejected before native work. Raw access, another wrapper
        /// and external UI are not coordinated. Each operation is bounded to 100,000 cases; larger native
        /// inventories or input lists are rejected before allocation/metadata retrieval. Native arrays are
        /// one-dimensional Int32 buffers with every slot written; replacement arrays/nonzero bounds are accepted.
        /// No unit conversion, assignment hydration, retries, analysis, activation, save or native mutation is added.
        /// Concurrent model changes can still invalidate sequential observations.</para></remarks>
        /// <exception cref="InvalidOperationException">OpenSTAAD reports failure or returns inconsistent or malformed metadata.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface does not expose a required signature.</exception>
        /// <example>
        /// With a connected IOSLoad named load:
        /// <code>
        /// var load = wrapper.Load;
        /// var cases = wrapper.Load.GetAllReferenceLoadCases();
        /// foreach (var lc in cases) Console.WriteLine(lc.Id + ": " + lc.Title + " (" + lc.Type + ")");
        /// </code>
        /// </example>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, a native response is malformed,
        /// or the managed case limit is exceeded.</exception>
        HashSet<ILoadCase> GetAllReferenceLoadCases();

        /// <summary>Reads the title and engineering type of one existing primary load case by ID.</summary>
        /// <param name="lcId">Positive STAAD primary case number in the connected model, not a
        /// zero-based index. Zero (the native active-case shortcut) and negative numbers are rejected.</param>
        /// <returns>A newly allocated, non-null <see cref="LoadCase"/> containing the requested Id,
        /// preserved Title, CaseType of PrimaryLoad, and validated engineering Type.
        /// Each call creates an independent metadata object; no assignments are loaded.</returns>
        /// <remarks>
        /// <para>Calls GetLoadCaseTitle(lcId), then GetLoadType(lcId), without activating a case
        /// or enumerating case numbers. Reference and primary IDs may overlap; metadata is read
        /// exclusively from the requested family. The active case and model assignments are unchanged.</para>
        /// <para>Titles retain whitespace, Unicode and empty text. A literal title "NONE" is preserved;
        /// it is not interpreted as absence. Non-string titles, native type errors and undefined
        /// LoadType codes throw. Valid type codes are 0 through 23, including None.
        /// A missing ID is reported through the native metadata error contract and is not returned as null.</para>
        /// <para>This is a metadata read, not an atomic model snapshot. Use a connected, undisposed
        /// wrapper and serialize shared-session operations/model edits on the appropriate COM thread.
        /// Changing the returned object does not write to STAAD. Before changing Id, CaseType or Type
        /// on an object stored in a HashSet, remove it or rebuild the set to preserve hash consistency.</para>
        /// <para>Runs on the wrapper constructing thread under one current Load operation lease spanning input
        /// enumeration, validation, count/ID qualification and metadata calls. RefreshComObjects invalidates this
        /// adapter; reacquire wrapper.Load. Disposed, stale/manual-noncurrent, other-thread and reentrant
        /// Root/Load/Output/refresh/disposal calls are rejected before native work. Raw access, another wrapper
        /// and external UI are not coordinated. Each operation is bounded to 100,000 cases; larger native
        /// inventories or input lists are rejected before allocation/metadata retrieval. Native arrays are
        /// one-dimensional Int32 buffers with every slot written; replacement arrays/nonzero bounds are accepted.
        /// No unit conversion, assignment hydration, retries, analysis, activation, save or native mutation is added.
        /// Concurrent model changes can still invalidate sequential observations.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lcId"/> is not positive.</exception>
        /// <exception cref="InvalidOperationException">OpenSTAAD returns malformed metadata or reports a type lookup failure.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <example>
        /// With a connected IOSLoad named load and an existing primary case:
        /// <code>
        /// var load = wrapper.Load;
        /// var lc = wrapper.Load.GetPrimaryLoadCaseFromId(lcId: 17);
        /// Console.WriteLine(lc.Id + ": " + lc.Title + " (" + lc.Type + ")");
        /// </code>
        /// </example>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, a native response is malformed,
        /// or the managed case limit is exceeded.</exception>
        ILoadCase GetPrimaryLoadCaseFromId(int lcId);

        /// <summary>Reads the title and engineering type of one existing reference load case by ID.</summary>
        /// <param name="lcId">Positive STAAD reference case number in the connected model, not a
        /// zero-based index. Zero (the native active-case shortcut) and negative numbers are rejected.</param>
        /// <returns>A newly allocated, non-null <see cref="LoadCase"/> containing the requested Id,
        /// preserved Title, CaseType of ReferenceLoad, and validated engineering Type.
        /// Each call creates an independent metadata object; no assignments are loaded.</returns>
        /// <remarks>
        /// <para>Calls GetReferenceLoadCaseTitle(lcId), then GetReferenceLoadType(lcId), without activating a case
        /// or enumerating case numbers. Reference and primary IDs may overlap; metadata is read
        /// exclusively from the requested family. The active case and model assignments are unchanged.</para>
        /// <para>Titles retain whitespace, Unicode and empty text. A literal title "NONE" is preserved;
        /// it is not interpreted as absence. Non-string titles, native type errors and undefined
        /// LoadType codes throw. Valid type codes are 0 through 23, including None.
        /// A missing ID is reported through the native metadata error contract and is not returned as null.</para>
        /// <para>This is a metadata read, not an atomic model snapshot. Use a connected, undisposed
        /// wrapper and serialize shared-session operations/model edits on the appropriate COM thread.
        /// Changing the returned object does not write to STAAD. Before changing Id, CaseType or Type
        /// on an object stored in a HashSet, remove it or rebuild the set to preserve hash consistency.</para>
        /// <para>Runs on the wrapper constructing thread under one current Load operation lease spanning input
        /// enumeration, validation, count/ID qualification and metadata calls. RefreshComObjects invalidates this
        /// adapter; reacquire wrapper.Load. Disposed, stale/manual-noncurrent, other-thread and reentrant
        /// Root/Load/Output/refresh/disposal calls are rejected before native work. Raw access, another wrapper
        /// and external UI are not coordinated. Each operation is bounded to 100,000 cases; larger native
        /// inventories or input lists are rejected before allocation/metadata retrieval. Native arrays are
        /// one-dimensional Int32 buffers with every slot written; replacement arrays/nonzero bounds are accepted.
        /// No unit conversion, assignment hydration, retries, analysis, activation, save or native mutation is added.
        /// Concurrent model changes can still invalidate sequential observations.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lcId"/> is not positive.</exception>
        /// <exception cref="InvalidOperationException">OpenSTAAD returns malformed metadata or reports a type lookup failure.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <example>
        /// With a connected IOSLoad named load and an existing reference case:
        /// <code>
        /// var load = wrapper.Load;
        /// var lc = wrapper.Load.GetReferenceLoadCaseFromId(lcId: 17);
        /// Console.WriteLine(lc.Id + ": " + lc.Title + " (" + lc.Type + ")");
        /// </code>
        /// </example>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, a native response is malformed,
        /// or the managed case limit is exceeded.</exception>
        ILoadCase GetReferenceLoadCaseFromId(int lcId);

        /// <summary>Reads existing primary load case metadata for an ordered sequence of case IDs.</summary>
        /// <param name="loadCasesIds">Non-null sequence of positive STAAD primary case numbers.
        /// The sequence is enumerated once and all IDs are validated before any COM metadata access.</param>
        /// <returns>A newly allocated, non-null list of new <see cref="LoadCase"/> objects, one per
        /// supplied ID, in input order. Duplicate IDs are preserved as distinct objects. Every CaseType
        /// is PrimaryLoad. An empty input returns a fresh empty list without contacting OpenSTAAD.</returns>
        /// <remarks>
        /// <para>The operation is eager: input enumeration, validation and all metadata reads complete
        /// before returning. Each ID uses GetLoadCaseTitle followed by GetLoadType; no case is activated
        /// and no case-number enumeration is performed. The active case and assignments are unchanged.</para>
        /// <para>Titles preserve whitespace, Unicode, empty strings and literal "NONE". Types must be
        /// defined LoadType codes (0 through 23, including None). Malformed/native-error metadata throws
        /// and no partial list is returned. Exceptions from the input enumerator propagate unchanged;
        /// enumeration and argument failures occur before any COM metadata call.</para>
        /// <para>List entries are independent metadata objects, including repeated IDs. Editing them
        /// does not write to STAAD. Input IDs are snapshotted, but metadata reads are not an atomic model
        /// snapshot. Use a connected, undisposed wrapper and serialize shared-session operations and
        /// model edits on the appropriate COM thread. No assignments are loaded.</para>
        /// <para>Runs on the wrapper constructing thread under one current Load operation lease spanning input
        /// enumeration, validation, count/ID qualification and metadata calls. RefreshComObjects invalidates this
        /// adapter; reacquire wrapper.Load. Disposed, stale/manual-noncurrent, other-thread and reentrant
        /// Root/Load/Output/refresh/disposal calls are rejected before native work. Raw access, another wrapper
        /// and external UI are not coordinated. Each operation is bounded to 100,000 cases; larger native
        /// inventories or input lists are rejected before allocation/metadata retrieval. Native arrays are
        /// one-dimensional Int32 buffers with every slot written; replacement arrays/nonzero bounds are accepted.
        /// No unit conversion, assignment hydration, retries, analysis, activation, save or native mutation is added.
        /// Concurrent model changes can still invalidate sequential observations.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="loadCasesIds"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Any element of <paramref name="loadCasesIds"/> is not positive.</exception>
        /// <exception cref="InvalidOperationException">OpenSTAAD returns malformed metadata or reports a type lookup failure.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <example>
        /// With a connected IOSLoad named load and existing primary cases:
        /// <code>
        /// var load = wrapper.Load;
        /// var cases = wrapper.Load.GetPrimaryLoadCasesFromIds(new[] { 17, 41, 17 });
        /// foreach (var lc in cases) Console.WriteLine(lc.Id + ": " + lc.Title);
        /// </code>
        /// </example>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, a native response is malformed,
        /// or the managed case limit is exceeded.</exception>
        List<ILoadCase> GetPrimaryLoadCasesFromIds(IEnumerable<int> loadCasesIds);
        /// <summary>Reads existing reference load case metadata for an ordered sequence of case IDs.</summary>
        /// <param name="loadCasesIds">Non-null sequence of positive STAAD reference case numbers.
        /// The sequence is enumerated once and all IDs are validated before any COM metadata access.</param>
        /// <returns>A newly allocated, non-null list of new <see cref="LoadCase"/> objects, one per
        /// supplied ID, in input order. Duplicate IDs are preserved as distinct objects. Every CaseType
        /// is ReferenceLoad. An empty input returns a fresh empty list without contacting OpenSTAAD.</returns>
        /// <remarks>
        /// <para>The operation is eager: input enumeration, validation and all metadata reads complete
        /// before returning. Each ID uses GetReferenceLoadCaseTitle followed by GetReferenceLoadType; no case is activated
        /// and no case-number enumeration is performed. The active case and assignments are unchanged.</para>
        /// <para>Titles preserve whitespace, Unicode, empty strings and literal "NONE". Types must be
        /// defined LoadType codes (0 through 23, including None). Malformed/native-error metadata throws
        /// and no partial list is returned. Exceptions from the input enumerator propagate unchanged;
        /// enumeration and argument failures occur before any COM metadata call.</para>
        /// <para>List entries are independent metadata objects, including repeated IDs. Editing them
        /// does not write to STAAD. Input IDs are snapshotted, but metadata reads are not an atomic model
        /// snapshot. Use a connected, undisposed wrapper and serialize shared-session operations and
        /// model edits on the appropriate COM thread. No assignments are loaded.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current Load operation lease spanning
        /// input enumeration, validation, native inventory/membership checks and metadata qualification.
        /// Reacquire wrapper.Load after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access.
        /// Raw calls, other wrappers and external UI are not coordinated. Each native inventory and supplied
        /// sequence is limited to 100,000 entries, including duplicate inputs before deduplication.
        /// No worker-thread COM, automatic hydration, activation, analysis, unit conversion, save, retry
        /// or rollback is introduced. Synchronous native calls may block; failures preserve their original
        /// exception/HRESULT. Sequential metadata observations are not an atomic model snapshot.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="loadCasesIds"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Any element of <paramref name="loadCasesIds"/> is not positive.</exception>
        /// <exception cref="InvalidOperationException">OpenSTAAD returns malformed metadata or reports a type lookup failure.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The captured binding/thread/operation is invalid or a native inventory exceeds 100,000 cases.</exception>
        /// <exception cref="ArgumentException">A requested ID sequence contains more than 100,000 entries, including duplicates.</exception>
        /// <example>
        /// With a connected IOSLoad named load and existing reference cases:
        /// <code>
        /// var cases = wrapper.Load.GetReferenceLoadCasesFromIds(new[] { 17, 41, 17 });
        /// foreach (var lc in cases) Console.WriteLine(lc.Id + ": " + lc.Title);
        /// </code>
        /// </example>
        List<ILoadCase> GetReferenceLoadCasesFromIds(IEnumerable<int> loadCasesIds);

        /// <summary>Reads metadata for one existing load combination, after verifying its membership.</summary>
        /// <param name="lcId">Positive STAAD load combination number in the connected model.
        /// Zero (the native active-case shortcut) and negative IDs are rejected before COM access.</param>
        /// <returns>A new, non-null <see cref="LoadCase"/> with the requested Id, preserved Title,
        /// CaseType of LoadCombination and Type of None. No factors or assignments are loaded.</returns>
        /// <remarks>
        /// <para>Reads and validates GetLoadCombinationCaseCount and GetLoadCombinationCaseNumbers
        /// before calling GetLoadCaseTitle only for the requested combination. An ID absent from the
        /// combination list throws, including an ID belonging only to a primary or reference case.
        /// An empty combination list throws without reading a title. This method enumerates combination
        /// IDs on each call; use GetAllLoadCombinationCases when reading many combinations.</para>
        /// <para>Combinations have no engineering LoadType category: Type is always None (23) and
        /// GetLoadType is not called. The title must be a string; whitespace, Unicode, empty strings
        /// and literal "NONE" are preserved. Nonpositive/duplicate IDs, malformed arrays or counts,
        /// native errors and non-string titles throw. Missing combinations are not returned as null.</para>
        /// <para>No case is activated and the active case/model assignments are unchanged. Each call
        /// returns an independent metadata object. Use a connected, undisposed wrapper and serialize
        /// shared-session access/model edits on the appropriate COM thread. Membership and title reads
        /// are not an atomic snapshot. Editing the result does not write to STAAD; remove it from any
        /// HashSet before changing Id, CaseType or Type, or rebuild the set afterward.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current Load operation lease spanning
        /// input enumeration, validation, native inventory/membership checks and metadata qualification.
        /// Reacquire wrapper.Load after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access.
        /// Raw calls, other wrappers and external UI are not coordinated. Each native inventory and supplied
        /// sequence is limited to 100,000 entries, including duplicate inputs before deduplication.
        /// No worker-thread COM, automatic hydration, activation, analysis, unit conversion, save, retry
        /// or rollback is introduced. Synchronous native calls may block; failures preserve their original
        /// exception/HRESULT. Sequential metadata observations are not an atomic model snapshot.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lcId"/> is not positive.</exception>
        /// <exception cref="InvalidOperationException">The ID is absent from the combination list, or OpenSTAAD reports failure or malformed metadata.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The captured binding/thread/operation is invalid or a native inventory exceeds 100,000 cases.</exception>
        /// <example>
        /// With a connected IOSLoad named load and an existing combination:
        /// <code>
        /// var lc = wrapper.Load.GetLoadCombinationCaseFromId(lcId: 201);
        /// Console.WriteLine(lc.Id + ": " + lc.Title);
        /// </code>
        /// </example>
        ILoadCase GetLoadCombinationCaseFromId(int lcId);

        /// <summary>Reads metadata for a distinct set of existing load combination IDs.</summary>
        /// <param name="lcIds">Non-null sequence of positive STAAD combination numbers.
        /// Input is enumerated once and every ID is validated before any COM access. Duplicate IDs
        /// are collapsed; output order is unspecified.</param>
        /// <returns>A fresh, non-null HashSet exposed as IEnumerable, containing one new
        /// <see cref="LoadCase"/> per distinct requested ID. CaseType is LoadCombination and Type
        /// is None. An empty input returns a fresh empty set without contacting OpenSTAAD.</returns>
        /// <remarks>
        /// <para>This operation is eager. It snapshots input, reads and validates the combination
        /// ID list once, and checks every requested ID for membership before reading any title.
        /// A missing or primary-only ID throws; no entries are silently skipped and no partial
        /// set is returned. Titles are read once per distinct requested ID. No factors,
        /// constituent cases, assignments or engineering type categories are loaded.</para>
        /// <para>Native counts, number returns and arrays use the same strict validation as
        /// GetAllLoadCombinationCases. Titles retain whitespace, Unicode, empty text and literal
        /// "NONE"; non-string results throw. Input-enumerator, COM and binding failures propagate.
        /// Re-enumerating the returned set performs no COM reads; call again to refresh metadata.</para>
        /// <para>No case is activated or model data changed. Use a connected, undisposed wrapper
        /// and serialize shared-session operations/model edits on the appropriate COM thread.
        /// Membership and titles are not an atomic snapshot. Results are independent of input,
        /// native buffers and previous calls. Remove an object from any HashSet before changing
        /// its Id, CaseType or Type, or rebuild the set afterward to preserve hash consistency.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current Load operation lease spanning
        /// input enumeration, validation, native inventory/membership checks and metadata qualification.
        /// Reacquire wrapper.Load after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access.
        /// Raw calls, other wrappers and external UI are not coordinated. Each native inventory and supplied
        /// sequence is limited to 100,000 entries, including duplicate inputs before deduplication.
        /// No worker-thread COM, automatic hydration, activation, analysis, unit conversion, save, retry
        /// or rollback is introduced. Synchronous native calls may block; failures preserve their original
        /// exception/HRESULT. Sequential metadata observations are not an atomic model snapshot.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="lcIds"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Any element of <paramref name="lcIds"/> is not positive.</exception>
        /// <exception cref="InvalidOperationException">A requested ID is absent or native data is malformed/inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The captured binding/thread/operation is invalid or a native inventory exceeds 100,000 cases.</exception>
        /// <exception cref="ArgumentException">A requested ID sequence contains more than 100,000 entries, including duplicates.</exception>
        /// <example>
        /// With a connected IOSLoad named load and existing combinations:
        /// <code>
        /// var combinations = wrapper.Load.GetLoadCombinationCasesFromIds(new[] { 201, 301, 201 });
        /// foreach (var lc in combinations) Console.WriteLine(lc.Id + ": " + lc.Title);
        /// </code>
        /// </example>
        IEnumerable<ILoadCase> GetLoadCombinationCasesFromIds(IEnumerable<int> lcIds);

        /// <summary>Reads metadata for every existing load combination in the connected model.</summary>
        /// <returns>A fresh, non-null HashSet of new <see cref="LoadCase"/> metadata objects, with
        /// positive Id, preserved Title, CaseType of LoadCombination and Type of None.
        /// A successful zero count returns a fresh empty set. Iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads GetLoadCombinationCaseCount and GetLoadCombinationCaseNumbers, then
        /// GetLoadCaseTitle for each validated ID. Number retrieval accepts Boolean true or an
        /// integral return matching the separate count; false and mismatched integral counts fail.
        /// A Boolean success requires a matching count read again before titles are retrieved.
        /// True is never converted to integer one. Numeric zero is not success for a nonempty inventory.
        /// IDs must be positive and unique
        /// in a one-dimensional Int32 array of exactly the expected length. Nonzero bounds are supported.</para>
        /// <para>Combinations have no engineering LoadType classification, so Type is always None (23);
        /// GetLoadType is not called. Titles preserve whitespace, Unicode, empty text and literal "NONE".
        /// No combination factors, constituent cases, assignments or analysis results are loaded.</para>
        /// <para>No case is activated and no model data is changed. Negative/malformed counts, native
        /// errors, inconsistent counts, invalid ID arrays and non-string titles throw instead of returning
        /// partial results. Returned objects are independent of native arrays and previous calls.</para>
        /// <para>Use a connected, undisposed wrapper and serialize shared-session operations/model edits
        /// on the appropriate COM thread. This is not an atomic snapshot. LoadCase hashing uses Id,
        /// CaseType and Type; remove objects from a HashSet before changing those fields or rebuild it.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current Load operation lease spanning
        /// input enumeration, validation, native inventory/membership checks and metadata qualification.
        /// Reacquire wrapper.Load after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access.
        /// Raw calls, other wrappers and external UI are not coordinated. Each native inventory and supplied
        /// sequence is limited to 100,000 entries, including duplicate inputs before deduplication.
        /// No worker-thread COM, automatic hydration, activation, analysis, unit conversion, save, retry
        /// or rollback is introduced. Synchronous native calls may block; failures preserve their original
        /// exception/HRESULT. Sequential metadata observations are not an atomic model snapshot.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">OpenSTAAD reports failure or returns inconsistent or malformed metadata.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The captured binding/thread/operation is invalid or a native inventory exceeds 100,000 cases.</exception>
        /// <example>
        /// With a connected IOSLoad named load:
        /// <code>
        /// var combinations = wrapper.Load.GetAllLoadCombinationCases();
        /// foreach (var lc in combinations) Console.WriteLine(lc.Id + ": " + lc.Title);
        /// </code>
        /// </example>
        HashSet<ILoadCase> GetAllLoadCombinationCases();

        /// <summary>Reads all case metadata for one supported load case family.</summary>
        /// <param name="loadCaseType">PrimaryLoad (the default), ReferenceLoad or LoadCombination.
        /// RepeatLoad and undefined enum values are rejected before COM access.</param>
        /// <returns>A fresh, non-null HashSet exposed as IEnumerable, with new <see cref="LoadCase"/>
        /// metadata objects for the selected family. Zero native count returns a fresh empty set.
        /// Iteration order is unspecified; combination Type is None and other types are validated.</returns>
        /// <remarks>
        /// <para>Delegates to GetAllPrimaryLoadCases, GetAllReferenceLoadCases or
        /// GetAllLoadCombinationCases. Each family retains its strict count, number, array and
        /// title validation; primary/reference engineering types use their own native APIs.
        /// No families are merged. Repeat-load discovery is not implemented.</para>
        /// <para>The read is eager: all native metadata calls finish before returning, and
        /// re-enumerating the result makes no COM calls. Failures throw instead of returning a
        /// partial set. No case is activated and no assignments or model data are changed.
        /// Factors, constituent cases and analysis results are not loaded.</para>
        /// <para>Use a connected, undisposed wrapper and serialize shared-session operations
        /// and model edits on the appropriate COM thread. This is not an atomic snapshot.
        /// Returned objects are independent of earlier calls; editing them does not write to
        /// STAAD. Remove objects from HashSets before changing Id, CaseType or Type, or rebuild
        /// the set afterward because those fields determine LoadCase equality and hashing.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current Load operation lease spanning
        /// input enumeration, validation, native inventory/membership checks and metadata qualification.
        /// Reacquire wrapper.Load after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access.
        /// Raw calls, other wrappers and external UI are not coordinated. Each native inventory and supplied
        /// sequence is limited to 100,000 entries, including duplicate inputs before deduplication.
        /// No worker-thread COM, automatic hydration, activation, analysis, unit conversion, save, retry
        /// or rollback is introduced. Synchronous native calls may block; failures preserve their original
        /// exception/HRESULT. Sequential metadata observations are not an atomic model snapshot.</para>
        /// </remarks>
        /// <exception cref="NotSupportedException"><paramref name="loadCaseType"/> is RepeatLoad or undefined.</exception>
        /// <exception cref="InvalidOperationException">OpenSTAAD reports failure or returns malformed/inconsistent metadata.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The underlying COM operation fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed Load interface lacks a required signature.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The captured binding/thread/operation is invalid or a native inventory exceeds 100,000 cases.</exception>
        /// <example>
        /// With a connected IOSLoad named load:
        /// <code>
        /// var primaryCases = wrapper.Load.GetAllLoadCasesOfType();
        /// var referenceCases = wrapper.Load.GetAllLoadCasesOfType(StaadPro.Interop.Core.Enums.LoadCaseType.ReferenceLoad);
        /// foreach (var lc in referenceCases) Console.WriteLine(lc.Id + ": " + lc.Title);
        /// </code>
        /// </example>
        IEnumerable<ILoadCase> GetAllLoadCasesOfType(LoadCaseType loadCaseType = LoadCaseType.PrimaryLoad);

        /// <summary>Finds one case by ID within a fully read load-case family.</summary>
        /// <param name="lcId">Positive case number. A positive number may be absent from the selected family.</param>
        /// <param name="loadCaseType">PrimaryLoad, ReferenceLoad or LoadCombination. RepeatLoad and undefined values throw NotSupportedException before native access. No optional default is supplied.</param>
        /// <returns>A new LoadCase exposed as ILoadCase when the ID belongs to the selected family, otherwise null. An empty family returns null. Combination Type is None; primary/reference types are validated.</returns>
        /// <remarks><para>Reads the entire selected family through the shared inventory/metadata reader before looking for the ID, preserving family-wide validation. Malformed metadata for another case still throws. Primary/reference IDs may overlap, so only the requested family is searched. Use the existing family-specific single-ID methods when their direct-request behavior fits the operation.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current Load operation lease, including input enumeration, validation, native dispatch and result qualification. Reacquire wrapper.Load after RefreshComObjects. Disposed wrappers throw ObjectDisposedException; stale/manual-noncurrent bindings, other threads and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access. Raw calls, other wrappers and external UI are not coordinated. Calls can block and native exceptions/HRESULTs propagate without a partial result. Sequential observations do not form an atomic model snapshot.</para>
        /// <para>Reads metadata only. Editing returned objects does not update STAAD. No case activation, selection, assignment/factor hydration, analysis, save, conversion or retry occurs. Native inventories and supplied ID sequences are bounded to 100,000 entries. Results own their storage and contain independent detached metadata objects. Coordinate model edits with the owning COM thread.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">lcId is not positive.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native status/count/arrays/metadata are malformed, or a supported inventory exceeds the managed bound.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">An original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected native Load lacks a required signature.</exception>
        /// <exception cref="NotSupportedException">loadCaseType is RepeatLoad or undefined.</exception>
        /// <example><code>
        /// var item = wrapper.Load.GetLoadCase(7, StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad);
        /// if (item != null) Console.WriteLine(item.Title);
        /// </code></example>
        ILoadCase GetLoadCase(int lcId, LoadCaseType loadCaseType);

        /// <summary>Reads existing cases within an inclusive ID range in one load-case family.</summary>
        /// <param name="fromId">Positive lower case number, included in the range.</param>
        /// <param name="toId">Positive upper case number, included in the range and at least fromId.</param>
        /// <param name="loadCaseType">PrimaryLoad, ReferenceLoad or LoadCombination. RepeatLoad and undefined values throw NotSupportedException before native access. No optional default is supplied.</param>
        /// <returns>A fresh List exposed as IEnumerable containing one new metadata record per matching existing ID. No matches returns an empty list. Result order is unspecified.</returns>
        /// <remarks><para>Validates both bounds, reads the entire family eagerly and then filters. Equal bounds select at most one case. Reversed bounds throw. Does not enumerate the integers between bounds, synthesize absent IDs or sort records. Native/malformed metadata failures propagate even for cases outside the range. Re-enumerating the returned collection makes no native calls.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current Load operation lease, including input enumeration, validation, native dispatch and result qualification. Reacquire wrapper.Load after RefreshComObjects. Disposed wrappers throw ObjectDisposedException; stale/manual-noncurrent bindings, other threads and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access. Raw calls, other wrappers and external UI are not coordinated. Calls can block and native exceptions/HRESULTs propagate without a partial result. Sequential observations do not form an atomic model snapshot.</para>
        /// <para>Reads metadata only. Editing returned objects does not update STAAD. No case activation, selection, assignment/factor hydration, analysis, save, conversion or retry occurs. Native inventories and supplied ID sequences are bounded to 100,000 entries. Results own their storage and contain independent detached metadata objects. Coordinate model edits with the owning COM thread.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">fromId or toId is not positive.</exception>
        /// <exception cref="ArgumentException">toId is less than fromId.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native status/count/arrays/metadata are malformed, or a supported inventory exceeds the managed bound.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">An original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected native Load lacks a required signature.</exception>
        /// <exception cref="NotSupportedException">loadCaseType is RepeatLoad or undefined.</exception>
        /// <example><code>
        /// var cases = wrapper.Load.GetLoadCases(1, int.MaxValue, StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad);
        /// foreach (var item in cases) Console.WriteLine(item.Id);
        /// </code></example>
        IEnumerable<ILoadCase> GetLoadCases(int fromId, int toId, LoadCaseType loadCaseType);

        /// <summary>Filters one family's complete metadata by a supplied set of requested IDs.</summary>
        /// <param name="loadCasesIds">Non-null positive case numbers, enumerated once and fully validated before native access. At most 100,000 entries including duplicates. Repetitions collapse; input order does not determine result order. Missing IDs are omitted.</param>
        /// <param name="loadCaseType">PrimaryLoad, ReferenceLoad or LoadCombination. RepeatLoad and undefined values throw NotSupportedException before native access. No optional default is supplied. The family is validated even for empty input.</param>
        /// <returns>A fresh List exposed as IEnumerable containing one independent record for each existing matching ID in the selected family. Empty input or no matches returns an empty list. Iteration order is unspecified.</returns>
        /// <remarks><para>Captures the ID sequence once under the operation lease. Empty valid input returns without native calls. Otherwise reads the entire family before filtering, including metadata for unrequested cases. Such metadata failures propagate. Input/enumerator failures occur before native access. Unlike primary/reference direct-request lists, repetitions do not produce multiple records. Unlike combination direct requests, missing IDs are omitted after a successful inventory read.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current Load operation lease, including input enumeration, validation, native dispatch and result qualification. Reacquire wrapper.Load after RefreshComObjects. Disposed wrappers throw ObjectDisposedException; stale/manual-noncurrent bindings, other threads and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access. Raw calls, other wrappers and external UI are not coordinated. Calls can block and native exceptions/HRESULTs propagate without a partial result. Sequential observations do not form an atomic model snapshot.</para>
        /// <para>Reads metadata only. Editing returned objects does not update STAAD. No case activation, selection, assignment/factor hydration, analysis, save, conversion or retry occurs. Native inventories and supplied ID sequences are bounded to 100,000 entries. Results own their storage and contain independent detached metadata objects. Coordinate model edits with the owning COM thread.</para></remarks>
        /// <exception cref="ArgumentNullException">loadCasesIds is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A requested ID is not positive.</exception>
        /// <exception cref="ArgumentException">More than 100,000 input entries are supplied.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native status/count/arrays/metadata are malformed, or a supported inventory exceeds the managed bound.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">An original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected native Load lacks a required signature.</exception>
        /// <exception cref="NotSupportedException">loadCaseType is RepeatLoad or undefined.</exception>
        /// <example><code>
        /// var cases = wrapper.Load.GetLoadCasesFromIds(new[] { 7, 3, 7 }, StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad);
        /// foreach (var item in cases) Console.WriteLine(item.Id);
        /// </code></example>
        IEnumerable<ILoadCase> GetLoadCasesFromIds(IEnumerable<int> loadCasesIds, LoadCaseType loadCaseType);

        /// <summary>Reads all metadata for a supported family into an owned editable list.</summary>
        /// <param name="loadCaseType">PrimaryLoad, ReferenceLoad or LoadCombination. RepeatLoad and undefined values throw NotSupportedException before native access. No optional default is supplied.</param>
        /// <returns>A fresh non-null List of new metadata records from exactly the requested family. Zero inventory count returns an empty list. Order is unspecified. Combination Type is None.</returns>
        /// <remarks><para>Uses the same qualified inventory and metadata path as GetAllLoadCasesOfType, then copies the set into a list. All reads finish before return; enumeration and subsequent list edits make no native calls. The list and records are independent of earlier queries. Primary/reference IDs and titles/types come from their own family APIs.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current Load operation lease, including input enumeration, validation, native dispatch and result qualification. Reacquire wrapper.Load after RefreshComObjects. Disposed wrappers throw ObjectDisposedException; stale/manual-noncurrent bindings, other threads and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access. Raw calls, other wrappers and external UI are not coordinated. Calls can block and native exceptions/HRESULTs propagate without a partial result. Sequential observations do not form an atomic model snapshot.</para>
        /// <para>Reads metadata only. Editing returned objects does not update STAAD. No case activation, selection, assignment/factor hydration, analysis, save, conversion or retry occurs. Native inventories and supplied ID sequences are bounded to 100,000 entries. Results own their storage and contain independent detached metadata objects. Coordinate model edits with the owning COM thread.</para></remarks>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native status/count/arrays/metadata are malformed, or a supported inventory exceeds the managed bound.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">An original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected native Load lacks a required signature.</exception>
        /// <exception cref="NotSupportedException">loadCaseType is RepeatLoad or undefined.</exception>
        /// <example><code>
        /// var cases = wrapper.Load.GetLoadCasesOfType(StaadPro.Interop.Core.Enums.LoadCaseType.ReferenceLoad);
        /// Console.WriteLine(cases.Count);
        /// </code></example>
        List<ILoadCase> GetLoadCasesOfType(LoadCaseType loadCaseType);

        /// <summary>Reads the positive identifiers of all load envelopes in the connected model.</summary>
        /// <returns>A fresh HashSet of distinct positive envelope IDs. A successful zero count returns an empty set. Ordering is unspecified; these IDs identify envelopes, not load cases.</returns>
        /// <remarks><para>Calls GetEnvelopeCount and validates an integral count between zero and 100,000. Zero skips ID retrieval. Otherwise allocates an unwritten Int32 sentinel buffer and calls GetEnvelopeIDs once. Its documented long return must be integer 1 for success. Boolean true, an array count, zero, negative and unsupported scalar values fail. Every ID must be written, positive and unique in an exact-length rank-one Int32 array; replacements and nonzero lower bounds are accepted. Rechecks GetEnvelopeCount after buffer qualification and throws if it changed.</para>
        /// <para>The original source's empty-inventory null is represented by a fresh empty set. Native failures and malformed buffers throw. The returned set is independent of native buffers and earlier calls. Envelope types, member case IDs and factors are not retrieved. No new data model is required.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current Load operation lease, including input enumeration, validation, native dispatch and result qualification. Reacquire wrapper.Load after RefreshComObjects. Disposed wrappers throw ObjectDisposedException; stale/manual-noncurrent bindings, other threads and reentrant Root/Load/Output/refresh/disposal calls are rejected before native access. Raw calls, other wrappers and external UI are not coordinated. Calls can block and native exceptions/HRESULTs propagate without a partial result. Sequential observations do not form an atomic model snapshot.</para>
        /// <para>Reads metadata only. Editing returned objects does not update STAAD. No case activation, selection, assignment/factor hydration, analysis, save, conversion or retry occurs. Native inventories and supplied ID sequences are bounded to 100,000 entries. Results own their storage and contain independent detached metadata objects. Coordinate model edits with the owning COM thread.</para></remarks>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native status/count/arrays/metadata are malformed, or a supported inventory exceeds the managed bound.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">An original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected native Load lacks a required signature.</exception>
        /// <example><code>
        /// foreach (int id in wrapper.Load.GetEnvelopeIds()) Console.WriteLine("Envelope " + id);
        /// </code></example>
        HashSet<int> GetEnvelopeIds();

    }
}

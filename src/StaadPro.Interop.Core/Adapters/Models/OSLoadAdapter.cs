using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Services.Load;
using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Output;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Load binding with guarded member-force foundation operations.</summary>
    public sealed class OSLoadAdapter : OSBaseAdapter, IOSLoad
    {
        /// <summary>Captures the native Load object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Load binding is unavailable.</exception>
        public OSLoadAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawLoad ?? throw new ArgumentException("The Load binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this adapter was constructed.</summary>
        public dynamic ComObject { get; }

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
        public string GetLoadCaseTitle(int lcId) => Wrapper.UseLoadBinding(this, wrapper => MemberForceReader.GetLoadCaseTitle(wrapper.RawLoad, lcId));

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
        public HashSet<ILoadCase> GetAllPrimaryLoadCases() => Wrapper.UseLoadBinding(this, wrapper => LoadCaseMetadataReader.ReadAll(wrapper.RawLoad, true));

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
        public HashSet<ILoadCase> GetAllReferenceLoadCases() => Wrapper.UseLoadBinding(this, wrapper => LoadCaseMetadataReader.ReadAll(wrapper.RawLoad, false));

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
        public ILoadCase GetPrimaryLoadCaseFromId(int lcId) => Wrapper.UseLoadBinding(this, wrapper => LoadCaseMetadataReader.Read(wrapper.RawLoad, lcId, true));

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
        public ILoadCase GetReferenceLoadCaseFromId(int lcId) => Wrapper.UseLoadBinding(this, wrapper => LoadCaseMetadataReader.Read(wrapper.RawLoad, lcId, false));

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
        public List<ILoadCase> GetPrimaryLoadCasesFromIds(IEnumerable<int> loadCasesIds) => Wrapper.UseLoadBinding(this, wrapper => LoadCaseMetadataReader.ReadPrimary(wrapper.RawLoad, loadCasesIds));
    }
}

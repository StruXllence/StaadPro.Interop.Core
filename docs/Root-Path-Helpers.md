# Root filename, analysis-path and model identity helpers

| Function | Contract |
| --- | --- |
| `GetStaadFileNameOnly()` | Extracts the Windows filename and extension from one raw path snapshot; preserves case/Unicode; explicit blank yields empty |
| `GetAnlFileFullPath()` | Derives the directory and final-extension-free filename stem plus uppercase `.ANL` from one snapshot; explicit blank yields empty; relative native input stays relative |
| `GetOpenedStaadFilename()` | Normalizes one named native snapshot to an absolute Windows path; explicit blank yields empty; nonempty whitespace is invalid |
| `IsTarget(string staadFileFullPath)` | Validates the request, then compares normalized complete requested/native paths with ordinal case-insensitive comparison |
| `ModelIsOpen(string fileFullPath)` | Uses the same corrected complete-path comparison, preserving its original public parameter name |

## C# usage

With a connected wrapper on its constructing STA thread:

~~~csharp
var root = wrapper.Root;
Console.WriteLine(root.GetStaadFileNameOnly());
Console.WriteLine(root.GetAnlFileFullPath()); // Prediction only; no analysis file is opened or checked.
Console.WriteLine(root.GetOpenedStaadFilename());
Console.WriteLine(root.IsTarget(@"C:\Models\Frame.std"));
Console.WriteLine(root.ModelIsOpen(@"C:\Other\Frame.std")); // Same basename alone does not match.
~~~
For `C:\Models\Frame.v2.STD`, the filename is `Frame.v2.STD` and the expected analysis path is `C:\Models\Frame.v2.ANL`. Only the last extension is replaced. For `relative.std`, the analysis path is `relative.ANL`; the historic `FullPath` name does not force an absolute output. A reported directory/root is not validated as a real filename and can derive a `.ANL` filename under the runtime's directory/combine rules. No file existence, result freshness or analysis availability is implied.

## Blank, malformed and native outputs

The existing reader initializes an internal BSTR-shaped sentinel. An explicitly written empty string is a blank observation: the three string helpers return empty and the two predicates return false. Unwritten, null, DBNull, numeric, Boolean and other non-string outputs throw `InvalidOperationException`; they never become false or a fabricated filename. Native exceptions, including their HRESULT/identity, propagate without retry or an extra error-message query.

These helpers reject quotes, angle brackets, pipe, NUL and control characters 1-31. This retains the reviewed Framework path-character contract even where modern extraction is more permissive. It is not a complete filesystem filename validator: no `.std` extension is required, names need not exist, and Windows device/drive/UNC interpretation still follows the Path API. The raw `GetStaadFileFullPath()` observation continues to return exact native text; these derived helpers add their documented path processing.

## Identity and normalization

The two predicates normalize the requested path before native access. Null/whitespace/invalid-character requests throw with their original public parameter name and cause zero native queries. Both paths use one captured process working directory through the modern two-argument `Path.GetFullPath` API, preventing a native callback changing the working directory from giving the two relative paths different bases. For drive-relative forms the base-path overload's rules apply; use a fully qualified path when a particular drive directory is intended. The standalone `GetOpenedStaadFilename()` reads the native path first and then resolves a named snapshot against the observed working directory.

Dot segments and normal separator variations are normalized by the Windows runtime; letter case is preserved and text is not trimmed by this library. Comparison is `OrdinalIgnoreCase` and includes the whole path. Equal basenames in different directories remain different. Filesystem links, aliases, case-sensitive directories, network availability and physical file identity are not resolved. A true result describes a textual snapshot of this root alone, not every STAAD process or Geometry/Load readiness. Coordinate process-wide working-directory changes and native session edits outside the wrapper too.

## Lifetime, tests and qualification

Use the wrapper's constructing STA thread and reacquire Root after `RefreshComObjects()`. `RefreshUnits()` preserves the binding. Disposed/replaced/manual-noncurrent bindings, another thread and nested Root/lifecycle operations are rejected before native dispatch. The lease is released after validation and native failures. It cannot prevent changes by other wrappers/raw callers, make the multi-call consumer workflow atomic or interrupt a hung COM call.

The increment adds **199 path cases**, bringing the offline suite to **555 tests**. Cases cover Unicode/UNC/relative paths, multiple extensions, exact `.ANL` derivation and one-read behavior, full-path comparison, a working-directory-changing callback, empty/unwritten/malformed output, invalid request parameter names, explicit character validation, native failures, binding/thread/refresh/reentrancy guards and matching complete XML on interfaces/concrete adapters. A fresh NuGet consumer exercises all fifteen Root contracts, checks the predicate signatures and compiles **34 XML examples**, README usage and all three Root guides.

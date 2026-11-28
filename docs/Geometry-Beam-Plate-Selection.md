# Selected beams and plates

Use `wrapper.Geometry` to read the analytical member and plate element IDs currently selected in STAAD.Pro. Run the queries on the thread that constructed the wrapper, and reacquire the adapter after `RefreshComObjects()`.

| Method | Result |
| --- | --- |
| `GetSelectedBeamsIds()` | An unordered set of analytical member IDs. |
| `GetSelectedPlatesIds()` | An unordered set of plate element IDs. |
| `GetSelectedBeamsListAsync()` | An array of member IDs in the order returned by OpenSTAAD. |
| `GetSelectedPlatesListAsync()` | An array of plate IDs in the order returned by OpenSTAAD. |
| `GetSelectedBeamsExt(IEnumerable<Beam> fromModelBeams)` | The first supplied beam for each selected member ID. One `null` entry marks any missing IDs. |

The readers use `GetNoOfSelectedBeams` or `GetNoOfSelectedPlates` to size the native Int32 array. A zero count returns an empty collection without fetching a list. Positive counts up to 1,000,000 must match the number of unique positive IDs returned. The reader rechecks the count and throws on a change or malformed output. External changes that leave the count unchanged can still affect the read.

The array methods request native selection order and finish the native calls before returning a completed or faulted task. The beam-object overload scans the supplied sequence at most once and uses its existing beam references, including any supplied endpoints or beta angles. A `Beam` uses its ID as its hash key. Remove it from the set before changing its ID and reinsert it afterward.

~~~csharp
var plateIds = wrapper.Geometry.GetSelectedPlatesListAsync().GetAwaiter().GetResult();
foreach (int id in plateIds) Console.WriteLine(id);

var beams = new[] { new StaadPro.Interop.Core.Entities.Beam { Id = 3 } };
foreach (var beam in wrapper.Geometry.GetSelectedBeamsExt(beams))
    if (beam != null) Console.WriteLine(beam.Id);
~~~

Custom `IOSGeometry` implementations need to supply the new methods when upgrading. The package verifies them with native-shaped fixtures. Live selection-array compatibility depends on the connected STAAD.Pro build.

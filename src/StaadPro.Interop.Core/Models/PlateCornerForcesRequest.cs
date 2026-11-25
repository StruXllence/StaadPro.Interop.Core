using System;
using System.Text.Json.Serialization;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Describes one requested plate-corner force observation without constructing a full Cartesian selection.</summary>
    /// <remarks>Stores editable caller-owned case/plate/corner references. Constructors do not validate or query STAAD.
    /// Query methods capture references and positive IDs before native work; later request edits do not redirect those
    /// observations. A request identifies a node number, not a corner ordinal. Coordinate/unit transformation is not included.</remarks>
    public class PlateCornerForcesRequest
    {
        /// <summary>Creates an empty request for object initializers.</summary>
        public PlateCornerForcesRequest() { }
        /// <summary>Stores the supplied metadata references without validation or copying.</summary>
        /// <param name="loadCase">Analyzed case or combination metadata.</param>
        /// <param name="plate">Plate metadata containing supplied A/B/C incidence and optional D.</param>
        /// <param name="cornerNode">Requested positive incidence-node number, resolved by the query against the plate.</param>
        public PlateCornerForcesRequest(ILoadCase loadCase, Plate plate, Node cornerNode)
        { LoadCase = loadCase; Plate = plate; CornerNode = cornerNode; }
        /// <summary>Gets or sets the caller-owned case metadata reference.</summary>
        /// <value>Editable metadata, including null in unvalidated manual requests; no native effect.</value>
        public ILoadCase LoadCase { get; set; }
        /// <summary>Gets or sets the caller-owned populated plate metadata reference.</summary>
        /// <value>A/B/C are required by sparse queries; no automatic hydration occurs.</value>
        public Plate Plate { get; set; }
        /// <summary>Gets or sets the requested corner-node metadata reference.</summary>
        /// <value>The node's ID selects the first matching supplied incidence reference; coordinates are unused.</value>
        public Node CornerNode { get; set; }
        /// <summary>Gets an immutable identifier key computed from current editable metadata.</summary>
        /// <value>A current case/plate/corner key, not a historical query identity or validation of plate membership. Excluded from System.Text.Json serialization.</value>
        /// <exception cref="InvalidOperationException">Case, plate or corner metadata is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A current identifier is not positive.</exception>
        [JsonIgnore]
        public PlateCornerForcesKey Key
        {
            get
            {
                ILoadCase load = LoadCase; Plate plate = Plate; Node node = CornerNode;
                if (load == null || plate == null || node == null)
                    throw new InvalidOperationException("A corner request requires case, plate and corner metadata.");
                return new PlateCornerForcesKey(load.Id, plate.Id, node.Id);
            }
        }
    }
}

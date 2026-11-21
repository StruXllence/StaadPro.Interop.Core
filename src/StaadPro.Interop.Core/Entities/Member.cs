using System.Collections.Generic;
using StaadPro.Interop.Core.Common;
namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Stores detached physical-member identity, endpoints and supplied node/beam membership.</summary>
    /// <remarks>Supports governing-member selection. Does not identify physical members, compute geometry, create
    /// native members or hydrate connectivity. Collections are editable supplied references; edits do not update STAAD.
    /// Node equality remains Core reference equality; this type does not imply the complete source geometry API.</remarks>
    public sealed class Member : ValidatablePropertyStore, IEntity
    {
        /// <summary>Creates zero identity, null endpoints and independent empty Nodes/Beams sets.</summary>
        public Member() { Nodes = new HashSet<Node>(); Beams = new HashSet<Beam>(); }
        /// <summary>Stores supplied endpoints and inserts them into Nodes.</summary>
        /// <param name="sNode">Stored start reference, including null for manual data.</param>
        /// <param name="eNode">Stored end reference, including null for manual data.</param>
        public Member(Node sNode, Node eNode) : this() { StartNode = sNode; EndNode = eNode; Nodes.Add(sNode); Nodes.Add(eNode); }
        /// <summary>Stores detached member identity and supplied endpoints.</summary>
        /// <param name="id">Identifier stored without native validation.</param>
        /// <param name="sNode">Stored start-node reference.</param>
        /// <param name="eNode">Stored end-node reference.</param>
        public Member(int id, Node sNode, Node eNode) : this(sNode, eNode) { Id = id; }
        /// <summary>Gets the constructor-supplied physical-member identifier.</summary>
        /// <value>The stored ID, initially zero; not used to rank utilization.</value>
        public int Id { get; private set; }
        /// <summary>Gets the supplied start-node reference.</summary>
        /// <value>The original reference or null; governing selection does not read it.</value>
        public Node StartNode { get; private set; }
        /// <summary>Gets the supplied end-node reference.</summary>
        /// <value>The original reference or null; no coordinate qualification is implied.</value>
        public Node EndNode { get; private set; }
        /// <summary>Gets or sets detached node membership.</summary>
        /// <value>The supplied set reference, initially a fresh empty set. Governing selection ignores it.</value>
        public HashSet<Node> Nodes { get => Get<HashSet<Node>>(); set => Set(value); }
        /// <summary>Gets or sets the supplied analytical beams forming this member.</summary>
        /// <value>The set reference, initially empty. Collection edits themselves do not raise property notifications.</value>
        public HashSet<Beam> Beams { get => Get<HashSet<Beam>>(); set => Set(value); }
    }
}

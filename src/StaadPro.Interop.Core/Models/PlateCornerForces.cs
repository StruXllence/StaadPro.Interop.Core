using System;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores six copied native plate-corner force and moment components with editable context metadata.</summary>
    /// <remarks>Inherits Fx,Fy,Fz,Mx,My,Mz and component notifications from Forces. Values retain the native
    /// output axes, signs and base force/length units; they are not plate-center stresses or moments per width.
    /// No rotation, unit conversion, averaging or equilibrium/design acceptance is performed. Public manual
    /// constructors/setters accept unvalidated scalar/context data; qualified query readers validate separately.</remarks>
    public class PlateCornerForces : Forces
    {
        /// <summary>Creates zero components with null metadata and no native access.</summary>
        public PlateCornerForces() { }
        /// <summary>Stores supplied context references and six literal components.</summary>
        /// <param name="loadCase">Case metadata retained by reference.</param><param name="plate">Plate metadata retained by reference.</param>
        /// <param name="cornerNode">Incidence-node metadata retained by reference.</param>
        /// <param name="fx">Literal force X.</param><param name="fy">Literal force Y.</param><param name="fz">Literal force Z.</param>
        /// <param name="mx">Literal moment X.</param><param name="my">Literal moment Y.</param><param name="mz">Literal moment Z.</param>
        public PlateCornerForces(ILoadCase loadCase, Plate plate, Node cornerNode, double fx, double fy, double fz, double mx, double my, double mz)
            : base(fx, fy, fz, mx, my, mz) { LoadCase = loadCase; Plate = plate; CornerNode = cornerNode; }
        /// <summary>Copies six components from another detached record.</summary>
        /// <param name="loadCase">Retained case metadata.</param><param name="plate">Retained plate metadata.</param>
        /// <param name="cornerNode">Retained incidence-node metadata.</param><param name="forces">Values to copy; null leaves zeros.</param>
        public PlateCornerForces(ILoadCase loadCase, Plate plate, Node cornerNode, Forces forces)
            : this(loadCase, plate, cornerNode, forces?.Fx ?? 0, forces?.Fy ?? 0, forces?.Fz ?? 0, forces?.Mx ?? 0, forces?.My ?? 0, forces?.Mz ?? 0) { }
        /// <summary>Copies six literal array components in Fx,Fy,Fz,Mx,My,Mz order.</summary>
        /// <param name="loadCase">Retained case metadata.</param><param name="plate">Retained plate metadata.</param>
        /// <param name="cornerNode">Retained incidence-node metadata.</param><param name="forcesArray">Non-null, exact six-element array; manual values are unvalidated.</param>
        /// <exception cref="ArgumentNullException">forcesArray is null.</exception>
        /// <exception cref="ArgumentException">forcesArray does not have six values.</exception>
        public PlateCornerForces(ILoadCase loadCase, Plate plate, Node cornerNode, double[] forcesArray)
            : this(loadCase, plate, cornerNode, Forces.ParseFromArray(forcesArray)) { }
        /// <summary>Gets or sets the shared case metadata reference.</summary>
        /// <value>Caller-owned metadata; edits do not requery or rebuild result-set indexes.</value>
        public ILoadCase LoadCase { get; set; }
        /// <summary>Gets or sets the shared plate metadata reference.</summary>
        /// <value>Caller plate or newly hydrated plate, depending on the query path.</value>
        public Plate Plate { get; set; }
        /// <summary>Gets or sets the resolved plate incidence-node reference.</summary>
        /// <value>The first matching incidence reference, which can differ from the supplied node of the same ID.</value>
        public Node CornerNode { get; set; }
        /// <summary>Gets the current metadata plate identifier, or zero for null.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int PlateId => Plate?.Id ?? 0;
        /// <summary>Gets the current metadata incidence-node identifier, or zero for null.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int CornerNodeId => CornerNode?.Id ?? 0;
        /// <summary>Gets the current metadata case identifier, or zero for null.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int LoadCaseId => LoadCase?.Id ?? 0;
        /// <summary>Gets a key computed from current editable metadata.</summary>
        /// <value>A new immutable key; may differ from a result set's originally captured observation key after edits.</value>
        /// <exception cref="ArgumentOutOfRangeException">Current metadata lacks positive IDs.</exception>
        public PlateCornerForcesKey Key => new(LoadCaseId, PlateId, CornerNodeId);
        /// <summary>Creates an independently copied six-component record from a managed array.</summary>
        /// <param name="forcesArray">Six literal components in Fx,Fy,Fz,Mx,My,Mz order.</param>
        /// <param name="loadCase">Retained case metadata.</param><param name="plate">Retained plate metadata.</param><param name="cornerNode">Retained node metadata.</param>
        /// <returns>A new editable record without native access, transformation or unit conversion.</returns>
        /// <exception cref="ArgumentNullException">forcesArray is null.</exception><exception cref="ArgumentException">The array length is not six.</exception>
        public static PlateCornerForces ParseFromArray(double[] forcesArray, ILoadCase loadCase, Plate plate, Node cornerNode) =>
            new(loadCase, plate, cornerNode, forcesArray);
    }
}

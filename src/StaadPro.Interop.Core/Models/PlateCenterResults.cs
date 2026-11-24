using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Interfaces;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Groups plate-center stresses, moments per width, von Mises and principal stresses for one case and plate.</summary>
    /// <remarks>Retains the four supplied component records without copying. Qualified Output queries create independent
    /// records from four sequential native observations; this container is not an atomic solver snapshot. Scalar edits
    /// and later context edits do not change native state. Does not provide unit conversion or geometry/design helpers.</remarks>
    public class PlateCenterResults : IPlateStress
    {
        /// <summary>Groups four existing detached records after checking their context compatibility.</summary>
        /// <param name="forces">Non-null local stress components; supplies the projected LoadCase and Plate references.</param>
        /// <param name="moments">Non-null moment-per-width components for the same case reference and analytical plate ID.</param>
        /// <param name="vonMises">Non-null surface von Mises stresses for the same case reference and analytical plate ID.</param>
        /// <param name="principal">Non-null normal principal stresses for the same case reference and analytical plate ID.</param>
        /// <remarks>Preserves source compatibility: cases must be the same object, even when IDs match; distinct non-null
        /// plate objects with equal IDs are accepted. Identical plate references (including all-null manual contexts) are
        /// accepted without reading IDs. Different plate references require equal captured IDs; null matches only null.
        /// Manual zero/negative IDs, null contexts and arbitrary scalar values are not validated. Checks occur once;
        /// subsequent mutable context edits can break agreement. No component is copied, recalculated or converted.</remarks>
        /// <exception cref="ArgumentNullException">A component record is null.</exception>
        /// <exception cref="InvalidOperationException">Case references or analytical plate identities differ.</exception>
        public PlateCenterResults(PlateCenterForces forces, PlateCenterMoments moments,
            PlateCenterVonMises vonMises, PlatePrincipalStresses principal)
        {
            ArgumentNullException.ThrowIfNull(forces);
            ArgumentNullException.ThrowIfNull(moments);
            ArgumentNullException.ThrowIfNull(vonMises);
            ArgumentNullException.ThrowIfNull(principal);
            if (!ReferenceEquals(forces.LoadCase, moments.LoadCase) || !ReferenceEquals(forces.LoadCase, vonMises.LoadCase) ||
                !ReferenceEquals(forces.LoadCase, principal.LoadCase))
                throw new InvalidOperationException("Plate-center components must share the same load-case reference.");
            Plate first = forces.Plate;
            if (!ReferenceEquals(first, moments.Plate) || !ReferenceEquals(first, vonMises.Plate) || !ReferenceEquals(first, principal.Plate))
            {
                int? id = first?.Id;
                if (!Matches(first, id, moments.Plate) || !Matches(first, id, vonMises.Plate) || !Matches(first, id, principal.Plate))
                    throw new InvalidOperationException("Plate-center components must share the same analytical plate identity.");
            }
            Forces = forces; Moments = moments; VonMises = vonMises; Principal = principal;
        }
        private static bool Matches(Plate first, int? id, Plate other) =>
            ReferenceEquals(first, other) || (first != null && other != null && id == other.Id);

        /// <summary>Gets the case metadata retained by Forces.</summary>
        /// <value>The original reference, including null for manually constructed records; no identity refresh occurs.</value>
        public ILoadCase LoadCase => Forces.LoadCase;
        /// <summary>Gets the analytical plate metadata retained by Forces.</summary>
        /// <value>The original reference; later ID changes do not update any scalar or native query.</value>
        public Plate Plate => Forces.Plate;
        /// <summary>Gets the retained plate-local stress record, despite its historical Forces name.</summary>
        /// <value>Five editable SQX/SQY/SX/SY/SXY components in native force/length-squared units.</value>
        public PlateCenterForces Forces { get; }
        /// <summary>Gets the retained local moment-per-unit-width record.</summary>
        /// <value>Editable MX/MY/MXY values in native moment/length units, without nodal/member-moment conversion.</value>
        public PlateCenterMoments Moments { get; }
        /// <summary>Gets the retained top/bottom von Mises stress record.</summary>
        /// <value>Editable native-surface stress components; surface labels are not global elevations.</value>
        public PlateCenterVonMises VonMises { get; }
        /// <summary>Gets the retained normal principal surface stress record.</summary>
        /// <value>Editable maximum/minimum top/bottom stresses; no principal direction or design check is calculated.</value>
        public PlatePrincipalStresses Principal { get; }

        /// <summary>Creates a container retaining four existing compatible component records.</summary>
        /// <param name="plateCenterForces">Non-null local stress record.</param>
        /// <param name="plateCenterMoments">Non-null local moment-per-width record.</param>
        /// <param name="plateCenterVonMises">Non-null surface von Mises stress record.</param>
        /// <param name="platePrincipalStresses">Non-null normal principal stress record.</param>
        /// <returns>A new container retaining the supplied records without scalar copies.</returns>
        /// <remarks>Uses the constructor's case-reference and plate-ID compatibility rules. No unit conversion,
        /// validation of manual scalar values, native access or component cloning occurs.</remarks>
        /// <exception cref="ArgumentNullException">A component is null.</exception>
        /// <exception cref="InvalidOperationException">The supplied contexts differ.</exception>
        public static PlateCenterResults Create(PlateCenterForces plateCenterForces, PlateCenterMoments plateCenterMoments,
            PlateCenterVonMises plateCenterVonMises, PlatePrincipalStresses platePrincipalStresses) =>
            new(plateCenterForces, plateCenterMoments, plateCenterVonMises, platePrincipalStresses);
    }
}

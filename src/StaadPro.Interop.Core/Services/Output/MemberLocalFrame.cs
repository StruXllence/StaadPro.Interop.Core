using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class MemberLocalFrame
    {
        // The source uses this absolute tolerance for node separation and beta in degrees,
        // and this unit-direction threshold for the near-vertical convention.
        private const double CoordinateTolerance = 1e-9;
        private const double BetaTolerance = 1e-9;
        private const double VerticalThreshold = 1 - 1e-9;

        internal static LocalCS Create(Beam member)
        {
            ArgumentNullException.ThrowIfNull(member);
            var (start, end, beta) = CaptureMember(member);
            return CreateFrame(start, end, beta, nameof(member));
        }

        internal static MemberForces Transform(PlateCornerForces plateCornerForces, Node otherMemberNode)
        {
            ArgumentNullException.ThrowIfNull(plateCornerForces);
            ArgumentNullException.ThrowIfNull(otherMemberNode);
            var (corner, loadCase) = CaptureCorner(plateCornerForces);
            return Prepare(corner, otherMemberNode).Apply(plateCornerForces, loadCase.Id, loadCase.Title);
        }

        internal static MemberForces Transform(PlateCornerForces plateCornerForces, Beam member)
        {
            ArgumentNullException.ThrowIfNull(plateCornerForces);
            ArgumentNullException.ThrowIfNull(member);
            int beamId = member.Id;
            if (beamId <= 0) throw new ArgumentOutOfRangeException(nameof(member), "Member ID must be positive.");
            var (corner, loadCase) = CaptureCorner(plateCornerForces);
            return Prepare(corner, member, beamId).Apply(plateCornerForces, loadCase.Id, loadCase.Title);
        }

        internal static Projection Prepare(Node corner, Node otherMemberNode) =>
            Prepare(Capture(corner, nameof(corner)), otherMemberNode);

        internal static Projection Prepare(Node corner, Beam member)
        {
            int beamId = member.Id;
            if (beamId <= 0) throw new ArgumentOutOfRangeException(nameof(member), "Member ID must be positive.");
            return Prepare(Capture(corner, nameof(corner)), member, beamId);
        }

        private static Projection Prepare(NodePosition corner, Node otherMemberNode)
        {
            var end = Capture(otherMemberNode, nameof(otherMemberNode));
            var frame = CreateFrame(corner, end, 0, nameof(otherMemberNode));
            return new Projection(frame, 0, MemberEnd.Start);
        }

        private static Projection Prepare(NodePosition corner, Beam member, int beamId)
        {
            var (start, end, beta) = CaptureMember(member);
            MemberEnd memberEnd;
            if (SameNode(corner, start)) memberEnd = MemberEnd.Start;
            else if (SameNode(corner, end)) memberEnd = MemberEnd.End;
            else throw new ArgumentException("The plate corner must be a member endpoint.", nameof(member));

            var frame = CreateFrame(start, end, beta, nameof(member));
            return new Projection(frame, beamId, memberEnd);
        }

        private static (NodePosition Start, NodePosition End, double Beta) CaptureMember(Beam member)
        {
            Node start = member.StartNode, end = member.EndNode;
            if (start == null || end == null)
                throw new ArgumentException("The member must have both endpoints.", nameof(member));
            double beta = member.BetaAngle;
            if (!double.IsFinite(beta)) throw new ArgumentException("The member beta angle must be finite.", nameof(member));
            return (Capture(start, nameof(member)), Capture(end, nameof(member)), beta);
        }

        private static (NodePosition Corner, ILoadCase LoadCase) CaptureCorner(PlateCornerForces plateCornerForces)
        {
            if (plateCornerForces.IsMemberLocal)
                throw new ArgumentException("The supplied result is already member-local.", nameof(plateCornerForces));
            var loadCase = plateCornerForces.LoadCase;
            var plate = plateCornerForces.Plate;
            var node = plateCornerForces.CornerNode;
            if (loadCase is null || plate is null || node is null)
                throw new ArgumentException("The result must specify a load case, a plate and a corner node.", nameof(plateCornerForces));
            var corner = Capture(node, nameof(plateCornerForces));
            if (!MatchesSlot(corner, plate.A) && !MatchesSlot(corner, plate.B) &&
                !MatchesSlot(corner, plate.C) && !MatchesSlot(corner, plate.D))
                throw new ArgumentException("The node must be a corner of the supplied plate.", nameof(plateCornerForces));
            return (corner, loadCase);
        }

        private static NodePosition Capture(Node node, string parameter)
        {
            var position = new CartesianVector3(node.X, node.Y, node.Z);
            if (!IsFinite(position)) throw new ArgumentException("Node coordinates must be finite.", parameter);
            return new NodePosition(node, node.Id, position);
        }

        private static bool MatchesSlot(NodePosition node, Node candidate)
        {
            if (candidate == null) return false;
            if (ReferenceEquals(node.Reference, candidate)) return true;
            int candidateId = candidate.Id;
            if (node.Id > 0 && candidateId > 0) return node.Id == candidateId;
            return WithinDistance(node.Position, new CartesianVector3(candidate.X, candidate.Y, candidate.Z));
        }

        private static bool SameNode(NodePosition first, NodePosition second)
        {
            if (ReferenceEquals(first.Reference, second.Reference)) return true;
            if (first.Id > 0 && second.Id > 0) return first.Id == second.Id;
            return WithinDistance(first.Position, second.Position);
        }

        private static bool WithinDistance(CartesianVector3 first, CartesianVector3 second)
        {
            var difference = Difference(first, second);
            // Check components before squaring so huge differences and nonfinite slot data cannot match.
            if (!(MaxAbs(difference) <= CoordinateTolerance)) return false;
            return Magnitude(difference) <= CoordinateTolerance;
        }

        private static LocalCS CreateFrame(NodePosition start, NodePosition end, double beta, string parameter)
        {
            if (SameNode(start, end) || WithinDistance(start.Position, end.Position))
                throw new ArgumentException("Member endpoints must be distinct and more than 1e-9 coordinate units apart.", parameter);

            var direction = Difference(end.Position, start.Position);
            // Halve coordinates only when subtraction would overflow. The common factor drops out on normalization.
            if (!IsFinite(direction)) direction = Difference(Multiply(end.Position, 0.5), Multiply(start.Position, 0.5));
            var u = Normalize(direction);

            // Keep the source's vertical seed and threshold, but project the seed into the transverse plane
            // using cross products so a near-vertical member still gives perpendicular local axes.
            var seed = Math.Abs(u.Y) >= VerticalThreshold
                ? new CartesianVector3(u.Y >= 0 ? -1 : 1, 0, 0)
                : new CartesianVector3(0, 1, 0);
            var w = Normalize(Cross(u, seed));
            var v = Normalize(Cross(w, u));
            if (Math.Abs(beta) > BetaTolerance)
            {
                double angle = beta % 360 * (Math.PI / 180);
                var (sin, cos) = Math.SinCos(angle);
                v = Add(Multiply(v, cos), Multiply(w, sin));
                w = Normalize(Cross(u, v));
                v = Normalize(Cross(w, u));
            }

            return new LocalCS(start.Id, start.Position, u, v, w);
        }

        internal readonly struct Projection(LocalCS frame, int beamId, MemberEnd end)
        {
            internal MemberForces Apply(Forces plateCornerForces, int loadCaseId, string loadCaseTitle)
            {
                var force = new CartesianVector3(plateCornerForces.Fx, plateCornerForces.Fy, plateCornerForces.Fz);
                var moment = new CartesianVector3(plateCornerForces.Mx, plateCornerForces.My, plateCornerForces.Mz);
                if (!IsFinite(force) || !IsFinite(moment))
                    throw new ArgumentException("All six supplied components must be finite.", nameof(plateCornerForces));

                // Project both vectors at the original corner. The frame origin does not enter a vector projection.
                force = ProjectVector(force, frame);
                moment = ProjectVector(moment, frame);
                return new MemberForces
                {
                    BeamId = beamId,
                    End = end,
                    LoadCaseId = loadCaseId,
                    LoadCaseTitle = loadCaseTitle,
                    Fx = force.X, Fy = force.Y, Fz = force.Z,
                    Mx = moment.X, My = moment.Y, Mz = moment.Z
                };
            }
        }

        private static CartesianVector3 ProjectVector(CartesianVector3 vector, LocalCS frame)
        {
            var result = new CartesianVector3(Dot(vector, frame.U), Dot(vector, frame.V), Dot(vector, frame.W));
            if (!IsFinite(result)) throw new OverflowException("A projected component is outside the finite Double range.");
            return result;
        }

        private static double Dot(CartesianVector3 left, CartesianVector3 right)
        {
            double x = left.X * right.X, y = left.Y * right.Y, z = left.Z * right.Z;
            double xy = x + y;
            // When the first sum overflows, add the last term first to allow a finite, cancelling result.
            return double.IsInfinity(xy) ? (x + z) + y : xy + z;
        }

        private static CartesianVector3 Normalize(CartesianVector3 vector)
        {
            double scale = MaxAbs(vector);
            var scaled = new CartesianVector3(vector.X / scale, vector.Y / scale, vector.Z / scale);
            return Multiply(scaled, 1 / Magnitude(scaled));
        }

        private static double Magnitude(CartesianVector3 vector) =>
            Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        private static double MaxAbs(CartesianVector3 vector) =>
            Math.Max(Math.Abs(vector.X), Math.Max(Math.Abs(vector.Y), Math.Abs(vector.Z)));
        private static bool IsFinite(CartesianVector3 vector) =>
            double.IsFinite(vector.X) && double.IsFinite(vector.Y) && double.IsFinite(vector.Z);
        private static CartesianVector3 Difference(CartesianVector3 left, CartesianVector3 right) =>
            new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        private static CartesianVector3 Add(CartesianVector3 left, CartesianVector3 right) =>
            new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        private static CartesianVector3 Multiply(CartesianVector3 vector, double scale) =>
            new(vector.X * scale, vector.Y * scale, vector.Z * scale);
        private static CartesianVector3 Cross(CartesianVector3 left, CartesianVector3 right) =>
            new(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X);

        private readonly record struct NodePosition(Node Reference, int Id, CartesianVector3 Position);
    }
}

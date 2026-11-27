using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlateCutLineReader
    {
        private const int MaxPlateOccurrences = 100_000;
        private const double CoordinateTolerance = 1e-9;

        internal static PlateCutLineResult Read(object output, IEnumerable<Plate> plateList, int loadCaseId,
            CartesianVector3 startPoint, CartesianVector3 endPoint, bool transformForceToGlobal)
        {
            ArgumentNullException.ThrowIfNull(plateList);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(loadCaseId);
            ValidatePoint(startPoint, nameof(startPoint));
            ValidatePoint(endPoint, nameof(endPoint));
            if (AreCoincident(startPoint, endPoint))
                throw new ArgumentException("Cut-line endpoints must be more than 1e-9 coordinate units apart.", nameof(endPoint));

            var plateIds = new List<int>();
            var seen = new HashSet<int>();
            int occurrences = 0;
            foreach (var plate in plateList)
            {
                if (++occurrences > MaxPlateOccurrences)
                    throw new ArgumentException("At most 100,000 plate occurrences are supported.", nameof(plateList));
                if (plate is null) continue;

                int id = plate.Id;
                if (id <= 0)
                    throw new ArgumentException("Every non-null plate must have a positive ID.", nameof(plateList));
                if (seen.Add(id)) plateIds.Add(id);
            }
            if (plateIds.Count == 0)
                throw new ArgumentException("Supply at least one non-null plate.", nameof(plateList));

            object plates = plateIds.ToArray();
            object cases = new[] { loadCaseId };
            object start = new[] { startPoint.X, startPoint.Y, startPoint.Z };
            object end = new[] { endPoint.X, endPoint.Y, endPoint.Z };
            object buffer = new double[,] { { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN } };
            const int defaultSurfaceAxisNode = -1;
            object status = ((dynamic)output).GetResultantForceAlongLineForPlateList(ref plates, plateIds.Count,
                ref cases, ref start, ref end, transformForceToGlobal ? 1 : 0,
                defaultSurfaceAxisNode, defaultSurfaceAxisNode, defaultSurfaceAxisNode, ref buffer);
            Forces forces = NativeValues.ReadBoolean(status, "plate cut-line resultant success") ? ReadForces(buffer) : null;
            return new PlateCutLineResult(plateIds, loadCaseId, startPoint, endPoint, transformForceToGlobal, forces);
        }

        private static Forces ReadForces(object buffer)
        {
            // The native example supplies one row. Also accept an equivalent flat six-Double buffer.
            if (buffer is Array { Rank: 2 } matrix && matrix.GetType().GetElementType() == typeof(double) &&
                matrix.GetLength(0) == 1 && matrix.GetLength(1) == 6)
            {
                var values = new double[6];
                int row = matrix.GetLowerBound(0), column = matrix.GetLowerBound(1);
                for (int i = 0; i < values.Length; i++)
                {
                    double component = (double)matrix.GetValue(row, column + i);
                    if (!double.IsFinite(component))
                        throw new InvalidOperationException("Plate cut-line results contain unwritten or nonfinite components.");
                    values[i] = component;
                }
                return Forces.ParseFromArray(values);
            }

            return SupportResultReader.ReadForces(buffer, "Plate cut-line results");
        }

        private static void ValidatePoint(CartesianVector3 point, string parameter)
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z))
                throw new ArgumentException("Cut-line coordinates must be finite.", parameter);
        }

        private static bool AreCoincident(CartesianVector3 start, CartesianVector3 end)
        {
            double x = Math.Abs(end.X - start.X), y = Math.Abs(end.Y - start.Y), z = Math.Abs(end.Z - start.Z);
            if (Math.Max(x, Math.Max(y, z)) > CoordinateTolerance) return false;
            return Math.Sqrt(x * x + y * y + z * z) <= CoordinateTolerance;
        }
    }
}

using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlatePointStressReader
    {
        internal static PlateStressResult Read(object output, int pId, int lcId, Node stressPoint, Node facingPoint)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lcId);
            ArgumentNullException.ThrowIfNull(stressPoint);
            ArgumentNullException.ThrowIfNull(facingPoint);
            var point = CapturePoint(stressPoint, nameof(stressPoint));
            var facing = CapturePoint(facingPoint, nameof(facingPoint));

            object pointArgument = new[] { point.X, point.Y, point.Z };
            object facingArgument = new[] { facing.X, facing.Y, facing.Z };
            var sentinels = new double[PlateStressResult.ComponentCount];
            Array.Fill(sentinels, double.NaN);
            object buffer = sentinels;
            object status = ((dynamic)output).GetPlateStressAtPoint(pId, lcId, ref pointArgument, ref facingArgument, ref buffer);
            if (!NativeValues.ReadBoolean(status, "plate point stress success"))
                throw new InvalidOperationException("OpenSTAAD could not retrieve plate stress at the supplied point.");

            if (buffer is not Array array || array.Rank != 1 || array.Length != sentinels.Length ||
                array.GetType().GetElementType() != typeof(double))
                throw new InvalidOperationException("Successful point stress requires a one-dimensional array of exactly 32 Doubles.");

            int lowerBound = array.GetLowerBound(0);
            var values = new double[sentinels.Length];
            for (int i = 0; i < values.Length; i++)
            {
                double value = (double)array.GetValue(lowerBound + i);
                if (!double.IsFinite(value))
                    throw new InvalidOperationException($"OpenSTAAD point stress component {i} is unwritten or nonfinite.");
                values[i] = value;
            }

            return new PlateStressResult(pId, lcId, point, facing, values);
        }

        private static CartesianVector3 CapturePoint(Node node, string parameter)
        {
            var point = new CartesianVector3(node.X, node.Y, node.Z);
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z))
                throw new ArgumentException("Point coordinates must be finite.", parameter);
            return point;
        }
    }
}

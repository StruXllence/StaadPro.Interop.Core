using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlateSurfaceReader
    {
        internal static PlateCenterMoments Moments(object output, ILoadCase loadCase, Plate plate)
        {
            CaptureIds(loadCase, plate, out int loadId, out int plateId);
            object buffer = new[] { double.NaN, double.NaN, double.NaN };
            object status = ((dynamic)output).GetAllPlateCenterMoments(plateId, loadId, ref buffer);
            RequireSuccess(status, "plate-center moments");
            if (buffer is not Array array || array.Rank != 1 || array.Length != 3 ||
                array.GetType().GetElementType() != typeof(double))
                throw new InvalidOperationException("Plate-center moments require exactly three Double values.");
            int lower = array.GetLowerBound(0);
            double mx = ReadScalar(array.GetValue(lower), "MX");
            double my = ReadScalar(array.GetValue(lower + 1), "MY");
            double mxy = ReadScalar(array.GetValue(lower + 2), "MXY");
            return new PlateCenterMoments(loadCase, plate, mx, my, mxy);
        }

        internal static PlateCenterVonMises VonMises(object output, ILoadCase loadCase, Plate plate)
        {
            CaptureIds(loadCase, plate, out int loadId, out int plateId);
            object top = double.NaN, bottom = double.NaN;
            object status = ((dynamic)output).GetPlateCenterVonMisesStresses(plateId, loadId, ref top, ref bottom);
            RequireSuccess(status, "plate-center von Mises stresses");
            double qualifiedTop = ReadScalar(top, "top von Mises stress");
            double qualifiedBottom = ReadScalar(bottom, "bottom von Mises stress");
            return new PlateCenterVonMises(loadCase, plate) { Top = qualifiedTop, Bottom = qualifiedBottom };
        }

        internal static PlatePrincipalStresses Principal(object output, ILoadCase loadCase, Plate plate)
        {
            CaptureIds(loadCase, plate, out int loadId, out int plateId);
            object maxTop = double.NaN, minTop = double.NaN, maxBottom = double.NaN, minBottom = double.NaN;
            object status = ((dynamic)output).GetPlateCenterNormalPrincipalStresses(plateId, loadId,
                ref maxTop, ref minTop, ref maxBottom, ref minBottom);
            RequireSuccess(status, "plate-center principal stresses");
            double a = ReadScalar(maxTop, "maximum top principal stress");
            double b = ReadScalar(minTop, "minimum top principal stress");
            double c = ReadScalar(maxBottom, "maximum bottom principal stress");
            double d = ReadScalar(minBottom, "minimum bottom principal stress");
            return new PlatePrincipalStresses(loadCase, plate)
                { SmaxTop = a, SminTop = b, SmaxBottom = c, SminBottom = d };
        }

        private static void CaptureIds(ILoadCase loadCase, Plate plate, out int loadId, out int plateId)
        {
            if (loadCase == null) throw new ArgumentNullException(nameof(loadCase));
            if (plate == null) throw new ArgumentNullException(nameof(plate));
            loadId = loadCase.Id;
            plateId = plate.Id;
            if (loadId <= 0) throw new ArgumentOutOfRangeException(nameof(loadCase), "Case ID must be positive.");
            if (plateId <= 0) throw new ArgumentOutOfRangeException(nameof(plate), "Plate ID must be positive.");
        }

        private static void RequireSuccess(object status, string description)
        {
            if (!NativeValues.ReadBoolean(status, description + " success"))
                throw new InvalidOperationException("OpenSTAAD could not retrieve " + description + ".");
        }

        private static double ReadScalar(object value, string description)
        {
            if (value is not double component || !double.IsFinite(component))
                throw new InvalidOperationException(description + " must be a written finite Double.");
            return component;
        }
    }
}

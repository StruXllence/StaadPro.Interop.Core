using System;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    internal static class RootNamedViewService
    {
        internal static void CreateNamedView(object root, string viewName, int creationFlag)
        {
            ValidateName(viewName);
            ((dynamic)root).CreateNamedView(viewName, creationFlag);
        }

        internal static void SaveNamedView(object root, string viewName)
        {
            ValidateName(viewName);
            ((dynamic)root).SaveNamedView(viewName);
        }

        private static void ValidateName(string viewName)
        {
            if (viewName == null) throw new ArgumentNullException(nameof(viewName));
            if (string.IsNullOrWhiteSpace(viewName))
                throw new ArgumentException("A view name must contain text.", nameof(viewName));
        }
    }
}

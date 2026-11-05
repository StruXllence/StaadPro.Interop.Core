using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace StaadPro.Interop.Core.Services.Wrapper
{
    // Protects native cache/lifetime operations; raw objects and domain calls retain their own context requirements.
    internal sealed class WrapperOperationContext
    {
        private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
        private bool operationInProgress;

        internal WrapperOperationContext(object root)
        {
            if (Marshal.IsComObject(root) && Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                throw new InvalidOperationException("Construct an OpenStaadWrapper for a COM root on its owning STA thread.");
        }

        internal IDisposable Enter()
        {
            if (Environment.CurrentManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Refresh and dispose OpenStaadWrapper on the thread that constructed it.");
            if (operationInProgress)
                throw new InvalidOperationException("OpenStaadWrapper refresh and disposal cannot be reentered during a native operation.");
            operationInProgress = true;
            return new OperationLease(this);
        }

        private sealed class OperationLease : IDisposable
        {
            private WrapperOperationContext context;
            internal OperationLease(WrapperOperationContext context) { this.context = context; }
            public void Dispose()
            {
                if (context == null) return;
                context.operationInProgress = false;
                context = null;
            }
        }
    }
}

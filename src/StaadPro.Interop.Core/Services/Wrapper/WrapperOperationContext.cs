using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace StaadPro.Interop.Core.Services.Wrapper
{
    // Serializes lifecycle work and Root cache observations; raw native objects bypass this guard.
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
                throw new InvalidOperationException("Use managed Root observations, refresh and dispose on the thread that constructed OpenStaadWrapper.");
            if (operationInProgress)
                throw new InvalidOperationException("OpenStaadWrapper refresh, disposal and Root observations cannot be reentered during an operation.");
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

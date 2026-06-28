using System;
using System.Runtime.CompilerServices;

namespace Task233
{
    public readonly struct Task233CancelSource : IDisposable
    {
        internal readonly int Id;
        internal readonly int Version;

        internal Task233CancelSource(int id, int version)
        {
            Id = id;
            Version = version;
        }

        public bool IsCreated => Id > 0;

        public bool IsCancellationRequested => Task233Cancellation.IsCancellationRequested(this);

        public void Cancel()
        {
            Task233Cancellation.Cancel(this);
        }

        public void Dispose()
        {
            Task233Cancellation.Dispose(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }
    }
}

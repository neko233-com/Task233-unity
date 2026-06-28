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

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Id > 0;
        }

        public bool IsCancellationRequested
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Task233Cancellation.IsCancellationRequested(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            Task233Cancellation.Cancel(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

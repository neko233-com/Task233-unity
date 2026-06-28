using System.Runtime.CompilerServices;

namespace Task233
{
    internal static class Task233Cancellation
    {
        private const byte Active = 1;
        private const byte Canceled = 2;
        private const byte Disposed = 4;

        private static int[] versions = new int[64];
        private static byte[] states = new byte[64];
        private static int[] refCounts = new int[64];
        private static int[] free = new int[64];
        private static int freeCount;
        private static int nextId = 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task233CancelSource Create()
        {
            int id;
            if (freeCount > 0)
            {
                id = free[--freeCount];
            }
            else
            {
                id = nextId++;
                EnsureCapacity(id + 1);
            }

            versions[id]++;
            states[id] = Active;
            refCounts[id] = 0;
            return new Task233CancelSource(id, versions[id]);
        }

        public static void Prewarm(int capacity)
        {
            if (capacity < 1)
            {
                return;
            }

            EnsureCapacity(capacity + 1);
            while (nextId <= capacity)
            {
                EnsureFreeCapacity();
                free[freeCount++] = nextId++;
            }
        }

        public static void Reset()
        {
            for (var i = 0; i < states.Length; i++)
            {
                states[i] = 0;
                refCounts[i] = 0;
            }

            freeCount = 0;
            nextId = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCancellationRequested(Task233CancelSource source)
        {
            return HasVersion(source) && (states[source.Id] & Canceled) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cancel(Task233CancelSource source)
        {
            if (HasVersion(source) && (states[source.Id] & (Active | Disposed)) != 0)
            {
                states[source.Id] |= Canceled;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose(Task233CancelSource source)
        {
            if (!IsValid(source))
            {
                return;
            }

            if (refCounts[source.Id] > 0)
            {
                states[source.Id] |= Disposed;
                return;
            }

            Free(source.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Retain(Task233CancelSource source)
        {
            if (!IsValid(source))
            {
                return false;
            }

            refCounts[source.Id]++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Release(Task233CancelSource source)
        {
            if (!HasVersion(source))
            {
                return;
            }

            var id = source.Id;
            if (refCounts[id] <= 0)
            {
                return;
            }

            refCounts[id]--;
            if (refCounts[id] == 0 && (states[id] & Disposed) != 0)
            {
                Free(id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValid(Task233CancelSource source)
        {
            return HasVersion(source) &&
                   (states[source.Id] & Active) != 0 &&
                   (states[source.Id] & Disposed) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasVersion(Task233CancelSource source)
        {
            return source.Id > 0 &&
                   source.Id < states.Length &&
                   versions[source.Id] == source.Version;
        }

        private static void EnsureCapacity(int capacity)
        {
            if (capacity <= states.Length)
            {
                return;
            }

            var next = states.Length;
            while (next < capacity)
            {
                next <<= 1;
            }

            System.Array.Resize(ref states, next);
            System.Array.Resize(ref versions, next);
            System.Array.Resize(ref refCounts, next);
        }

        private static void EnsureFreeCapacity()
        {
            if (freeCount < free.Length)
            {
                return;
            }

            System.Array.Resize(ref free, free.Length * 2);
        }

        private static void Free(int id)
        {
            states[id] = 0;
            refCounts[id] = 0;
            versions[id]++;
            EnsureFreeCapacity();
            free[freeCount++] = id;
        }
    }
}

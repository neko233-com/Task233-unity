namespace Task233
{
    internal static class Task233Cancellation
    {
        private const byte Active = 1;
        private const byte Canceled = 2;

        private static int[] versions = new int[64];
        private static byte[] states = new byte[64];
        private static int[] free = new int[64];
        private static int freeCount;
        private static int nextId = 1;

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
            }

            freeCount = 0;
            nextId = 1;
        }

        public static bool IsCancellationRequested(Task233CancelSource source)
        {
            return IsValid(source) && (states[source.Id] & Canceled) != 0;
        }

        public static void Cancel(Task233CancelSource source)
        {
            if (IsValid(source))
            {
                states[source.Id] = Active | Canceled;
            }
        }

        public static void Dispose(Task233CancelSource source)
        {
            if (!IsValid(source))
            {
                return;
            }

            states[source.Id] = 0;
            versions[source.Id]++;
            EnsureFreeCapacity();
            free[freeCount++] = source.Id;
        }

        private static bool IsValid(Task233CancelSource source)
        {
            return source.Id > 0 &&
                   source.Id < states.Length &&
                   versions[source.Id] == source.Version &&
                   (states[source.Id] & Active) != 0;
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
        }

        private static void EnsureFreeCapacity()
        {
            if (freeCount < free.Length)
            {
                return;
            }

            System.Array.Resize(ref free, free.Length * 2);
        }
    }
}

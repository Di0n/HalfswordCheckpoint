using HSCheckpoint.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HSCheckpoint.Mem.ProcessMemory;

namespace HSCheckpoint.Mem
{
    internal class MemoryWatcher<T> : IUpdatable  where T : unmanaged
    {
        private readonly ProcessMemory procMem;
        private readonly PointerPath pointerPath;

        private T lastValue;
        private bool initialized;

        public string Name { get; }
        public event EventHandler<MemoryChangedEventArgs<T>>? ValueChanged;

        public MemoryWatcher(string name, ProcessMemory procMem, PointerPath pPath)
        {
            this.procMem = procMem;
            this.Name = name;
            this.pointerPath = pPath;
        }

        public void Update()
        {
            if (ValueChanged == null) return;

            IntPtr baseAddress = pointerPath.BaseAddress;
            int[] offsets = pointerPath.Offsets;

            IntPtr address = (offsets.Length == 0) ? baseAddress : procMem.FindDynamicAddress(baseAddress, offsets);

            // If not able to read memory return
            if (address == IntPtr.Zero || procMem.TryRead(address, out T currentValue) != MemoryResult.NO_ERROR)
                return;

            if (!initialized)
            {
                lastValue = currentValue;
                initialized = true;
                return;
            }

            if (!currentValue.Equals(lastValue))
            {
                var eventArgs = new MemoryChangedEventArgs<T>(Name, lastValue, currentValue);
                ValueChanged?.Invoke(this, eventArgs);
                if (eventArgs.ValueModified) initialized = false; // Value has been changed, reset initialization
                else lastValue = currentValue;
            }
        }
    }
}

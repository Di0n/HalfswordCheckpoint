using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            T currentValue = procMem.Read<T>(address);

            if (!initialized)
            {
                lastValue = currentValue;
                initialized = true;
                return;
            }

            if (!currentValue.Equals(lastValue))
            {
                ValueChanged?.Invoke(this, new MemoryChangedEventArgs<T>(Name, lastValue, currentValue));
                lastValue = currentValue;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Events
{
    public class MemoryChangedEventArgs<T> : EventArgs
    {
        public string Name { get; }
        public T OldValue { get; }
        public T NewValue { get; }

        public MemoryChangedEventArgs(string name, T oldVal, T newVal)
        {
            Name = name;
            OldValue = oldVal;
            NewValue = newVal;
        }
    }
}

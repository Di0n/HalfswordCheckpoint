using HSCheckpoint.Mem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint
{
    public abstract class MemoryUser
    {
        protected readonly ProcessMemory procMem;
        protected readonly IntPtr moduleBase;
        public MemoryUser(ProcessMemory procMem, IntPtr moduleBase)
        {
            this.procMem = procMem;
            this.moduleBase = moduleBase;
        }
    }
}

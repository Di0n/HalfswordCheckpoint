using HSCheckpoint.Mem;
using HSCheckpoint.Offsets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HSCheckpoint.Mem.ProcessMemory;

namespace HSCheckpoint.GameObjects
{
    public class GameState
    {
        private readonly ProcessMemory procMem;
        private readonly Offsets.Offsets offsets;
        public GameState(ProcessMemory procMem, nint moduleBase)
        {
            this.procMem = procMem;
            offsets = new(moduleBase);
        }

        public bool IsOverlayMenuOpen
        {
            get
            {
                nint isOpen = procMem.FindDynamicAddress(offsets.IsOverlayMenuOpen);
                return procMem.Read<bool>(isOpen);
            }
        }

        public bool IsInMainMenu
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.IsMainMenuOpen), out bool val);
                if (res != MemoryResult.NO_ERROR) return false;

                return val;
            }
        }

    }
}

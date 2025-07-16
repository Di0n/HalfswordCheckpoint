using HSCheckpoint.Mem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.GameObjects
{
    public class Player
    {
        private readonly ProcessMemory procMem;
        private readonly PlayerOffsets offsets;

        public Player(ProcessMemory procMem, nint moduleBase)
        {
            this.procMem = procMem;
            offsets = new(moduleBase);
        }

        public int Rank
        {
            get
            {
                return procMem.Read<int>(procMem.FindDynamicAddress(offsets.PlayerRank));
            }
            set
            {
                procMem.Write(procMem.FindDynamicAddress(offsets.PlayerRank), value);
            }
        }

        public bool IsInAbyss => procMem.Read<bool>(procMem.FindDynamicAddress(offsets.IsInAbyss));

        public bool IsOverlayMenuOpen
        {
            get
            {
                nint playerRankAddr = procMem.FindDynamicAddress(offsets.PlayerRank);
                nint isOpen = playerRankAddr - 0xb8;
                return procMem.Read<bool>(isOpen);
            }
        }
           
    }
}

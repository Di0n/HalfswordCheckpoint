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
    public class Player
    {
        private readonly ProcessMemory procMem;
        private readonly HalfSwordGameMode_Offsets offsets;

        private int rank = 0;
        private int prestige = 0;
        private int points = 0;
        private bool gauntledModeEnabled = false;

        public Player(ProcessMemory procMem, nint moduleBase)
        {
            this.procMem = procMem;
            offsets = new(moduleBase);
        }

        public int Rank
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.AvailableRank), out int ret);
                if (res == MemoryResult.NO_ERROR)
                    rank = ret;

                return rank;
            }
            set
            {
                procMem.Write(procMem.FindDynamicAddress(offsets.PlayerRank), rank = value);

            }
        }

        public int Points
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.CurrentPoints), out int ret);
                if (res == MemoryResult.NO_ERROR)
                    points = ret;
                return points;
            }
            set
            {
                procMem.Write(procMem.FindDynamicAddress(offsets.CurrentPoints), points = value);
            }
        }

        public bool InFight()
        {
            MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.EnemyCount), out int enemies);
            if (res == MemoryResult.NO_ERROR)
                return enemies > 0;
            return false;
        }

        public bool GauntledModeEnabled
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.GauntledModeEnabled), out bool enabled);
                
                return (res == MemoryResult.NO_ERROR) ? gauntledModeEnabled = enabled : gauntledModeEnabled;
            }
        }

        public bool IsDead
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.PlayerDead), out bool dead);
                return (res == MemoryResult.NO_ERROR) ? dead : false;
            }
        }
        public bool MatchWon
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.MatchWon), out bool ret);
                return (res == MemoryResult.NO_ERROR) ? ret : false;
            }
        }

        public bool LastChance
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.LastChance), out bool ret);
                if (res == MemoryResult.NO_ERROR)
                    return ret;
                return false;
            }
            set
            {
                procMem.Write(procMem.FindDynamicAddress(offsets.CurrentPoints), value);
            }
        }
        [Obsolete]
        public int Prestige
        {
            get
            {
                MemoryResult res = procMem.TryRead(procMem.FindDynamicAddress(offsets.AvailableRank), out int ret);
                if (res == MemoryResult.NO_ERROR)
                    rank = ret;

                return prestige;
            }
            set
            {
                procMem.Write(procMem.FindDynamicAddress(offsets.PlayerRank), prestige = value);
            }
        }
        public bool IsInAbyss()
        {
            IntPtr addr = procMem.FindDynamicAddress(offsets.LastChance);
            if (addr == IntPtr.Zero) return false;

            MemoryResult res = procMem.TryRead<bool>(procMem.FindDynamicAddress(offsets.LastChance), out bool val);
            if (res == MemoryResult.NO_ERROR)
                return val;
            else
                return false;
        }
    }
}

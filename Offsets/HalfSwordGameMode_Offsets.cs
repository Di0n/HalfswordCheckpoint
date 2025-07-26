using HSCheckpoint.Mem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Offsets
{
    public class HalfSwordGameMode_Offsets : Offsets
    {
        public HalfSwordGameMode_Offsets(nint modBase) : base (modBase)
        {
            EnemyCount = new(new(modBase + 0x7DC0AF0), 0x158, 0x390); // int32
            MatchWon = new(new(modBase + 0x7DC0AF0), 0x158, 0x394); // bool
            PlayerDead = new(new(modBase + 0x7DC0AF0), 0x158, 0x395);  // bool
            AllEnemiesDead = new(new(modBase + 0x7DC0AF0), 0x158, 0x3a8); // bool
            PlayerRank = new(new(modBase + 0x7DC0AF0), 0x158, 0x3d0); // int32
            CurrentPoints = new(new(modBase + 0x7DC0AF0), 0x158, 0x3d8); //int32
            AvailableRank = new(new(modBase + 0x7DC0AF0), 0x158, 0x3e0); // int32
            RanksUnlocked = new(new(modBase + 0x7DC0AF0), 0x158, 0x3e4); // int32
            GauntledModeEnabled = new(new(modBase + 0x7DC0AF0), 0x158, 0x3e8); // bool
            BossFightInProgress = new(new(modBase + 0x7DC0AF0), 0x158, 0x3ee); // bool
            LastChance = new(new(modBase + 0x7DC0AF0), 0x158, 0x40a); // bool
        }
        public PointerPath EnemyCount { get; }
        public PointerPath MatchWon { get; }
        public PointerPath PlayerDead { get; }
        public PointerPath AllEnemiesDead { get; }
        public PointerPath PlayerRank { get; }
        public PointerPath CurrentPoints { get; }
        public PointerPath AvailableRank { get; }
        public PointerPath RanksUnlocked { get; }
        public PointerPath GauntledModeEnabled { get; }
        public PointerPath BossFightInProgress { get; }
        public PointerPath LastChance { get; }
    }
}

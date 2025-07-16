using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Mem
{
    // Public related offsets
    public class PlayerOffsets : Offsets
    {
        public PlayerOffsets(IntPtr modBase) : base(modBase)
        {
            IsInAbyss = new(new IntPtr(modBase + 0x7DB0FC0));
            PlayerRank = new(new(modBase + 0x7DC0AF0), 0x158, 0x3d8);
        }

        public PointerPath IsInAbyss { get; }
        public PointerPath PlayerRank { get; }
    }
}

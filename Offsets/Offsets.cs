using HSCheckpoint.Mem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Offsets
{
    // This class contains offsets which do not fall in specific categories
    public class Offsets
    {
        protected readonly nint modBase;
       
        public Offsets(nint modBase)
        {
            this.modBase = modBase;

            IsMainMenuOpen = new(new(modBase + 0x79AD2A0), 0x60, 0x218, 0x340);
            IsOverlayMenuOpen = new(new(modBase + 0x7DC0AF0), 0x158, 0x320); // - b8 from player rank
            IsInAbyss = new(new nint(modBase + 0x7DB0FC0));
        }

        public PointerPath IsMainMenuOpen { get; }
        public PointerPath IsOverlayMenuOpen { get; }
        public PointerPath IsInAbyss { get; }
    }
}

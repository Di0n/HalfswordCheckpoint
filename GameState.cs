using HSCheckpoint.Mem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint
{
    internal class GameState
    {
        private const IntPtr isInAbyssAddress = (IntPtr)0x7DB0FC0; // STATIC POINTER alt: 0x7DB0FC8

        private readonly ProcessMemory processMemory;
        private readonly IntPtr moduleBase;

        public GameState(ProcessMemory processMemory, IntPtr moduleBase)
        {
            this.processMemory = processMemory;
            this.moduleBase = moduleBase;
        }

        public bool IsInAbyss => processMemory.Read<int>(moduleBase + isInAbyssAddress) == 0 ? false : true;

        public bool IsMainMenuOpened
        {
            get => false;
        }

        public bool IsOverlayMenuOpened
        {
            get => false;
        }

        public bool IsFightInProgress
        {
            get => false;
        }
    }
}

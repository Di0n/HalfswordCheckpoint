using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Mem
{
    // This class contains offsets which do not fall in specific categories
    public class Offsets
    {
        protected readonly IntPtr modBase;
       
        public Offsets(IntPtr modBase)
        {
            this.modBase = modBase;
        }
    }
}

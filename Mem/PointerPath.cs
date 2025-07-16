using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Mem
{
    public record PointerPath(IntPtr BaseAddress, params int[] Offsets);
}

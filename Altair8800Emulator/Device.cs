using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altair8800Emulator
{
    public class Device
    {
        Queue<byte> inData = new Queue<byte>();
        public int DeviceNumber { get; set; }
        public byte ReadByte() { return 0; }
        public void WriteByte(byte b) { inData.Enqueue(b); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xiaomi.mimc.packet
{
    class PacketWrapper : MIMCObject
    {
        private string type;

        public PacketWrapper(string type, Object packet) : base(packet)
        {
            this.Type = type;
        }

        public string Type { get => type; set => type = value; }
    }
}

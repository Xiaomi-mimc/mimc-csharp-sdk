using System;
using log4net;
using mimc;
/*

* ==============================================================================
*
* Filename: $safeitemname$
* Description: 
*
* Created: $time$
* Compiler: Visual Studio 2017
*
* Author: zhangming8
* Company: Xiaomi.com
*
* ==============================================================================
*/
namespace com.xiaomi.mimc.packet
{
    public class MIMCUCPacket
    {
        public MIMCUCPacket()
        {
        }
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private String packetId;
        private MIMCUser user;
        private UC_MSG_TYPE type;      
        private byte[] payload;

        public string PacketId { get => packetId; set => packetId = value; }
        public MIMCUser User { get => user; set => user = value; }
        public UC_MSG_TYPE Type { get => type; set => type = value; }
        public byte[] Payload { get => payload; set => payload = value; }

        public MIMCUCPacket(String packetId, MIMCUser user, UC_MSG_TYPE type, byte[] payload)
        {
            this.PacketId = packetId;
            this.User = user;
            this.Type = type;
            this.Payload = payload;
        }


        public override String ToString()
        {
            return "packetId:" + packetId + " type:" + type +  " payload:" + payload;
        }
    }
}

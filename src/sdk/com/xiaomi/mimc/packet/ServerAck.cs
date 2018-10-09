using System;
using log4net;
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
    public class ServerAck
    {
        public ServerAck()
        {
        }
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private String packetId;
        private long sequence;
        private long timestamp;
        private String errorMsg;

        public string PacketId { get => packetId; set => packetId = value; }
        public long Sequence { get => sequence; set => sequence = value; }
        public long Timestamp { get => timestamp; set => timestamp = value; }
        public string ErrorMsg { get => errorMsg; set => errorMsg = value; }

        public ServerAck(String packetId, long sequence, long timestamp, String errorMsg)
        {
            this.PacketId = packetId;
            this.Sequence = sequence;
            this.Timestamp = timestamp;
            this.ErrorMsg = errorMsg;
        }

        public override String ToString()
        {
            return "packetId:" + PacketId + " sequence:" + sequence + " timestamp:" + timestamp + " ErrorMsg:" + ErrorMsg;
        }

    }
}

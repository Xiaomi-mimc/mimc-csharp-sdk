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
    public class P2UMessage
    {
        public P2UMessage()
        {
        }
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private String packetId;
        private long sequence;
        private long timestamp;
        private String fromAccount;
        private String fromResource;
        private long groupId;
        private byte[] payload;

        public string PacketId { get => packetId; set => packetId = value; }
        public long Sequence { get => sequence; set => sequence = value; }
        public long Timestamp { get => timestamp; set => timestamp = value; }
        public string FromAccount { get => fromAccount; set => fromAccount = value; }
        public string FromResource { get => fromResource; set => fromResource = value; }
        public long GroupId { get => groupId; set => groupId = value; }
        public byte[] Payload { get => payload; set => payload = value; }

        public P2UMessage(String packetId, long sequence, String fromAccount, String fromResource, long groupId, byte[] payload, long timestamp)
        {
            this.PacketId = packetId;
            this.Sequence = sequence;
            this.Timestamp = timestamp;
            this.FromAccount = fromAccount;
            this.FromAccount = fromResource;
            this.GroupId = groupId;
            this.Payload = payload;
        }


        public override String ToString()
        {
            return "packetId:" + packetId + " sequence:" + sequence + " timestamp:" + timestamp + " fromAccount:" + fromAccount +
                    " fromResource:" + fromResource + " roupId" + groupId + " payload:" + payload;
        }

        public int CompareTo(Object o)
        {
            return (int)(this.sequence - ((P2UMessage)o).sequence);
        }

    }
}

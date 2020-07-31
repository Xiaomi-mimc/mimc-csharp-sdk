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
    public class P2PMessage
    {
        public P2PMessage()
        {
        }
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private String packetId;
        private long sequence;
        private long timestamp;
        private String fromAccount;
        private String fromResource;
        private String toAccount;
        private String toResource;
        private byte[] payload;
        private string bizType;

        public string PacketId { get => packetId; set => packetId = value; }
        public long Sequence { get => sequence; set => sequence = value; }
        public long Timestamp { get => timestamp; set => timestamp = value; }
        public string FromAccount { get => fromAccount; set => fromAccount = value; }
        public string FromResource { get => fromResource; set => fromResource = value; }
        public byte[] Payload { get => payload; set => payload = value; }
        public string BizType { get => bizType; set => bizType = value; }
        public string ToAccount { get => toAccount; set => toAccount = value; }
        public string ToResource { get => toResource; set => toResource = value; }

        public P2PMessage(String packetId, long sequence, String fromAccount, String fromResource, string toAccount, string toResource, byte[] payload, string bizType, long timestamp)
        {
            this.PacketId = packetId;
            this.Sequence = sequence;
            this.Timestamp = timestamp;
            this.FromAccount = fromAccount;
            this.FromResource = fromResource;
            this.ToAccount = toAccount;
            this.ToResource = toResource;
            this.Payload = payload;
            this.BizType = bizType;
        }

      
        public override String ToString()
        {
            return "packetId:" + packetId + " sequence:" + sequence + " timestamp:" + timestamp + " fromAccount:" + fromAccount +
                    " fromResource:" + fromResource + " toAccount:" + toAccount +
                    " toResource:" + toResource + " payload:" + payload + " bizType:" + bizType;
        }

        public int CompareTo(Object o)
        {
            return (int)(this.sequence - ((P2PMessage)o).sequence);
        }
    }
}

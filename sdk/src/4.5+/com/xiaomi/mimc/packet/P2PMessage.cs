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
        private byte[] payload;

        public string PacketId { get;}
        public long Sequence { get; }
        public long Timestamp { get;}
        public string FromAccount { get; }
        public string FromResource { get; }
        public byte[] Payload { get;}

        public P2PMessage(String packetId, long sequence, String fromAccount, String fromResource, byte[] payload, long timestamp)
        {
            this.PacketId = packetId;
            this.Sequence = sequence;
            this.Timestamp = timestamp;
            this.FromAccount = fromAccount;
            this.FromResource = fromResource;
            this.Payload = payload;
        }

      
        public override String ToString()
        {
            return "packetId:" + packetId + " sequence:" + sequence + " timestamp:" + timestamp + " fromAccount:" + fromAccount +
                    " fromResource:" + fromResource + " payload:" + payload;
        }

        public int CompareTo(Object o)
        {
            return (int)(this.sequence - ((P2PMessage)o).sequence);
        }
    }
}

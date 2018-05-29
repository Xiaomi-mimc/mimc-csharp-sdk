using System;

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
    public class P2TMessage
    {
        public P2TMessage()
        {
        }
        

        private String packetId;
        private long sequence;
        private long timestamp;
        private String fromAccount;
        private String fromResource;
        private long groupId;
        private byte[] payload;

        public string PacketId { get; }
        public long Sequence { get; }
        public long Timestamp { get; }
        public string FromResource { get; }
        public string FromAccount { get; }
        public long GroupId { get; }
        public byte[] Payload { get; }

        public P2TMessage(String packetId, long sequence, String fromAccount, String fromResource, long groupId, byte[] payload, long timestamp)
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
            return (int)(this.sequence - ((P2TMessage)o).sequence);
        }

    }
}

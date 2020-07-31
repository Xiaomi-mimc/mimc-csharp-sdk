using System;
using System.Collections.Generic;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc.handler
{
    public class MessageEventArgs : EventArgs
    {
        private List<P2PMessage> packets;

        public MessageEventArgs(List<P2PMessage> packets)
        {
            this.Packets = packets;
        }

        public List<P2PMessage> Packets { get => packets; set => packets = value; }
    }
}

using System;
using System.Collections.Generic;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc.handler
{
    public class MessageEventArgs : EventArgs
    {
        private readonly MIMCUser user;

        private readonly List<P2PMessage> packets;

        public MessageEventArgs(MIMCUser user, List<P2PMessage> packets)
        {
            this.user = user;
            this.packets = packets;

        }

        public List<P2PMessage> Packets => packets;

        public MIMCUser User => user;
    }
}

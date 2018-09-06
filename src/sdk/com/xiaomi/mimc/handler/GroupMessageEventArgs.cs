using System;
using System.Collections.Generic;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc
{
    public class GroupMessageEventArgs : EventArgs
    {
        private readonly MIMCUser user;

        private List<P2TMessage> packets;

        public GroupMessageEventArgs(MIMCUser user,List<P2TMessage> packets)
        {
            this.user = user;
            this.packets = packets;
        }

        public List<P2TMessage> Packets { get => packets; set => packets = value; }

        public MIMCUser User => user;
    }
}
using System;
using System.Collections.Generic;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc
{
    public class GroupMessageEventArgs : EventArgs
    {
        private List<P2TMessage> packets;

        public GroupMessageEventArgs(List<P2TMessage> packets)
        {
            this.packets = packets;
        }

        public List<P2TMessage> Packets { get => packets; set => packets = value; }
    }
}
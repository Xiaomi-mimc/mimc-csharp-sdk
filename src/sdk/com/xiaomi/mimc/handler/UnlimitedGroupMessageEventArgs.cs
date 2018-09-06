using System;
using mimc;

namespace com.xiaomi.mimc
{
    public class UnlimitedGroupMessageEventArgs : EventArgs
    {
        private readonly MIMCUser user;
    
        private UCPacket packet;

        public UnlimitedGroupMessageEventArgs(MIMCUser user, UCPacket packet)
        {
            this.user = user;
            this.packet = packet;
        }

        public UCPacket Packet { get => packet; set => packet = value; }

        public MIMCUser User => user;
    }
}
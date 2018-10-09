using System;
using com.xiaomi.mimc.packet;
using mimc;

namespace com.xiaomi.mimc
{
    public class SendUnlimitedGroupMessageTimeoutEventArgs : EventArgs
    {
        private readonly MIMCUser user;

        private P2UMessage packet;

        public SendUnlimitedGroupMessageTimeoutEventArgs(MIMCUser user, P2UMessage packet)
        {
            this.user = user;
            this.packet = packet;
        }

        public P2UMessage Packet { get => packet; set => packet = value; }

        public MIMCUser User => user;
    }
}
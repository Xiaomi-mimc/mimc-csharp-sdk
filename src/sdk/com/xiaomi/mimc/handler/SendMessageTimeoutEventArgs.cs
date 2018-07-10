using System;

using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc.handler
{
    public class SendMessageTimeoutEventArgs : EventArgs
    {
        private readonly MIMCUser user;

        private readonly P2PMessage p2PMessage;

        public SendMessageTimeoutEventArgs(MIMCUser user, P2PMessage p2PMessage)
        {
            this.user = user;
            this.p2PMessage = p2PMessage;

        }

        public P2PMessage P2PMessage => p2PMessage;

        public MIMCUser User => user;
    }
}

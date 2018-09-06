using System;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc
{
    public class SendGroupMessageTimeoutEventArgs : EventArgs
    {
        private readonly MIMCUser user;

        private P2TMessage p2tMessage;

        public SendGroupMessageTimeoutEventArgs(MIMCUser user,P2TMessage p2tMessage)
        {
            this.user = user;
            this.p2tMessage = p2tMessage;
        }

        public P2TMessage P2tMessage { get => p2tMessage; set => p2tMessage = value; }

        public MIMCUser User => user;
    }
}
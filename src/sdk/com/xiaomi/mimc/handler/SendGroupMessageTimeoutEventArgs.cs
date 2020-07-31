using System;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc
{
    public class SendGroupMessageTimeoutEventArgs : EventArgs
    {
        private P2TMessage p2tMessage;

        public SendGroupMessageTimeoutEventArgs(P2TMessage p2tMessage)
        {
            this.P2tMessage = p2tMessage;
        }

        public P2TMessage P2tMessage { get => p2tMessage; set => p2tMessage = value; }
    }
}
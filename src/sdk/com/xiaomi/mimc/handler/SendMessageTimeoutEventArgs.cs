using System;

using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc.handler
{
    public class SendMessageTimeoutEventArgs : EventArgs
    {
        private P2PMessage p2PMessage;

        public SendMessageTimeoutEventArgs(P2PMessage p2PMessage)
        {
            this.P2PMessage = p2PMessage;

        }

        public P2PMessage P2PMessage { get => p2PMessage; set => p2PMessage = value; }
    }
}

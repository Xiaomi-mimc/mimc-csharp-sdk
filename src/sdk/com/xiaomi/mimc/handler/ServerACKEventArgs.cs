using System;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc
{
    public class ServerACKEventArgs : EventArgs
    {
        private ServerAck serverAck;

        public ServerACKEventArgs(ServerAck serverAck)
        {
            this.ServerAck = serverAck;
        }

        public ServerAck ServerAck { get => serverAck; set => serverAck = value; }
    }
}
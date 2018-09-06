using System;
using com.xiaomi.mimc.packet;

namespace com.xiaomi.mimc
{
    public class ServerACKEventArgs : EventArgs
    {
        private readonly MIMCUser user;
        private readonly ServerAck serverAck;

        public ServerACKEventArgs(MIMCUser user,ServerAck serverAck)
        {
            this.user = user;
            this.serverAck = serverAck;
        }

        public MIMCUser User => user;

        public ServerAck ServerAck => serverAck;
    }
}
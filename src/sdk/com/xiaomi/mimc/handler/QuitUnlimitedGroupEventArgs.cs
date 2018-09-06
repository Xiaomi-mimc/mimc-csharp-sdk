using System;
using mimc;

namespace com.xiaomi.mimc
{
    public class QuitUnlimitedGroupEventArgs : EventArgs
    {
        private readonly MIMCUser user;

        private UCQuitResp packet;

        public QuitUnlimitedGroupEventArgs(MIMCUser user, UCQuitResp packet)
        {
            this.user = user;
            this.packet = packet;
        }

        public UCQuitResp Packet { get => packet; set => packet = value; }

        public MIMCUser User => user;
    }
}
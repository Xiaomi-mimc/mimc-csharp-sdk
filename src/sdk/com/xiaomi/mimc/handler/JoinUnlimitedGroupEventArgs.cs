using mimc;

namespace com.xiaomi.mimc
{
    public class JoinUnlimitedGroupEventArgs
    {
        private readonly MIMCUser user;

        private UCJoinResp packet;

        public JoinUnlimitedGroupEventArgs(MIMCUser user, UCJoinResp packet)
        {
            this.user = user;
            this.packet = packet;
        }

        public UCJoinResp Packet { get => packet; set => packet = value; }

        public MIMCUser User => user;
    }
}
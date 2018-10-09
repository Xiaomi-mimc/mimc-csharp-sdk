using System;
using System.Collections.Generic;
using com.xiaomi.mimc.packet;
using mimc;

namespace com.xiaomi.mimc
{
    public class UnlimitedGroupMessageEventArgs : EventArgs
    {
        private readonly MIMCUser user;
        private List<P2UMessage> p2uMessagesList;

        public UnlimitedGroupMessageEventArgs(MIMCUser user, List<P2UMessage> p2uMessagesList)
        {
            this.user = user ?? throw new ArgumentNullException(nameof(user));
            this.p2uMessagesList = p2uMessagesList ?? throw new ArgumentNullException(nameof(p2uMessagesList));
        }

        public MIMCUser User => user;

        public List<P2UMessage> P2uMessagesList { get => p2uMessagesList; set => p2uMessagesList = value; }
    }
}
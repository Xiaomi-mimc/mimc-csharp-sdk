using System;
using System.Collections.Generic;
using com.xiaomi.mimc.packet;
using mimc;

namespace com.xiaomi.mimc
{
    public class UnlimitedGroupMessageEventArgs : EventArgs
    {
        private List<P2UMessage> p2uMessagesList;

        public UnlimitedGroupMessageEventArgs(List<P2UMessage> p2uMessagesList)
        {
            this.P2uMessagesList = p2uMessagesList;
        }

        public List<P2UMessage> P2uMessagesList { get => p2uMessagesList; set => p2uMessagesList = value; }
    }
}
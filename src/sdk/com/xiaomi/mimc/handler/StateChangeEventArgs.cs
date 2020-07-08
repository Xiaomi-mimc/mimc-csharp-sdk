using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xiaomi.mimc.handler
{
    public class StateChangeEventArgs : EventArgs
    {
        private readonly MIMCUser user;
        private readonly bool isOnline;
        private readonly string type;
        private readonly string reason;
        private readonly string desc;

        public StateChangeEventArgs(MIMCUser user, bool isOnline, string type, string reason, string desc)
        {
            this.user = user;
            this.isOnline = isOnline;
            this.type = type;
            this.reason = reason;
            this.desc = desc;
        }

        public bool IsOnline => isOnline;

        public string Type => type;

        public string Reason => reason;

        public string Desc => desc;

        public MIMCUser User => user;
    }
}
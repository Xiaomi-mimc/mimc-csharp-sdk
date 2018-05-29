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
        private readonly string errType;
        private readonly string errReason;
        private readonly string errDescription;

        public StateChangeEventArgs(MIMCUser user,bool isOnline, string errType, string errReason, string errDescription)
        {
            this.user = user;
            this.isOnline = isOnline;
            this.errType = errType;
            this.errReason = errReason;
            this.errDescription = errDescription;
        }

        public bool IsOnline => isOnline;

        public string ErrType => errType;

        public string ErrReason => errReason;

        public string ErrDescription => errDescription;

        public MIMCUser User => user;
    }
}

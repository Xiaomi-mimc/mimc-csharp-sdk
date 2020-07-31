using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xiaomi.mimc.handler
{
    public class StateChangeEventArgs : EventArgs
    {
        private bool isOnline;
        private string type;
        private string reason;
        private string desc;

        public StateChangeEventArgs(bool isOnline, string type, string reason, string desc)
        {
            this.IsOnline = isOnline;
            this.Type = type;
            this.Reason = reason;
            this.Desc = desc;
        }

        public bool IsOnline { get => isOnline; set => isOnline = value; }
        public string Type { get => type; set => type = value; }
        public string Reason { get => reason; set => reason = value; }
        public string Desc { get => desc; set => desc = value; }
    }
}
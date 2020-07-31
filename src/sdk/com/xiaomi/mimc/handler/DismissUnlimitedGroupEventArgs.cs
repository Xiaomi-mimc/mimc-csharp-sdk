using System;
using mimc;

namespace com.xiaomi.mimc
{
    public class DismissUnlimitedGroupEventArgs : EventArgs
    {
        private ulong topicId;


        public DismissUnlimitedGroupEventArgs() { }

        public DismissUnlimitedGroupEventArgs(ulong topicId)
        {
            this.TopicId = topicId;
        }

        public ulong TopicId { get => topicId; set => topicId = value; }
    }
}
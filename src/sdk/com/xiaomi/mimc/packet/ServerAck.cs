﻿using System;
using log4net;
/*

* ==============================================================================
*
* Filename: $safeitemname$
* Description: 
*
* Created: $time$
* Compiler: Visual Studio 2017
*
* Author: zhangming8
* Company: Xiaomi.com
*
* ==============================================================================
*/
namespace com.xiaomi.mimc.packet
{
    public class ServerAck
    {
        public ServerAck()
        {
        }
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private String packetId;
        private long sequence;
        private long timestamp;
        private int code;
        private String desc;

        public string PacketId { get => packetId; set => packetId = value; }
        public long Sequence { get => sequence; set => sequence = value; }
        public long Timestamp { get => timestamp; set => timestamp = value; }
        public string Desc { get => desc; set => desc = value; }
        public int Code { get => code; set => code = value; }

        public ServerAck(String packetId, long sequence, long timestamp, int code, String desc)
        {
            this.PacketId = packetId;
            this.Sequence = sequence;
            this.Timestamp = timestamp;
            this.Code = code;
            this.desc = desc;
        }

        public override String ToString()
        {
            return "packetId:" + PacketId + " sequence:" + sequence + " timestamp:" + timestamp + " code:" + code + "desc:" + desc;
        }

    }
}

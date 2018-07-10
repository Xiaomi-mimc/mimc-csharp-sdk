using System;
using System.IO;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.utils;
using log4net;
using ProtoBuf;
using sdk.protobuf;
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
*
* V6Packet struct:
*
* | V6Header    |
* | Packet      |
* 
* V6Header struct:
*
* | magic       |
* | version     |
* | packetLen   |
* 
* Packet struct:
*
* | payloadType |
* | headerLen   |
* | payloadLen  |
* | ClientHeader|
* | payload     |
* 
* ClientHeader struct:
*
* | chid        |
* | uuid        |
* | server      |
* | resource    |
* | cmd         |
* | subcmd      |
* | id          |
* | dir_flag    |
* | cipher      |
* | error_code  |
* | error_str   |
*/
namespace com.xiaomi.mimc.packet

{
    [Serializable]
    public class V6Packet
    {
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public V6Packet()
        {
        }

        private char magic;
        private char version;
        private int packetLen;
        private byte[] v6BodyBin;
        private V6Body body;

        public char Magic { get; set; }
        public char Version { get; set; }
        public int PacketLen { get; set; }
        public byte[] V6BodyBin { get; set; }
        public V6Body Body { get; set; }

        [Serializable]
        public class V6Body
        {
            private short payloadType;
            private int clientHeaderLen;
            private int payloadLen;
            private ClientHeader clientHeader;
            private byte[] payload;

            public short PayloadType { get; set; }
            public int ClientHeaderLen { get; set; }
            public int PayloadLen { get; set; }
            public ClientHeader ClientHeader { get; set; }
            public byte[] Payload { get; set; }
        }


        public byte[] ToByteArray(byte[] v6bodyKey, byte[] payloadKey,MIMCUser user)
        {
            //Ping packet
            if (v6BodyBin == null && Body == null)
            {
                ByteBuffer v6PingByteBuffer = ByteBuffer.Allocate(Constant.V6_HEAD_LENGTH + Constant.CRC_LEN);
                v6PingByteBuffer.putChar(Constant.MAGIC);
                v6PingByteBuffer.putChar(Constant.V6_VERSION);
                v6PingByteBuffer.putInt(0);
                uint pingcrc = Adler32.checkCRC(v6PingByteBuffer.ToArray(), 0, Constant.V6_HEAD_LENGTH);
                v6PingByteBuffer.putUint(pingcrc);
                logger.InfoFormat("---> send v6 packet Ping appAccount:{0}", user.AppAccount);
                return v6PingByteBuffer.ToArray();
            }
            ByteBuffer bodyBuffer = null;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, Body.ClientHeader);
                byte[] clientHeaderBins = ms.ToArray();
                short clientHeaderLen = (short)clientHeaderBins.Length;
                ms.Dispose();
          

            int payloadLen = (Body.Payload == null || Body.Payload.Length == 0) ? 0 : Body.Payload.Length;            
             bodyBuffer = ByteBuffer.Allocate(Constant.V6_BODY_HEADER_LENGTH + clientHeaderLen + payloadLen);
            bodyBuffer.putShort(Constant.PAYLOAD_TYPE);
            bodyBuffer.putShort(clientHeaderLen);
            bodyBuffer.putInt(payloadLen);
            bodyBuffer.putBytes(clientHeaderBins);
            }
            if (Body.Payload != null)
            {
               bodyBuffer.putBytes(payloadKey != null?RC4Cryption.DoEncrypt(payloadKey, Body.Payload): Body.Payload);
            }

            v6BodyBin = bodyBuffer.ToArray();
            int v6BodyLen = (v6BodyBin == null || v6BodyBin.Length == 0) ? 0 : v6BodyBin.Length;

            if (!Constant.CMD_CONN.ToUpper().Equals(Body.ClientHeader.cmd.ToUpper()))
            {
                v6BodyBin = RC4Cryption.DoEncrypt(v6bodyKey, v6BodyBin);
            }

            ByteBuffer v6ByteBuffer = ByteBuffer.Allocate(Constant.V6_HEAD_LENGTH + v6BodyLen + Constant.CRC_LEN);
            v6ByteBuffer.putChar(Constant.MAGIC);
            v6ByteBuffer.putChar(Constant.V6_VERSION);
            v6ByteBuffer.putInt(v6BodyLen);
            v6ByteBuffer.putBytes(v6BodyBin);
            uint crc = Adler32.checkCRC(v6ByteBuffer.ToArray(), 0, Constant.V6_HEAD_LENGTH + v6BodyLen);
            v6ByteBuffer.putUint(crc);
            logger.InfoFormat("---> send v6 packet cmd:{0},appAccount:{1}", Body.ClientHeader.cmd, user.AppAccount);
            return v6ByteBuffer.ToArray();
        }
    }
}
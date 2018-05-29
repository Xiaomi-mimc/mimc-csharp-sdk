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
*/
namespace com.xiaomi.mimc.packet
{
    public class V6PacketDecoder
    {
        public V6PacketDecoder()
        {
        }
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static V6Packet DecodeV6(byte[] v6HeaderBins, byte[] v6BodyBins, byte[] crcBins, byte[] bodyKey,MIMCUser user)
        {
        
            byte[] v6Bins = new byte[v6HeaderBins.Length + v6BodyBins.Length + crcBins.Length];
            //logger.DebugFormat("decodeV6 ============== v6BodyBins.Length:{0}", v6BodyBins.Length);

            v6HeaderBins.CopyTo(v6Bins, 0);
            v6BodyBins.CopyTo(v6Bins, v6HeaderBins.Length);
            crcBins.CopyTo(v6Bins, v6HeaderBins.Length + v6BodyBins.Length);
            //logger.DebugFormat("decodeV6 ============== data v6HeaderBins :{0}", BitConverter.ToString(v6HeaderBins));
            //logger.DebugFormat("decodeV6 ============== data v6BodyBins:{0}", BitConverter.ToString(v6BodyBins));
            //logger.DebugFormat("decodeV6 ============== data crcBins:{0}", BitConverter.ToString(crcBins));
            //logger.DebugFormat("decodeV6 ============== data v6Bins:{0}", BitConverter.ToString(v6Bins));

            uint fecrc = GetUint(crcBins, 0);
            uint crc = Adler32.checkCRC(v6Bins, 0, v6Bins.Length - 4);
            if (fecrc != crc)
            {
                logger.WarnFormat("decodeV6, INVALID_CRC, {0}!={1}", fecrc, crc);
                return null;
            }
            V6Packet v6Packet = new V6Packet();
            v6Packet.Magic = V6PacketDecoder.GetChar(v6HeaderBins, 0);
            v6Packet.Version = V6PacketDecoder.GetChar(v6HeaderBins, 2);
            v6Packet.PacketLen =(int) V6PacketDecoder.GetUint(v6HeaderBins, 4);

            if (v6Packet.PacketLen == 0)
            {
                return v6Packet;
            }

            if (bodyKey != null && bodyKey.Length > 0 && v6BodyBins != null && v6BodyBins.Length > 0)
            {
                v6BodyBins = RC4Cryption.DoEncrypt(bodyKey, v6BodyBins);
            }
            v6Packet.PacketLen = v6BodyBins == null ? 0 : v6BodyBins.Length;

            short payloadType = GetShort(v6BodyBins, 0);            
            short clientHeaderLen = GetShort(v6BodyBins, 2);
            uint v6PayloadLen = GetUint(v6BodyBins, 4);
            if (payloadType != Constant.PAYLOAD_TYPE || clientHeaderLen < 0 || v6PayloadLen < 0)
            {
                logger.WarnFormat("decodeV6, INVALID_HEADER, payloadType:{0}, clientHeaderLen:{1}, v6PayloadLen:{2}",
                    payloadType, clientHeaderLen, v6PayloadLen);
                return null;
            }

            byte[] clientHeaderBins = new byte[clientHeaderLen];
            Array.Copy(v6BodyBins, Constant.V6_BODY_HEADER_LENGTH, clientHeaderBins, 0, clientHeaderLen);

            byte[] v6PayloadBins = new byte[v6PayloadLen];
            Array.Copy(v6BodyBins, Constant.V6_BODY_HEADER_LENGTH + clientHeaderLen, v6PayloadBins, 0, v6PayloadLen);

            ClientHeader clientHeader = null;
            using (MemoryStream ms = new MemoryStream(clientHeaderBins))
            {
                clientHeader = Serializer.Deserialize<ClientHeader>(ms);
                ms.Dispose();
            }
                
            //logger.InfoFormat("receive v6 packet cmd:{0}, user:{1}", clientHeader.cmd, user.AppAccount());
            if (Constant.CMD_SECMSG == clientHeader.cmd)
            {
                byte[] payloadKey = RC4Cryption.GenerateKeyForRC4(user.SecurityKey, clientHeader.id);
                v6PayloadBins = RC4Cryption.DoEncrypt(payloadKey, v6PayloadBins);
            }
            v6Packet.V6BodyBin = v6BodyBins;

            V6Packet.V6Body v6Body = new V6Packet.V6Body();
            v6Body.PayloadType = payloadType;
            v6Body.ClientHeaderLen = clientHeaderLen;
            v6Body.PayloadLen = (int)v6PayloadLen;
            v6Body.ClientHeader = clientHeader;
            v6Body.Payload = v6PayloadBins;

            v6Packet.Body = v6Body;

            return v6Packet;
        }

        public static short GetShort(byte[] buf, int index)
        {
            int firstByte = 0;
            int secondByte = 0;

            firstByte = (0x000000FF & ((int)buf[index]));
            secondByte = (0x000000FF & ((int)buf[index + 1]));

            return (short)((firstByte << 8 | secondByte) & 0xFFFFFFFF);
        }

        public static char GetChar(byte[] buf, int index)
        {
            int firstByte = 0;
            int secondByte = 0;

            firstByte = (0x000000FF & ((int)buf[index]));
            secondByte = (0x000000FF & ((int)buf[index + 1]));

            return (char)((firstByte << 8 | secondByte) & 0xFFFFFFFF);
        }
        public static int GetInt(byte[] buf, int index)
        {
            int firstByte = 0;
            int secondByte = 0;
            int thirdByte = 0;
            int fourthByte = 0;

            firstByte = (0x000000FF & ((int)buf[index]));
            secondByte = (0x000000FF & ((int)buf[index + 1]));
            thirdByte = (0x000000FF & ((int)buf[index + 2]));
            fourthByte = (0x000000FF & ((int)buf[index + 3]));

            return (int)((firstByte << 24 | secondByte << 16 | thirdByte << 8 | fourthByte) & 0xFFFFFFFF);
        }
        public static uint GetUint(byte[] buf, int index)
        {
            int firstByte = 0;
            int secondByte = 0;
            int thirdByte = 0;
            int fourthByte = 0;

            firstByte = (0x000000FF & ((int)buf[index]));
            secondByte = (0x000000FF & ((int)buf[index + 1]));
            thirdByte = (0x000000FF & ((int)buf[index + 2]));
            fourthByte = (0x000000FF & ((int)buf[index + 3]));

            return (uint)((firstByte << 24 | secondByte << 16 | thirdByte << 8 | fourthByte) & 0xFFFFFFFF);
        }
    }
}

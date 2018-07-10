using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.packet;
using log4net;
using mimc;
using ProtoBuf;
using sdk.protobuf;
using static com.xiaomi.mimc.packet.V6Packet;
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
namespace com.xiaomi.mimc.utils
{
    public class MIMCUtil
    {
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal static string GenerateSig(ClientHeader header, XMMsgBind bind, string challenge, string securityKey)
        {
            Hashtable paramsMap = new Hashtable();
            paramsMap.Add("challenge", challenge);
            paramsMap.Add("token", bind.token);
            paramsMap.Add("chid", header.chid);
            paramsMap.Add("from", String.Format("{0}@xiaomi.com/{1}", header.uuid, header.resource));
            paramsMap.Add("id", header.id);
            paramsMap.Add("to", Constant.SERVER);
            paramsMap.Add("kick", bind.kick);
            paramsMap.Add("client_attrs", bind.client_attrs == null ? "" : bind.client_attrs);
            paramsMap.Add("cloud_attrs", bind.cloud_attrs == null ? "" : bind.cloud_attrs);

            ArrayList exps = new ArrayList();
            exps.Add(bind.method.ToUpper());
            ArrayList lst = new ArrayList(paramsMap.Keys);
            lst.Sort();
            foreach (string key in lst)
            {
                exps.Add(String.Format("{0}={1}", key, paramsMap[key]));

            }

            exps.Add(securityKey);
            //logger.DebugFormat("GenerateSig =======securityKey:{0}", securityKey);

            bool first = true;
            StringBuilder sb = new StringBuilder();
            foreach (String s in exps)
            {
                if (!first)
                {
                    sb.Append('&');
                }
                sb.Append(s);
                first = false;
            }
            //logger.DebugFormat(sb.ToString());
            return Hash4SHA1(sb.ToString());
        }

        public static string Hash4SHA1(string str_sha1_in)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] bytes_sha1_in = UTF8Encoding.Default.GetBytes(str_sha1_in);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            string str_sha1_out = Convert.ToBase64String(bytes_sha1_out);

            return str_sha1_out;
        }

        public static long CurrentTimeMillis()
        {
            DateTime ZERO_TIME = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (DateTime.Now.Ticks - ZERO_TIME.Ticks) / 10000;
        }
        ///<summary>
        /// 取得当前源码的哪一行
        /// </summary>
        /// <returns></returns>
        public static int GetLineNum()
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1, true);
            return st.GetFrame(0).GetFileLineNumber();
        }

        /// <summary>
        /// 取当前源码的源文件名
        /// </summary>
        /// <returns></returns>
        public static string GetCurSourceFileName()
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1, true);

            return st.GetFrame(0).GetFileName();
        }
        /// <summary>
        ///生成随机字符串 
        ///</summary>
        ///<param name="length">目标字符串的长度</param>
        ///<param name="useNum">是否包含数字，1=包含，默认为包含</param>
        ///<param name="useLow">是否包含小写字母，1=包含，默认为包含</param>
        ///<param name="useUpp">是否包含大写字母，1=包含，默认为包含</param>
        ///<param name="useSpe">是否包含特殊字符，1=包含，默认为不包含</param>
        ///<param name="custom">要包含的自定义字符，直接输入要包含的字符列表</param>
        ///<returns>指定长度的随机字符串</returns>
        public static string GetRandomString(int length, bool useNum, bool useLow, bool useUpp, bool useSpe, string custom)
        {
            byte[] b = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(b);
            Random r = new Random(BitConverter.ToInt32(b, 0));
            string s = null, str = custom;
            if (useNum == true) { str += "0123456789"; }
            if (useLow == true) { str += "abcdefghijklmnopqrstuvwxyz"; }
            if (useUpp == true) { str += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; }
            if (useSpe == true) { str += "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"; }
            for (int i = 0; i < length; i++)
            {
                s += str.Substring(r.Next(0, str.Length - 1), 1);
            }
            return s;
        }

        /// <summary>
        /// 将对象序列化后保存到文件中
        /// </summary>
        public static void SerializeToFile<T>(T obj, string dataFile)
        {
            FileStream fs = null;
            if (!File.Exists(dataFile))
            {
                logger.DebugFormat("SerializeToFile {0} not exists! CreateNewFile", dataFile);
                fs = new FileStream(dataFile, FileMode.CreateNew);
            }
            else
            {
                logger.DebugFormat("SerializeToFile {0} already exists! OpenOrCreate overite it.", dataFile);
                fs = new FileStream(dataFile, FileMode.OpenOrCreate);
            }
            BinaryFormatter binSerializer = new BinaryFormatter();
            binSerializer.Serialize(fs, obj.ToString());
            fs.Close();
        }

        /// <summary>
        /// 从文件中读取数据并反序列化成对象
        /// </summary>
        public static T Deserialize<T>(string dataFile)
        {
            T obj = default(T);
            if (!File.Exists(dataFile))
            {
                logger.DebugFormat("Deserialize {0} not exists!", dataFile);
                return obj;
            }

            using (FileStream fileStream = File.OpenRead(dataFile))
            {
                BinaryFormatter binDeserializer = new BinaryFormatter();
                obj = (T)binDeserializer.Deserialize(fileStream);
                fileStream.Close();
            }
            return obj;
        }


        public static ClientHeader CreateClientHeader(MIMCUser user, String cmd, int cipher, String msgId)
        {
            logger.DebugFormat("{0} CreateClientHeader cmd:{1},msgId:{2}", user.AppAccount, cmd, msgId);
            ClientHeader clientHeader = new ClientHeader();
            clientHeader.id = msgId;
            clientHeader.uuid = user.Uuid;
            clientHeader.chid = Constant.MIMC_CHID;
            clientHeader.resource = user.Resource;
            clientHeader.cmd = cmd;
            clientHeader.server = Constant.SERVER;
            clientHeader.cipher = cipher;
            clientHeader.dir_flag = ClientHeader.MSG_DIR_FLAG.CS_REQ;
            return clientHeader;
        }

        public static string CreateMsgId(MIMCUser user)
        {
            return MIMCUtil.GetRandomString(15, true, true, false, false, null) + "-" + user.Atomic.IncrementAndGet();
        }

        public static V6Packet BuildConnectionPacket(MIMCUser user)
        {
            logger.DebugFormat("{0} User BuildConnectionPacket", user.AppAccount);
            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();

            XMMsgConn xMMsgConn = new XMMsgConn();
            xMMsgConn.os = "macOS";
            xMMsgConn.udid = user.Connection.Udid;
            xMMsgConn.version = Constant.CONN_BIN_PROTO_VERSION;

            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_CONN, Constant.CIPHER_NONE, MIMCUtil.CreateMsgId(user));

            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, xMMsgConn);
                byte[] payload = ms.ToArray();
                v6Body.Payload = payload;
                v6Packet.Body = v6Body;
                ms.Dispose();
            }

            return v6Packet;
        }
        public static V6Packet BuildSecMsgPacket(MIMCUser user, string packetId, byte[] packetBin)
        {

            logger.DebugFormat("{0} User BuildSecMsgPacket", user.AppAccount);

            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_SECMSG, Constant.CIPHER_RC4, packetId);

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;

            v6Body.Payload = packetBin;
            v6Packet.Body = v6Body;

            return v6Packet;
        }

        public static V6Packet BuildSequenceAckPacket(MIMCUser user, MIMCPacketList packetList)
        {
            logger.DebugFormat("{0} User BuildSequenceAckPacket", user.AppAccount);

            MIMCPacket packet = new MIMCPacket();
            packet.packetId = MIMCUtil.CreateMsgId(user);
            packet.package = user.AppPackage;
            packet.type = MIMC_MSG_TYPE.SEQUENCE_ACK;

            MIMCSequenceAck sequenceAck = new MIMCSequenceAck();
            sequenceAck.uuid = packetList.uuid;
            sequenceAck.resource = packetList.resource;
            sequenceAck.sequence = packetList.maxSequence;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, sequenceAck);
                byte[] sequenceAckBin = ms.ToArray();
                packet.payload = sequenceAckBin;
                ms.Dispose();
            }
            byte[] mimcBins = null;
            using (MemoryStream mimcStream = new MemoryStream())
            {
                Serializer.Serialize(mimcStream, packet);
                mimcBins = mimcStream.ToArray();
                mimcStream.Dispose();
            }
            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_SECMSG, Constant.CIPHER_RC4, packet.packetId);

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;

            v6Body.Payload = mimcBins;
            v6Packet.Body = v6Body;

            return v6Packet;
        }

        public static V6Packet BuildBindPacket(MIMCUser user)
        {
            if (string.IsNullOrEmpty(user.Token))
            {
                logger.DebugFormat("{0} User BuildBindPacket fail Token is null,wait ...", user.AppAccount);
                return null;
            }
            logger.DebugFormat("{0} User BuildBindPacket", user.AppAccount);

            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_BIND, Constant.CIPHER_NONE, MIMCUtil.CreateMsgId(user));

            XMMsgBind xMMsgBind = new XMMsgBind();
            xMMsgBind.token = user.Token;
            xMMsgBind.method = Constant.METHOD;
            xMMsgBind.client_attrs = user.ClientAttrs;
            xMMsgBind.cloud_attrs = user.CloudAttrs;
            xMMsgBind.kick = Constant.NO_KICK;

            string sign = MIMCUtil.GenerateSig(clientHeader, xMMsgBind, user.Connection.Challenge, user.SecurityKey);
            if (String.IsNullOrEmpty(sign))
            {
                logger.WarnFormat("GenerateSig fail sign is null");
                return null;
            }
            xMMsgBind.sig = sign;

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, xMMsgBind);
                byte[] payload = ms.ToArray();
                v6Body.Payload = payload;
                v6Packet.Body = v6Body;
                ms.Dispose();
            }
            return v6Packet;
        }

        public static V6Packet BuildUnBindPacket(MIMCUser user)
        {
            logger.DebugFormat("{0} User BuildUnBindPacket", user.AppAccount);
            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_UNBIND, Constant.CIPHER_NONE, MIMCUtil.CreateMsgId(user));

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;
            v6Packet.Body = v6Body;

            return v6Packet;
        }

        public static string GenerateRandomString(int length)
        {
            char[] constant = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
            string checkCode = String.Empty;

            Random rd = new Random();
            for (int i = 0; i < length; i++)
            {
                checkCode += constant[rd.Next(constant.Length)].ToString();
            }
            return checkCode;
        }

    }
}

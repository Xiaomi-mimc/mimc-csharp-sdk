using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using com.xiaomi.mimc.client;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.handler;
using com.xiaomi.mimc.packet;
using com.xiaomi.mimc.utils;

using mimc;
using mimc.com.xiaomi.mimc.packet;
using mimc.com.xiaomi.mimc.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
namespace com.xiaomi.mimc
{   /// <summary>
    /// MIMCUser
    /// </summary>
    public class MIMCUser
    {
        

        private long appId;
        private string appAccount;
        private string appPackage;
        private Constant.OnlineStatus status;
        private MIMCConnection connection;
        private string clientAttrs;
        private string cloudAttrs;

        private int chid;
        private long uuid;
        private string resource;

        private bool logoutFlag;
        private string securityKey = "";
        private string token = "";

        private long lastLoginTimestamp;
        private long lastCreateConnTimestamp;
        private long lastPingTimestamp;
        private AtomicInteger atomic;
        private Dictionary<string, TimeoutPacket> timeoutPackets = new Dictionary<string, TimeoutPacket>();

        private IMIMCTokenFetcher tokenFetcher;
        private IMIMCOnlineStatusHandler onlineStatusHandler;
        private IMIMCMessageHandler messageHandler;

        public long LastLoginTimestamp { get; set; }
        public long LastCreateConnTimestamp { get; set; }
        public long LastPingTimestamp { get; set; }
        public Constant.OnlineStatus Status { get; set; }
        public string Token { get; set; }
        public string SecurityKey { get; set; }
        public bool AutoLogin { get; set; }
        public string ClientAttrs { get; set; }
        public string CloudAttrs { get; set; }
        public IMIMCOnlineStatusHandler OnlineStatusHandler { get; set; }
        public Dictionary<string, TimeoutPacket> TimeoutPackets { get; set; }

        /// <summary>
        /// SDK User 构造函数
        /// </summary>
        /// <param name="appId">开发者在小米开放平台申请的appId</param>
        /// <param name="appAccount">用户在APP帐号系统内的唯一帐号ID</param>
        public MIMCUser(string appAccount) : this(appAccount, null)
        {
        }

        /// <summary>
        /// SDK User 构造函数
        /// </summary>
        /// <param name="appId">开发者在小米开放平台申请的appId</param>
        /// <param name="appAccount">用户在APP帐号系统内的唯一帐号ID</param>
        /// <param name="path">用户缓存目录</param>
        public MIMCUser(string appAccount, string path)
        {
            this.appAccount = appAccount;
            this.resource = this.GenerateRandomString(10);
            this.tokenFetcher = null;
            this.Status = Constant.OnlineStatus.Offline;
            this.logoutFlag = false;
            this.LastLoginTimestamp = 0;
            this.LastCreateConnTimestamp = 0;
            this.LastPingTimestamp = 0;
            this.atomic = new AtomicInteger();
            this.TimeoutPackets = new Dictionary<string, TimeoutPacket>();
            this.SetResource(path);

            MIMCConnection connection = new MIMCConnection();
            this.connection = connection;
            this.connection.User = this;

            Thread writeThread = new Thread(new ThreadStart(ThreadWrite));
            writeThread.Start();
            Thread receiveThread = new Thread(new ThreadStart(ThreadReceive));
            receiveThread.Start();
            Thread triggerThread = new Thread(new ThreadStart(ThreadTrigger));
            triggerThread.Start();
        }

        private void SetResource(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = Environment.CurrentDirectory + "\\catch\\";
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine("userCatch {0} Folder already exists, skip creation.", path);
                    }
                    else
                    {
                        Directory.CreateDirectory(path);
                        Console.WriteLine("userCatch {0} Folder does not exist, create success.", path);
                    }
                }


                string catchFile = path + appAccount + ".txt";
                JsonSerializer serializer = new JsonSerializer();
                String result = MIMCUtil.Deserialize<String>(catchFile);
                var currentData = (JObject)JsonConvert.DeserializeObject(result == null ? "" : result);
                currentData = currentData == null ? new JObject() : currentData;
                Console.WriteLine("userCatch before appAccount:{0},resource:{1}", appAccount, this.resource);
                if (!currentData.ContainsKey("resource"))
                {
                    currentData.Add("resource", this.resource);
                    Console.WriteLine("userCatch add：{0}-{1}", appAccount, this.resource);
                    MIMCUtil.SerializeToFile(currentData, catchFile);
                }
                else
                {
                    this.resource = (String)currentData.GetValue("resource");
                    Console.WriteLine("userCatch read：{0}-{1}", appAccount, currentData.GetValue("resource"));
                }
                Console.WriteLine("userCatch currentData after ,appAccount:{0},resource:{1}", appAccount, this.resource);
            }
            catch (Exception e)
            {
                Console.WriteLine("SetResource fail! read or write file error!path:{0}，error：{1}", path, e.StackTrace);
                this.resource = Constant.RESOURCE;
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <returns>bool</returns>
        public bool Login()
        {
            String tokenStr = tokenFetcher.FetchToken();

            if (string.IsNullOrEmpty(tokenStr))
            {
                 Console.WriteLine("{0} Login fail FetchToken IsNullOrEmpty", this.appAccount);
                return false;
            }

            JObject jo = (JObject)JsonConvert.DeserializeObject(tokenStr);
            string code = jo.GetValue("code").ToString();
            if (string.IsNullOrEmpty(code))
            {
                 Console.WriteLine("{0} Login fail code IsNullOrEmpty", this.appAccount);
                return false;
            }
            if (!code.Equals("200"))
            {
                 Console.WriteLine("{0} Login fail code {1}", this.appAccount, code);
                return false;
            }

             Console.WriteLine("{0} Login FetchToken sucesss", this.appAccount);

            JObject data = (JObject)jo.GetValue("data");
            this.appId = long.Parse(data.GetValue("appId").ToString());
            this.appPackage = data.GetValue("appPackage").ToString();
            this.chid = Convert.ToInt32(data.GetValue("miChid"));
            this.uuid = long.Parse(data.GetValue("miUserId").ToString());
            this.SecurityKey = data.GetValue("miUserSecurityKey").ToString();
            Console.WriteLine("{0} get token SecurityKey :{1}", this.appAccount, this.SecurityKey);
            if (!this.appAccount.Equals(data.GetValue("appAccount").ToString()))
            {
                Console.WriteLine("{0} Login fail appAccount does  not match {1}!={2}", this.appAccount, this.appAccount, data.GetValue("appAccount").ToString());
                return false;
            }
            if (string.IsNullOrEmpty(data.GetValue("token").ToString()))
            {
                Console.WriteLine("{0} Login fail token IsNullOrEmpty uuid:{1}", this.appAccount, this.uuid);
                return false;
            }
             Console.WriteLine("{0} Login success uuid:{1}", this.appAccount, this.uuid);

            this.token = data.GetValue("token").ToString();
            return true;
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>bool</returns>
        public bool Logout()
        {
            if (this.status == Constant.OnlineStatus.Offline)
            {
                 Console.WriteLine("Logout FAIL SENDPACKET:{0}, FAIL_FOR_NOT_ONLINE,CHID:{1}, UUID:{2}", Constant.CMD_UNBIND, this.chid, this.uuid);
                return false;
            }

            MIMCObject mIMCObject = new MIMCObject();
            V6Packet v6Packet = BuildUnBindPacket();
            mIMCObject.Type = Constant.CMD_UNBIND;
            mIMCObject.Packet = v6Packet;
            this.connection.PacketWaitToSend.Enqueue(mIMCObject);

            return true;
        }

        private void ThreadWrite()
        {
             Console.WriteLine("{0} ThreadWrite started", this.appAccount);
            if (connection == null)
            {
                 Console.WriteLine("MIMCConnection is null, ThreadWrite not started");
                return;
            }

            string msgType = Constant.MIMC_C2S_DOUBLE_DIRECTION;
            while (true)
            {
               
                V6Packet v6Packet = null;
                if (connection.ConnState == MIMCConnection.State.NOT_CONNECTED)
                {
                    long currentTime = MIMCUtil.CurrentTimeMillis();
                    if (currentTime - this.LastCreateConnTimestamp <= Constant.CONNECT_TIMEOUT)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    LastCreateConnTimestamp = MIMCUtil.CurrentTimeMillis();

                    if (!connection.Connect())
                    {
                         Console.WriteLine("{0} MIMCConnection fail, host:{1}-{2}", this.appAccount, connection.Host, connection.Port);
                        continue;
                    }
                    Console.WriteLine("{0} connection success, host:{1}-{2}", this.appAccount, connection.Host, connection.Port);

                    connection.ConnState = MIMCConnection.State.SOCK_CONNECTED;
                    this.LastCreateConnTimestamp = 0;
                    v6Packet = this.BuildConnectionPacket();
                }
                if (connection.ConnState == MIMCConnection.State.SOCK_CONNECTED)
                {
                    Thread.Sleep(100);
                }
                if (connection.ConnState == MIMCConnection.State.HANDSHAKE_CONNECTED)
                {
                    if (logoutFlag)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    long currentTime = MIMCUtil.CurrentTimeMillis();
                    if (this.Status == Constant.OnlineStatus.Offline
                        && currentTime - this.LastLoginTimestamp <= Constant.LOGIN_TIMEOUT)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    if (this.Status == Constant.OnlineStatus.Offline
                        && currentTime - this.LastLoginTimestamp > Constant.LOGIN_TIMEOUT)
                    {
                        v6Packet = this.BuildBindPacket();
                        if (v6Packet == null)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        this.LastLoginTimestamp = MIMCUtil.CurrentTimeMillis();
                    }
                    if (this.Status == Constant.OnlineStatus.Online)
                    {
                        MIMCObject mimcObject = null;

                        lock (this.connection.PacketWaitToSend)
                        {
                            if (this.connection.PacketWaitToSend.Count != 0)
                            {
                                mimcObject = this.connection.PacketWaitToSend.Dequeue();
                            }
                        }
                        if (mimcObject != null)
                        {
                            msgType = mimcObject.Type;
                            v6Packet = (V6Packet)mimcObject.Packet;
                        }
                        else
                        {
                            long currentTimestamp = MIMCUtil.CurrentTimeMillis();

                            if (currentTimestamp - this.LastPingTimestamp > Constant.PING_TIMEVAL_MS)
                            {
                                v6Packet = new V6Packet();
                            }
                            else
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                        }
                    }
                }
                if (v6Packet == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (msgType == Constant.MIMC_C2S_DOUBLE_DIRECTION)
                {
                    this.connection.TrySetNextResetSockTs();
                }

                try
                {
                    byte[] data = V6PacketEncoder.Encode(this.connection, v6Packet);
                    if (data != null)
                    {
                        this.LastPingTimestamp = MIMCUtil.CurrentTimeMillis();
                        this.connection.TcpConnection.GetStream().Write(data, 0, data.Length);
                        //Console.WriteLine("ThreadWrite, cmd:{0}", v6Packet.Body.ClientHeader.cmd);
                    }
                    else
                    {
                         Console.WriteLine("connection.reset reason: V6PacketEncoder.Encode fail data is null");
                        this.connection.reset();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                     Console.WriteLine("connection.reset reason: {0}", ex.StackTrace);
                    this.connection.reset();
                }
                if (this.logoutFlag)
                {
                    continue;
                }
            }
        }

        private void ThreadReceive()
        {
             Console.WriteLine("{0} ThreadReceive started", this.appAccount);

            if (this.connection == null)
            {
                 Console.WriteLine("ThreadReceive, MIMCConnection is null, ThreadReceive not started");
                return;
            }

            while (true)
            {
                if (this.connection.ConnState == MIMCConnection.State.NOT_CONNECTED )
                {
                    Thread.Sleep(100);
                    continue;
                }

                NetworkStream stream = this.connection.TcpConnection.GetStream();

                byte[] headerBins = new byte[Constant.V6_HEAD_LENGTH];
                int readLength = MIMCConnection.readn(stream, headerBins, Constant.V6_HEAD_LENGTH);
                if (readLength != Constant.V6_HEAD_LENGTH)
                {
                     Console.WriteLine("ThreadReceive connection.reset V6_HEAD not equal{0}!={1}", Constant.V6_HEAD_LENGTH, readLength);
                    this.connection.reset();
                    continue;
                }

                char magic = V6PacketDecoder.GetChar(headerBins, 0);
                if (magic != Constant.MAGIC)
                {
                     Console.WriteLine("ThreadReceive connection.reset V6_MAGIC not equal {0}!={1}", magic, Constant.MAGIC);
                    this.connection.reset();
                    continue;
                }

                char version = V6PacketDecoder.GetChar(headerBins, 2);
                if (version != Constant.V6_VERSION)
                {
                     Console.WriteLine("ThreadReceive  connection.reset V6_VERSION not equal {0}!={1}", version, Constant.V6_VERSION);
                    this.connection.reset();
                    continue;
                }

                uint bodyLen = V6PacketDecoder.GetUint(headerBins, 4);

                if (bodyLen < 0)
                {
                     Console.WriteLine("ThreadReceive  connection.reset V6_BODY, bodyLen={0}", bodyLen);
                    this.connection.reset();
                    continue;
                }
                byte[] bodyBins = new byte[bodyLen];
                if (bodyLen > 0)
                {
                    int v6BodyBytesRead = MIMCConnection.readn(stream, bodyBins, Convert.ToInt32(bodyLen));

                    if (v6BodyBytesRead != bodyLen)
                    {
                         Console.WriteLine("ThreadReceive  connection.reset V6_BODY, {0}!={1}", v6BodyBytesRead, bodyLen);
                        this.connection.reset();
                        continue;
                    }
                }

                byte[] crcBins = new byte[Constant.CRC_LEN];
                int crcBytesRead = MIMCConnection.readn(stream, crcBins, Constant.CRC_LEN);

                if (crcBytesRead != Constant.CRC_LEN)
                {
                     Console.WriteLine("ThreadReceive  connection.reset V6_CRC, {0}!={1}", crcBytesRead, Constant.CRC_LEN);
                    this.connection.reset();
                    continue;
                }

                this.connection.ClearNextResetSockTimestamp();

                V6Packet feV6Packet = V6PacketDecoder.DecodeV6(headerBins, bodyBins, crcBins, this.connection.Rc4Key, this);
                if (feV6Packet == null)
                {
                    Console.WriteLine("ThreadReceive  connection.reset V6Packet Decode fail!");
                    this.connection.reset();
                    continue;
                }

                if (feV6Packet.V6BodyBin == null || feV6Packet.V6BodyBin.Length == 0 || feV6Packet.PacketLen == 0)
                {
                     Console.WriteLine("<--- receive v6 packet ping-pong packet. appAccount:{0}", this.appAccount);
                    continue;
                }

                 Console.WriteLine("<--- receive v6 packet cmd:{0}, appAccount:{1}", feV6Packet.Body.ClientHeader.cmd, this.appAccount);
                if (Constant.CMD_CONN == feV6Packet.Body.ClientHeader.cmd)
                {
                    XMMsgConnResp connResp = null;
                    using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
                    {
                        connResp = Serializer.Deserialize<XMMsgConnResp>(ms);
                        ms.Dispose();
                    }
                    if (null == connResp)
                        {
                             Console.WriteLine("ThreadReceive  connection.reset, cmd:{0}, ConnResp is null", feV6Packet.Body.ClientHeader.cmd);
                            this.connection.reset();
                            continue;
                        }

                        this.connection.ConnState = MIMCConnection.State.HANDSHAKE_CONNECTED;
                        this.connection.Challenge = connResp.challenge;
                        this.connection.setChallengeAndRc4Key(connResp.challenge);
                        continue;
                   
                }
                if (Constant.CMD_BIND == feV6Packet.Body.ClientHeader.cmd)
                {
                    XMMsgBindResp xMMsgBindResp = null;
                    using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
                    {
                        xMMsgBindResp = Serializer.Deserialize<XMMsgBindResp>(ms);
                        ms.Dispose();
                    }
                    this.Status = xMMsgBindResp.result ? Constant.OnlineStatus.Online : Constant.OnlineStatus.Offline;
                    this.OnlineStatusHandler.StatusChange(xMMsgBindResp.result, xMMsgBindResp.error_type, xMMsgBindResp.error_reason, xMMsgBindResp.error_desc);
                    continue;
                  
                }
                if (Constant.CMD_PING == feV6Packet.Body.ClientHeader.cmd)
                {
                    MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload);
                    XMMsgPing xMMsgPing = Serializer.Deserialize<XMMsgPing>(ms);
                    ms.Dispose();

                     Console.WriteLine("<--- receive v6 packet cmd:short ping-pong appAccount:{0}", this.appAccount);
                    continue;
                }
                if (Constant.CMD_KICK == feV6Packet.Body.ClientHeader.cmd)
                {
                    this.logoutFlag = true;
                     Console.WriteLine("appAccount:{0} logout.", this.appAccount);
                    this.OnlineStatusHandler.StatusChange(false, "KICK", "KICK", "KICK");
                    continue;

                }
                if (Constant.CMD_SECMSG == feV6Packet.Body.ClientHeader.cmd)
                {
                    HandleSecMsg(feV6Packet);
                    continue;
                }
            }
        }

        private void ThreadTrigger()
        {
             Console.WriteLine("{0} ThreadTrigger started", this.appAccount);

            if (this.connection == null)
            {
                 Console.WriteLine("ThreadTrigger fail,conn is null");
                return;
            }

            while (true)
            {
                try
                {
                    long currentTime = MIMCUtil.CurrentTimeMillis();
                    if (connection.NextResetSockTimestamp > 0 && currentTime - connection.NextResetSockTimestamp > 0)
                    {
                         Console.WriteLine("ThreadTrigger  connection.reset currentTime:{0},connection.NextResetSockTimestamp:{1}", currentTime, connection.NextResetSockTimestamp);
                        connection.reset();
                    }
                    ScanAndCallBack();
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// 用于超时检测通知和重连
        /// </summary>
        private void ScanAndCallBack()
        {
            if (TimeoutPackets.Count == 0)
            {
                return;
            }
            Dictionary<string, TimeoutPacket> temPackets = new Dictionary<string, TimeoutPacket>();

            foreach (KeyValuePair<string, TimeoutPacket> item in timeoutPackets)
            {
                TimeoutPacket timeoutPacket = item.Value;
                if (MIMCUtil.CurrentTimeMillis() - timeoutPacket.Timestamp < Constant.CHECK_TIMEOUT_TIMEVAL_MS)
                {
                    continue;
                }

                MIMCPacket mimcPacket = (MIMCPacket)timeoutPacket.Packet;
                if (mimcPacket.type == MIMC_MSG_TYPE.P2P_MESSAGE)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        MIMCP2PMessage p2p = Serializer.Deserialize<MIMCP2PMessage>(ms);
                        ms.Dispose();
                        P2PMessage p2pMessage = new P2PMessage(mimcPacket.packetId, mimcPacket.sequence, p2p.from.appAccount, p2p.from.resource, p2p.payload, mimcPacket.timestamp);
                        messageHandler.HandleSendMessageTimeout(p2pMessage);
                        Console.WriteLine("{0} ThreadCallback SendMessageTimeout packetId:{1}", this.appAccount, p2pMessage.PacketId);
                    }
                }
                if (mimcPacket.type == MIMC_MSG_TYPE.P2T_MESSAGE)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        MIMCP2TMessage p2t = Serializer.Deserialize<MIMCP2TMessage>(ms);
                        ms.Dispose();
                        P2TMessage p2tMessage = new P2TMessage(mimcPacket.packetId, mimcPacket.sequence, p2t.from.appAccount, p2t.from.resource, p2t.to.topicId, p2t.payload, mimcPacket.timestamp);
                        messageHandler.HandleSendGroupMessageTimeout(p2tMessage);
                        Console.WriteLine("{0} ThreadCallback SendMessageTimeout packetId:{1}", this.appAccount, p2tMessage.PacketId);
                    }
                }
                temPackets.Add(item.Key, item.Value);
            }
            lock (TimeoutPackets)
            {
                foreach (KeyValuePair<string, TimeoutPacket> item in temPackets)
                {
                    TimeoutPackets.Remove(item.Key);
                    Console.WriteLine("{0} Loop Detection timeoutPackets Remove sucess,packetId:{1}", this.appAccount, item.Key);
                }
            }
        }

        private void HandleSecMsg(V6Packet feV6Packet)
        {
            if (feV6Packet == null || feV6Packet.Body.Payload == null || feV6Packet.Body.Payload.Length == 0)
            {
                 Console.WriteLine("HandleSecMsg fail, invalid packet");
                return;
            }
            MIMCPacket packet = null;
            using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
            {
                packet = Serializer.Deserialize<MIMCPacket>(ms);
                ms.Dispose();
            }
            if (packet == null)
            {
                 Console.WriteLine("{0} HandleSecMsg fail, parse MIMCPacket fail", this.appAccount);
                return;
            }
             Console.WriteLine("HandleSecMsg, type:{0} , uuid:{1}, packetId:{2}, chid:{3}",
               packet.type, uuid, packet.packetId, chid);

            if (packet.type == MIMC_MSG_TYPE.PACKET_ACK)
            {
                MIMCPacketAck packetAck = null;
                using (MemoryStream ackStream = new MemoryStream(packet.payload))
                {
                    packetAck = Serializer.Deserialize<MIMCPacketAck>(ackStream);
                    ackStream.Dispose();
                }
                if (packetAck == null)
                {
                     Console.WriteLine("{0} HandleSecMsg parse MIMCPacketAck fail", this.appAccount);
                    return;
                }
                messageHandler.HandleServerACK(new ServerAck(packetAck.packetId, packetAck.sequence, packetAck.timestamp));

                TimeoutPacket timeoutPacket = new TimeoutPacket(packet, MIMCUtil.CurrentTimeMillis());
                lock (TimeoutPackets)
                {
                    TimeoutPackets.Remove(packetAck.packetId);
                    Console.WriteLine("{0} HandleSecMsg timeoutPackets Remove sucess,packetId:{1}", this.appAccount, packetAck.packetId);
                }

                return;
            }
            if (packet.type == MIMC_MSG_TYPE.COMPOUND)
            {
                MIMCPacketList packetList = null;
                using (MemoryStream comStream = new MemoryStream(packet.payload))
                {
                    packetList = Serializer.Deserialize<MIMCPacketList>(comStream);
                    comStream.Dispose();
                }
                if (packetList == null || packetList.packets.ToArray().Length == 0)
                {
                     Console.WriteLine("HandleSecMsg parse MIMCPacketList fail ,packetList:{0}, packetList.packets.ToArray().Length:{1}", packetList, packetList.packets.ToArray().Length);
                    return;
                }
                Console.WriteLine("HandleSecMsg MIMCPacketList resource:{0}", resource);

                if (this.resource != packetList.resource)
                {
                     Console.WriteLine("HandleSecMsg MIMCPacketList parse fail,resource not match,{0}!={1}", resource, packetList.resource);
                    return;
                }

                V6Packet mimcSequenceAckPacket = BuildSequenceAckPacket(packetList);
                MIMCObject miObj = new MIMCObject();
                miObj.Type = Constant.MIMC_C2S_SINGLE_DIRECTION;
                miObj.Packet = mimcSequenceAckPacket;
                this.connection.PacketWaitToSend.Enqueue(miObj);

                int packetListNum = packetList.packets.Count;
                List<P2PMessage> p2pMessagesList = new List<P2PMessage>();
                List<P2TMessage> p2tMessagesList = new List<P2TMessage>();
                Console.WriteLine("{0} HandleSecMsg MIMCPacketList packetListNum:{1}", this.appAccount, packetListNum);

                foreach (MIMCPacket p in packetList.packets)
                {
                    if (p == null)
                    {
                         Console.WriteLine("{0} HandleSecMsg packet is null", this.appAccount);
                        continue;
                    }
                    if (p.type == MIMC_MSG_TYPE.P2P_MESSAGE)
                    {
                        Console.WriteLine("HandleSecMsg P2P_MESSAGE packetId:{0}", p.packetId);
                        MIMCP2PMessage p2pMessage = null;
                        using (MemoryStream p2pStream = new MemoryStream(p.payload))
                        {
                            p2pMessage = Serializer.Deserialize<MIMCP2PMessage>(p2pStream);
                            p2pStream.Dispose();
                        }
                        if (p2pMessage == null)
                        {
                             Console.WriteLine("HandleSecMsg p2pMessage is null");
                            continue;
                        }
                      
                        p2pMessagesList.Add(new P2PMessage(p.packetId, p.sequence,
                            p2pMessage.from.appAccount, p2pMessage.from.resource,
                            p2pMessage.payload, p.timestamp));

                        continue;
                    }
                    if (p.type == MIMC_MSG_TYPE.P2T_MESSAGE)
                    {
                        Console.WriteLine("HandleSecMsg P2T_MESSAGE packetId:{0}", p.packetId);
                        MIMCP2TMessage p2tMessage = null;
                        using (MemoryStream p2tStream = new MemoryStream(p.payload))
                        {
                            p2tMessage = Serializer.Deserialize<MIMCP2TMessage>(p2tStream);
                            p2tStream.Dispose();
                        }
                        if (p2tMessage == null)
                        {
                             Console.WriteLine("HandleSecMsg p2tMessage is null");
                            continue;
                        }

                        p2tMessagesList.Add(new P2TMessage(p.packetId, p.sequence,
                            p2tMessage.from.appAccount, p2tMessage.from.resource,
                            p2tMessage.to.topicId, p2tMessage.payload, p.timestamp));
                        continue;
                    }

                     Console.WriteLine("HandleSecMsg RECV_MIMC_PACKET ,invalid type, Type：{0}", p.type);
                }

                if (p2pMessagesList.Count > 0)
                {
                    messageHandler.HandleMessage(p2pMessagesList);
                }
                if (p2tMessagesList.Count > 0)
                {
                    messageHandler.HandleGroupMessage(p2tMessagesList);
                }
            }
        }

        private V6Packet BuildSequenceAckPacket(MIMCPacketList packetList)
        {
            MIMCPacket packet = new MIMCPacket();
            packet.packetId = CreateMsgId();
            packet.package = appPackage;
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
            ClientHeader clientHeader = CreateClientHeader(Constant.CMD_SECMSG, Constant.CIPHER_RC4, packet.packetId);

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;

            v6Body.Payload = mimcBins;
            v6Packet.Body = v6Body;

            return v6Packet;
        }

        public bool IsOnline()
        {
            Console.WriteLine("{0} IsOnline :{1}", this.appAccount, this.Status == Constant.OnlineStatus.Online);
            return this.Status == Constant.OnlineStatus.Online;
        }

        public string AppAccount()
        {
            return appAccount;
        }


        /// <summary>
        /// 发送单聊消息
        /// </summary>
        /// <param name="toAppAccount">消息接收者在APP帐号系统内的唯一帐号ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <returns>packetId客户端生成的消息ID</returns>
        public string SendMessage(string toAppAccount, byte[] msg)
        {
            if (string.IsNullOrEmpty(toAppAccount) || msg == null || msg.Length == 0 || msg.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                 Console.WriteLine("SendMessage fail!,toAppAccount:{0},msg :{1}", toAppAccount, msg);
                return null;
            }
            return SendMessage(toAppAccount, msg, true);
        }
        /// <summary>
        /// 发送单聊消息
        /// </summary>
        /// <param name="toAppAccount">消息接收者在APP帐号系统内的唯一帐号ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <param name="isStore">是否保存历史记录，true：保存，false：不存</param>
        /// <returns>packetId客户端生成的消息ID</returns>
        public string SendMessage(string toAppAccount, byte[] msg, bool isStore)
        {
            if (string.IsNullOrEmpty(toAppAccount) || msg == null || msg.Length == 0 || msg.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                 Console.WriteLine("SendMessage fail!,toAppAccount:{0},msg :{1}", toAppAccount, msg);
                return null;
            }
            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;

            global::mimc.MIMCUser toUser = new global::mimc.MIMCUser();
            toUser.appId = appId;
            toUser.appAccount = toAppAccount;

            MIMCP2PMessage p2pMessage = new MIMCP2PMessage();
            p2pMessage.from = fromUser;
            p2pMessage.to = toUser;
            p2pMessage.payload = msg;
            p2pMessage.isStore = isStore;

            MIMCPacket packet = new MIMCPacket();
            String packetId = CreateMsgId();
            packet.packetId = packetId;
            packet.package = appPackage;
            packet.type = MIMC_MSG_TYPE.P2P_MESSAGE;

            using(MemoryStream ms = new MemoryStream()){
                Serializer.Serialize(ms, p2pMessage);
                byte[] p2pPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;
                packet.payload = p2pPacket;
                ms.Dispose();
            }
            packet.timestamp = MIMCUtil.CurrentTimeMillis();
            byte[] packetBin = null;
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, packet);
                packetBin = stream.ToArray();
                stream.Flush();
                stream.Position = 0;
                stream.Dispose();
            }
            if (SendPacket(packetId, packetBin, Constant.MIMC_C2S_DOUBLE_DIRECTION))
            {
                TimeoutPacket timeoutPacket = new TimeoutPacket(packet, MIMCUtil.CurrentTimeMillis());
                lock (TimeoutPackets)
                {
                    TimeoutPackets.Add(packetId, timeoutPacket);
                }
                Console.WriteLine("{0}  timeoutPackets init add one size now:{1}", this.appAccount, TimeoutPackets.Count);

                return packetId;
            }

            return null;
        }
        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <returns></returns>
        public string SendGroupMessage(long topicId, byte[] msg)
        {
            if (topicId == 0 || msg == null || msg.Length == 0 || msg.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                 Console.WriteLine("{0} SendGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount, topicId, msg, (long)msg.Length);
                return null;
            }
            return SendGroupMessage(topicId, msg, true);
        }
        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <param name="isStore">是否保存历史记录，true：保存，false：不存</param>
        /// <returns></returns>
        public string SendGroupMessage(long topicId, byte[] msg, bool isStore)
        {
            if (topicId == 0 || msg == null || msg.Length == 0 || msg.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                 Console.WriteLine("{0} SendGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount, topicId, msg, (long)msg.Length);
                return null;
            }
            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;

            global::mimc.MIMCGroup to = new global::mimc.MIMCGroup();
            to.appId = appId;
            to.topicId = topicId;

            MIMCP2TMessage p2tMessage = new MIMCP2TMessage();
            p2tMessage.from = fromUser;
            p2tMessage.to = to;
            p2tMessage.payload = msg;
            p2tMessage.isStore = isStore;

            MIMCPacket packet = new MIMCPacket();
            String packetId = CreateMsgId();
            packet.packetId = packetId;
            packet.package = appPackage;
            packet.type = MIMC_MSG_TYPE.P2T_MESSAGE;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, p2tMessage);
                byte[] p2tPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;
                ms.Dispose();
                packet.payload = p2tPacket;
            }

            packet.timestamp = MIMCUtil.CurrentTimeMillis();
            byte[] packetBin = null;
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, packet);
                packetBin = stream.ToArray();
                stream.Flush();
                stream.Position = 0;
                stream.Dispose();
            }

            if (SendPacket(packetId, packetBin, Constant.MIMC_C2S_DOUBLE_DIRECTION))
            {
                TimeoutPacket timeoutPacket = new TimeoutPacket(packet, MIMCUtil.CurrentTimeMillis());
                lock (TimeoutPackets)
                {
                    TimeoutPackets.Add(packetId, timeoutPacket);
                }
                Console.WriteLine("{0}  timeoutPackets init add one size now:{1}", this.appAccount, TimeoutPackets.Count);

                return packetId;
            }

            return null;
        }

        private bool SendPacket(string packetId, byte[] packetBin, string msgType)
        {
            if (string.IsNullOrEmpty(packetId) || packetBin == null || packetBin.Length == 0 || (msgType != Constant.MIMC_C2S_DOUBLE_DIRECTION && msgType != Constant.MIMC_C2S_SINGLE_DIRECTION))
            {
                 Console.WriteLine("SendPacket fail!packetId:{0},packetBin :{1}，msgType：{2}", packetId, packetBin, msgType);
                return false;
            }
            V6Packet v6Packet = BuildSecMsgPacket(packetId, packetBin);

            MIMCObject mIMCObject = new MIMCObject();
            mIMCObject.Type = msgType;
            mIMCObject.Packet = v6Packet;
            Console.WriteLine("SendPacket packetId:{0},resource :{1}，msgType：{2}", packetId, resource, msgType);

            this.connection.PacketWaitToSend.Enqueue(mIMCObject);

            return true;
        }

        private V6Packet BuildSecMsgPacket(string packetId, byte[] packetBin)
        {

            Console.WriteLine("{0} User buildSecMsgPacket", this.appAccount);

            ClientHeader clientHeader = CreateClientHeader(Constant.CMD_SECMSG, Constant.CIPHER_RC4, packetId);

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;

            v6Body.Payload = packetBin;
            v6Packet.Body = v6Body;

            return v6Packet;
        }

        private V6Packet BuildConnectionPacket()
        {
            Console.WriteLine("{0} User buildConnectionPacket", this.appAccount);
            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();

            XMMsgConn xMMsgConn = new XMMsgConn();
            xMMsgConn.os = "macOS";
            xMMsgConn.udid = this.connection.Udid;
            xMMsgConn.version = Constant.CONN_BIN_PROTO_VERSION;

            ClientHeader clientHeader = CreateClientHeader(Constant.CMD_CONN, Constant.CIPHER_NONE, CreateMsgId());

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

        private V6Packet BuildBindPacket()
        {
            if (string.IsNullOrEmpty(this.token))
            {
                return null;
            }
            Console.WriteLine("{0} User BuildBindPacket", this.appAccount);

            ClientHeader clientHeader = CreateClientHeader(Constant.CMD_BIND, Constant.CIPHER_NONE, CreateMsgId());

            XMMsgBind xMMsgBind = new XMMsgBind();
            xMMsgBind.token = this.token;
            xMMsgBind.method = Constant.METHOD;
            xMMsgBind.client_attrs = this.ClientAttrs;
            xMMsgBind.cloud_attrs = this.CloudAttrs;
            xMMsgBind.kick = Constant.NO_KICK;

            string sign = MIMCUtil.GenerateSig(clientHeader, xMMsgBind, this.connection.Challenge, this.SecurityKey);
            if (String.IsNullOrEmpty(sign))
            {
                Console.WriteLine("GenerateSig fail sign is null");
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

        private V6Packet BuildUnBindPacket()
        {
            Console.WriteLine("{0} User BuildUnBindPacket", this.appAccount);
            ClientHeader clientHeader = CreateClientHeader(Constant.CMD_UNBIND, Constant.CIPHER_NONE, CreateMsgId());

            V6Packet v6Packet = new V6Packet();
            V6Body v6Body = new V6Body();
            v6Body.PayloadType = Constant.PAYLOAD_TYPE;
            v6Body.ClientHeader = clientHeader;
            v6Packet.Body = v6Body;

            return v6Packet;
        }

        private string GenerateRandomString(int length)
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

        private ClientHeader CreateClientHeader(String cmd, int cipher, String msgId)
        {
            Console.WriteLine("{0} cmd:{1},msgId:{2}", this.appAccount, cmd, msgId);
            ClientHeader clientHeader = new ClientHeader();
            clientHeader.id = msgId;
            clientHeader.uuid = this.uuid;
            clientHeader.chid = Constant.MIMC_CHID;
            clientHeader.resource = this.resource;
            clientHeader.cmd = cmd;
            clientHeader.server = Constant.SERVER;
            clientHeader.cipher = cipher;
            clientHeader.dir_flag = ClientHeader.MSG_DIR_FLAG.CS_REQ;
            return clientHeader;
        }

        private string CreateMsgId()
        {
            return MIMCUtil.GetRandomString(15, true, true, false, false, null) + "-" + atomic.IncrementAndGet();
        }

        /// <summary>
        /// 在线状态回调接口
        /// </summary>
        /// <param name="onlineStatusHandler">传入实现IMIMCOnlineStatusHandler的实现类</param>
        public void RegisterOnlineStatusHandler(IMIMCOnlineStatusHandler onlineStatusHandler)
        {
            if (onlineStatusHandler == null)
            {
                Console.WriteLine("OnlineStatusHandler, HandlerIsNull");
                return;
            }
            this.OnlineStatusHandler = onlineStatusHandler;
        }

        /// <summary>
        /// 消息回调接口
        /// </summary>
        /// <param name="messageHandler">传入实现IMIMCMessageHandler的实现类</param>
        public void RegisterMIMCMessageHandler(IMIMCMessageHandler messageHandler)
        {
            this.messageHandler = messageHandler;
        }

        /// <summary>
        /// 注册获取Token接口
        /// </summary>
        /// <param name="tokenFetcher">传入实现IMIMCTokenFetcher的实现类</param>
        public void RegisterMIMCTokenFetcher(IMIMCTokenFetcher tokenFetcher)
        {
            this.tokenFetcher = tokenFetcher;
        }
    }
}

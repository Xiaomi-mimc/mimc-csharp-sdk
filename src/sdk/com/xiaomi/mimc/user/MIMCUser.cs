﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.xiaomi.mimc.client;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.handler;
using com.xiaomi.mimc.packet;
using com.xiaomi.mimc.utils;
using log4net;
using mimc;
using mimc.com.xiaomi.mimc.utils;
using mimc_csharp_sdk.com.xiaomi.mimc.common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
namespace com.xiaomi.mimc
{
    //事件所用的委托(链表)
    public delegate void StateEventHandler(object sender, StateChangeEventArgs e);

    public delegate void MessageEventHandler(object sender, MessageEventArgs e);

    public delegate void MessageTimeoutEventHandler(object sender, SendMessageTimeoutEventArgs e);

    public delegate void GroupMessageEventHandler(object sender, GroupMessageEventArgs e);

    public delegate void GroupMessageTimeoutEventHandler(object sender, SendGroupMessageTimeoutEventArgs e);

    public delegate void UnlimitedGroupMessageEventHandler(object sender, UnlimitedGroupMessageEventArgs e);

    public delegate void UnlimitedGroupMessageTimeoutEventHandler(object sender,
        SendUnlimitedGroupMessageTimeoutEventArgs e);

    public delegate void JoinUnlimitedGroupEventHandler(object sender, JoinUnlimitedGroupEventArgs e);

    public delegate void QuitUnlimitedGroupEventHandler(object sender, QuitUnlimitedGroupEventArgs e);

    public delegate void DismissUnlimitedGroupEventHandler(object sender, DismissUnlimitedGroupEventArgs e);

    public delegate void ServerACKEventHandler(object sender, ServerACKEventArgs e);

    /// <summary>
    /// MIMCUser
    /// </summary>
    public class MIMCUser
    {
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        private volatile bool logoutFlag;
        private string securityKey;
        private string token;
        private long lastLoginTimestamp;
        private long lastCreateConnTimestamp;
        private long lastPingTimestamp;
        private long lastUcPingTimestamp;
        private Thread writeThread;
        private Thread receiveThread;
        private Thread triggerThread;
        private QueryUnlimitedGroupsThread queryUnlimitedGroupsThread = null;

        private AtomicInteger atomic;
        private ConcurrentDictionary<string, TimeoutPacket> timeoutPackets;
        private IMIMCTokenFetcher tokenFetcher;
        private MIMCUserHandler userHandler;
        private List<long> ucTopics;
        private HashSet<long> ucAckSequenceSet;
        private HashSet<long> p2pAckSequenceSet;
        private HashSet<long> p2tAckSequenceSet;
        private volatile bool exit = false;


        public string AppAccount
        {
            get => appAccount;
            set => appAccount = value;
        }

        public long Uuid
        {
            get => uuid;
            set => uuid = value;
        }

        public string Resource
        {
            get => resource;
            set => resource = value;
        }

        internal AtomicInteger Atomic
        {
            get => atomic;
            set => atomic = value;
        }

        public MIMCConnection Connection
        {
            get => connection;
            set => connection = value;
        }

        public string Token
        {
            get => token;
            set => token = value;
        }

        public string AppPackage
        {
            get => appPackage;
            set => appPackage = value;
        }

        public int Chid
        {
            get => chid;
            set => chid = value;
        }

        public MIMCUserHandler UserHandler
        {
            get => userHandler;
            set => userHandler = value;
        }

        public ConcurrentDictionary<string, TimeoutPacket> TimeoutPackets
        {
            get => timeoutPackets;
            set => timeoutPackets = value;
        }

        public long AppId
        {
            get => appId;
            set => appId = value;
        }

        public long LastUcPingTimestamp
        {
            get => LastUcPingTimestamp1;
            set => LastUcPingTimestamp1 = value;
        }

        public HashSet<long> UCAckSequenceSet
        {
            get => ucAckSequenceSet;
            set => ucAckSequenceSet = value;
        }

        public HashSet<long> P2pAckSequenceSet
        {
            get => p2pAckSequenceSet;
            set => p2pAckSequenceSet = value;
        }

        public HashSet<long> P2tAckSequenceSet
        {
            get => p2tAckSequenceSet;
            set => p2tAckSequenceSet = value;
        }

        public List<long> UcTopics
        {
            get => ucTopics;
            set => ucTopics = value;
        }
        public long LastLoginTimestamp { get => lastLoginTimestamp; set => lastLoginTimestamp = value; }
        public long LastCreateConnTimestamp { get => lastCreateConnTimestamp; set => lastCreateConnTimestamp = value; }
        public long LastPingTimestamp { get => lastPingTimestamp; set => lastPingTimestamp = value; }
        public long LastUcPingTimestamp1 { get => lastUcPingTimestamp; set => lastUcPingTimestamp = value; }
        public string ClientAttrs { get => clientAttrs; set => clientAttrs = value; }
        public string CloudAttrs { get => cloudAttrs; set => cloudAttrs = value; }
        public Constant.OnlineStatus Status { get => status; set => status = value; }
        public string SecurityKey { get => securityKey; set => securityKey = value; }


        //定义事件处理器
        public event StateEventHandler stateChangeEvent;

        public event MessageEventHandler messageEvent;
        public event MessageTimeoutEventHandler messageTimeOutEvent;
        public event GroupMessageEventHandler groupMessageEvent;
        public event GroupMessageTimeoutEventHandler groupMessageTimeoutEvent;

        public event ServerACKEventHandler serverACKEvent;

        public event UnlimitedGroupMessageEventHandler unlimitedGroupMessageEvent;
        public event UnlimitedGroupMessageTimeoutEventHandler unlimitedGroupMessageTimeoutEvent;
        public event JoinUnlimitedGroupEventHandler joinUnlimitedGroupEvent;
        public event QuitUnlimitedGroupEventHandler quitUnlimitedGroupEvent;
        public event DismissUnlimitedGroupEventHandler dismissUnlimitedGroupEvent;

        /// <summary>
        /// SDK User 构造函数
        /// </summary>
        /// <param name="appAccount">用户在APP帐号系统内的唯一帐号ID</param>
        public MIMCUser(string appAccount) : this(appAccount, null)
        {
        }

        /// <summary>
        /// SDK User 构造函数
        /// </summary>
        /// <param name="appAccount">用户在APP帐号系统内的唯一帐号ID</param>
        /// <param name="path">用户缓存目录</param>
        public MIMCUser(string appAccount, string path)
        {
            this.appAccount = appAccount;
            this.resource = MIMCUtil.GenerateRandomString(10);
            this.tokenFetcher = null;
            this.Status = Constant.OnlineStatus.Offline;
            this.Token = null;
            this.logoutFlag = false;
            this.LastLoginTimestamp = 0;
            this.LastCreateConnTimestamp = 0;
            this.LastPingTimestamp = 0;
            this.LastUcPingTimestamp1 = 0;
            this.atomic = new AtomicInteger();
            this.timeoutPackets = new ConcurrentDictionary<string, TimeoutPacket>();
            this.userHandler = new MIMCUserHandler();
            this.SetResource(path);
            this.ucAckSequenceSet = new HashSet<long>();
            this.P2pAckSequenceSet = new HashSet<long>();
            this.P2tAckSequenceSet = new HashSet<long>();

            MIMCConnection connection = new MIMCConnection();
            this.connection = connection;
            this.connection.User = this;

            this.writeThread = new Thread(new ThreadStart(ThreadWrite));
            writeThread.Start();
            this.receiveThread = new Thread(new ThreadStart(ThreadReceive));
            receiveThread.Start();
            this.triggerThread = new Thread(new ThreadStart(ThreadTrigger));
            triggerThread.Start();
        }

        public void HandleStateChange(StateChangeEventArgs args)
        {
            if (null == stateChangeEvent)
            {
                logger.WarnFormat("{0} OnStateChange fail StateChangeEvent is null. ", this.appAccount);
                return;
            }

            stateChangeEvent(this, args);
        }

        internal void HandleMessage(List<P2PMessage> packets)
        {
            if (null == messageEvent)
            {
                logger.WarnFormat("{0} HandleMessage fail MessageEvent is null. ", this.appAccount);
                return;
            }

            if (packets.Count == 0 || packets == null)
            {
                logger.WarnFormat("{0} HandleMessage fail packets is null. ", this.appAccount);
                return;
            }

            logger.DebugFormat("{0} HandleMessage ,packets size：{1}", this.AppAccount, packets.Count);

            for (int i = packets.Count - 1; i >= 0; i--)
            {
                if (this.P2pAckSequenceSet.Contains(packets[i].Sequence))
                {
                    logger.WarnFormat("{0} HandleMessage fail,packet.Sequence already existed ：{1}", this.AppAccount,
                        packets[i].Sequence);
                    packets.RemoveAt(i);
                    continue;
                }

                logger.DebugFormat("{0} HandleMessage ,packet.Sequence add：{1}", this.AppAccount, packets[i].Sequence);
                this.P2pAckSequenceSet.Add(packets[i].Sequence);
            }

            if (packets.Count == 0 || packets == null)
            {
                return;
            }

            logger.DebugFormat("{0} HandleMessage ,packets size：{1}", this.AppAccount, packets.Count);
            MessageEventArgs eventArgs = new MessageEventArgs(packets);
            messageEvent(this, eventArgs);
        }

        private void HandleSendMessageTimeout(P2PMessage p2PMessage)
        {
            if (null == messageTimeOutEvent)
            {
                logger.WarnFormat("{0} HandleSendMessageTimeout fail MessageTimeOutEvent is null. ", this.appAccount);
                return;
            }

            SendMessageTimeoutEventArgs eventArgs = new SendMessageTimeoutEventArgs(p2PMessage);
            messageTimeOutEvent(this, eventArgs);
        }

        internal void HandleGroupMessage(List<P2TMessage> packets)
        {
            if (null == groupMessageEvent)
            {
                logger.WarnFormat("{0} HandleGroupMessage fail GroupMessageEvent is null. ", this.appAccount);
                return;
            }

            logger.InfoFormat("{0} HandleGroupMessage success. ", this.appAccount);

            for (int i = packets.Count - 1; i >= 0; i--)
            {
                P2TMessage message = packets[i];
                if (this.P2tAckSequenceSet.Contains(message.Sequence))
                {
                    logger.WarnFormat("{0} HandleGroupMessage fail,packet.Sequence already existed ：{1},Count:{2}",
                        this.AppAccount, message.Sequence, P2tAckSequenceSet.Count);
                    packets.RemoveAt(i);
                    continue;
                }

                logger.DebugFormat("{0} HandleGroupMessage ,packet.Sequence add：{1}", this.AppAccount,
                    message.Sequence);
                this.P2tAckSequenceSet.Add(message.Sequence);
            }

            if (packets.Count == 0 || packets == null)
            {
                return;
            }

            GroupMessageEventArgs eventArgs = new GroupMessageEventArgs(packets);
            groupMessageEvent(this, eventArgs);
        }

        private void HandleSendGroupMessageTimeout(P2TMessage message)
        {
            if (null == groupMessageTimeoutEvent)
            {
                logger.WarnFormat("{0} HandleSendGroupMessageTimeout fail GroupMessageTimeoutEvent is null. ",
                    this.appAccount);
                return;
            }

            SendGroupMessageTimeoutEventArgs eventArgs = new SendGroupMessageTimeoutEventArgs(message);
            groupMessageTimeoutEvent(this, eventArgs);
        }

        public void HandleServerACK(ServerAck serverAck)
        {
            if (null == serverACKEvent)
            {
                logger.WarnFormat("{0} HandleServerACK fail ServerACKEvent is null. ", this.appAccount);
                return;
            }

            logger.InfoFormat("{0} HandleServerACK success. ", this.appAccount);

            ServerACKEventArgs eventArgs = new ServerACKEventArgs(serverAck);
            serverACKEvent(this, eventArgs);
        }

        internal void HandleUnlimitedGroupMessage(List<P2UMessage> p2uMessagesList, UCGroup group, long maxSequence)
        {
            if (null == unlimitedGroupMessageEvent)
            {
                logger.WarnFormat("{0} HandleUnlimitedGroupMessage fail ServerACKEvent is null. ", this.appAccount);
                return;
            }

            if (p2uMessagesList.Count > 1)
                logger.WarnFormat(" HandleUnlimitedGroupMessage p2uMessagesList size {0} ", p2uMessagesList.Count);

            for (int i = p2uMessagesList.Count - 1; i >= 0; i--)
            {
                P2UMessage p2uMessage = p2uMessagesList[i];
                if (this.UCAckSequenceSet.Contains(p2uMessage.Sequence))
                {
                    logger.WarnFormat(
                        "{0} HandleUnlimitedGroupMessage fail,packet.sequence already existed ：{1}，packetId:{2},Count:{3}",
                        this.AppAccount, p2uMessage.Sequence, p2uMessage.PacketId, UCAckSequenceSet.Count);
                    p2uMessagesList.Remove(p2uMessage);
                    continue;
                }

                this.UCAckSequenceSet.Add(p2uMessage.Sequence);
                logger.DebugFormat("{0} sequence add success ：{1}，packetId:{2},Count:{3}", this.AppAccount,
                    p2uMessage.Sequence, p2uMessage.PacketId, UCAckSequenceSet.Count);
            }

            if (p2uMessagesList.Count == 0 || p2uMessagesList == null)
            {
                return;
            }

            UCPacket ucAckPacket = BuildUcSeqAckPacket(this, group, maxSequence);
            String packetId = MIMCUtil.CreateMsgId(this);

            if (SendUCPacket(packetId, ucAckPacket))
            {
                this.LastUcPingTimestamp1 = MIMCUtil.CurrentTimeMillis();
                logger.DebugFormat("---> Send UcSeqAck Packet sucess,{0} packetId:{1}, lastUcPingTimestamp:{2}",
                    this.appAccount, packetId, this.LastUcPingTimestamp1);
            }
            else
            {
                logger.WarnFormat("---> Send UcSeqAck Packet fail,{0} packetId:{1},lastUcPingTimestamp:{2}",
                    this.appAccount, packetId, this.LastUcPingTimestamp1);
            }

            logger.InfoFormat("{0} HandleUnlimitedGroupMessage success. ", this.appAccount);

            UnlimitedGroupMessageEventArgs eventArgs = new UnlimitedGroupMessageEventArgs(p2uMessagesList);
            unlimitedGroupMessageEvent(this, eventArgs);
        }

        internal void HandleJoinUnlimitedGroup(UCPacket ucPacket)
        {
            if (null == joinUnlimitedGroupEvent)
            {
                logger.WarnFormat("{0} HandleJoinUnlimitedGroup fail ServerACKEvent is null. ", this.appAccount);
                return;
            }

            logger.InfoFormat("{0} HandleJoinUnlimitedGroup success. ", this.appAccount);
            UCJoinResp uCJoinResp = null;
            using (MemoryStream ucStream = new MemoryStream(ucPacket.payload))
            {
                uCJoinResp = Serializer.Deserialize<UCJoinResp>(ucStream);
            }

            JoinUnlimitedGroupEventArgs eventArgs = new JoinUnlimitedGroupEventArgs(this, uCJoinResp);
            joinUnlimitedGroupEvent(this, eventArgs);
        }

        internal void HandleQuitUnlimitedGroup(UCPacket ucPacket)
        {
            if (null == quitUnlimitedGroupEvent)
            {
                logger.WarnFormat("{0} HandleQuitUnlimitedGroup fail ServerACKEvent is null. ", this.appAccount);
                return;
            }

            logger.InfoFormat("{0}HandleQuitUnlimitedGroup success. ", this.appAccount);
            UCQuitResp resp = null;
            using (MemoryStream ucStream = new MemoryStream(ucPacket.payload))
            {
                resp = Serializer.Deserialize<UCQuitResp>(ucStream);
            }

            QuitUnlimitedGroupEventArgs eventArgs = new QuitUnlimitedGroupEventArgs(this, resp);
            quitUnlimitedGroupEvent(this, eventArgs);
        }

        internal void HandleDismissUnlimitedGroup(DismissUnlimitedGroupEventArgs args)
        {
            if (null == dismissUnlimitedGroupEvent)
            {
                logger.WarnFormat("{0} HandleDismissUnlimitedGroup fail ServerACKEvent is null. ", this.appAccount);
                return;
            }

            logger.InfoFormat("{0}HandleDismissUnlimitedGroup success. ", this.appAccount);
            dismissUnlimitedGroupEvent(this, args);
        }

        private void HandleSendUnlimitedGroupMessageTimeout(UCPacket ucPacket)
        {
            if (null == unlimitedGroupMessageTimeoutEvent)
            {
                logger.WarnFormat(
                    "{0} HandleSendUnlimitedGroupMessageTimeout fail unlimitedGroupMessageTimeoutEvent is null. ",
                    this.appAccount);
                return;
            }

            logger.DebugFormat(" UC_PACKET packetId:{0}", ucPacket.packetId);
            UCMessage ucMessage = null;
            using (MemoryStream ucStream = new MemoryStream(ucPacket.payload))
            {
                ucMessage = Serializer.Deserialize<UCMessage>(ucStream);
            }

            if (ucMessage == null)
            {
                logger.WarnFormat("HandleSecMsg p2tMessage is null");
            }

            logger.DebugFormat("UC_PACKET UC_MSG_TYPE：{0}", ucPacket.type);

            SendUnlimitedGroupMessageTimeoutEventArgs eventArgs = new SendUnlimitedGroupMessageTimeoutEventArgs(this,
                new P2UMessage(ucMessage.packetId, ucMessage.sequence,
                    ucMessage.user.appAccount, ucMessage.user.resource,
                    (long)ucMessage.group.topicId, ucMessage.payload, ucMessage.bizType, ucMessage.timestamp));
            unlimitedGroupMessageTimeoutEvent(this, eventArgs);
        }

        /// <summary>
        /// 注册获取Token接口
        /// </summary>
        /// <param name="tokenFetcher">传入实现IMIMCTokenFetcher的实现类</param>
        public void RegisterMIMCTokenFetcher(IMIMCTokenFetcher tokenFetcher)
        {
            this.tokenFetcher = tokenFetcher;
        }

        private void SetResource(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = Environment.CurrentDirectory + "\\cache\\";
                    if (Directory.Exists(path))
                    {
                        //logger.DebugFormat("user cache {0} Folder already exists, skip creation.", path);
                    }
                    else
                    {
                        Directory.CreateDirectory(path);
                        //logger.DebugFormat("user cache {0} Folder does not exist, create success.", path);
                    }
                }

                string cacheFile = path + appAccount + ".txt";
                JsonSerializer serializer = new JsonSerializer();
                String result = MIMCUtil.Deserialize<String>(cacheFile);
                var currentData = (JObject) JsonConvert.DeserializeObject(result == null ? "" : result);
                currentData = currentData == null ? new JObject() : currentData;
                logger.DebugFormat("userCache before appAccount:{0},resource:{1}", appAccount, this.resource);
                if (!currentData.ContainsKey("resource"))
                {
                    currentData.Add("resource", this.resource);
                    logger.InfoFormat("user cache add：{0}-{1}", appAccount, this.resource);
                    MIMCUtil.SerializeToFile(currentData, cacheFile);
                }
                else
                {
                    this.resource = (String) currentData.GetValue("resource");
                    logger.InfoFormat("user cache read：{0}-{1}", appAccount, currentData.GetValue("resource"));
                }

                logger.DebugFormat("user cache currentData after ,appAccount:{0},resource:{1}", appAccount,
                    this.resource);
            }
            catch (Exception e)
            {
                logger.DebugFormat("SetResource fail! read or write file error! path:{0}，error：{1}", path, e.StackTrace);
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
            if (tokenStr == null || tokenStr.Length == 0)
            {
                return false;
            }

            return LoginRule(tokenStr);
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <returns>bool</returns>
        public async Task<bool> LoginAsync()
        {
            String tokenStr = await tokenFetcher.FetchTokenAsync();
            if (tokenStr == null || tokenStr.Length == 0)
            {
                return false;
            }

            return LoginRule(tokenStr);
        }

        private bool LoginRule(String tokenStr)
        {
            logger.InfoFormat("{0} Start Login Request...", this.appAccount);

            if (string.IsNullOrEmpty(tokenStr))
            {
                logger.WarnFormat(
                    "{0} Login fail FetchToken fail，tokenStr IsNullOrEmpty，Please make sure has register tokenFetcher and Implement methods in the interface. ",
                    this.appAccount);
                return false;
            }

            logger.DebugFormat("{0} Login tokenStr {1}", this.appAccount, tokenStr);

            JObject jo = (JObject) JsonConvert.DeserializeObject(tokenStr);
            string code = jo.GetValue("code").ToString();
            if (string.IsNullOrEmpty(code))
            {
                logger.WarnFormat("{0} Login fail code IsNullOrEmpty", this.appAccount);
                return false;
            }

            if (!code.Equals("200"))
            {
                logger.WarnFormat("{0} Login fail code {1}", this.appAccount, code);
                return false;
            }

            logger.InfoFormat("{0} Login FetchToken sucesss", this.appAccount);

            JObject data = (JObject) jo.GetValue("data");
            this.appId = long.Parse(data.GetValue("appId").ToString());
            this.appPackage = data.GetValue("appPackage").ToString();
            this.chid = Convert.ToInt32(data.GetValue("miChid"));
            this.uuid = long.Parse(data.GetValue("miUserId").ToString());
            this.SecurityKey = data.GetValue("miUserSecurityKey").ToString();
            logger.DebugFormat("{0} get token SecurityKey :{1}", this.appAccount, this.SecurityKey);

            if (!this.appAccount.Equals(data.GetValue("appAccount").ToString()))
            {
                logger.WarnFormat("{0} Login fail appAccount does  not match {1}!={2}", this.appAccount,
                    this.appAccount, data.GetValue("appAccount").ToString());
                return false;
            }

            if (string.IsNullOrEmpty(data.GetValue("token").ToString()))
            {
                logger.DebugFormat("{0} Login fail token IsNullOrEmpty uuid:{1}", this.appAccount, this.uuid);
                return false;
            }

            logger.InfoFormat("{0} Login success uuid:{1}", this.appAccount, this.uuid);

            this.token = data.GetValue("token").ToString();

            if (queryUnlimitedGroupsThread == null || !queryUnlimitedGroupsThread.isAlive())
            {
                queryUnlimitedGroupsThread = new QueryUnlimitedGroupsThread(this);
                queryUnlimitedGroupsThread.Start();
            }
            return true;
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>bool</returns>
        public bool Logout()
        {
            if (this.Status == Constant.OnlineStatus.Offline)
            {
                logger.WarnFormat("Logout FAIL SENDPACKET:{0}, FAIL_FOR_NOT_ONLINE,CHID:{1}, UUID:{2}",
                    Constant.CMD_UNBIND, this.chid, this.uuid);
                return false;
            }

            V6Packet v6Packet = MIMCUtil.BuildUnBindPacket(this);
            PacketWrapper packetWrapper = new PacketWrapper(Constant.MIMC_C2S_DOUBLE_DIRECTION, v6Packet);
            this.connection.PacketWaitToSend.Enqueue(packetWrapper);

            return true;
        }

        public void Destroy()
        {
            logger.InfoFormat("{0} destroy", this.appAccount);
            exit = true;
            if (writeThread != null)
            {
                writeThread.Interrupt();
                writeThread = null;
            }

            if (receiveThread != null)
            {
                receiveThread.Interrupt();
                receiveThread = null;
            }

            if (triggerThread != null)
            {
                triggerThread.Interrupt();
                triggerThread = null;
            }
            if (queryUnlimitedGroupsThread != null)
            {
                queryUnlimitedGroupsThread.Exit = true;
                queryUnlimitedGroupsThread = null;
            }
            this.connection.Close();
            timeoutPackets.Clear();
            ucAckSequenceSet.Clear();
            p2pAckSequenceSet.Clear();
            p2tAckSequenceSet.Clear();
        }

        private void ThreadWrite()
        {
            logger.InfoFormat("{0} ThreadWrite started", this.appAccount);
            if (connection == null)
            {
                logger.WarnFormat("MIMCConnection is null, ThreadWrite not started");
                return;
            }

            string msgType = Constant.MIMC_C2S_DOUBLE_DIRECTION;
            while (!exit)
            {
                try {
                    V6Packet v6Packet = null;
                    if (connection.ConnState == MIMCConnection.State.NOT_CONNECTED)
                    {
                        while (Token == null) {
                            Thread.Sleep(1);
                        }
                        long currentTime = MIMCUtil.CurrentTimeMillis();
                        if (currentTime - this.LastCreateConnTimestamp <= Constant.CONNECT_TIMEOUT)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        LastCreateConnTimestamp = MIMCUtil.CurrentTimeMillis();

                        if (!connection.Connect())
                        {
                            logger.WarnFormat("{0} MIMCConnection fail, host:{1}-{2}", this.appAccount, connection.Host,
                                connection.Port);
                            continue;
                        }

                        logger.DebugFormat("{0} connection success, host:{1}-{2}", this.appAccount, connection.Host,
                            connection.Port);

                        connection.ConnState = MIMCConnection.State.SOCK_CONNECTED;
                        this.LastCreateConnTimestamp = 0;
                        v6Packet = MIMCUtil.BuildConnectionPacket(this);
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
                        if (this.Status == Constant.OnlineStatus.Offline &&
                            currentTime - this.LastLoginTimestamp <= Constant.LOGIN_TIMEOUT)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        if (this.Status == Constant.OnlineStatus.Offline
                            && currentTime - this.LastLoginTimestamp > Constant.LOGIN_TIMEOUT)
                        {
                            v6Packet = MIMCUtil.BuildBindPacket(this);
                            if (v6Packet == null)
                            {
                                Thread.Sleep(100);
                                continue;
                            }

                            this.LastLoginTimestamp = MIMCUtil.CurrentTimeMillis();
                        }

                        if (this.Status == Constant.OnlineStatus.Online)
                        {
                            long now = MIMCUtil.CurrentTimeMillis();
                            if (now - this.LastUcPingTimestamp1 > Constant.UC_PING_TIMEVAL_MS)
                            {
                                if (ucTopics != null && ucTopics.Count > 0)
                                {
                                    logger.DebugFormat("appAccount:{0} LastUcPingTimestamp:{1},now:{2},timediv:{3}",
                                        this.appAccount, this.LastUcPingTimestamp1, now, now - this.LastUcPingTimestamp1);

                                    UCPacket ucPacket = BuildUcPingPacket(this);
                                    String packetId = MIMCUtil.CreateMsgId(this);
                                    if (SendUCPacket(packetId, ucPacket))
                                    {
                                        logger.DebugFormat(
                                            "---> Send UcPing Packet sucess,{0} packetId:{1}, lastUcPingTimestamp:{2}",
                                            this.appAccount, packetId, this.LastUcPingTimestamp1);
                                    }
                                    else
                                    {
                                        logger.WarnFormat(
                                            "---> Send UcPing Packet fail,{0} packetId:{1},lastUcPingTimestamp:{2}",
                                            this.appAccount, packetId, this.LastUcPingTimestamp1);
                                    }
                                }
                            }

                            PacketWrapper packetWrapper;
                            bool ret = this.connection.PacketWaitToSend.TryDequeue(out packetWrapper);
                            if (!ret)
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

                            if (packetWrapper != null)
                            {
                                msgType = packetWrapper.Type;
                                v6Packet = (V6Packet)packetWrapper.Content;
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

                    byte[] data = V6PacketEncoder.Encode(this.connection, v6Packet);
                    if (data == null)
                    {
                        logger.ErrorFormat("connection.reset reason: V6PacketEncoder.Encode fail data is null");
                        continue;
                    }

                    this.LastPingTimestamp = MIMCUtil.CurrentTimeMillis();
                    try
                    {
                        this.connection.TcpConnection.GetStream().Write(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorFormat("Write exception:{0}", ex.StackTrace);
                        this.connection.Reset();
                        continue;
                    }
                } catch(Exception ex)
                {
                    logger.ErrorFormat("ThreadWrite:{0}", ex.StackTrace);
                }
            }
        }

        private void ThreadReceive()
        {
            logger.InfoFormat("{0} ThreadReceive started", this.appAccount);

            if (this.connection == null)
            {
                logger.WarnFormat("ThreadReceive, MIMCConnection is null, ThreadReceive not started");
                return;
            }

            while (!exit)
            {
                try
                {
                    if (this.connection.ConnState == MIMCConnection.State.NOT_CONNECTED)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    NetworkStream stream = this.connection.TcpConnection.GetStream();
                    byte[] headerBins = new byte[Constant.V6_HEAD_LENGTH];
                    int readLength = MIMCConnection.Readn(stream, headerBins, Constant.V6_HEAD_LENGTH);
                    if (readLength != Constant.V6_HEAD_LENGTH)
                    {
                        logger.WarnFormat("ThreadReceive connection.reset V6_HEAD not equal{0}!={1}",
                            Constant.V6_HEAD_LENGTH, readLength);
                        this.connection.Reset();
                        continue;
                    }

                    char magic = V6PacketDecoder.GetChar(headerBins, 0);
                    if (magic != Constant.MAGIC)
                    {
                        logger.WarnFormat("ThreadReceive connection.reset V6_MAGIC not equal {0}!={1}", magic,
                            Constant.MAGIC);
                        this.connection.Reset();
                        continue;
                    }

                    char version = V6PacketDecoder.GetChar(headerBins, 2);
                    if (version != Constant.V6_VERSION)
                    {
                        logger.WarnFormat("ThreadReceive  connection.reset V6_VERSION not equal {0}!={1}", version,
                            Constant.V6_VERSION);
                        this.connection.Reset();
                        continue;
                    }

                    uint bodyLen = V6PacketDecoder.GetUint(headerBins, 4);

                    if (bodyLen < 0)
                    {
                        logger.WarnFormat("ThreadReceive  connection.reset V6_BODY, bodyLen={0}", bodyLen);
                        this.connection.Reset();
                        continue;
                    }

                    byte[] bodyBins = new byte[bodyLen];
                    if (bodyLen > 0)
                    {
                        int v6BodyBytesRead = MIMCConnection.Readn(stream, bodyBins, Convert.ToInt32(bodyLen));
                        if (v6BodyBytesRead != bodyLen)
                        {
                            logger.WarnFormat("ThreadReceive  connection.reset V6_BODY, {0}!={1}", v6BodyBytesRead,
                                bodyLen);
                            this.connection.Reset();
                            continue;
                        }
                    }

                    byte[] crcBins = new byte[Constant.CRC_LEN];
                    int crcBytesRead = MIMCConnection.Readn(stream, crcBins, Constant.CRC_LEN);
                    if (crcBytesRead != Constant.CRC_LEN)
                    {
                        logger.WarnFormat("ThreadReceive  connection.reset V6_CRC, {0}!={1}", crcBytesRead,
                            Constant.CRC_LEN);
                        this.connection.Reset();
                        continue;
                    }

                    this.connection.ClearNextResetSockTimestamp();
                    V6Packet feV6Packet =
                        V6PacketDecoder.DecodeV6(headerBins, bodyBins, crcBins, this.connection.Rc4Key, this);
                    if (feV6Packet == null)
                    {
                        logger.DebugFormat("ThreadReceive  connection.reset V6Packet Decode fail!");
                        this.connection.Reset();
                        continue;
                    }

                    if (feV6Packet.V6BodyBin == null || feV6Packet.V6BodyBin.Length == 0 || feV6Packet.PacketLen == 0)
                    {
                        logger.InfoFormat("<--- receive v6 packet ping-pong packet. appAccount:{0}", this.appAccount);
                        continue;
                    }

                    logger.InfoFormat("<--- receive v6 packet cmd:{0}, appAccount:{1}", feV6Packet.Body.ClientHeader.cmd,
                        this.appAccount);
                    if (Constant.CMD_CONN == feV6Packet.Body.ClientHeader.cmd)
                    {
                        XMMsgConnResp connResp = null;
                        using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
                        {
                            connResp = Serializer.Deserialize<XMMsgConnResp>(ms);
                        }

                        if (null == connResp)
                        {
                            logger.WarnFormat("ThreadReceive  connection.reset, cmd:{0}, ConnResp is null",
                                feV6Packet.Body.ClientHeader.cmd);
                            this.connection.Reset();
                            continue;
                        }

                        this.connection.ConnState = MIMCConnection.State.HANDSHAKE_CONNECTED;
                        this.connection.Challenge = connResp.challenge;
                        this.connection.SetChallengeAndRc4Key(connResp.challenge);
                        continue;
                    }

                    if (Constant.CMD_BIND == feV6Packet.Body.ClientHeader.cmd)
                    {
                        XMMsgBindResp xMMsgBindResp = null;
                        using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
                        {
                            xMMsgBindResp = Serializer.Deserialize<XMMsgBindResp>(ms);
                        }

                        this.Status = xMMsgBindResp.result ? Constant.OnlineStatus.Online : Constant.OnlineStatus.Offline;
                        this.HandleStateChange(new StateChangeEventArgs(xMMsgBindResp.result, xMMsgBindResp.error_type, xMMsgBindResp.error_reason, xMMsgBindResp.error_desc));
                        // invalid-token
                        ClearToken(this, xMMsgBindResp);
                        continue;
                    }

                    if (Constant.CMD_PING == feV6Packet.Body.ClientHeader.cmd)
                    {
                        MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload);
                        XMMsgPing xMMsgPing = Serializer.Deserialize<XMMsgPing>(ms);


                        logger.InfoFormat("<--- receive v6 packet cmd:Short-Ping-Pong appAccount:{0}", this.appAccount);
                        continue;
                    }

                    if (Constant.CMD_KICK == feV6Packet.Body.ClientHeader.cmd)
                    {
                        this.logoutFlag = true;
                        logger.InfoFormat("appAccount:{0} logout.", this.appAccount);
                        this.HandleStateChange(new StateChangeEventArgs(false, "KICK", "KICK", "KICK"));
                        continue;
                    }

                    if (Constant.CMD_SECMSG == feV6Packet.Body.ClientHeader.cmd)
                    {
                        userHandler.HandleSecMsg(this, feV6Packet);
                        continue;
                    }
                } catch(Exception ex)
                {
                    logger.ErrorFormat("ThreadReceive:{0}", ex.StackTrace);
                }
            }
        }

        private void ThreadTrigger()
        {
            logger.InfoFormat("{0} ThreadTrigger started", this.appAccount);

            if (this.connection == null)
            {
                logger.WarnFormat("ThreadTrigger fail,conn is null");
                return;
            }

            while (!exit)
            {
                try
                {
                    long currentTime = MIMCUtil.CurrentTimeMillis();
                    if (connection.NextResetSockTimestamp > 0 && currentTime - connection.NextResetSockTimestamp > 0)
                    {
                        logger.WarnFormat(
                            "ThreadTrigger  connection.reset currentTime:{0},connection.NextResetSockTimestamp:{1}",
                            currentTime, connection.NextResetSockTimestamp);
                        connection.Reset();
                    }

                    ScanAndCallBack();
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("ThreadTrigger:{0}", ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// 用于超时检测通知和重连
        /// </summary>
        private void ScanAndCallBack()
        {
            foreach (KeyValuePair<string, TimeoutPacket> item in timeoutPackets)
            {
                logger.DebugFormat("{0} ThreadCallback  timeoutPackets size:{1}", this.appAccount,
                    timeoutPackets.Count);
                TimeoutPacket timeoutPacket = item.Value;
                if (MIMCUtil.CurrentTimeMillis() - timeoutPacket.Timestamp < Constant.CHECK_TIMEOUT_TIMEVAL_MS)
                {
                    continue;
                }

                MIMCPacket mimcPacket = (MIMCPacket)timeoutPacket.Content;
                if (mimcPacket.type == MIMC_MSG_TYPE.P2P_MESSAGE)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        MIMCP2PMessage p2p = Serializer.Deserialize<MIMCP2PMessage>(ms);

                        P2PMessage p2pMessage = new P2PMessage(mimcPacket.packetId, mimcPacket.sequence,
                            p2p.from.appAccount, p2p.from.resource, p2p.to.appAccount, p2p.to.resource, p2p.payload, p2p.bizType, mimcPacket.timestamp);
                        HandleSendMessageTimeout(p2pMessage);
                        logger.DebugFormat("{0} ThreadCallback SendMessageTimeout packetId:{1}", this.appAccount,
                            p2pMessage.PacketId);
                    }
                } else if (mimcPacket.type == MIMC_MSG_TYPE.P2T_MESSAGE)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        MIMCP2TMessage p2t = Serializer.Deserialize<MIMCP2TMessage>(ms);

                        P2TMessage p2tMessage = new P2TMessage(mimcPacket.packetId, mimcPacket.sequence,
                            p2t.from.appAccount, p2t.from.resource, (long)p2t.to.topicId, p2t.payload, p2t.bizType, mimcPacket.timestamp);
                        HandleSendGroupMessageTimeout(p2tMessage);
                        logger.DebugFormat("{0} ThreadCallback SendGroupMessageTimeout packetId:{1}", this.appAccount,
                            p2tMessage.PacketId);
                    }
                } else if (mimcPacket.type == MIMC_MSG_TYPE.UC_PACKET)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        UCPacket ucPacket = Serializer.Deserialize<UCPacket>(ms);

                        if (ucPacket.type == UC_MSG_TYPE.MESSAGE_LIST)
                        {
                            HandleSendUnlimitedGroupMessageTimeout(ucPacket);
                        }

                        logger.DebugFormat(
                            "{0} ThreadCallback SendUnlimitedMessageTimeout packetId:{1},type:{2},ucType{3}",
                            this.appAccount, mimcPacket.packetId, mimcPacket.type, ucPacket.type);
                    }
                }

                if (timeoutPackets.TryRemove(mimcPacket.packetId, out timeoutPacket))
                {
                    logger.DebugFormat("{0} ScanAndCallBack timeoutPackets TryRemove sucess, packetId:{1}",
                        this.appAccount, mimcPacket.packetId);
                }
                else
                {
                    logger.WarnFormat("{0} ScanAndCallBack timeoutPackets TryRemove fail,packetId:{1}", this.appAccount,
                        mimcPacket.packetId);
                }
            }
        }


        public bool IsOnline()
        {
            logger.DebugFormat("{0} IsOnline :{1}", this.appAccount, this.Status == Constant.OnlineStatus.Online);
            return this.Status == Constant.OnlineStatus.Online;
        }

        public string SendMessage(string toAppAccount, byte[] payload, string bizType)
        {
            if (string.IsNullOrEmpty(toAppAccount) || payload == null || payload.Length == 0 ||
                payload.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("SendMessage fail!,toAppAccount:{0},msg :{1}", toAppAccount, payload);
                return null;
            }

            return SendMessage(toAppAccount, payload, bizType, true);
        }

        /// <summary>
        /// 发送单聊消息
        /// </summary>
        /// <param name="toAppAccount">消息接收者在APP帐号系统内的唯一帐号ID</param>
        /// <param name="payload">开发者自定义消息体，二级制数组格式</param>
        /// <param name="bizType">消息类型</param>
        /// <param name="isStore">消息是否存储在MIMC服务端，true存储, false不存储, 默认存储</param>
        /// <returns>packetId客户端生成的消息ID</returns>
        public string SendMessage(string toAppAccount, byte[] payload, string bizType, bool isStore)
        {
            if (string.IsNullOrEmpty(toAppAccount) || payload == null || payload.Length == 0 ||
                payload.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("SendMessage fail!,toAppAccount:{0},msg :{1}", toAppAccount, payload);
                return null;
            }

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;

            global::mimc.MIMCUser toUser = new global::mimc.MIMCUser();
            toUser.appId = (ulong)appId;
            toUser.appAccount = toAppAccount;

            MIMCP2PMessage p2pMessage = new MIMCP2PMessage();
            p2pMessage.from = fromUser;
            p2pMessage.to = toUser;
            p2pMessage.payload = payload;
            p2pMessage.isStore = isStore;
            p2pMessage.bizType = bizType;

            MIMCPacket packet = new MIMCPacket();
            String packetId = MIMCUtil.CreateMsgId(this);
            packet.packetId = packetId;
            packet.package = appPackage;
            packet.type = MIMC_MSG_TYPE.P2P_MESSAGE;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, p2pMessage);
                byte[] p2pPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;
                packet.payload = p2pPacket;
            }

            packet.timestamp = MIMCUtil.CurrentTimeMillis();
            byte[] packetBin = null;
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, packet);
                packetBin = stream.ToArray();
                stream.Flush();
                stream.Position = 0;
            }

            if (SendPacket(packetId, packetBin, Constant.MIMC_C2S_DOUBLE_DIRECTION))
            {
                TimeoutPacket timeoutPacket = new TimeoutPacket(packet, MIMCUtil.CurrentTimeMillis());
                if (timeoutPackets.TryAdd(packetId, timeoutPacket))
                {
                    logger.DebugFormat("{0} timeoutPackets TryAdd sucess,packetId:{1}", this.appAccount, packetId);
                }
                else
                {
                    logger.WarnFormat("{0} timeoutPackets TryAdd fail,packetId:{1}", this.appAccount, packetId);
                }

                logger.DebugFormat("{0} SendPacket timeoutPackets size:{1}", this.appAccount, timeoutPackets.Count);

                return packetId;
            }

            return null;
        }

        public string SendGroupMessage(long topicId, byte[] payload, string bizType)
        {
            if (topicId == 0 || payload == null || payload.Length == 0 ||
                payload.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("{0} SendGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount,
                    topicId, payload, (long)payload.Length);
                return null;
            }

            return SendGroupMessage(topicId, payload, bizType, true);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="payload">开发者自定义消息体，二级制数组格式</param>
        /// <param name="bizType">消息类型</param>
        /// <param name="isStore">消息是否存储在MIMC服务端，true存储, false不存储, 默认存储</param>
        /// <returns></returns>
        public string SendGroupMessage(long topicId, byte[] payload, string bizType, bool isStore)
        {
            if (topicId == 0 || payload == null || payload.Length == 0 ||
                payload.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("{0} SendGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount,
                    topicId, payload, (long) payload.Length);
                return null;
            }

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;

            global::mimc.MIMCGroup to = new global::mimc.MIMCGroup();
            to.appId = (ulong)appId;
            to.topicId = (ulong)topicId;

            MIMCP2TMessage p2tMessage = new MIMCP2TMessage();
            p2tMessage.from = fromUser;
            p2tMessage.to = to;
            p2tMessage.payload = payload;
            p2tMessage.isStore = isStore;
            p2tMessage.bizType = bizType;

            MIMCPacket packet = new MIMCPacket();
            String packetId = MIMCUtil.CreateMsgId(this);
            packet.packetId = packetId;
            packet.package = appPackage;
            packet.type = MIMC_MSG_TYPE.P2T_MESSAGE;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, p2tMessage);
                byte[] p2tPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;

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
            }

            if (SendPacket(packetId, packetBin, Constant.MIMC_C2S_DOUBLE_DIRECTION))
            {
                TimeoutPacket timeoutPacket = new TimeoutPacket(packet, MIMCUtil.CurrentTimeMillis());
                if (timeoutPackets.TryAdd(packetId, timeoutPacket))
                {
                    logger.DebugFormat("{0} timeoutPackets TryAdd sucess,packetId:{1}", this.appAccount, packetId);
                }
                else
                {
                    logger.WarnFormat("{0} timeoutPackets TryAdd fail,packetId:{1}", this.appAccount, packetId);
                }

                logger.DebugFormat("{0} SendPacket timeoutPackets size:{1}", this.appAccount, timeoutPackets.Count);
                return packetId;
            }

            return null;
        }

        /// <summary>
        /// 创建无限大群
        /// </summary>
        /// <returns></returns>
        public string CreateUnlimitedGroup(String topicName)
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            logger.DebugFormat("{0} CreateUnlimitedGroup uuid:{1}", appAccount, this.uuid.ToString());
            parameters.Add("ownerUuid", this.uuid.ToString());
            parameters.Add("topicName", topicName);
            string output = JsonConvert.SerializeObject(parameters);
            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);

            HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(Constant.CREATE_UNLIMITE_CHAT_URL,
                output, null, null, Encoding.UTF8, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            JObject jo = (JObject) JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("CreateUnlimitedGroup error:{0}", content);
                return null;
            }

            JObject data = (JObject) jo.GetValue("data");
            logger.DebugFormat("CreateUnlimitedGroup success content:{0},topicId:{1}", content,
                data.GetValue("topicId").ToString());

            this.JoinUnlimitedGroup(long.Parse(data.GetValue("topicId").ToString()));

            reader.Close();
            myResponse.Close();
            return data.GetValue("topicId").ToString();
        }

        /// <summary>
        /// 解散无限大群
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public bool DismissUnlimitedGroup(long topicId)
        {
            logger.DebugFormat("{0} DismissUnlimitedGroup uuid:{1}", appAccount, this.uuid.ToString());

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);
            headers.Add("topicId", topicId.ToString());

            HttpWebResponse myResponse =
                HttpWebResponseUtil.CreateDeleteHttpResponse(Constant.DISMISS_UNLIMITE_CHAT_URL, null, null, null,
                    Encoding.UTF8, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("DismissUnlimitedGroup :{0}", content);

            JObject jo = (JObject) JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            string message = jo.GetValue("message").ToString();
            reader.Close();
            myResponse.Close();
            if (code == "200" && message == "success")
            {
                logger.DebugFormat("{0} DismissUnlimitedGroup sucesss:{1}", this.appAccount, content);
                return true;
            }
            else
            {
                logger.DebugFormat("DismissUnlimitedGroup error:{0}", content);
                return false;
            }
        }

        /// <summary>
        /// 获取无限大群人数
        /// </summary>
        /// <returns></returns>
        public string GetUnlimitedGroupUsersNum(long topicId)
        {
            logger.DebugFormat("{0} CreateUnlimitedGroup uuid:{1}", appAccount, this.uuid.ToString());
            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);
            headers.Add("topicId", topicId.ToString());

            HttpWebResponse myResponse =
                HttpWebResponseUtil.CreateGetHttpResponse(Constant.QUERY_UNLIMITE_CHAT_ONLINE_INFO_URL, null, null,
                    null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            JObject jo = (JObject) JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("GetUnlimitedGroupUsersNum error:{0}", content);
                return null;
            }

            JObject data = (JObject) jo.GetValue("data");
            logger.DebugFormat("GetUnlimitedGroupUsersNum success content:{0},topicId:{1}", content,
                data.GetValue("topicId").ToString());

            reader.Close();
            myResponse.Close();
            return data.ToString();
        }

        /// <summary>
        /// 获取无限大群用户列表
        /// </summary>
        /// <returns></returns>
        public string GetUnlimitedGroupUsers(long topicId)
        {
            logger.DebugFormat("{0} CreateUnlimitedGroup uuid:{1}", appAccount, this.uuid.ToString());
            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);
            headers.Add("topicId", topicId.ToString());

            HttpWebResponse myResponse =
                HttpWebResponseUtil.CreateGetHttpResponse(Constant.QUERY_UNLIMITE_CHAT_USERLIST_URL, null, null, null,
                    headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("GetUnlimitedGroupUsers content:{0}", content);

            JObject jo = (JObject) JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("GetUnlimitedGroupUsers error:{0}", content);
                return null;
            }

            JObject data = (JObject) jo.GetValue("data");
            reader.Close();
            myResponse.Close();
            return data.ToString();
        }

        /// <summary>
        /// 获取加入的无限大群列表
        /// </summary>
        /// <returns></returns>
        public bool QueryUnlimitedGroups()
        {
            logger.DebugFormat("{0} QueryUnlimitedGroups uuid:{1}", appAccount, this.uuid.ToString());

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);

            HttpWebResponse myResponse =
                HttpWebResponseUtil.CreateGetHttpResponse(Constant.QUERY_UNLIMITE_CHAT_URL, null, null, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("QueryUnlimitedGroups :{0}", content);

            JObject jo = (JObject) JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            string message = jo.GetValue("message").ToString();
            reader.Close();
            myResponse.Close();
            if (code == "200" && message == "success")
            {
                //设置转化JSON格式时字段长度
                List<long> topicIdList = JsonConvert.DeserializeObject<List<long>>(jo.GetValue("data").ToString());
                if (topicIdList.Count == 0)
                {
                    return false;
                }

                this.ucTopics = topicIdList;
                logger.DebugFormat("{0} QueryUnlimitedGroups sucesss:ucTopics.Count {1}", this.appAccount,
                    ucTopics.Count);
                return true;
            }
            else
            {
                logger.DebugFormat("{0} QueryUnlimitedGroups error:{1}", this.appAccount, content);
                return false;
            }
        }

        /// <summary>
        /// 加入无限大群
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public string JoinUnlimitedGroup(long topicId)
        {
            if (topicId == 0)
            {
                logger.WarnFormat("{0} JoinUnlimitedGroup fail,topicId:{1}", this.appAccount, topicId);
                return null;
            }

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;

            UCGroup to = new UCGroup();
            to.appId = (ulong)appId;
            to.topicId = (ulong)topicId;
            String packetId = MIMCUtil.CreateMsgId(this);

            UCJoin join = new UCJoin();
            join.group = to;

            UCPacket ucPacket = new UCPacket();
            ucPacket.user = fromUser;
            ucPacket.type = UC_MSG_TYPE.JOIN;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, join);
                byte[] tempPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;

                ucPacket.payload = tempPacket;
            }

            ucPacket.packetId = packetId;

            if (SendUCPacket(packetId, ucPacket))
            {
                logger.DebugFormat("{0} JoinUnlimitedGroup sucess,packetId:{1}", this.appAccount, packetId);
            }
            else
            {
                logger.DebugFormat("{0} JoinUnlimitedGroup fail,packetId:{1}", this.appAccount, packetId);
            }

            return packetId;
        }

        /// <summary>
        /// 离开无限大群
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public string QuitUnlimitedGroup(long topicId)
        {
            if (topicId == 0)
            {
                logger.WarnFormat("{0} QuitUnlimitedGroup fail,topicId:{1}", this.appAccount, topicId);
                return null;
            }

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;

            UCGroup to = new UCGroup();
            to.appId = (ulong)appId;
            to.topicId = (ulong)topicId;
            String packetId = MIMCUtil.CreateMsgId(this);
            UCQuit quit = new UCQuit();
            quit.group = to;

            UCPacket ucPacket = new UCPacket();
            ucPacket.user = fromUser;
            ucPacket.type = UC_MSG_TYPE.QUIT;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, quit);
                byte[] tempPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;

                ucPacket.payload = tempPacket;
            }

            ucPacket.packetId = packetId;

            if (SendUCPacket(packetId, ucPacket))
            {
                logger.DebugFormat("{0} QuitUnlimitedGroup sucess,packetId:{1}", this.appAccount, packetId);
            }
            else
            {
                logger.DebugFormat("{0} QuitUnlimitedGroup fail,packetId:{1}", this.appAccount, packetId);
            }

            return packetId;
        }

        public string SendUnlimitedGroupMessage(long topicId, byte[] payload, string bizType)
        {
            if (topicId == 0 || payload == null || payload.Length == 0 ||
                payload.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("{0} SendUnlimitedGroupMessage fail, topicId:{1} msg:{2} msg.Length:{3}", this.appAccount, topicId, payload, (long)payload.Length);
                return null;
            }

            return SendUnlimitedGroupMessage(topicId, payload, bizType, true);
        }

        /// <summary>
        /// 发送无限大群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="payload">开发者自定义消息体，二级制数组格式</param>
        /// <param name="bizType">消息类型</param>
        /// <param name="isStore">消息是否存储在MIMC服务端，true存储, false不存储, 默认存储</param>
        /// <returns></returns>
        public string SendUnlimitedGroupMessage(long topicId, byte[] payload, string bizType, bool isStore)
        {
            if (topicId == 0 || payload == null || payload.Length == 0 ||
                payload.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("{0} SendUnlimitedGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}",
                    this.appAccount, topicId, payload, (long) payload.Length);
                return null;
            }

            logger.DebugFormat("{0} start SendUnlimitedGroupMessage,topicId:{1},msg:{2},msg.Length:{3}",
                this.appAccount, topicId, payload, (long) payload.Length);

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;

            UCGroup to = new UCGroup();
            to.appId = (ulong)appId;
            to.topicId = (ulong)topicId;
            String packetId = MIMCUtil.CreateMsgId(this);

            UCMessage message = new UCMessage();
            message.user = fromUser;
            message.group = to;
            message.payload = payload;
            message.packetId = packetId;
            message.bizType = bizType;
            message.isStore = isStore;


            UCPacket ucPacket = new UCPacket();
            ucPacket.user = fromUser;
            ucPacket.type = UC_MSG_TYPE.MESSAGE;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, message);
                byte[] tempPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;

                ucPacket.payload = tempPacket;
            }

            ucPacket.packetId = packetId;

            if (SendUCPacket(packetId, ucPacket))
            {
                logger.DebugFormat("{0} SendUnlimitedGroupMessage sucess,packetId:{1}", this.appAccount, packetId);
            }
            else
            {
                logger.DebugFormat("{0} SendUnlimitedGroupMessage fail,packetId:{1}", this.appAccount, packetId);
            }

            return packetId;
        }

        internal UCPacket BuildUcPingPacket(MIMCUser user)
        {
            logger.DebugFormat("{0} User BuildUcPingPacket", user.AppAccount);

            if (string.IsNullOrEmpty(user.Token))
            {
                logger.WarnFormat("{0} User BuildUcPingPacket fail Token is null,wait ...", user.AppAccount);
                return null;
            }

            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_SECMSG, Constant.CIPHER_NONE,
                MIMCUtil.CreateMsgId(user));

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;

            UCPacket ucPacket = new UCPacket();
            ucPacket.type = UC_MSG_TYPE.PING;
            ucPacket.user = fromUser;

            UCPing ucPing = new UCPing();
            List<UCGroup> groups = new List<UCGroup>();
            foreach (ulong topicId in ucTopics)
            {
                UCGroup group = new UCGroup();
                group.appId = (ulong)appId;
                group.topicId = topicId;
                groups.Add(group);
            }

            ucPing.group.AddRange(groups);

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, ucPing);
                byte[] payload = ms.ToArray();
                ucPacket.payload = payload;
            }

            return ucPacket;
        }

        internal UCPacket BuildUcSeqAckPacket(MIMCUser user, UCGroup group, long maxSequence)
        {
            logger.DebugFormat("{0} User BuildUcSeqAckPacket", user.AppAccount);

            if (string.IsNullOrEmpty(user.Token))
            {
                logger.WarnFormat("{0} User BuildUcSeqAckPacket fail Token is null,wait ...", user.AppAccount);
                return null;
            }

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = (ulong)appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = (ulong)uuid;
            fromUser.resource = resource;


            UCPacket ucPacket = new UCPacket();
            ucPacket.user = fromUser;
            ucPacket.type = UC_MSG_TYPE.SEQ_ACK;

            UCSequenceAck ucSeqAck = new UCSequenceAck();
            ucSeqAck.group = group;
            ucSeqAck.sequence = maxSequence;
            logger.DebugFormat("ucSeqAck.sequence:{0}, messageList.maxSequence:{1}", ucSeqAck.sequence, maxSequence);

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, ucSeqAck);
                byte[] payload = ms.ToArray();
                ucPacket.payload = payload;
            }

            return ucPacket;
        }


        private bool SendUCPacket(String packetId, UCPacket ucPacket)
        {
            logger.DebugFormat("{0} SendUCPacket ,packetId:{1}", this.appAccount, packetId);
            this.LastUcPingTimestamp1 = MIMCUtil.CurrentTimeMillis();

            MIMCPacket mimcPacket = new MIMCPacket();
            mimcPacket.packetId = packetId;
            mimcPacket.package = appPackage;
            mimcPacket.type = MIMC_MSG_TYPE.UC_PACKET;
            ucPacket.packetId = packetId;

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, ucPacket);
                byte[] tempPacket = ms.ToArray();
                ms.Flush();
                ms.Position = 0;

                mimcPacket.payload = tempPacket;
            }

            mimcPacket.timestamp = MIMCUtil.CurrentTimeMillis();
            byte[] packetBin = null;
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize(stream, mimcPacket);
                packetBin = stream.ToArray();
                stream.Flush();
                stream.Position = 0;
            }

            logger.DebugFormat("--> {0} SendUCPacket ,packetId:{1},type:{2}", this.appAccount, packetId, ucPacket.type);

            if (SendPacket(packetId, packetBin, Constant.MIMC_C2S_DOUBLE_DIRECTION))
            {
                if (ucPacket.type == UC_MSG_TYPE.MESSAGE)
                {
                    TimeoutPacket timeoutPacket = new TimeoutPacket(mimcPacket, MIMCUtil.CurrentTimeMillis());

                    if (timeoutPackets.TryAdd(packetId, timeoutPacket))
                    {
                        logger.DebugFormat("{0} timeoutPackets TryAdd sucess,packetId:{1},type:{2}", this.appAccount,
                            packetId, mimcPacket.type);
                        return true;
                    }
                    else
                    {
                        logger.WarnFormat("{0} timeoutPackets TryAdd fail,packetId:{1},type:{2}", this.appAccount,
                            packetId, mimcPacket.type);
                        return false;
                    }
                }

                return true;
            }

            logger.DebugFormat("{0} SendUCPacket fail!,packetId:{1}", this.appAccount, packetId);
            return false;
        }

        private bool SendPacket(string packetId, byte[] packetBin, string msgType)
        {
            if (string.IsNullOrEmpty(packetId) || packetBin == null || packetBin.Length == 0 ||
                (msgType != Constant.MIMC_C2S_DOUBLE_DIRECTION && msgType != Constant.MIMC_C2S_SINGLE_DIRECTION))
            {
                logger.WarnFormat("SendPacket fail!packetId:{0},packetBin :{1}，msgType：{2}", packetId, packetBin,
                    msgType);
                return false;
            }

            V6Packet v6Packet = MIMCUtil.BuildSecMsgPacket(this, packetId, packetBin);

            PacketWrapper packetWrapper = new PacketWrapper(msgType, v6Packet);
            logger.DebugFormat("SendPacket packetId:{0},resource :{1}，msgType：{2}", packetId, resource, msgType);
            this.connection.PacketWaitToSend.Enqueue(packetWrapper);
            return true;
        }

        /// <summary>
        /// 获取无限大群用户列表
        /// </summary>
        /// <returns></returns>
        public string GetP2PHistory(String toAccount, String fromAccount, long utcFromTime, long utcToTime,
            String bizType, String extra)
        {
            logger.DebugFormat("{0} CreateUnlimitedGroup uuid:{1}", appAccount, this.uuid.ToString());
            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);

            IDictionary<string, string> paramsMap = new Dictionary<string, string>();
            paramsMap.Add("toAccount", toAccount);
            paramsMap.Add("fromAccount", fromAccount);
            paramsMap.Add("utcFromTime", utcFromTime.ToString());
            paramsMap.Add("utcToTime", utcToTime.ToString());
            paramsMap.Add("bizType", bizType);
            paramsMap.Add("extra", extra);
            string output = JsonConvert.SerializeObject(paramsMap);

            HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(Constant.QUERY_P2P_ONTIME_URL,
                output, null, null, Encoding.UTF8, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("GetP2PHistory content:{0}", content);

            JObject jo = (JObject) JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("GetUnlimitedGroupUsers error:{0}", content);
                return null;
            }

            JObject data = (JObject) jo.GetValue("data");
            reader.Close();
            myResponse.Close();
            return data.ToString();
        }

        private void ClearToken(MIMCUser mimcUser, XMMsgBindResp resp)
        {
            if ("invalid-token".Equals(resp.error_reason) || "token-expired".Equals(resp.error_type))
            {
                mimcUser.Token = null;
                mimcUser.LastLoginTimestamp = 0;
            }
        }
    }
}
using System;
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
using mimc.com.xiaomi.mimc.packet;
using mimc.com.xiaomi.mimc.utils;
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
    public delegate void UnlimitedGroupMessageTimeoutEventHandler(object sender, SendUnlimitedGroupMessageTimeoutEventArgs e);
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
        private bool logoutFlag;
        private string securityKey;
        private string token;
        private long lastLoginTimestamp;
        private long lastCreateConnTimestamp;
        private long lastPingTimestamp;
        private long lastUcPingTimestamp;

        private AtomicInteger atomic;
        private ConcurrentDictionary<string, TimeoutPacket> timeoutPackets;
        private IMIMCTokenFetcher tokenFetcher;
        private MIMCUserHandler userHandler;
        private List<long> ucTopics;
        private HashSet<long> ucAckSequenceSet;
        private HashSet<long> p2pAckSequenceSet;
        private HashSet<long> p2tAckSequenceSet;

        public long LastLoginTimestamp { get; set; }
        public long LastCreateConnTimestamp { get; set; }
        public long LastPingTimestamp { get; set; }
        public Constant.OnlineStatus Status { get; set; }
        public string SecurityKey { get; set; }
        public bool AutoLogin { get; set; }
        public string ClientAttrs { get; set; }
        public string CloudAttrs { get; set; }
        public string AppAccount { get => appAccount; set => appAccount = value; }
        public long Uuid { get => uuid; set => uuid = value; }
        public string Resource { get => resource; set => resource = value; }
        internal AtomicInteger Atomic { get => atomic; set => atomic = value; }
        public MIMCConnection Connection { get => connection; set => connection = value; }
        public string Token { get => token; set => token = value; }
        public string AppPackage { get => appPackage; set => appPackage = value; }
        public int Chid { get => chid; set => chid = value; }
        public MIMCUserHandler UserHandler { get => userHandler; set => userHandler = value; }
        public ConcurrentDictionary<string, TimeoutPacket> TimeoutPackets { get => timeoutPackets; set => timeoutPackets = value; }
        public long AppId { get => appId; set => appId = value; }
        public long LastUcPingTimestamp { get => lastUcPingTimestamp; set => lastUcPingTimestamp = value; }
        public HashSet<long> UCAckSequenceSet { get => ucAckSequenceSet; set => ucAckSequenceSet = value; }
        public HashSet<long> P2pAckSequenceSet { get => p2pAckSequenceSet; set => p2pAckSequenceSet = value; }
        public HashSet<long> P2tAckSequenceSet { get => p2tAckSequenceSet; set => p2tAckSequenceSet = value; }


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
            this.logoutFlag = false;
            this.LastLoginTimestamp = 0;
            this.LastCreateConnTimestamp = 0;
            this.LastPingTimestamp = 0;
            this.lastUcPingTimestamp = 0;
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

            Thread writeThread = new Thread(new ThreadStart(ThreadWrite));
            writeThread.Start();
            Thread receiveThread = new Thread(new ThreadStart(ThreadReceive));
            receiveThread.Start();
            Thread triggerThread = new Thread(new ThreadStart(ThreadTrigger));
            triggerThread.Start();
        }

        public void HandleStateChange(bool isOnline, string errType, string errReason, string errDescription)
        {
            if (null == stateChangeEvent)
            {
                logger.WarnFormat("{0} OnStateChange fail StateChangeEvent is null. ", this.appAccount);
                return;
            }
            StateChangeEventArgs eventArgs = new StateChangeEventArgs(this, isOnline, errType, errReason, errDescription);
            //触发事件，第一个参数是触发事件的对象的引用，第二个参数是用来传你要的处理数据。
            stateChangeEvent(this, eventArgs);
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
                    logger.WarnFormat("{0} HandleMessage fail,packet.Sequence already existed ：{1}", this.AppAccount, packets[i].Sequence);
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
            MessageEventArgs eventArgs = new MessageEventArgs(this, packets);
            messageEvent(this, eventArgs);
        }

        private void HandleSendMessageTimeout(P2PMessage p2PMessage)
        {
            if (null == messageTimeOutEvent)
            {
                logger.WarnFormat("{0} HandleSendMessageTimeout fail MessageTimeOutEvent is null. ", this.appAccount);
                return;
            }
            SendMessageTimeoutEventArgs eventArgs = new SendMessageTimeoutEventArgs(this, p2PMessage);
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
                    logger.WarnFormat("{0} HandleGroupMessage fail,packet.Sequence already existed ：{1}", this.AppAccount, message.Sequence);
                    packets.RemoveAt(i);
                    return;
                }
                logger.DebugFormat("{0} HandleGroupMessage ,packet.Sequence add：{1}", this.AppAccount, message.Sequence);
                this.P2tAckSequenceSet.Add(message.Sequence);
            }
            if (packets.Count == 0 || packets == null)
            {
                return;
            }
            GroupMessageEventArgs eventArgs = new GroupMessageEventArgs(this, packets);
            groupMessageEvent(this, eventArgs);
        }

        private void HandleSendGroupMessageTimeout(P2TMessage message)
        {
            if (null == groupMessageTimeoutEvent)
            {
                logger.WarnFormat("{0} HandleSendGroupMessageTimeout fail GroupMessageTimeoutEvent is null. ", this.appAccount);
                return;
            }
            SendGroupMessageTimeoutEventArgs eventArgs = new SendGroupMessageTimeoutEventArgs(this, message);
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

            ServerACKEventArgs eventArgs = new ServerACKEventArgs(this, serverAck);
            serverACKEvent(this, eventArgs);
        }

        internal void HandleUnlimitedGroupMessage(UCPacket ucPacket)
        {

            if (null == unlimitedGroupMessageEvent)
            {
                logger.WarnFormat("{0} HandleUnlimitedGroupMessage fail ServerACKEvent is null. ", this.appAccount);
                return;
            }

            UCMessageList messageList = null;
            using (MemoryStream stream = new MemoryStream(ucPacket.payload))
            {
                messageList = Serializer.Deserialize<UCMessageList>(stream);
            }
            if (messageList == null)
            {
                logger.WarnFormat("HandleUnlimitedGroupMessage seqAck is null");
                return;
            }

            for (int i = messageList.message.Count - 1; i >= 0; i--)
            {
                UCMessage ucMessage = messageList.message[i];
                if (this.UCAckSequenceSet.Contains(ucMessage.sequence))
                {
                    logger.WarnFormat("{0} HandleUnlimitedGroupMessage fail,packet.sequence already existed ：{1}", this.AppAccount, ucMessage.sequence);
                    messageList.message.Remove(ucMessage);
                    return;
                }
                logger.DebugFormat("{0} HandleUnlimitedGroupMessage ,packet.sequence add ：{1}", this.AppAccount, ucMessage.sequence);
                this.UCAckSequenceSet.Add(ucMessage.sequence);
            }
            if (messageList.message.Count == 0 || messageList.message == null)
            {
                return;
            }
            UCPacket ucAckPacket = BuildUcSeqAckPacket(this, messageList);
            String packetId = MIMCUtil.CreateMsgId(this);

            if (SendUCPacket(packetId, ucAckPacket))
            {
                this.lastUcPingTimestamp = MIMCUtil.CurrentTimeMillis();
                logger.DebugFormat("---> Send UcSeqAck Packet sucess,{0} packetId:{1}, lastUcPingTimestamp:{2}", this.appAccount, packetId, this.lastUcPingTimestamp);
            }
            else
            {
                logger.WarnFormat("---> Send UcSeqAck Packet fail,{0} packetId:{1},lastUcPingTimestamp:{2}", this.appAccount, packetId, this.lastUcPingTimestamp);
            }

            logger.InfoFormat("{0} HandleUnlimitedGroupMessage success. ", this.appAccount);

            UnlimitedGroupMessageEventArgs eventArgs = new UnlimitedGroupMessageEventArgs(this, ucPacket);
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

        internal void HandleDismissUnlimitedGroup(UCPacket ucPacket)
        {
            if (null == dismissUnlimitedGroupEvent)
            {
                logger.WarnFormat("{0} HandleDismissUnlimitedGroup fail ServerACKEvent is null. ", this.appAccount);
                return;
            }
            logger.InfoFormat("{0}HandleDismissUnlimitedGroup success. ", this.appAccount);

            DismissUnlimitedGroupEventArgs eventArgs = new DismissUnlimitedGroupEventArgs(this, ucPacket);
            dismissUnlimitedGroupEvent(this, eventArgs);
        }
        private void HandleSendUnlimitedGroupMessageTimeout(UCPacket message)
        {
            if (null == unlimitedGroupMessageTimeoutEvent)
            {
                logger.WarnFormat("{0} HandleSendUnlimitedGroupMessageTimeout fail unlimitedGroupMessageTimeoutEvent is null. ", this.appAccount);
                return;
            }
            SendUnlimitedGroupMessageTimeoutEventArgs eventArgs = new SendUnlimitedGroupMessageTimeoutEventArgs(this, message);
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
                    path = Environment.CurrentDirectory + "\\catch\\";
                    if (Directory.Exists(path))
                    {
                        //logger.DebugFormat("userCatch {0} Folder already exists, skip creation.", path);
                    }
                    else
                    {
                        Directory.CreateDirectory(path);
                        //logger.DebugFormat("userCatch {0} Folder does not exist, create success.", path);
                    }
                }

                string catchFile = path + appAccount + ".txt";
                JsonSerializer serializer = new JsonSerializer();
                String result = MIMCUtil.Deserialize<String>(catchFile);
                var currentData = (JObject)JsonConvert.DeserializeObject(result == null ? "" : result);
                currentData = currentData == null ? new JObject() : currentData;
                logger.DebugFormat("userCatch before appAccount:{0},resource:{1}", appAccount, this.resource);
                if (!currentData.ContainsKey("resource"))
                {
                    currentData.Add("resource", this.resource);
                    logger.InfoFormat("userCatch add：{0}-{1}", appAccount, this.resource);
                    MIMCUtil.SerializeToFile(currentData, catchFile);
                }
                else
                {
                    this.resource = (String)currentData.GetValue("resource");
                    logger.InfoFormat("userCatch read：{0}-{1}", appAccount, currentData.GetValue("resource"));
                }
                logger.DebugFormat("userCatch currentData after ,appAccount:{0},resource:{1}", appAccount, this.resource);
            }
            catch (Exception e)
            {
                logger.DebugFormat("SetResource fail! read or write file error!path:{0}，error：{1}", path, e.StackTrace);
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
                logger.WarnFormat("{0} Login fail FetchToken fail，tokenStr IsNullOrEmpty，Please make sure has register tokenFetcher and Implement methods in the interface. ", this.appAccount);
                return false;
            }
            logger.DebugFormat("{0} Login tokenStr {1}", this.appAccount, tokenStr);

            JObject jo = (JObject)JsonConvert.DeserializeObject(tokenStr);
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

            JObject data = (JObject)jo.GetValue("data");
            this.appId = long.Parse(data.GetValue("appId").ToString());
            this.appPackage = data.GetValue("appPackage").ToString();
            this.chid = Convert.ToInt32(data.GetValue("miChid"));
            this.uuid = long.Parse(data.GetValue("miUserId").ToString());
            this.SecurityKey = data.GetValue("miUserSecurityKey").ToString();
            logger.DebugFormat("{0} get token SecurityKey :{1}", this.appAccount, this.SecurityKey);

            if (!this.appAccount.Equals(data.GetValue("appAccount").ToString()))
            {
                logger.WarnFormat("{0} Login fail appAccount does  not match {1}!={2}", this.appAccount, this.appAccount, data.GetValue("appAccount").ToString());
                return false;
            }
            if (string.IsNullOrEmpty(data.GetValue("token").ToString()))
            {
                logger.DebugFormat("{0} Login fail token IsNullOrEmpty uuid:{1}", this.appAccount, this.uuid);
                return false;
            }
            logger.InfoFormat("{0} Login success uuid:{1}", this.appAccount, this.uuid);

            this.token = data.GetValue("token").ToString();
            return true;
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>bool</returns>
        public async Task<bool> LogoutAsync()
        {
            return Logout();
        }
        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>bool</returns>
        public bool Logout()
        {
            if (this.status == Constant.OnlineStatus.Offline)
            {
                logger.WarnFormat("Logout FAIL SENDPACKET:{0}, FAIL_FOR_NOT_ONLINE,CHID:{1}, UUID:{2}", Constant.CMD_UNBIND, this.chid, this.uuid);
                return false;
            }

            MIMCObject mIMCObject = new MIMCObject();
            V6Packet v6Packet = MIMCUtil.BuildUnBindPacket(this);
            mIMCObject.Type = Constant.MIMC_C2S_DOUBLE_DIRECTION;
            mIMCObject.Packet = v6Packet;
            this.connection.PacketWaitToSend.Enqueue(mIMCObject);

            return true;
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
                        logger.WarnFormat("{0} MIMCConnection fail, host:{1}-{2}", this.appAccount, connection.Host, connection.Port);
                        continue;
                    }
                    logger.DebugFormat("{0} connection success, host:{1}-{2}", this.appAccount, connection.Host, connection.Port);

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
                    if (this.Status == Constant.OnlineStatus.Offline
                        && currentTime - this.LastLoginTimestamp <= Constant.LOGIN_TIMEOUT)
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
                        if (now - this.lastUcPingTimestamp > Constant.UC_PING_TIMEVAL_MS)
                        {
                            if (ucTopics != null && ucTopics.Count > 0)
                            {
                                logger.DebugFormat("appAccount:{0} LastUcPingTimestamp:{1},now:{2},timediv:{3}", this.appAccount, this.lastUcPingTimestamp, now, now - this.lastUcPingTimestamp);

                                UCPacket ucPacket = BuildUcPingPacket(this);
                                String packetId = MIMCUtil.CreateMsgId(this);
                                if (SendUCPacket(packetId, ucPacket))
                                {
                                    logger.DebugFormat("---> Send UcPing Packet sucess,{0} packetId:{1}, lastUcPingTimestamp:{2}", this.appAccount, packetId, this.lastUcPingTimestamp);
                                }
                                else
                                {
                                    logger.WarnFormat("---> Send UcPing Packet fail,{0} packetId:{1},lastUcPingTimestamp:{2}", this.appAccount, packetId, this.lastUcPingTimestamp);
                                }
                            }

                        }

                        MIMCObject mimcObject;
                        bool ret = this.connection.PacketWaitToSend.TryDequeue(out mimcObject);
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
                        if (mimcObject != null)
                        {
                            msgType = mimcObject.Type;
                            v6Packet = (V6Packet)mimcObject.Packet;
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
                        //logger.DebugFormat("ThreadWrite, cmd:{0}", v6Packet.Body.ClientHeader.cmd);
                    }
                    else
                    {
                        logger.WarnFormat("connection.reset reason: V6PacketEncoder.Encode fail data is null");
                        this.connection.Reset();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    logger.WarnFormat("connection.reset reason: {0}", ex.StackTrace);
                    this.connection.Reset();
                }
                if (this.logoutFlag)
                {
                    continue;
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

            while (true)
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
                    logger.WarnFormat("ThreadReceive connection.reset V6_HEAD not equal{0}!={1}", Constant.V6_HEAD_LENGTH, readLength);
                    this.connection.Reset();
                    continue;
                }

                char magic = V6PacketDecoder.GetChar(headerBins, 0);
                if (magic != Constant.MAGIC)
                {
                    logger.WarnFormat("ThreadReceive connection.reset V6_MAGIC not equal {0}!={1}", magic, Constant.MAGIC);
                    this.connection.Reset();
                    continue;
                }

                char version = V6PacketDecoder.GetChar(headerBins, 2);
                if (version != Constant.V6_VERSION)
                {
                    logger.WarnFormat("ThreadReceive  connection.reset V6_VERSION not equal {0}!={1}", version, Constant.V6_VERSION);
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
                        logger.WarnFormat("ThreadReceive  connection.reset V6_BODY, {0}!={1}", v6BodyBytesRead, bodyLen);
                        this.connection.Reset();
                        continue;
                    }
                }

                byte[] crcBins = new byte[Constant.CRC_LEN];
                int crcBytesRead = MIMCConnection.Readn(stream, crcBins, Constant.CRC_LEN);

                if (crcBytesRead != Constant.CRC_LEN)
                {
                    logger.WarnFormat("ThreadReceive  connection.reset V6_CRC, {0}!={1}", crcBytesRead, Constant.CRC_LEN);
                    this.connection.Reset();
                    continue;
                }

                this.connection.ClearNextResetSockTimestamp();

                V6Packet feV6Packet = V6PacketDecoder.DecodeV6(headerBins, bodyBins, crcBins, this.connection.Rc4Key, this);
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

                logger.InfoFormat("<--- receive v6 packet cmd:{0}, appAccount:{1}", feV6Packet.Body.ClientHeader.cmd, this.appAccount);
                if (Constant.CMD_CONN == feV6Packet.Body.ClientHeader.cmd)
                {
                    XMMsgConnResp connResp = null;
                    using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
                    {
                        connResp = Serializer.Deserialize<XMMsgConnResp>(ms);

                    }
                    if (null == connResp)
                    {
                        logger.WarnFormat("ThreadReceive  connection.reset, cmd:{0}, ConnResp is null", feV6Packet.Body.ClientHeader.cmd);
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
                    this.HandleStateChange(xMMsgBindResp.result, xMMsgBindResp.error_type, xMMsgBindResp.error_reason, xMMsgBindResp.error_desc);
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
                    this.HandleStateChange(false, "KICK", "KICK", "KICK");
                    continue;

                }
                if (Constant.CMD_SECMSG == feV6Packet.Body.ClientHeader.cmd)
                {
                    userHandler.HandleSecMsg(this, feV6Packet);
                    continue;
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

            while (true)
            {
                try
                {
                    long currentTime = MIMCUtil.CurrentTimeMillis();
                    if (connection.NextResetSockTimestamp > 0 && currentTime - connection.NextResetSockTimestamp > 0)
                    {
                        logger.WarnFormat("ThreadTrigger  connection.reset currentTime:{0},connection.NextResetSockTimestamp:{1}", currentTime, connection.NextResetSockTimestamp);
                        connection.Reset();
                    }

                    ScanAndCallBack();
                    Thread.Sleep(200);

                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.StackTrace);
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
                logger.DebugFormat("{0} ThreadCallback  timeoutPackets size:{1}", this.appAccount, timeoutPackets.Count);
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

                        P2PMessage p2pMessage = new P2PMessage(mimcPacket.packetId, mimcPacket.sequence, p2p.from.appAccount, p2p.from.resource, p2p.payload, mimcPacket.timestamp);
                        HandleSendMessageTimeout(p2pMessage);
                        logger.DebugFormat("{0} ThreadCallback SendMessageTimeout packetId:{1}", this.appAccount, p2pMessage.PacketId);
                    }
                }
                if (mimcPacket.type == MIMC_MSG_TYPE.P2T_MESSAGE)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        MIMCP2TMessage p2t = Serializer.Deserialize<MIMCP2TMessage>(ms);

                        P2TMessage p2tMessage = new P2TMessage(mimcPacket.packetId, mimcPacket.sequence, p2t.from.appAccount, p2t.from.resource, p2t.to.topicId, p2t.payload, mimcPacket.timestamp);
                        HandleSendGroupMessageTimeout(p2tMessage);
                        logger.DebugFormat("{0} ThreadCallback SendGroupMessageTimeout packetId:{1}", this.appAccount, p2tMessage.PacketId);
                    }
                }

                if (mimcPacket.type == MIMC_MSG_TYPE.UC_PACKET)
                {
                    using (MemoryStream ms = new MemoryStream(mimcPacket.payload))
                    {
                        UCPacket packet = Serializer.Deserialize<UCPacket>(ms);

                        HandleSendUnlimitedGroupMessageTimeout(packet);
                        logger.DebugFormat("{0} ThreadCallback SendUnlimitedMessageTimeout packetId:{1},type:{2},ucType{3}", this.appAccount, mimcPacket.packetId, mimcPacket.type, packet.type);
                    }
                }

                if (timeoutPackets.TryRemove(mimcPacket.packetId, out timeoutPacket))
                {
                    logger.DebugFormat("{0} ScanAndCallBack timeoutPackets TryRemove sucess,packetId:{1}", this.appAccount, mimcPacket.packetId);
                }
                else
                {
                    logger.WarnFormat("{0} ScanAndCallBack timeoutPackets TryRemove fail,packetId:{1}", this.appAccount, mimcPacket.packetId);
                }
            }
        }



        public bool IsOnline()
        {
            logger.DebugFormat("{0} IsOnline :{1}", this.appAccount, this.Status == Constant.OnlineStatus.Online);
            return this.Status == Constant.OnlineStatus.Online;
        }

        /// <summary>
        /// 发送单聊消息
        /// </summary>
        /// <param name="toAppAccount">消息接收者在APP帐号系统内的唯一帐号ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <returns>packetId客户端生成的消息ID</returns>
        public async Task<string> SendMessageAsync(string toAppAccount, byte[] msg)
        {
            return SendMessage(toAppAccount, msg);
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
                logger.WarnFormat("SendMessage fail!,toAppAccount:{0},msg :{1}", toAppAccount, msg);
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
        public async Task<string> SendMessageAsync(string toAppAccount, byte[] msg, bool isStore)
        {
            return SendMessage(toAppAccount, msg, isStore);
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
                logger.WarnFormat("SendMessage fail!,toAppAccount:{0},msg :{1}", toAppAccount, msg);
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
                logger.WarnFormat("{0} SendGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount, topicId, msg, (long)msg.Length);
                return null;
            }
            return SendGroupMessage(topicId, msg, true);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <returns></returns>
        public async Task<string> SendGroupMessageAsync(long topicId, byte[] msg)
        {
            return SendGroupMessage(topicId, msg);
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
                logger.WarnFormat("{0} SendGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount, topicId, msg, (long)msg.Length);
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
        /// 发送群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <param name="isStore">是否保存历史记录，true：保存，false：不存</param>
        /// <returns></returns>
        public async Task<string> SendGroupMessageAsync(long topicId, byte[] msg, bool isStore)
        {
            return SendGroupMessage(topicId, msg, isStore);
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

            HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(Constant.CREATE_UNLIMITE_CHAT_URL, output, null, null, Encoding.UTF8, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            JObject jo = (JObject)JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("CreateUnlimitedGroup error:{0}", content);
                return null;
            }
            JObject data = (JObject)jo.GetValue("data");
            logger.DebugFormat("CreateUnlimitedGroup success content:{0},topicId:{1}", content, data.GetValue("topicId").ToString());

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

            HttpWebResponse myResponse = HttpWebResponseUtil.CreateDeleteHttpResponse(Constant.DISMISS_UNLIMITE_CHAT_URL, null, null, null, Encoding.UTF8, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("DismissUnlimitedGroup :{0}", content);

            JObject jo = (JObject)JsonConvert.DeserializeObject(content);
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

            HttpWebResponse myResponse = HttpWebResponseUtil.CreateGetHttpResponse(Constant.QUERY_UNLIMITE_CHAT_ONLINE_INFO_URL, null, null, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            JObject jo = (JObject)JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("GetUnlimitedGroupUsersNum error:{0}", content);
                return null;
            }
            JObject data = (JObject)jo.GetValue("data");
            logger.DebugFormat("GetUnlimitedGroupUsersNum success content:{0},topicId:{1}", content, data.GetValue("topicId").ToString());

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

            HttpWebResponse myResponse = HttpWebResponseUtil.CreateGetHttpResponse(Constant.QUERY_UNLIMITE_CHAT_USERLIST_URL, null, null, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("GetUnlimitedGroupUsers ------------------content:{0}", content);

            JObject jo = (JObject)JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("GetUnlimitedGroupUsers error:{0}", content);
                return null;
            }
            JObject data = (JObject)jo.GetValue("data");
            reader.Close();
            myResponse.Close();
            return data.ToString();
        }

        /// <summary>
        /// 获取无限大群列表
        /// </summary>
        /// <returns></returns>
        public bool QueryUnlimitedGroups()
        {
            logger.DebugFormat("{0} QueryUnlimitedGroups uuid:{1}", appAccount, this.uuid.ToString());

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("token", Token);

            HttpWebResponse myResponse = HttpWebResponseUtil.CreateGetHttpResponse(Constant.QUERY_UNLIMITE_CHAT_URL, null, null, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            logger.DebugFormat("QueryUnlimitedGroups :{0}", content);

            JObject jo = (JObject)JsonConvert.DeserializeObject(content);
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
                logger.DebugFormat("{0} QueryUnlimitedGroups sucesss:ucTopics.Count {1}", this.appAccount, ucTopics.Count);
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
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;

            UCGroup to = new UCGroup();
            to.appId = appId;
            to.topicId = topicId;
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
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;

            UCGroup to = new UCGroup();
            to.appId = appId;
            to.topicId = topicId;
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


        /// <summary>
        /// 发送无限大群聊消息
        /// </summary>
        /// <param name="topicId">群ID</param>
        /// <param name="msg">开发者自定义消息体，二级制数组格式</param>
        /// <returns></returns>
        public string SendUnlimitedGroupMessage(long topicId, byte[] msg)
        {
            if (topicId == 0 || msg == null || msg.Length == 0 || msg.Length > Constant.MIMC_MAX_PACKET_SIZE)
            {
                logger.WarnFormat("{0} SendUnlimitedGroupMessage fail,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount, topicId, msg, (long)msg.Length);
                return null;
            }
            logger.DebugFormat("{0} start SendUnlimitedGroupMessage,topicId:{1},msg:{2},msg.Length:{3}", this.appAccount, topicId, msg, (long)msg.Length);

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;

            UCGroup to = new UCGroup();
            to.appId = appId;
            to.topicId = topicId;

            UCMessage message = new UCMessage();
            message.user = fromUser;
            message.group = to;
            message.payload = msg;


            String packetId = MIMCUtil.CreateMsgId(this);

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

            ClientHeader clientHeader = MIMCUtil.CreateClientHeader(user, Constant.CMD_SECMSG, Constant.CIPHER_NONE, MIMCUtil.CreateMsgId(user));

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;

            UCPacket ucPacket = new UCPacket();
            ucPacket.type = UC_MSG_TYPE.PING;
            ucPacket.user = fromUser;

            UCPing ucPing = new UCPing();
            List<UCGroup> groups = new List<UCGroup>();
            foreach (long topicId in ucTopics)
            {
                UCGroup group = new UCGroup();
                group.appId = appId;
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

        internal UCPacket BuildUcSeqAckPacket(MIMCUser user, UCMessageList messageList)
        {
            logger.DebugFormat("{0} User BuildUcSeqAckPacket", user.AppAccount);

            if (string.IsNullOrEmpty(user.Token))
            {
                logger.WarnFormat("{0} User BuildUcSeqAckPacket fail Token is null,wait ...", user.AppAccount);
                return null;
            }

            global::mimc.MIMCUser fromUser = new global::mimc.MIMCUser();
            fromUser.appId = appId;
            fromUser.appAccount = appAccount;
            fromUser.uuid = uuid;
            fromUser.resource = resource;


            UCPacket ucPacket = new UCPacket();
            ucPacket.user = fromUser;
            ucPacket.type = UC_MSG_TYPE.SEQ_ACK;

            UCSequenceAck ucSeqAck = new UCSequenceAck();
            ucSeqAck.group = messageList.group;
            ucSeqAck.sequence = messageList.maxSequence;
            logger.DebugFormat("ucSeqAck.sequence:{0}, messageList.maxSequence:{1}", ucSeqAck.sequence, messageList.maxSequence);

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
            this.lastUcPingTimestamp = MIMCUtil.CurrentTimeMillis();

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
                        logger.DebugFormat("{0} timeoutPackets TryAdd sucess,packetId:{1},type:{2}", this.appAccount, packetId, mimcPacket.type);
                        return true;
                    }
                    else
                    {
                        logger.WarnFormat("{0} timeoutPackets TryAdd fail,packetId:{1},type:{2}", this.appAccount, packetId, mimcPacket.type);
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
            if (string.IsNullOrEmpty(packetId) || packetBin == null || packetBin.Length == 0 || (msgType != Constant.MIMC_C2S_DOUBLE_DIRECTION && msgType != Constant.MIMC_C2S_SINGLE_DIRECTION))
            {
                logger.WarnFormat("SendPacket fail!packetId:{0},packetBin :{1}，msgType：{2}", packetId, packetBin, msgType);
                return false;
            }
            V6Packet v6Packet = MIMCUtil.BuildSecMsgPacket(this, packetId, packetBin);

            MIMCObject mIMCObject = new MIMCObject();
            mIMCObject.Type = msgType;
            mIMCObject.Packet = v6Packet;
            logger.DebugFormat("SendPacket packetId:{0},resource :{1}，msgType：{2}", packetId, resource, msgType);

            this.connection.PacketWaitToSend.Enqueue(mIMCObject);
            return true;
        }


    }
}

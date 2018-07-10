using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.xiaomi.mimc;
using com.xiaomi.mimc.handler;
using com.xiaomi.mimc.packet;
using com.xiaomi.mimc.utils;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sdk.demo
{   /// <summary>
    /// MIMC C# Demo用例，请确保更新到0.0.6以上版本
    /// </summary>
    public class MIMCDemo
    {
        /**
        * @Important:
        *   以下appId/appKey/appSecurity是小米MIMCDemo APP所有，会不定期更新
        *   所以，开发者应该将以下三个值替换为开发者拥有APP的appId/appKey/appSecurity
        * @Important:
        *   开发者访问小米开放平台(https://dev.mi.com/console/man/)，申请appId/appKey/appSecurity
        **/
        private static string topicUrl = "https://mimc.chat.xiaomi.net/api/topic/";

        private static string url = "https://mimc.chat.xiaomi.net/api/account/token";
        private static string appId = "2882303761517613988";
        private static string appKey = "5361761377988";
        private static string appSecret = "2SZbrJOAL1xHRKb7L9AiRQ==";

        private string appAccount1 = "leijun" + GenerateRandomString(5);
        private string appAccount2 = "linbin" + GenerateRandomString(5);
        private MIMCUser leijun;
        private MIMCUser linbin;

        static ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            RunAsync();
            Console.ReadKey();
        }

        static void Run()
        {
            logger.InfoFormat("demo start");

            MIMCDemo demo = new MIMCDemo();
            if (!demo.Ready())
            {
                return;
            }

            demo.SendMessage();
            Thread.Sleep(1000);

            demo.SendGroupMessage();
            Thread.Sleep(2000);

            if (!demo.Over())
            {
                return;
            }
        }

        static async void RunAsync()
        {
            logger.InfoFormat("demo start");

            MIMCDemo demo = new MIMCDemo();
            if (!await demo.ReadyAsync())
            {
                return;
            }

            //demo.SendMessageAsync();
            //Thread.Sleep(1000);

            //demo.SendGroupMessageAsync();
            Thread.Sleep(1000);

            demo.SendUnlimitedGroupMessage();
            Thread.Sleep(1000);

            //demo.JoinInexistentUnlimitedGroup();
            //Thread.Sleep(1000);

            //demo.QuitInexistentUnlimitedGroup();
            //Thread.Sleep(5000);

            //if (!await demo.OverAsync())
            //{
            //    return;
            //}
        }

        public MIMCDemo()
        {
            leijun = new MIMCUser(appAccount1);
            leijun.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecret, url, appAccount1));
            leijun.stateChangeEvent += HandleStatusChange;
            leijun.messageEvent += HandleMessage;
            leijun.messageTimeOutEvent += HandleMessageTimeout;
            leijun.groupMessageEvent += HandleGroupMessage;
            leijun.groupMessageTimeoutEvent += HandleGroupMessageTimeout;
            leijun.serverACKEvent += HandleServerACK;
            leijun.unlimitedGroupMessageEvent += HandleUnlimitedGroupMessage;
            leijun.unlimitedGroupMessageTimeoutEvent += HandleUnlimitedGroupMessageTimeout;
            leijun.joinUnlimitedGroupEvent += HandleJoinUnlimitedGroup;
            leijun.quitUnlimitedGroupEvent += HandleQuitUnlimitedGroup;
            leijun.dismissUnlimitedGroupEvent += HandleDismissUnlimitedGroup;

            linbin = new MIMCUser(appAccount2);
            linbin.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecret, url, appAccount2));
            linbin.stateChangeEvent += HandleStatusChange;
            linbin.messageEvent += HandleMessage;
            linbin.messageTimeOutEvent += HandleMessageTimeout;
            linbin.groupMessageEvent += HandleGroupMessage;
            linbin.groupMessageTimeoutEvent += HandleGroupMessageTimeout;
            linbin.serverACKEvent += HandleServerACK;
            linbin.unlimitedGroupMessageEvent += HandleUnlimitedGroupMessage;
            linbin.unlimitedGroupMessageTimeoutEvent += HandleUnlimitedGroupMessageTimeout;
            linbin.joinUnlimitedGroupEvent += HandleJoinUnlimitedGroup;
            linbin.quitUnlimitedGroupEvent += HandleQuitUnlimitedGroup;
            linbin.dismissUnlimitedGroupEvent += HandleDismissUnlimitedGroup;

        }



        bool Ready()
        {
            if (!leijun.Login())
            {
                logger.ErrorFormat("Login Fail, {0}", leijun.AppAccount);
                return false;
            }
            if (!linbin.Login())
            {
                logger.ErrorFormat("Login Fail, {0}", linbin.AppAccount);
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        async Task<bool> ReadyAsync()
        {
            if (!await leijun.LoginAsync())
            {
                logger.ErrorFormat("Login Fail, {0}", leijun.AppAccount);
                return false;
            }


            if (!await linbin.LoginAsync())
            {
                logger.ErrorFormat("Login Fail, {0}", linbin.AppAccount);
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }
        /// <summary>
        /// 注销用户
        /// </summary>
        bool Over()
        {
            if (!leijun.Logout())
            {
                logger.ErrorFormat("Logout Fail, {0}", leijun.AppAccount);
                return false;
            }
            if (!linbin.Logout())
            {
                logger.ErrorFormat("Logout Fail, {0}", linbin.AppAccount);
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        /// <summary>
        /// 注销用户
        /// </summary>
        async Task<bool> OverAsync()
        {
            if (!await leijun.LogoutAsync())
            {
                logger.ErrorFormat("Logout Fail, {0}", leijun.AppAccount);
                return false;
            }
            if (!await linbin.LogoutAsync())
            {
                logger.ErrorFormat("Logout Fail, {0}", linbin.AppAccount);
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        /// <summary>
        /// 发送单聊消息测试
        /// </summary>
        void SendMessage()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", leijun.AppAccount);
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", linbin.AppAccount);
                return;
            }

            String packetId = leijun.SendMessage(linbin.AppAccount, UTF8Encoding.Default.GetBytes("Are you OK?" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount, linbin.AppAccount, packetId);
            Thread.Sleep(100);

            packetId = linbin.SendMessage(leijun.AppAccount, UTF8Encoding.Default.GetBytes("I'm OK!" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", linbin.AppAccount, leijun.AppAccount, packetId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送单聊消息测试
        /// </summary>
        async void SendMessageAsync()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", leijun.AppAccount);
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", linbin.AppAccount);
                return;
            }

            String packetId = await leijun.SendMessageAsync(linbin.AppAccount, UTF8Encoding.Default.GetBytes("Are you OK?" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount, linbin.AppAccount, packetId);
            Thread.Sleep(100);

            packetId = await linbin.SendMessageAsync(leijun.AppAccount, UTF8Encoding.Default.GetBytes("I'm OK!" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", linbin.AppAccount, leijun.AppAccount, packetId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送群聊消息测试
        /// </summary>
        void SendGroupMessage()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", linbin.AppAccount);
                return;
            }
            long topicId = CreateNormalTopic(topicUrl, appAccount1, appAccount1 + "," + appAccount2);

            String packetId = leijun.SendGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")));

            logger.InfoFormat("SendGroupMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount, linbin.AppAccount, packetId);
            Thread.Sleep(100);
        }


        /// <summary>
        /// 发送群聊消息测试
        /// </summary>
        async void SendGroupMessageAsync()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", linbin.AppAccount);
                return;
            }
            long topicId = CreateNormalTopic(topicUrl, appAccount1, appAccount1 + "," + appAccount2);

            String packetId = await leijun.SendGroupMessageAsync(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")));

            logger.InfoFormat("SendGroupMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount, linbin.AppAccount, packetId);
            Thread.Sleep(100);
        }



        /// <summary>
        /// 创建无限大群测试
        /// </summary>
        void CreateUnlimitedGroup()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }
            string topicId = leijun.CreateUnlimitedGroup("test");
            logger.InfoFormat("CreateUnlimitedGroup, {0}, topicId:{1}", leijun.AppAccount, topicId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 解散无限大群测试
        /// </summary>
        void DismissUnlimitedGroup(long topicId)
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }

            leijun.DismissUnlimitedGroup(topicId);
            logger.InfoFormat("DismissUnlimitedGroup, {0}, topicId:{1}", leijun.AppAccount, topicId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 加入不存在的无限大群测试
        /// </summary>
        void JoinInexistentUnlimitedGroup()
        {
            if (!leijun.IsOnline())
            {
                logger.ErrorFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }

            leijun.JoinUnlimitedGroup(12311111111111111L);
            logger.InfoFormat("JoinInexistentUnlimitedGroup, {0}, topicId:{1}", leijun.AppAccount, 12311111111111111L);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 退出不存在的无限大群测试
        /// </summary>
        void QuitInexistentUnlimitedGroup()
        {
            if (!leijun.IsOnline())
            {
                logger.ErrorFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }

            leijun.QuitUnlimitedGroup(12311111111111111L);
            logger.InfoFormat("QuitUnlimitedGroup, {0}, topicId:{1}", leijun.AppAccount, 12311111111111111L);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送无限大群消息测试
        /// </summary>
        void SendUnlimitedGroupMessage()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount);
                return;
            }
            long topicId = long.Parse(leijun.CreateUnlimitedGroup("test"));
            Thread.Sleep(100);

            if (topicId == 0)
            {
                logger.ErrorFormat("SendUnlimitedGroupMessage CreateUnlimitedGroup error , {0}-->{1},uuid:{2}", leijun.AppAccount, topicId, leijun.Uuid);
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", linbin.AppAccount);
                return;
            }
            linbin.JoinUnlimitedGroup(topicId);
            Thread.Sleep(2000);
            leijun.QueryUnlimitedGroups();
            Thread.Sleep(2000);
            linbin.QueryUnlimitedGroups();

            String packetId = leijun.SendUnlimitedGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")));

            logger.InfoFormat("SendUnlimitedGroupMessage, {0}-->{1}, PacketId:{2},uuid:{3},uuid2:{4}", leijun.AppAccount, topicId, packetId, leijun.Uuid, linbin.Uuid);
            Thread.Sleep(5000);
            linbin.QuitUnlimitedGroup(topicId);
            Thread.Sleep(1000);

            //bool dismissFlag = leijun.DismissUnlimitedGroup(topicId);
            Thread.Sleep(1000);

            //if (dismissFlag)
            //{
            //    logger.InfoFormat("DismissUnlimitedGroup sucesss, {0} topicId:{1},uuid:{2}", leijun.AppAccount, topicId, leijun.Uuid);
            //    //测试不存在的情况
            //    packetId = leijun.SendUnlimitedGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")));
            //}
            //else
            //{
            //    logger.InfoFormat("DismissUnlimitedGroup fail, {0} topicId:{1},uuid:{2}", leijun.AppAccount, topicId, leijun.Uuid);
            //}
        }

        /// <summary>
        /// 获取token接口实现
        /// </summary>
        class MIMCCaseTokenFetcher : IMIMCTokenFetcher
        {
            private string httpUrl;
            private string appId;
            private string appKey;
            private string appSecret;
            private string appAccount;

            public MIMCCaseTokenFetcher(String appId, String appKey, String appSecret, String httpUrl, String appAccount)
            {
                this.httpUrl = httpUrl;
                this.appId = appId;
                this.appKey = appKey;
                this.appSecret = appSecret;
                this.appAccount = appAccount;
            }

            /// <summary>
            ///1、此例中，fetchToken()直接上传(appId/appKey/appSecurity/appAccount)给小米TokenService，获取Token使用
            ///实际上，在生产环境中，fetchToken()应该只上传appAccount+password/cookies给AppProxyService，AppProxyService
            ///验证鉴权通过后，再上传(appId/appKey/appSecurity/appAccount)给小米TokenService，获取Token后返回给fetchToken()
            ///2、 appId/appKey/appSecurity绝对不能如此用例一般存放于APP本地
            /// </summary>
            /// <returns>tokenStr</returns>
            public String FetchToken()
            {
                logger.DebugFormat("FetchToken:{0}", appAccount);

                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("appId", appId);
                parameters.Add("appKey", appKey);
                parameters.Add("appSecret", appSecret);
                parameters.Add("appAccount", appAccount);
                string output = JsonConvert.SerializeObject(parameters);

                HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(httpUrl, output, null, null, Encoding.UTF8, null);
                string cookieString = myResponse.Headers["Set-Cookie"];
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                string content = reader.ReadToEnd();
                JObject jo = (JObject)JsonConvert.DeserializeObject(content);
                string code = jo.GetValue("code").ToString();
                if (code != "200")
                {
                    logger.DebugFormat("get token error:{0}", content);
                    return null;
                }
                logger.DebugFormat("get token sucesss:{0}", content);

                reader.Close();
                myResponse.Close();
                return content;
            }

            /// <summary>
            ///1、此例中，fetchToken()直接上传(appId/appKey/appSecurity/appAccount)给小米TokenService，获取Token使用
            ///实际上，在生产环境中，fetchToken()应该只上传appAccount+password/cookies给AppProxyService，AppProxyService
            ///验证鉴权通过后，再上传(appId/appKey/appSecurity/appAccount)给小米TokenService，获取Token后返回给fetchToken()
            ///2、 appId/appKey/appSecurity绝对不能如此用例一般存放于APP本地
            /// </summary>
            /// <returns>tokenStr</returns>
            public async Task<String> FetchTokenAsync()
            {
                logger.DebugFormat("FetchTokenAsync:{0}", appAccount);

                Encoding encoding = Encoding.GetEncoding("utf-8");
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("appId", appId);
                parameters.Add("appKey", appKey);
                parameters.Add("appSecret", appSecret);
                parameters.Add("appAccount", appAccount);
                string output = JsonConvert.SerializeObject(parameters);

                HttpWebResponse myResponse = await HttpWebResponseUtil.CreatePostHttpResponseAsync(httpUrl, output, null, null, encoding, null, null);
                string cookieString = myResponse.Headers["Set-Cookie"];
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                string content = reader.ReadToEnd();
                logger.DebugFormat("get token :{0}", content);
                JObject jo = (JObject)JsonConvert.DeserializeObject(content);
                string code = jo.GetValue("code").ToString();
                if (code != "200")
                {
                    logger.DebugFormat("get token error:{0}", content);
                    return null;
                }
                logger.DebugFormat("get token sucesss:{0}", content);
                reader.Close();
                myResponse.Close();
                return content;
            }
        }

        /// <summary>
        /// 测试创建一个群
        /// </summary>
        /// <param name="httpUrl">创建群url</param>
        /// <param name="appAccount">群主id</param>
        /// <param name="appAccounts">群成员ids，逗号分隔</param>
        /// <returns></returns>
        public long CreateNormalTopic(string httpUrl, string appAccount, string appAccounts)
        {
            Encoding encoding = Encoding.GetEncoding("utf-8");
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            IDictionary<string, string> headers = new Dictionary<string, string>();

            headers.Add("Content-Type", "application/json");

            headers.Add("appKey", appKey);
            headers.Add("appSecret", appSecret);
            headers.Add("appAccount", appAccount);

            parameters.Add("topicName", "test");
            parameters.Add("accounts", appAccounts);

            string output = JsonConvert.SerializeObject(parameters);

            HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(httpUrl + "/" + appId, output, null, null, encoding, null, headers);
            string cookieString = myResponse.Headers["Set-Cookie"];
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd();
            JObject jo = (JObject)JsonConvert.DeserializeObject(content);
            string code = jo.GetValue("code").ToString();
            if (code != "200")
            {
                logger.DebugFormat("create topic error:{0}", content);
                return 0;
            }
            JObject data = (JObject)jo.GetValue("data");
            JObject topicInfo = (JObject)data.GetValue("topicInfo");

            return long.Parse(topicInfo.GetValue("topicId").ToString());
        }

        private static string GenerateRandomString(int length)
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

        public void HandleStatusChange(object source, StateChangeEventArgs e)
        {
            logger.InfoFormat("{0} OnlineStatusHandler status:{1},errType:{2},errReason:{3},errDescription:{4}!", e.User.AppAccount, e.IsOnline, e.ErrType, e.ErrReason, e.ErrDescription);
        }

        public void HandleMessage(object source, MessageEventArgs e)
        {
            List<P2PMessage> packets = e.Packets;
            logger.InfoFormat("HandleMessage, to:{0}, packetCount:{1}", e.User.AppAccount, packets.Count);
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleMessage packets.Count==0");
                return;
            }
            foreach (P2PMessage msg in packets)
            {
                logger.InfoFormat("HandleMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                    e.User.AppAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                    Encoding.UTF8.GetString(msg.Payload));
            }
        }

        public void HandleMessageTimeout(object source, SendMessageTimeoutEventArgs e)
        {
            P2PMessage msg = e.P2PMessage;
            logger.InfoFormat("HandleSendMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                                   e.User.AppAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                                   Encoding.UTF8.GetString(msg.Payload));
        }

        public void HandleGroupMessage(object source, GroupMessageEventArgs e)
        {
            List<P2TMessage> packets = e.Packets;
            logger.InfoFormat("HandleGroupdMessage, to:{0}, packetCount:{1}", e.User.AppAccount, packets.Count);
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleGroupdMessage packets.Count==0");
                return;
            }
            foreach (P2TMessage msg in packets)
            {
                logger.InfoFormat("HandleGroupdMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                      e.User.AppAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                    Encoding.UTF8.GetString(msg.Payload));
            }
        }

        public void HandleGroupMessageTimeout(object source, SendGroupMessageTimeoutEventArgs e)
        {
            P2TMessage msg = e.P2tMessage;
            logger.InfoFormat("HandleSendGroupdMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                       e.User.AppAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                     Encoding.UTF8.GetString(msg.Payload));
        }

        public void HandleUnlimitedGroupMessage(object source, UnlimitedGroupMessageEventArgs e)
        {
            mimc.UCPacket msg = e.Packet;
            logger.InfoFormat("HandleUnlimitedGroupMessage, to:{0}, packetId:{1}, type:{2}",
                     e.User.AppAccount, msg.packetId, msg.type);

        }

        public void HandleUnlimitedGroupMessageTimeout(object source, SendUnlimitedGroupMessageTimeoutEventArgs e)
        {
            mimc.UCPacket msg = e.Packet;
            logger.InfoFormat("HandleUnlimitedGroupMessageTimeout, to:{0}, packetId:{1}, type:{2}",
                     e.User.AppAccount, msg.packetId, msg.type);
        }

        public void HandleJoinUnlimitedGroup(object source, JoinUnlimitedGroupEventArgs e)
        {
            mimc.UCJoinResp msg = e.Packet;
            logger.InfoFormat("HandleJoinUnlimitedGroup, to:{0}, code:{1}, msg:{2}",
                     e.User.AppAccount, msg.code, msg.message);
        }

        public void HandleQuitUnlimitedGroup(object source, QuitUnlimitedGroupEventArgs e)
        {
            mimc.UCQuitResp msg = e.Packet;
            logger.InfoFormat("HandleQuitUnlimitedGroup, to:{0}, code:{1}, msg:{2}",
                    e.User.AppAccount, msg.code, msg.message);
        }
        public void HandleDismissUnlimitedGroup(object source, DismissUnlimitedGroupEventArgs e)
        {
            mimc.UCPacket msg = e.Packet;
            logger.InfoFormat("HandleDismissUnlimitedGroup, to:{0}, packetId:{1}, type:{2}",
                     e.User.AppAccount, msg.packetId, msg.type);
        }

        public void HandleServerACK(object source, ServerACKEventArgs e)
        {
            ServerAck serverAck = e.ServerAck;
            if (serverAck.ErrorMsg!=null&& serverAck.ErrorMsg != "")
            {
                logger.WarnFormat("{0} HandleServerACK, some errors ErrorMsg:{1}",
                 e.User.AppAccount, serverAck.ErrorMsg);
            }
            logger.InfoFormat("HandleServerACK, appAccount:{0}, packetId:{1}, sequence:{2}, ts:{3},ErrorMsg:{4}",
                  e.User.AppAccount, serverAck.PacketId, serverAck.Sequence, serverAck.Timestamp, serverAck.ErrorMsg);
        }
    }
}
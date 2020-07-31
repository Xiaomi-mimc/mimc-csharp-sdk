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
        // online
        private static string topicUrl = "https://mimc.chat.xiaomi.net/api/topic/";
        private static string url = "https://mimc.chat.xiaomi.net/api/account/token";
        private static string appId = "2882303761517669588";
        private static string appKey = "5111766983588";
        private static string appSecret = "b0L3IOz/9Ob809v8H2FbVg==";

        // staging
        //private static string topicUrl = "http://10.38.162.149/api/topic/";
        //private static string url = "http://10.38.162.149/api/account/token";
        //private static string appId = "2882303761517479657";
        //private static string appKey = "5221747911657";
        //private static string appSecret = "PtfBeZyC+H8SIM/UXhZx1w==";

        private string appAccount1 = "5566";
        private string appAccount2 = "9527";
        private MIMCUser user5566;
        private MIMCUser user9527;

        static ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            Run();
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
            Thread.Sleep(5 * 1000);

            demo.SendUnlimitedGroupMessage();

            Thread.Sleep(5 * 1000);

            if (!demo.Over())
            {
                return;
            }
        }

        public MIMCDemo()
        {
            user5566 = new MIMCUser(appAccount1);
            user5566.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecret, url, appAccount1));
            user5566.stateChangeEvent += HandleStatusChange;
            user5566.messageEvent += HandleMessage;
            user5566.messageTimeOutEvent += HandleMessageTimeout;
            user5566.groupMessageEvent += HandleGroupMessage;
            user5566.groupMessageTimeoutEvent += HandleGroupMessageTimeout;
            user5566.serverACKEvent += HandleServerACK;
            user5566.unlimitedGroupMessageEvent += HandleUnlimitedGroupMessage;
            user5566.unlimitedGroupMessageTimeoutEvent += HandleUnlimitedGroupMessageTimeout;
            user5566.joinUnlimitedGroupEvent += HandleJoinUnlimitedGroup;
            user5566.quitUnlimitedGroupEvent += HandleQuitUnlimitedGroup;
            user5566.dismissUnlimitedGroupEvent += HandleDismissUnlimitedGroup;

            user9527 = new MIMCUser(appAccount2);
            user9527.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecret, url, appAccount2));
            user9527.stateChangeEvent += HandleStatusChange;
            user9527.messageEvent += HandleMessage;
            user9527.messageTimeOutEvent += HandleMessageTimeout;
            user9527.groupMessageEvent += HandleGroupMessage;
            user9527.groupMessageTimeoutEvent += HandleGroupMessageTimeout;
            user9527.serverACKEvent += HandleServerACK;
            user9527.unlimitedGroupMessageEvent += HandleUnlimitedGroupMessage;
            user9527.unlimitedGroupMessageTimeoutEvent += HandleUnlimitedGroupMessageTimeout;
            user9527.joinUnlimitedGroupEvent += HandleJoinUnlimitedGroup;
            user9527.quitUnlimitedGroupEvent += HandleQuitUnlimitedGroup;
            user9527.dismissUnlimitedGroupEvent += HandleDismissUnlimitedGroup;
        }

        bool Ready()
        {
            if (!user5566.Login())
            {
                logger.ErrorFormat("Login Fail, {0}", user5566.AppAccount);
                return false;
            }
            if (!user9527.Login())
            {
                logger.ErrorFormat("Login Fail, {0}", user9527.AppAccount);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 注销用户
        /// </summary>
        bool Over()
        {
            if (!user5566.Logout())
            {
                logger.ErrorFormat("Logout Fail, {0}", user5566.AppAccount);
                return false;
            }
            user5566.Destroy();
            if (!user9527.Logout())
            {
                logger.ErrorFormat("Logout Fail, {0}", user9527.AppAccount);
                return false;
            }
            user9527.Destroy();

            Thread.Sleep(1000);
            return true;
        }

        /// <summary>
        /// 发送单聊消息测试
        /// </summary>
        void SendMessage()
        {
            if (!user5566.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", user5566.AppAccount);
                return;
            }
            if (!user9527.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", user9527.AppAccount);
                return;
            }

            String packetId = user5566.SendMessage(user9527.AppAccount, UTF8Encoding.Default.GetBytes("Are you OK?" + DateTime.Now.ToString("u")), "P2P");
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", user5566.AppAccount, user9527.AppAccount, packetId);
            Thread.Sleep(100);

            packetId = user9527.SendMessage(user5566.AppAccount, UTF8Encoding.Default.GetBytes("I'm OK!" + DateTime.Now.ToString("u")), "P2P");
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", user9527.AppAccount, user5566.AppAccount, packetId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送单聊消息测试
        /// </summary>
        void GetP2PHistoryMessage()
        {
            if (!user5566.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", user5566.AppAccount);
                return;
            }
            if (!user9527.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", user9527.AppAccount);
                return;
            }

            String data = user5566.GetP2PHistory(user9527.AppAccount, user5566.AppAccount, 0,DateTime.Now.Millisecond,null,null);
            logger.InfoFormat("GetP2PHistoryMessage, {0}-->{1}, PacketId:{2}", user5566.AppAccount, user9527.AppAccount, data);
            Thread.Sleep(100);

        }

        /// <summary>
        /// 发送群聊消息测试
        /// </summary>
        void SendGroupMessage()
        {
            if (!user5566.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user5566.AppAccount);
                return;
            }
            if (!user9527.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user9527.AppAccount);
                return;
            }
            long topicId = CreateNormalTopic(topicUrl, appAccount1, appAccount1 + "," + appAccount2);

            String packetId = user5566.SendGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")), "P2T");

            logger.InfoFormat("SendGroupMessage, {0}-->{1}, PacketId:{2}", user5566.AppAccount, user9527.AppAccount, packetId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 创建无限大群测试
        /// </summary>
        void CreateUnlimitedGroup()
        {
            if (!user5566.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user5566.AppAccount);
                return;
            }
            string topicId = user5566.CreateUnlimitedGroup("test");
            logger.InfoFormat("CreateUnlimitedGroup, {0}, topicId:{1}", user5566.AppAccount, topicId);
            Thread.Sleep(100);
        }

        void JoinUnlimitedGroup()
        {
            if (!user9527.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user9527.AppAccount);
                return;
            }

            user9527.JoinUnlimitedGroup(29516394685530112);
            Thread.Sleep(3 * 1000);
        }

        /// <summary>
        /// 解散无限大群测试
        /// </summary>
        void DismissUnlimitedGroup(long topicId)
        {
            if (!user5566.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user5566.AppAccount);
                return;
            }

            user5566.DismissUnlimitedGroup(topicId);
            logger.InfoFormat("DismissUnlimitedGroup, {0}, topicId:{1}", user5566.AppAccount, topicId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 加入不存在的无限大群测试
        /// </summary>
        void JoinInexistentUnlimitedGroup()
        {
            if (!user5566.IsOnline())
            {
                logger.ErrorFormat("{0} offline, quit!", user5566.AppAccount);
                return;
            }

            user5566.JoinUnlimitedGroup(12311111111111111L);
            logger.InfoFormat("JoinInexistentUnlimitedGroup, {0}, topicId:{1}", user5566.AppAccount, 12311111111111111L);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 退出不存在的无限大群测试
        /// </summary>
        void QuitInexistentUnlimitedGroup()
        {
            if (!user5566.IsOnline())
            {
                logger.ErrorFormat("{0} offline, quit!", user5566.AppAccount);
                return;
            }

            user5566.QuitUnlimitedGroup(12311111111111111L);
            logger.InfoFormat("QuitUnlimitedGroup, {0}, topicId:{1}", user5566.AppAccount, 12311111111111111L);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送无限大群消息测试
        /// </summary>
        void SendUnlimitedGroupMessage()
        {
            if (!user5566.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user5566.AppAccount);
                return;
            }
            long topicId = long.Parse(user5566.CreateUnlimitedGroup("testUC"));
            Thread.Sleep(100);

            if (topicId == 0)
            {
                logger.ErrorFormat("SendUnlimitedGroupMessage CreateUnlimitedGroup error , {0}-->{1},uuid:{2}", user5566.AppAccount, topicId, user5566.Uuid);
                return;
            }
            if (!user9527.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", user9527.AppAccount);
                return;
            }
            user9527.JoinUnlimitedGroup(topicId);
            Thread.Sleep(2000);
            String result = user5566.GetUnlimitedGroupUsersNum(topicId);
            logger.InfoFormat("SendUnlimitedGroupMessage  result:{0}", result);
            String userListResult = user5566.GetUnlimitedGroupUsers(topicId);
            logger.InfoFormat("SendUnlimitedGroupMessage userListResult:{0}", userListResult);
            String packetId = null;
            for (int i = 0; i < 10000000; i++)
            {
                packetId = user5566.SendUnlimitedGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody! I am 5566 " + DateTime.Now.ToString("u")), "UC");
                logger.DebugFormat("SendUnlimitedGroupMessage, {0}-->{1}, PacketId:{2},uuid:{3}, uuid2:{4}", user5566.AppAccount, topicId, packetId, user5566.Uuid, user9527.Uuid);
                packetId = user9527.SendUnlimitedGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody! I am 9527 " + DateTime.Now.ToString("u")), "UC");
                logger.DebugFormat("SendUnlimitedGroupMessage, {0}-->{1}, PacketId:{2},uuid:{3}, uuid2:{4}", user9527.AppAccount, topicId, packetId, user9527.Uuid, user5566.Uuid);
                Thread.Sleep(5 *1000);
            }
            Thread.Sleep(20000);
            user9527.QuitUnlimitedGroup(topicId);
            Thread.Sleep(1000);

            bool dismissFlag = user5566.DismissUnlimitedGroup(topicId);
            logger.InfoFormat("SendUnlimitedGroupMessage, DismissUnlimitedGroup AppAccount:{0}-->topicId:{1}, PacketId:{2},uuid:{3},uuid2:{4}", user5566.AppAccount, topicId, packetId, user5566.Uuid, user9527.Uuid);

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
            logger.InfoFormat("OnlineStatusHandler status:{0}, errType:{1}, errReason:{2}, errDescription:{3}!", e.IsOnline, e.Type, e.Reason, e.Desc);
        }

        public void HandleMessage(object source, MessageEventArgs e)
        {
            List<P2PMessage> packets = e.Packets;
            logger.InfoFormat("HandleMessage, packetCount:{1}", packets.Count);
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleMessage packets.Count==0");
                return;
            }
            foreach (P2PMessage msg in packets)
            {
                logger.InfoFormat(">>>> HandleMessage, from:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                    msg.FromAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                    Encoding.UTF8.GetString(msg.Payload));
            }
        }

        public void HandleMessageTimeout(object source, SendMessageTimeoutEventArgs e)
        {
            P2PMessage msg = e.P2PMessage;
            logger.InfoFormat(">>>> HandleSendMessageTimeout, from:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                                   msg.FromAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                                   Encoding.UTF8.GetString(msg.Payload));
        }

        public void HandleGroupMessage(object source, GroupMessageEventArgs e)
        {
            List<P2TMessage> packets = e.Packets;
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleGroupdMessage packets.Count==0");
                return;
            }
            foreach (P2TMessage msg in packets)
            {
                logger.InfoFormat("HandleGroupdMessage, from:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                      msg.FromAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                    Encoding.UTF8.GetString(msg.Payload));
            }
        }

        public void HandleGroupMessageTimeout(object source, SendGroupMessageTimeoutEventArgs e)
        {
            P2TMessage msg = e.P2tMessage;
            logger.InfoFormat("HandleSendGroupdMessageTimeout, from:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                      msg.FromAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                     Encoding.UTF8.GetString(msg.Payload));
        }

        public void HandleUnlimitedGroupMessage(object source, UnlimitedGroupMessageEventArgs e)
        {
            List<P2UMessage> packets = e.P2uMessagesList;
            logger.InfoFormat("HandleUnlimitedGroupMessage, packetCount:{0}", packets.Count);
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleUnlimitedGroupMessage packets.Count==0");
                return;
            }
            foreach (P2UMessage msg in packets)
            {
                logger.InfoFormat(">>>> HandleUnlimitedGroupMessage, from:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}, type:{5}",
                      msg.FromAccount, msg.PacketId, msg.Sequence, msg.Timestamp, Encoding.UTF8.GetString(msg.Payload), msg.BizType);
            }

        }

        public void HandleUnlimitedGroupMessageTimeout(object source, SendUnlimitedGroupMessageTimeoutEventArgs e)
        {
            P2UMessage msg = e.Packet;
            logger.InfoFormat(">>>> HandleUnlimitedGroupMessageTimeout, from:{0}, packetId:{1}, type:{2}",
                     msg.FromAccount, msg.PacketId, msg.BizType);
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
            logger.InfoFormat("HandleDismissUnlimitedGroup, topicId:{0}", e.TopicId);
        }

        public void HandleServerACK(object source, ServerACKEventArgs e)
        {
            ServerAck serverAck = e.ServerAck;
            logger.InfoFormat("HandleServerACK, packetId:{0}, sequence:{1}, ts:{2}, code:{3}, desc:{4}",
                  serverAck.PacketId, serverAck.Sequence, serverAck.Timestamp, serverAck.Code, serverAck.Desc);
        }
    }
}
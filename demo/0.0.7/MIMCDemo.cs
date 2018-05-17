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
    /// MIMC C# Demo用例，请确保更新到1.0.4以上版本
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
        private static string url = "https://mimc.chat.xiaomi.net/api/account/token";
        private static string topicUrl = "https://mimc.chat.xiaomi.net/api/topic/";
        private static string appId = "2882303761517613988";
        private static string appKey = "5361761377988";
        private static string appSecret = "2SZbrJOAL1xHRKb7L9AiRQ==";

        private string appAccount1 = "leijun"+ GenerateRandomString(5);
        private string appAccount2 = "linbin"+ GenerateRandomString(5);
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

            demo.SendMessageAsync();
            Thread.Sleep(1000);

            demo.SendGroupMessageAsync();
            Thread.Sleep(2000);

            if (!await demo.OverAsync())
            {
                return;
            }
        }

        public MIMCDemo()
        {
            leijun = new MIMCUser(appAccount1);
            leijun.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecret, url, appAccount1));
            leijun.StateChangeEvent += HandleStatusChange;

            leijun.MessageEvent += HandleMessage;
            leijun.MessageTimeOutEvent += HandleMessageTimeout;
            leijun.GroupMessageEvent += HandleGroupMessage;
            leijun.GroupMessageTimeoutEvent += HandleGroupMessageTimeout;
            leijun.ServerACKEvent += HandleServerACK;

            linbin = new MIMCUser(appAccount2);
            linbin.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecret, url, appAccount2));
            linbin.StateChangeEvent += HandleStatusChange;
            linbin.MessageEvent += HandleMessage;
            linbin.MessageTimeOutEvent += HandleMessageTimeout;
            linbin.GroupMessageEvent += HandleGroupMessage;
            linbin.GroupMessageTimeoutEvent += HandleGroupMessageTimeout;
            linbin.ServerACKEvent += HandleServerACK;
        }

        public void HandleStatusChange(object source, StateChangeEventArgs e)
        {
            logger.InfoFormat("{0} OnlineStatusHandler status:{1},errType:{2},errReason:{3},errDescription:{4}!",e.User.AppAccount(), e.IsOnline, e.ErrType, e.ErrReason, e.ErrDescription);
        }

        public void HandleMessage(object source, MessageEventArgs e)
        {
            List<P2PMessage> packets = e.Packets;
            logger.InfoFormat("MIMCMessageHandler HandleMessage, to:{0}, packetCount:{1}", e.User.AppAccount(), packets.Count);
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleMessage packets.Count==0");
                return;
            }
            foreach (P2PMessage msg in packets)
            {
                logger.InfoFormat("MIMCMessageHandler HandleMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                    e.User.AppAccount(), msg.PacketId, msg.Sequence, msg.Timestamp,
                    Encoding.UTF8.GetString(msg.Payload));
            }
        }

        public void HandleMessageTimeout(object source, SendMessageTimeoutEventArgs e)
        {
            P2PMessage msg = e.P2PMessage;
            logger.InfoFormat("MIMCMessageHandler HandleSendMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                                   e.User.AppAccount(), msg.PacketId, msg.Sequence, msg.Timestamp,
                                   Encoding.UTF8.GetString(msg.Payload));
        }

        public void HandleGroupMessage(object source, GroupMessageEventArgs e)
        {
            List<P2TMessage> packets = e.Packets;
            logger.InfoFormat("MIMCMessageHandler HandleGroupdMessage, to:{0}, packetCount:{1}", e.User.AppAccount(), packets.Count);
            if (packets.Count == 0)
            {
                logger.WarnFormat("HandleGroupdMessage packets.Count==0");
                return;
            }
            foreach (P2TMessage msg in packets)
            {
                logger.InfoFormat("MIMCMessageHandler HandleGroupdMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                      e.User.AppAccount(), msg.PacketId, msg.Sequence, msg.Timestamp,
                    Encoding.UTF8.GetString(msg.Payload));
            }
        }

        public void HandleGroupMessageTimeout(object source, SendGroupMessageTimeoutEventArgs e)
        {
            P2TMessage msg = e.P2tMessage;
            logger.InfoFormat("MIMCMessageHandler HandleSendGroupdMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                       e.User.AppAccount(), msg.PacketId, msg.Sequence, msg.Timestamp,
                     Encoding.UTF8.GetString(msg.Payload));
        }

        public void HandleServerACK(object source, ServerACKEventArgs e)
        {
            ServerAck serverAck = e.ServerAck;
            logger.InfoFormat("{0} MIMCMessageHandler HandleServerACK, appAccount:{0}, packetId:{1}, sequence:{2}, ts:{3}",
                  e.User.AppAccount(), serverAck.PacketId, serverAck.Sequence, serverAck.Timestamp);
        }

        bool Ready()
        {
            if (!leijun.Login())
            {
                logger.ErrorFormat("Login Fail, {0}", leijun.AppAccount());
                return false;
            }
            if (!linbin.Login())
            {
                logger.ErrorFormat("Login Fail, {0}", linbin.AppAccount());
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        async Task<bool> ReadyAsync()
        {
            if (!await leijun.LoginAsync())
            {
                logger.ErrorFormat("Login Fail, {0}", leijun.AppAccount());
                return false;
            }


            if (!await linbin.LoginAsync())
            {
                logger.ErrorFormat("Login Fail, {0}", linbin.AppAccount());
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
                logger.ErrorFormat("Logout Fail, {0}", leijun.AppAccount());
                return false;
            }
            if (!linbin.Logout())
            {
                logger.ErrorFormat("Logout Fail, {0}", linbin.AppAccount());
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
                logger.ErrorFormat("Logout Fail, {0}", leijun.AppAccount());
                return false;
            }
            if (!await linbin.LogoutAsync())
            {
                logger.ErrorFormat("Logout Fail, {0}", linbin.AppAccount());
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
                logger.DebugFormat("{0} login fail, quit!", leijun.AppAccount());
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", linbin.AppAccount());
                return;
            }

            String packetId = leijun.SendMessage(linbin.AppAccount(), UTF8Encoding.Default.GetBytes("Are you OK?" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount(), linbin.AppAccount(), packetId);
            Thread.Sleep(100);

            packetId = linbin.SendMessage(leijun.AppAccount(), UTF8Encoding.Default.GetBytes("I'm OK!" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", linbin.AppAccount(), leijun.AppAccount(), packetId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送单聊消息测试
        /// </summary>
        async void SendMessageAsync()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", leijun.AppAccount());
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", linbin.AppAccount());
                return;
            }

            String packetId = await leijun.SendMessageAsync(linbin.AppAccount(), UTF8Encoding.Default.GetBytes("Are you OK?" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount(), linbin.AppAccount(), packetId);
            Thread.Sleep(100);

            packetId = await linbin.SendMessageAsync(leijun.AppAccount(), UTF8Encoding.Default.GetBytes("I'm OK!" + DateTime.Now.ToString("u")));
            logger.InfoFormat("SendMessage, {0}-->{1}, PacketId:{2}", linbin.AppAccount(), leijun.AppAccount(), packetId);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 发送群聊消息测试
        /// </summary>
        void SendGroupMessage()
        {
           if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount());
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", linbin.AppAccount());
                return;
            }
            long topicId = CreateTopic(topicUrl,appAccount1, appAccount1 + "," + appAccount2);

            String packetId = leijun.SendGroupMessage(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")));

            logger.InfoFormat("SendGroupMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount(), linbin.AppAccount(), packetId);
            Thread.Sleep(100);
        }


        /// <summary>
        /// 发送群聊消息测试
        /// </summary>
        async void SendGroupMessageAsync()
        {
            if (!leijun.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", leijun.AppAccount());
                return;
            }
            if (!linbin.IsOnline())
            {
                logger.DebugFormat("{0} offline, quit!", linbin.AppAccount());
                return;
            }
            long topicId = CreateTopic(topicUrl, appAccount1, appAccount1 + "," + appAccount2);

            String packetId =await leijun.SendGroupMessageAsync(topicId, UTF8Encoding.Default.GetBytes("Hi,everybody!" + DateTime.Now.ToString("u")));

            logger.InfoFormat("SendGroupMessage, {0}-->{1}, PacketId:{2}", leijun.AppAccount(), linbin.AppAccount(), packetId);
            Thread.Sleep(100);
        }
        
        /// <summary>
        /// 在线状态回调接口实现
        /// </summary>
        class OnlineStatusHandler : IMIMCOnlineStatusHandler
        {
            private string appAccount;
            public OnlineStatusHandler(String appAccount)
            {
                this.appAccount = appAccount;
            }

            public void StatusChange(bool isOnline, string errType, string errReason, string errDescription)
            {
                logger.InfoFormat("{0} OnlineStatusHandler status:{1},errType:{2},errReason:{3},errDescription:{4}!", this.appAccount, isOnline, errType, errReason, errDescription);
            }
        }

        /// <summary>
        /// 消息回调接口实现
        /// </summary>
        class MIMCMessageHandler : IMIMCMessageHandler
        {
            private string appAccount;
            public MIMCMessageHandler(String appAccount)
            {
                this.appAccount = appAccount;
            }

            public void HandleMessage(List<P2PMessage> packets)
            {
                logger.InfoFormat("MIMCMessageHandler HandleMessage, to:{0}, packetCount:{1}", this.appAccount, packets.Count);
                if (packets.Count==0)
                {
                    logger.WarnFormat("HandleMessage packets.Count==0");
                    return;
                }
                foreach (P2PMessage msg in packets)
                {
                    logger.InfoFormat("MIMCMessageHandler HandleMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                        this.appAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                        Encoding.UTF8.GetString(msg.Payload));
                }
            }


            public void HandleGroupMessage(List<P2TMessage> packets)
            {
                logger.InfoFormat("MIMCMessageHandler HandleGroupdMessage, to:{0}, packetCount:{1}", this.appAccount, packets.Count);
                if (packets.Count == 0)
                {
                    logger.WarnFormat("HandleGroupdMessage packets.Count==0");
                    return;
                }
                foreach (P2TMessage msg in packets)
                {
                    logger.InfoFormat("MIMCMessageHandler HandleGroupdMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                        this.appAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                        Encoding.UTF8.GetString(msg.Payload));
                }
            }

            public void HandleSendMessageTimeout(P2PMessage msg)
            {
                logger.InfoFormat("MIMCMessageHandler HandleSendMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                       this.appAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                       Encoding.UTF8.GetString(msg.Payload));
            }

            public void HandleSendGroupMessageTimeout(P2TMessage msg)
            {
                logger.InfoFormat("MIMCMessageHandler HandleSendGroupdMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                         this.appAccount, msg.PacketId, msg.Sequence, msg.Timestamp,
                         Encoding.UTF8.GetString(msg.Payload));
            }

            public void HandleServerACK(ServerAck serverAck)
            {
                logger.InfoFormat("{0} MIMCMessageHandler HandleServerACK, appAccount:{0}, packetId:{1}, sequence:{2}, ts:{3}",
                    this.appAccount, serverAck.PacketId, serverAck.Sequence, serverAck.Timestamp);
            }
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
                Encoding encoding = Encoding.GetEncoding("utf-8");
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("appId", appId);
                parameters.Add("appKey", appKey);
                parameters.Add("appSecret", appSecret);
                parameters.Add("appAccount", appAccount);
                string output = JsonConvert.SerializeObject(parameters);

                HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(httpUrl, output, null, null, encoding, null);
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
                Encoding encoding = Encoding.GetEncoding("utf-8");
                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("appId", appId);
                parameters.Add("appKey", appKey);
                parameters.Add("appSecret", appSecret);
                parameters.Add("appAccount", appAccount);
                string output = JsonConvert.SerializeObject(parameters);

                HttpWebResponse myResponse =await HttpWebResponseUtil.CreatePostHttpResponseAsync(httpUrl, output, null, null, encoding, null,null);
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
        public long CreateTopic(string httpUrl, string appAccount, string appAccounts)
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

            HttpWebResponse myResponse = HttpWebResponseUtil.CreatePostHttpResponse(httpUrl+"/"+ appId, output, null, null, encoding, null, headers);
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
    }
}
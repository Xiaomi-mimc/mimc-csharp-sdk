using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using com.xiaomi.mimc;
using com.xiaomi.mimc.handler;
using com.xiaomi.mimc.packet;
using com.xiaomi.mimc.utils;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sdk.demo
{
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
        private static string appId = "2882303761517613988";
        private static string appKey = "5361761377988";
        private static string appSecurity = "2SZbrJOAL1xHRKb7L9AiRQ==";

        private string appAccount1 = "leijun";
        private string appAccount2 = "linbin";
        private User leijun;
        private User linbin;

        static ILog logger = LogManager.GetLogger("log");

        public static void Main(string[] args)
        {
            logger.DebugFormat("demo start");

            MIMCDemo demo = new MIMCDemo();
            if (!demo.ready())
            {
                return;
            }
            demo.sendMessage();
            Thread.Sleep(2000);

            demo.over();
            Console.ReadKey();
        }
        public MIMCDemo()
        {
            leijun = new User(long.Parse(appId), appAccount1);
            leijun.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecurity, url, appAccount1));
            leijun.RegisterOnlineStatusHandler(new OnlineStatusHandler(leijun.AppAccount()));
            leijun.RegisterMIMCMessageHandler(new MIMCMessageHandler(leijun.AppAccount()));

            linbin = new User(long.Parse(appId), appAccount2);
            linbin.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecurity, url, appAccount2));
            linbin.RegisterOnlineStatusHandler(new OnlineStatusHandler(linbin.AppAccount()));
            linbin.RegisterMIMCMessageHandler(new MIMCMessageHandler(linbin.AppAccount()));
        }

        bool ready()
        {
            if (!leijun.Login())
            {
                logger.ErrorFormat("LoginFail, {0}", leijun.AppAccount());
                return false;
            }
            if (!linbin.Login())
            {
                logger.ErrorFormat("LoginFail, {0}", linbin.AppAccount());
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        void over()
        {
            leijun.Logout();
            linbin.Logout();
            Thread.Sleep(2000);
        }

        void sendMessage()
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

        class OnlineStatusHandler : IMIMCOnlineStatusHandler
        {
            private string appAccount;
            public OnlineStatusHandler(String appAccount)
            {
                this.appAccount = appAccount;
            }

            public void statusChange(bool isOnline, string errType, string errReason, string errDescription)
            {
                logger.InfoFormat("{0} OnlineStatusHandler status:{1},errType:{2},errReason:{3},errDescription:{4}!", this.appAccount, isOnline, errType, errReason, errDescription);
            }
        }

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
                foreach (P2PMessage msg in packets)
                {
                    logger.InfoFormat("MIMCMessageHandler HandleMessage, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                        this.appAccount, msg.getPacketId(), msg.getSequence(), msg.getTimestamp(),
                        Encoding.UTF8.GetString(msg.getPayload()));
                }
            }

            public void HandleSendMessageTimeout(P2PMessage msg)
            {
                logger.InfoFormat("MIMCMessageHandler HandleSendMessageTimeout, to:{0}, packetId:{1}, sequence:{2}, ts:{3}, payload:{4}",
                       this.appAccount, msg.getPacketId(), msg.getSequence(), msg.getTimestamp(),
                       Encoding.UTF8.GetString(msg.getPayload()));
            }

            public void HandleServerACK(ServerAck serverAck)
            {
                logger.InfoFormat("{0} MIMCMessageHandler HandleServerACK, appAccount:{0}, packetId:{1}, sequence:{2}, ts:{3}",
                    this.appAccount, serverAck.getPacketId(), serverAck.getSequence(), serverAck.getTimestamp());
            }
        }
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

            /**
             * @important:
             *     此例中，fetchToken()直接上传(appId/appKey/appSecurity/appAccount)给小米TokenService，获取Token使用
             *     实际上，在生产环境中，fetchToken()应该只上传appAccount+password/cookies给AppProxyService，AppProxyService
             *     验证鉴权通过后，再上传(appId/appKey/appSecurity/appAccount)给小米TokenService，获取Token后返回给fetchToken()
             * @important:
             *     appId/appKey/appSecurity绝对不能如此用例一般存放于APP本地
             **/
            public String fetchToken()
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
                return content;
            }
        }
    }
}

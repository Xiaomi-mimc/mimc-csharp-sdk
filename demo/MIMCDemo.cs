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
        private static string url = "http://10.38.162.149/api/account/token";
        private static string appId = "2882303761517479657";
        private static string appKey = "5221747911657";
        private static string appSecurity = "PtfBeZyC+H8SIM/UXhZx1w==";

        private string appAccount1 = "leijun";
        private string appAccount2 = "linbin";
        private User leijunUser;
        private User linbinUser;

        static ILog logger = LogManager.GetLogger("log");
        public static void Main(string[] args)
        {
            logger.DebugFormat("demo start");
            MIMCDemo demo = new MIMCDemo();
            demo.ready();
            demo.sendMessage();
            Thread.Sleep(2000);

            demo.over();
            Console.ReadKey();
        }
        public MIMCDemo()
        {
            leijunUser = new User(long.Parse(appId), appAccount1);
            leijunUser.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecurity, url, appAccount1));
            leijunUser.RegisterOnlineStatusHandler(new OnlineStatusHandler(leijunUser.AppAccount()));
            leijunUser.RegisterMIMCMessageHandler(new MIMCMessageHandler(leijunUser.AppAccount()));

            linbinUser = new User(long.Parse(appId), appAccount2);
            linbinUser.RegisterMIMCTokenFetcher(new MIMCCaseTokenFetcher(appId, appKey, appSecurity, url, appAccount2));
            linbinUser.RegisterOnlineStatusHandler(new OnlineStatusHandler(linbinUser.AppAccount()));
            linbinUser.RegisterMIMCMessageHandler(new MIMCMessageHandler(linbinUser.AppAccount()));
        }

        void ready()
        {
            leijunUser.Login();
            Thread.Sleep(200);
            linbinUser.Login();
            Thread.Sleep(200);
        }


        void over()
        { 
            leijunUser.Logout();
            Thread.Sleep(2000);
            linbinUser.Logout();
        }
        void sendMessage()
        {
            logger.DebugFormat("demo start send message", leijunUser.AppAccount());

            if (!leijunUser.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", leijunUser.AppAccount());
                return;
            }
            if (!linbinUser.IsOnline())
            {
                logger.DebugFormat("{0} login fail, quit!", linbinUser.AppAccount());
                return;
            }

            leijunUser.SendMessage(linbinUser.AppAccount(), UTF8Encoding.Default.GetBytes("Are you OK?"+ DateTime.Now.ToString("u")));
            Thread.Sleep(100);
            linbinUser.SendMessage(leijunUser.AppAccount(), UTF8Encoding.Default.GetBytes("I'm OK!"+ DateTime.Now.ToString("u")));
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
                logger.DebugFormat("{0} OnlineStatusHandler status:{1},errType:{2},errReason:{3},errDescription:{4}!", this.appAccount, isOnline, errType, errReason, errDescription);
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
                logger.DebugFormat("{0} MIMCMessageHandler HandleMessage packets size:{1}", this.appAccount, packets.Capacity);
            }

            public void HandleGroupMessage(List<P2TMessage> packets)
            {
                logger.DebugFormat("{0} MIMCMessageHandler HandleGroupMessage packets size:{1}", this.appAccount, packets.Capacity);
            }

            //public void HandleSendMessageTimeout(P2PMessage message)
            //{
            //    logger.DebugFormat("{0} MIMCMessageHandler HandleSendMessageTimeout message:{1}", this.appAccount, message.getPacketId());
            //}

            //public void HandleSendGroupMessageTimeout(P2TMessage message)
            //{
            //    logger.DebugFormat("{0} MIMCMessageHandler HandleSendGroupMessageTimeout message:{1}", this.appAccount, message.getPacketId());
            //}

            public void HandleServerACK(ServerAck serverAck)
            {
                logger.DebugFormat("{0} MIMCMessageHandler HandleServerACK message:{1}", this.appAccount, serverAck.getPacketId());
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

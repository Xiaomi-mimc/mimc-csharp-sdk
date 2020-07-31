using com.xiaomi.mimc;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mimc_csharp_sdk.com.xiaomi.mimc.common
{
    class QueryUnlimitedGroupsThread
    {
        private static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private MIMCUser user;
        private Thread thread;
        private volatile bool exit = false;


        public QueryUnlimitedGroupsThread(MIMCUser user)
        {
            this.user = user;
            thread = new Thread(new ThreadStart(run));
        }

        public bool isAlive()
        {
            return thread != null && thread.IsAlive;
        }

        public bool Exit {
            get => exit;
            set {
                exit = value;
                if (thread != null) {
                    thread.Interrupt();
                }
            }
        }

        public void Start()
        {
            thread.Start();
        }

        private void run()
        {
            while (!Exit)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.Token))
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    if (user.QueryUnlimitedGroups())
                    {
                        break;
                    } else
                    {
                        Thread.Sleep(200);
                    }
                } catch(Exception ex)
                {
                    logger.ErrorFormat("QueryUnlimitedGroupsThread.run {0}", ex.StackTrace);
                }
            }
        }
    }
}

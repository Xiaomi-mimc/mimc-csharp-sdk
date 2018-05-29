using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.frontend;
using com.xiaomi.mimc.packet;
using com.xiaomi.mimc.utils;

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
namespace com.xiaomi.mimc.client
{
    public class MIMCConnection
    { 
        public enum State
        {
            NOT_CONNECTED,
            SOCK_CONNECTED,
            HANDSHAKE_CONNECTED
        }

        private string host;
        private int port;

        private long lastPingTimestampMs;
        private long nextResetSockTimestamp;

        private IFrontendPeerFetcher peerFetcher;
        private Queue<MIMCObject> packetWaitToSend;

        private byte[] rc4Key = null;

        private string challenge;

        private string connpt = "";
        private string model = "";
        private string os = "";
        private string udid = "";
        private string sdk = "";
        private string locale = "";
        private int andVer = 0;
        private MIMCUser user;
        private TcpClient tcpConnection;
        private State connState;

        public String Udid { get => udid; }
        public State ConnState { get => connState; set => connState = value; }
        public TcpClient TcpConnection { get => tcpConnection; set => tcpConnection = value; }
        public MIMCUser User { get => user; set => user = value; }
        public string Host { get => host; set => host = value; }
        public int Port { get => port; set => port = value; }
        public Queue<MIMCObject> PacketWaitToSend { get => packetWaitToSend; set => packetWaitToSend = value; }
        public string Challenge { get => challenge; set => challenge = value; }
        public byte[] Rc4Key { get => rc4Key; set => rc4Key = value; }
        public long NextResetSockTimestamp { get => nextResetSockTimestamp; set => nextResetSockTimestamp = value; }

        public MIMCConnection()
        {
            this.NextResetSockTimestamp = -1;
            this.ConnState = State.NOT_CONNECTED;
            this.PacketWaitToSend = new Queue<MIMCObject>();
            this.TcpConnection = null;
            peerFetcher = new ProdFrontendPeerFetcher();
        }

        public void reset()
        {
            Console.WriteLine("{0} MIMCConnection reset {1}",this.User.AppAccount(), this.User.Status);
            if (this.TcpConnection != null)
            {
                this.TcpConnection.Close();
            }

            this.ConnState = State.NOT_CONNECTED;
            this.user.LastLoginTimestamp = 0;
            this.user.LastCreateConnTimestamp = 0;
            this.user.Status = Constant.OnlineStatus.Offline;

            this.rc4Key = null;
            this.TcpConnection = null;
            this.ClearNextResetSockTimestamp();

            peerFetcher = new ProdFrontendPeerFetcher();

            this.user.OnlineStatusHandler.StatusChange(false, null, "NETWORK_RESET", "NETWORK_RESET");
            Console.WriteLine("{0} MIMCConnection reset {1}", this.User.AppAccount(), this.User.Status);


        }

        internal bool Connect()
        {
            Peer peer = peerFetcher.Peer();
            try
            {
                this.Host = peer.host;
                this.Port = peer.port;
                this.Rc4Key = null;

                this.TcpConnection = new TcpClient();
                this.TcpConnection.Connect(peer.host, peer.port);
                Console.WriteLine("MIMCConnection Server Connected! {0}-->{1}", this.TcpConnection.Client.LocalEndPoint, this.TcpConnection.Client.RemoteEndPoint);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return this.TcpConnection.Connected;
        }

        public static int readn(NetworkStream stream, byte[] buffer, int length)
        {
            if (stream == null || length <= 0 || buffer == null)
            {
                Console.WriteLine("readn fail,stream:{0},buffer.Length:{1}", stream, buffer.Length);
                return -1;
            }

            try
            {
                int leftLength = length;
                while (leftLength > 0)
                {
                    int bytesRead = stream.Read(buffer, 0, leftLength);
                    leftLength = leftLength - bytesRead;
                }
                return length;
            }
            catch (Exception ex)
            {
                Console.WriteLine("readn fail,exception:{0}", ex.Message);
                return -1;
            }
        }

        public void setChallengeAndRc4Key(String challenge)
        {
            this.challenge = challenge;
            String key = udid.Substring(udid.Length / 2) + challenge.Substring(challenge.Length / 2);
            this.rc4Key = RC4Cryption.doEncrypt(Encoding.Default.GetBytes(challenge), Encoding.Default.GetBytes(key));

        }

        internal void TrySetNextResetSockTs()
        {
            if (this.NextResetSockTimestamp > 0)
            {
                return;
            }
            this.nextResetSockTimestamp = MIMCUtil.CurrentTimeMillis() + Constant.RESET_SOCKET_TIMEOUT_TIMEVAL_MS;
        }
        public void ClearNextResetSockTimestamp()
        {
            this.NextResetSockTimestamp = -1;
        }
    }
}

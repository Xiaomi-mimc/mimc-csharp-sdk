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
namespace com.xiaomi.mimc.frontend
{
    public interface IFrontendPeerFetcher
    {
        Peer Peer();
    }
    public class Peer
    {
        public string host;
        public int port;

        public Peer()
        {
        }

        public Peer(string host)
        {
            this.host = host;
        }

        public Peer(string host, int port)
        {
            this.host = host;
            this.port = port;
        }
    }
}

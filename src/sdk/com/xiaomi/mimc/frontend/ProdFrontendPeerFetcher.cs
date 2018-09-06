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
using com.xiaomi.mimc.common;

namespace com.xiaomi.mimc.frontend
{
    public class ProdFrontendPeerFetcher : IFrontendPeerFetcher
    {
        public Peer Peer()
        {
            return new Peer(Constant.FE_URL_STAGING, Constant.FE_PORT_STAGING);
        }
    }
}

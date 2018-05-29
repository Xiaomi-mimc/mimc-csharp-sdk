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
namespace com.xiaomi.mimc.handler
{
   public interface IMIMCTokenFetcher
    {
        /// <summary>
        ///  FetchToken()访问APP应用方自行实现的AppProxyService服务，
        ///  该服务实现以下功能：
        ///1. 存储appId/appKey/appSecret(appKey/appSecret不可存储在APP客户端，以防泄漏)
        ///2. 用户在APP系统内的合法鉴权
        ///3. 调用小米TokenService服务，并将小米TokenService服务返回结果通过fetchToken()原样返回
        /// </summary>
        /// <returns>小米TokenService服务下发的原始数据</returns>
        string FetchToken();
    }
}

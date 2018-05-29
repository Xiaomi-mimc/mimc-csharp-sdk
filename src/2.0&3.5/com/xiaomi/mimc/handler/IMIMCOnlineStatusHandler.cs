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
    public interface IMIMCOnlineStatusHandler
    {
        /// <summary>
        /// 用户在线状态回调接口
        /// </summary>
        /// <param name="isOnline">登录状态，Constant.OnlineStatus.Online 在线，Constant.OnlineStatus.Offline 离线。</param>
        /// <param name="errType">状态码</param>
        /// <param name="errReason">状态原因</param>
        /// <param name="errDescription">状态描述</param>
        void StatusChange(bool isOnline, string errType, string errReason,string errDescription);
    }
}

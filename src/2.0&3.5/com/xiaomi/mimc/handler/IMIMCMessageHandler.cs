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
using com.xiaomi.mimc.packet;
using System.Collections.Generic;

namespace com.xiaomi.mimc.handler
{
    public interface IMIMCMessageHandler
    {
        /// <summary>
        /// 发送单聊消息回调接口
        /// </summary>
        /// <param name="packets"> 
        /// param[packets]: 单聊消息集
        /// @note: P2PMessage 单聊消息
        /// P2PMessage.packetId: 消息ID
        /// P2PMessage.sequence: 序列号
        /// P2PMessage.fromAccount: 发送方帐号
        /// P2PMessage.fromResource: 发送方终端id
        /// P2PMessage.payload: 消息体
        /// P2PMessage.timestamp: 时间戳</param>
        void HandleMessage(List<P2PMessage> packets);

        /// <summary>
        /// 发送群消息回调接口
        /// </summary>
        /// <param name="packets"> 
        /// param[packets]: 群聊消息集</param>
        void HandleGroupMessage(List<P2TMessage> packets);

        /// <summary>
        /// 服务器返回ACK回调接口
        /// </summary>
        /// <param name="serverAck">
        /// @param[serverAck]: 服务器返回的serverAck对象
        /// serverAck.packetId: 客户端生成的消息ID
        /// serverAck.timestamp: 消息发送到服务器的时间(单位:ms)
        /// serverAck.sequence: 服务器为消息分配的递增ID，单用户空间内递增唯一，可用于去重/排序</param>
        void HandleServerACK(ServerAck serverAck);

        /// <summary>
        /// 单聊消息超时回调接口
        /// </summary>
        /// <param name="message">param[message]: 发送超时的单聊消息</param>
        void HandleSendMessageTimeout(P2PMessage message);

        /// <summary>
        /// 群聊消息超时回调接口
        /// </summary>
        /// <param name="message"> 发送超时的群聊消息</param>
        void HandleSendGroupMessageTimeout(P2TMessage message);
    }
}

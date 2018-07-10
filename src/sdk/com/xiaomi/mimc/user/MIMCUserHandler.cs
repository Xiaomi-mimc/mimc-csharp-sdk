using System.Collections.Generic;
using System.IO;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.packet;
using com.xiaomi.mimc.utils;
using log4net;
using mimc;
using mimc.com.xiaomi.mimc.packet;
using ProtoBuf;

namespace com.xiaomi.mimc

{
    public class MIMCUserHandler
    {
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal void HandleSecMsg(MIMCUser user, V6Packet feV6Packet)
        {
            if (feV6Packet == null || feV6Packet.Body.Payload == null || feV6Packet.Body.Payload.Length == 0)
            {
                logger.WarnFormat("HandleSecMsg fail, invalid packet");
                return;
            }
            MIMCPacket packet = null;
            using (MemoryStream ms = new MemoryStream(feV6Packet.Body.Payload))
            {
                packet = Serializer.Deserialize<MIMCPacket>(ms);
                ms.Dispose();
            }
            if (packet == null)
            {
                logger.WarnFormat("{0} HandleSecMsg fail, parse MIMCPacket fail", user.AppAccount);
                return;
            }
            logger.InfoFormat("HandleSecMsg, type:{0} , uuid:{1}, packetId:{2}, chid:{3}",
               packet.type, user.Uuid, packet.packetId, user.Chid);

            if (packet.type == MIMC_MSG_TYPE.PACKET_ACK)
            {
                logger.DebugFormat("{0} <--- receive PACKET_ACK :{1}", user.AppAccount, packet.packetId);

                MIMCPacketAck packetAck = null;
                using (MemoryStream ackStream = new MemoryStream(packet.payload))
                {
                    packetAck = Serializer.Deserialize<MIMCPacketAck>(ackStream);
                    ackStream.Dispose();
                }
                if (packetAck == null)
                {
                    logger.WarnFormat("{0} HandleSecMsg parse MIMCPacketAck fail", user.AppAccount);
                    return;
                }
                user.HandleServerACK(new ServerAck(packetAck.packetId, packetAck.sequence, packetAck.timestamp, packetAck.errorMsg));
                logger.DebugFormat("{0} HandleSecMsg MIMCPacketAck packetId:{1}, msg：{2}", user.AppAccount, packetAck.packetId, packetAck.errorMsg);
                logger.DebugFormat("{0} HandleSecMsg timeoutPackets TimeoutPackets before size :{1}", user.AppAccount, user.TimeoutPackets.Count);

                TimeoutPacket timeoutPacket = new TimeoutPacket(packet, MIMCUtil.CurrentTimeMillis());
                if (user.TimeoutPackets.TryRemove(packetAck.packetId, out timeoutPacket))
                {
                    logger.DebugFormat("{0} HandleSecMsg timeoutPackets TryRemove sucess,packetId:{1}", user.AppAccount, packetAck.packetId);
                }
                else
                {
                    logger.WarnFormat("{0} HandleSecMsg timeoutPackets TryRemove fail,packetId:{1}", user.AppAccount, packetAck.packetId);
                }
                logger.DebugFormat("{0} HandleSecMsg timeoutPackets TimeoutPackets after size :{1}", user.AppAccount, user.TimeoutPackets.Count);

                return;
            }
            if (packet.type == MIMC_MSG_TYPE.UC_PACKET)
            {
                logger.DebugFormat(" UC_PACKET packetId:{0}", packet.packetId);
                UCPacket ucPacket = null;
                using (MemoryStream ucStream = new MemoryStream(packet.payload))
                {
                    ucPacket = Serializer.Deserialize<UCPacket>(ucStream);
                    ucStream.Dispose();
                }
                if (ucPacket == null)
                {
                    logger.WarnFormat("HandleSecMsg p2tMessage is null");
                }
                logger.DebugFormat("UC_PACKET UC_MSG_TYPE：{0}", ucPacket.type);

                if (ucPacket.type == UC_MSG_TYPE.MESSAGE_LIST)
                {
                    logger.DebugFormat("HandleSecMsg UC_MSG_TYPE.MESSAGE_LIST：{0}", ucPacket.type);
                    user.HandleUnlimitedGroupMessage(ucPacket);

                }
                if (ucPacket.type == UC_MSG_TYPE.JOIN_RESP)
                {
                    logger.DebugFormat("HandleJoinUnlimitedGroup UC_MSG_TYPE.JOIN_RESP：{0}", ucPacket.type);
                    user.HandleJoinUnlimitedGroup(ucPacket);
                }
                if (ucPacket.type == UC_MSG_TYPE.QUIT_RESP)
                {
                    logger.DebugFormat("HandleQuitUnlimitedGroup UC_MSG_TYPE.QUIT_RESP：{0}", ucPacket.type);
                    user.HandleQuitUnlimitedGroup(ucPacket);
                }
                if (ucPacket.type == UC_MSG_TYPE.DISMISS)
                {
                    logger.DebugFormat("HandleDismissUnlimitedGroup UC_MSG_TYPE.DISMISS：{0}", ucPacket.type);
                    user.HandleDismissUnlimitedGroup(ucPacket);
                }

                else if (ucPacket.type == UC_MSG_TYPE.PONG)
                {
                    logger.DebugFormat("HandleSecMsg UC_MSG_TYPE.PONG：{0}", ucPacket.type);
                }
            }
            if (packet.type == MIMC_MSG_TYPE.COMPOUND)
            {
                MIMCPacketList packetList = null;
                using (MemoryStream comStream = new MemoryStream(packet.payload))
                {
                    packetList = Serializer.Deserialize<MIMCPacketList>(comStream);
                    comStream.Dispose();
                }
                if (packetList == null || packetList.packets.ToArray().Length == 0)
                {
                    logger.WarnFormat("HandleSecMsg parse MIMCPacketList fail ,packetList:{0}, packetList.packets.ToArray().Length:{1}", packetList, packetList.packets.ToArray().Length);
                    return;
                }
                logger.DebugFormat("HandleSecMsg MIMCPacketList resource:{0}", user.Resource);

                if (user.Resource != packetList.resource)
                {
                    logger.WarnFormat("HandleSecMsg MIMCPacketList parse fail,resource not match,{0}!={1}", user.Resource, packetList.resource);
                    return;
                }

                V6Packet mimcSequenceAckPacket = MIMCUtil.BuildSequenceAckPacket(user, packetList);
                MIMCObject miObj = new MIMCObject();
                miObj.Type = Constant.MIMC_C2S_SINGLE_DIRECTION;
                miObj.Packet = mimcSequenceAckPacket;
                user.Connection.PacketWaitToSend.Enqueue(miObj);

                int packetListNum = packetList.packets.Count;
                List<P2PMessage> p2pMessagesList = new List<P2PMessage>();
                List<P2TMessage> p2tMessagesList = new List<P2TMessage>();

                logger.DebugFormat("{0} HandleSecMsg MIMCPacketList packetListNum:{1}", user.AppAccount, packetListNum);

                foreach (MIMCPacket p in packetList.packets)
                {
                    if (p == null)
                    {
                        logger.WarnFormat("{0} HandleSecMsg packet is null", user.AppAccount);
                        continue;
                    }
                    logger.DebugFormat("HandleSecMsg MIMC_MSG_TYPE:{0}", p.type);

                    if (p.type == MIMC_MSG_TYPE.P2P_MESSAGE)
                    {
                        logger.DebugFormat("HandleSecMsg P2P_MESSAGE packetId:{0}", p.packetId);
                        MIMCP2PMessage p2pMessage = null;
                        using (MemoryStream p2pStream = new MemoryStream(p.payload))
                        {
                            p2pMessage = Serializer.Deserialize<MIMCP2PMessage>(p2pStream);
                            p2pStream.Dispose();
                        }
                        if (p2pMessage == null)
                        {
                            logger.WarnFormat("HandleSecMsg p2pMessage is null");
                            continue;
                        }

                        p2pMessagesList.Add(new P2PMessage(p.packetId, p.sequence,
                            p2pMessage.from.appAccount, p2pMessage.from.resource,
                            p2pMessage.payload, p.timestamp));

                        continue;
                    }
                    if (p.type == MIMC_MSG_TYPE.P2T_MESSAGE)
                    {
                        logger.DebugFormat("HandleSecMsg P2T_MESSAGE packetId:{0}", p.packetId);
                        MIMCP2TMessage p2tMessage = null;
                        using (MemoryStream p2tStream = new MemoryStream(p.payload))
                        {
                            p2tMessage = Serializer.Deserialize<MIMCP2TMessage>(p2tStream);
                            p2tStream.Dispose();
                        }
                        if (p2tMessage == null)
                        {
                            logger.WarnFormat("HandleSecMsg p2tMessage is null");
                            continue;
                        }

                        p2tMessagesList.Add(new P2TMessage(p.packetId, p.sequence,
                            p2tMessage.from.appAccount, p2tMessage.from.resource,
                            p2tMessage.to.topicId, p2tMessage.payload, p.timestamp));
                        continue;
                    }

                    logger.WarnFormat("HandleSecMsg RECV_MIMC_PACKET ,invalid type, Type：{0}", p.type);
                }



                if (p2pMessagesList.Count > 0)
                {
                    user.HandleMessage(p2pMessagesList);
                }
                if (p2tMessagesList.Count > 0)
                {
                    user.HandleGroupMessage(p2tMessagesList);
                }

            }
        }
    }
}

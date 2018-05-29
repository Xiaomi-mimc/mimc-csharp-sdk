using com.xiaomi.mimc.client;
using com.xiaomi.mimc.common;
using com.xiaomi.mimc.utils;
using log4net;
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
namespace com.xiaomi.mimc.packet
{
    public class V6PacketEncoder
    {
        public V6PacketEncoder()
        {
        }

        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static byte[] Encode(MIMCConnection connection, V6Packet v6Packet)
        {
            if (connection == null || v6Packet == null)
            {
                logger.WarnFormat("V6PacketEncoder encode fail! connection:{0},v6Packet:{1} ", connection, v6Packet);
                return null;
            }
            MIMCUser user = connection.User;

            if (v6Packet.Body !=null && Constant.CMD_SECMSG == v6Packet.Body.ClientHeader.cmd)
            {
                if (null == user)
                {
                    logger.WarnFormat("V6PacketEncoder encode fail! user is null");
                    return null;
                }

                byte[] payloadKey = RC4Cryption.GenerateKeyForRC4(user.SecurityKey, v6Packet.Body.ClientHeader.id);                
                return v6Packet.ToByteArray(connection.Rc4Key, payloadKey,user);
            }
            else
            {
                byte[] v6Bins = v6Packet.ToByteArray(connection.Rc4Key, null,user);
                return v6Bins;
            }
        }
    }
}

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
namespace mimc.com.xiaomi.mimc.packet
{
    public class TimeoutPacket
    {
        
         private long timestamp;
         private MIMCPacket packet;

         public TimeoutPacket(MIMCPacket packet, long timeStamp)
         {
             this.Packet = packet;
             this.Timestamp = timeStamp;
         }

        public long Timestamp { get; }
        public MIMCPacket Packet { get; }
    }
}

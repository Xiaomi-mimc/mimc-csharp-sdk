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

using mimc;

namespace com.xiaomi.mimc.packet
{
    public class TimeoutPacket : MIMCObject
    {
         private long timestamp;
     
         public TimeoutPacket(MIMCPacket packet, long timeStamp) : base(packet)
         {
             this.Timestamp = timeStamp;
         }

        public long Timestamp { get => timestamp; set => timestamp = value; }
    }
}
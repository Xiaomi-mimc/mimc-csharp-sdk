using System;
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
    public class MIMCObject
    {
        string type;
        Object packet;

        public MIMCObject()
        {
        }

        public MIMCObject(string type, object packet)
        {
            this.Type = type;
            this.Packet = packet;
        }

        public string Type { get; set; }
        public object Packet { get; set; }
    }
}

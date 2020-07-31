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
        private Object content;

        public MIMCObject()
        {
        }

        public MIMCObject(Object content)
        {
            this.Content = content;
        }

        public object Content { get => content; set => content = value; }
    }
}

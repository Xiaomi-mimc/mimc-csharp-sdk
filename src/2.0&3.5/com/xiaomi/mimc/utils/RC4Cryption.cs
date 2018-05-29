using System;
using System.Text;
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
namespace com.xiaomi.mimc.utils
{
    public class RC4Cryption 
    {
        private static int keylength = 8;
        private  byte[] S;
        private int the_i;
        private int the_j;
        private int next_j = -666;

        public RC4Cryption()
        {
            this.S = new byte[256];
            this.the_i = this.the_j = 0;
        }

        private void ksa(int n, byte[] key, bool printstats)
        {
            int keylength = key.Length;

            for (int i = 0; i < 256; ++i)
            {
                this.S[i] = (byte)i;
            }

            this.the_j = 0;

            for (this.the_i = 0; this.the_i < n; ++this.the_i)
            {
                this.the_j = (this.the_j + posify(this.S[this.the_i]) + posify(key[this.the_i % keylength])) % 256;
                sswap(this.S, this.the_i, this.the_j);
            }

            if (n != 256)
            {
                this.next_j = (this.the_j + posify(this.S[n]) + posify(key[n % keylength])) % 256;
            }
        }

        private void ksa(byte[] key)
        {
            this.ksa(256, key, false);
        }

        private void init()
        {
            this.the_i = this.the_j = 0;
        }

        byte nextVal()
        {
            this.the_i = (this.the_i + 1) % 256;
            this.the_j = (this.the_j + posify(this.S[this.the_i])) % 256;
            sswap(this.S, this.the_i, this.the_j);
            byte value = this.S[(posify(this.S[this.the_i]) + posify(this.S[this.the_j])) % 256];
            return value;
        }

        private static void sswap(byte[] S, int i, int j)
        {
            byte temp = S[i];
            S[i] = S[j];
            S[j] = temp;
        }

        public static int posify(byte b)
        {
            return b >= 0 ? b : 256 + b;
        }

        public static byte[] doEncrypt(byte[] key, byte[] content)
        {
            byte[] outbuf = new byte[content.Length];
            RC4Cryption r = new RC4Cryption();
            r.ksa(key);
            r.init();

            for (int i = 0; i < content.Length; ++i)
            {
                outbuf[i] = (byte)(content[i] ^ r.nextVal());
            }
            return outbuf;
        }

        public static byte[] generateKeyForRC4(string secretKey, string id)
        {
            byte[] keyBytes =  Convert.FromBase64String(secretKey);
            byte[] idbytes = UTF8Encoding.Default.GetBytes(id); ;
            byte[] result = new byte[keyBytes.Length + 1 + idbytes.Length];

            for (int i = 0; i < keyBytes.Length; ++i)
            {
                result[i] = keyBytes[i];
            }
            result[keyBytes.Length] = (byte)'_';
            for (int i = 0; i < idbytes.Length; ++i)
           {
                result[keyBytes.Length + 1 + i] = idbytes[i];
            }

           return result;
        }
    }
}

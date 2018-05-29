﻿using System;
namespace com.xiaomi.mimc.utils
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
{
    public class ByteBuffer
    {
        public ByteBuffer()
        {
        }
        //字节缓存区
        private byte[] buf;
        //读取索引
        private int readIndex = 0;
        //写入索引
        private int putIndex = 0;
        //读取索引标记
        private int markReadIndex = 0;
        //写入索引标记
        private int markWirteIndex = 0;
        //缓存区字节数组的长度
        private int capacity;

        /**
         * 构造方法
         */
        public ByteBuffer(int capacity)
        {
            buf = new byte[capacity];
            this.capacity = capacity;
        }

        /**
         * 构造方法
         */
        public ByteBuffer(byte[] bytes)
        {
            buf = bytes;
            this.capacity = bytes.Length;
        }

        /**
         * 构建一个capacity长度的字节缓存区ByteBuffer对象
         */
        public static ByteBuffer Allocate(int capacity)
        {
            return new ByteBuffer(capacity);
        }

        /**
         * 构建一个以bytes为字节缓存区的ByteBuffer对象，一般不推荐使用
         */
        public static ByteBuffer Allocate(byte[] bytes)
        {
            return new ByteBuffer(bytes);
        }

        /**
         * 根据length长度，确定大于此leng的最近的2次方数，如length=7，则返回值为8
         */
        private int FixLength(int length)
        {
            int n = 2;
            int b = 2;
            while (b < length)
            {
                b = 2 << n;
                n++;
            }
            return b;
        }

        /**
         * 翻转字节数组，如果本地字节序列为低字节序列，则进行翻转以转换为高字节序列
         */
        private byte[] flip(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /**
         * 确定内部字节缓存数组的大小
         */
        private int FixSizeAndReset(int currLen, int futureLen)
        {
            if (futureLen > currLen)
            {
                //以原大小的2次方数的两倍确定内部字节缓存区大小
                int size = FixLength(currLen) * 2;
                if (futureLen > size)
                {
                    //以将来的大小的2次方的两倍确定内部字节缓存区大小
                    size = FixLength(futureLen) * 2;
                }
                byte[] newbuf = new byte[size];
                Array.Copy(buf, 0, newbuf, 0, currLen);
                buf = newbuf;
                capacity = newbuf.Length;
            }
            return futureLen;
        }

        /**
         * 将bytes字节数组从startIndex开始的length字节写入到此缓存区
         */
        public void putBytes(byte[] bytes, int startIndex, int length)
        {
            lock (this)
            {
                int offset = length - startIndex;
                if (offset <= 0) return;
                int total = offset + putIndex;
                int len = buf.Length;
                FixSizeAndReset(len, total);
                for (int i = putIndex, j = startIndex; i < total; i++, j++)
                {
                    buf[i] = bytes[j];
                }
                putIndex = total;
            }
        }

        /**
         * 将字节数组中从0到length的元素写入缓存区
         */
        public void putBytes(byte[] bytes, int length)
        {
            putBytes(bytes, 0, length);
        }

        /**
         * 将字节数组全部写入缓存区
         */
        public void putBytes(byte[] bytes)
        {
            putBytes(bytes, bytes.Length);
        }

        /**
         * 将一个ByteBuffer的有效字节区写入此缓存区中
         */
        public void put(ByteBuffer buffer)
        {
            if (buffer == null) return;
            if (buffer.ReadableBytes() <= 0) return;
            putBytes(buffer.ToArray());
        }
        /**
        * 写入一个char数据
        */
        public void putChar(char value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个int16数据
         */
        public void putShort(short value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个uint16数据
         */
        public void putUshort(ushort value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个int32数据
         */
        public void putInt(int value)
        {
            //byte[] array = new byte[4];
            //for (int i = 3; i >= 0; i--)
            //{
            //    array[i] = (byte)(value & 0xff);
            //    value = value >> 8;
            //}
            //Array.Reverse(array);
            //put(array);
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个uint32数据
         */
        public void putUint(uint value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个int64数据
         */
        public void putLong(long value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个uint64数据
         */
        public void putUlong(ulong value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个float数据
         */
        public void putFloat(float value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 写入一个byte数据
         */
        public void putByte(byte value)
        {
            lock (this)
            {
                int afterLen = putIndex + 1;
                int len = buf.Length;
                FixSizeAndReset(len, afterLen);
                buf[putIndex] = value;
                putIndex = afterLen;
            }
        }

        /**
         * 写入一个double类型数据
         */
        public void putDouble(double value)
        {
            putBytes(flip(BitConverter.GetBytes(value)));
        }

        /**
         * 读取一个字节
         */
        public byte ReadByte()
        {
            byte b = buf[readIndex];
            readIndex++;
            return b;
        }

        /**
         * 从读取索引位置开始读取len长度的字节数组
         */
        public  byte[] Read(int len)
        {
            byte[] bytes = new byte[len];
            Array.Copy(buf, readIndex, bytes, 0, len);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            readIndex += len;
            return bytes;
        }

        /**
         * 读取一个char数据
         */
        public char ReadChar()
        {
            return BitConverter.ToChar(Read(2), 0);
        }

        public ushort ReadUshort()
        {
            return BitConverter.ToUInt16(Read(2), 0);
        }

        /**
         * 读取一个int16数据
         */
        public short ReadShort()
        {
            return BitConverter.ToInt16(Read(2), 0);
        }

        /**
         * 读取一个uint32数据
         */
        public uint ReadUint()
        {
            return BitConverter.ToUInt32(Read(4), 0);
        }

        /**
         * 读取一个int32数据
         */
        public int ReadInt()
        {
            return BitConverter.ToInt32(Read(4), 0);
        }

        /**
         * 读取一个uint64数据
         */
        public ulong ReadUlong()
        {
            return BitConverter.ToUInt64(Read(8), 0);
        }

        /**
         * 读取一个long数据
         */
        public long ReadLong()
        {
            return BitConverter.ToInt64(Read(8), 0);
        }

        /**
         * 读取一个float数据
         */
        public float ReadFloat()
        {
            return BitConverter.ToSingle(Read(4), 0);
        }

        /**
         * 读取一个double数据
         */
        public double ReadDouble()
        {
            return BitConverter.ToDouble(Read(8), 0);
        }

        /**
         * 从读取索引位置开始读取len长度的字节到disbytes目标字节数组中
         * @params disstart 目标字节数组的写入索引
         */
        public byte[] ReadBytes(byte[] disbytes, int disstart, int len)
        {
            int size = disstart + len;
            for (int i = disstart; i < size; i++)
            {
                disbytes[i] = this.ReadByte();
            }
            return disbytes;
        }

        /**
         * 清除已读字节并重建缓存区
         */
        public void DiscardReadBytes()
        {
            if (readIndex <= 0) return;
            int len = buf.Length - readIndex;
            byte[] newbuf = new byte[len];
            Array.Copy(buf, readIndex, newbuf, 0, len);
            buf = newbuf;
            putIndex -= readIndex;
            markReadIndex -= readIndex;
            if (markReadIndex < 0)
            {
                markReadIndex = readIndex;
            }
            markWirteIndex -= readIndex;
            if (markWirteIndex < 0 || markWirteIndex < readIndex || markWirteIndex < markReadIndex)
            {
                markWirteIndex = putIndex;
            }
            readIndex = 0;
        }

        /**
         * 清空此对象
         */
        public void Clear()
        {
            buf = new byte[buf.Length];
            readIndex = 0;
            putIndex = 0;
            markReadIndex = 0;
            markWirteIndex = 0;
        }

        /**
         * 设置开始读取的索引
         */
        public void SetReaderIndex(int index)
        {
            if (index < 0) return;
            readIndex = index;
        }

        /**
         * 标记读取的索引位置
         */
        public void MarkReaderIndex()
        {
            markReadIndex = readIndex;
        }

        /**
         * 标记写入的索引位置
         */
        public void MarkputrIndex()
        {
            markWirteIndex = putIndex;
        }

        /**
         * 将读取的索引位置重置为标记的读取索引位置
         */
        public void ResetReaderIndex()
        {
            readIndex = markReadIndex;
        }

        /**
         * 将写入的索引位置重置为标记的写入索引位置
         */
        public void ResetputrIndex()
        {
            putIndex = markWirteIndex;
        }

        /**
         * 可读的有效字节数
         */
        public int ReadableBytes()
        {
            return putIndex - readIndex;
        }

        /**
         * 获取可读的字节数组
         */
        public byte[] ToArray()
        {
            byte[] bytes = new byte[putIndex];
            Array.Copy(buf, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /**
         * 获取缓存区大小
         */
        public int GetCapacity()
        {
            return this.capacity;
        }
    }
}

using System;
using System.Threading;

namespace mimc.com.xiaomi.mimc.utils
{   
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
    class AtomicInteger
    {
        private int value;

        public AtomicInteger(int initialValue)
        {
            value = initialValue;
        }

        public AtomicInteger()
            : this(0)
        {
        }

        public int Get()
        {
            return value;
        }

        public void Set(int newValue)
        {
            value = newValue;
        }

        public int GetAndSet(int newValue)
        {
            for (; ; )
            {
                int current = Get();
                if (CompareAndSet(current, newValue))
                    return current;
            }
        }

        public bool CompareAndSet(int expect, int update)
        {
            return Interlocked.CompareExchange(ref value, update, expect) == expect;
        }

        public int GetAndIncrement()
        {
            for (; ; )
            {
                int current = Get();
                int next = current + 1;
                if (CompareAndSet(current, next))
                    return current;
            }
        }

        public int GetAndDecrement()
        {
            for (; ; )
            {
                int current = Get();
                int next = current - 1;
                if (CompareAndSet(current, next))
                    return current;
            }
        }

        public int GetAndAdd(int delta)
        {
            for (; ; )
            {
                int current = Get();
                int next = current + delta;
                if (CompareAndSet(current, next))
                    return current;
            }
        }

        public int IncrementAndGet()
        {
            for (; ; )
            {
                int current = Get();
                int next = current + 1;
                if (CompareAndSet(current, next))
                    return next;
            }
        }

        public int DecrementAndGet()
        {
            for (; ; )
            {
                int current = Get();
                int next = current - 1;
                if (CompareAndSet(current, next))
                    return next;
            }
        }

        public int AddAndGet(int delta)
        {
            for (; ; )
            {
                int current = Get();
                int next = current + delta;
                if (CompareAndSet(current, next))
                    return next;
            }
        }

        public override String ToString()
        {
            return Convert.ToString(Get());
        }
    }
}

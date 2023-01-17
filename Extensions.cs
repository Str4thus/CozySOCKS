using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    public static class Extensions
    {
        public static string ToHex(this byte[] buffer, int length)
        {
            return BitConverter.ToString(buffer.Skip(0).Take(length).ToArray());
        }
        public static string ToHex(this byte[] buffer)
        {
            return BitConverter.ToString(buffer);
        }

        public static byte[] SubArray(this byte[] buffer, int start, int end)
        {
            byte[] result = new byte[end - start + 1];
            for (int i = start; i <= end; i++)
            {
                result[i - start] = buffer[i];
            }

            return result;
        }

        public static byte ToValue(this byte[] buffer)
        {
            byte result = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                result |= buffer[i];
            }

            return result;
        }
    }
}

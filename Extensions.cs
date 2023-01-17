using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    public static class Extensions
    {
        // byte[]
        public static string ToHex(this byte[] buffer, int length)
        {
            return BitConverter.ToString(buffer.Skip(0).Take(length).ToArray());
        }
        public static string ToHex(this byte[] buffer)
        {
            return BitConverter.ToString(buffer);
        }

        public static byte[] ReadBytes(this byte[] buffer, int start, int end)
        {
            byte[] result = new byte[end - start + 1];
            for (int i = start; i <= end; i++)
            {
                result[i - start] = buffer[i];
            }

            return result;
        }

        public static byte[] ReadBytesToEnd(this byte[] buffer, int start)
        {
            byte[] result = new byte[buffer.Length - start];
            for (int i = start; i < buffer.Length; i++)
            {
                result[i - start] = buffer[i];
            }

            return result;
        }

        public static byte[] ReadBytesUntilNull(this byte[] buffer, int start)
        {
            int resultSize = buffer.NumberOfBytesUntilNull(start);
            byte[] result = new byte[resultSize];
            for (int i = start; i < resultSize; i++)
            {
                if (buffer[i] == 0)
                    break;
                result[i - start] = buffer[i];
            }

            return result;
        }

        public static int NumberOfBytesUntilNull(this byte[] buffer, int start)
        {
            int result = 0;
            for (int i = start; i <= buffer.Length; i++)
            {
                if (buffer[i] == 0)
                    break;

                result += 1;
            }

            return result;
        }

        public static int GetInt(this byte[] buffer)
        {
            int result = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                result += (int)(buffer[i] << ((buffer.Length - 1 - i) * 8));
            }

            return result;
        }

        public static string GetString(this byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }


        // TcpClient - https://github.com/rasta-mouse/SharpC2/blob/main/Drone/Utilities/Extensions.cs
        public static bool DataAvailable(this TcpClient client)
        {
            NetworkStream networkStream = client.GetStream();
            return networkStream.DataAvailable;
        }

        public static async Task<byte[]> ReadStream(this Stream stream)
        {
            const int bufSize = 1024;
            int read;

            MemoryStream ms = new MemoryStream();

            do
            {
                byte[] buf = new byte[bufSize];
                read = await stream.ReadAsync(buf, 0, bufSize);

                if (read == 0)
                    break;

                await ms.WriteAsync(buf, 0, read);

            } while (read >= bufSize);

            return ms.ToArray();
        }

        public static async Task WriteStream(this Stream stream, byte[] data)
        {
            var lengthBuf = BitConverter.GetBytes(data.Length);
            var lv = new byte[lengthBuf.Length + data.Length];

            Buffer.BlockCopy(lengthBuf, 0, lv, 0, lengthBuf.Length);
            Buffer.BlockCopy(data, 0, lv, lengthBuf.Length, data.Length);

            MemoryStream ms = new MemoryStream(lv);

            var bytesRemaining = lv.Length;
            do
            {
                var lengthToSend = bytesRemaining < 1024 ? bytesRemaining : 1024;
                var buf = new byte[lengthToSend];

                var read = await ms.ReadAsync(buf, 0, lengthToSend);

                if (read != lengthToSend)
                    throw new Exception("Could not read data from stream");

                await stream.WriteAsync(buf, 0, buf.Length);

                bytesRemaining -= lengthToSend;
            }
            while (bytesRemaining > 0);
        }

        public static async Task<byte[]> ReadClient(this TcpClient client)
        {
            var stream = client.GetStream();

            MemoryStream ms = new MemoryStream();
            int read;

            do
            {
                var buf = new byte[1024];
                read = await stream.ReadAsync(buf, 0, buf.Length);

                if (read == 0)
                    break;

                await ms.WriteAsync(buf, 0, read);
            }
            while (read >= 1024);

            return ms.ToArray();
        }

        public static async Task WriteClient(this TcpClient client, byte[] data)
        {
            var stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
        }
    }
}

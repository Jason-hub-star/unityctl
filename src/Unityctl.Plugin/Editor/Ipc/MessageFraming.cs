using System;
using System.IO;
using System.Text;

namespace Unityctl.Plugin.Editor.Ipc
{
    /// <summary>
    /// Length-prefixed message framing for Named Pipe communication.
    /// Wire format: [4 bytes int32 LE: payload length] [N bytes UTF-8: JSON body]
    /// Synchronous only — Unity Mono async pipe stability is unverified.
    /// </summary>
    public static class MessageFraming
    {
        public const int MaxMessageSize = 10 * 1024 * 1024; // 10 MB

        public static string ReadMessage(Stream stream)
        {
            var headerBuf = new byte[4];
            ReadExact(stream, headerBuf, 0, 4);

            int length = BitConverter.ToInt32(headerBuf, 0);
            if (length <= 0 || length > MaxMessageSize)
                throw new InvalidOperationException($"Invalid message length: {length}");

            var bodyBuf = new byte[length];
            ReadExact(stream, bodyBuf, 0, length);

            return Encoding.UTF8.GetString(bodyBuf);
        }

        public static void WriteMessage(Stream stream, string json)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(json);
            if (bodyBytes.Length > MaxMessageSize)
                throw new InvalidOperationException($"Message too large: {bodyBytes.Length} bytes (max {MaxMessageSize})");

            var header = BitConverter.GetBytes(bodyBytes.Length);
            stream.Write(header, 0, 4);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            stream.Flush();
        }

        private static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new EndOfStreamException("Pipe closed before full message was read.");
                totalRead += read;
            }
        }
    }
}

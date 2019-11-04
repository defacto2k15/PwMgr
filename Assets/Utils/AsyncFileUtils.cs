using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.MT;

namespace Assets.Utils
{
    public class AsyncFileUtils
    {
        public static Task<byte[]> ReadAllBytesAsync(string path)
        {
            if (TaskUtils.GetGlobalMultithreading())
            {
                return TaskUtils.MyFromResult(File.ReadAllBytes(path));
            }

            var tcs = new TaskCompletionSource<byte[]>();

            long bufferLength = new System.IO.FileInfo(path).Length;
            byte[] buffer = new byte[bufferLength];

            FileStream stream = File.Open(path, FileMode.Open);
            stream.BeginRead(buffer, 0, (int) bufferLength, asyncResult =>
            {
                tcs.SetResult(buffer);
                var readLength = stream.EndRead(asyncResult);
                Preconditions.Assert(bufferLength == readLength,
                    "Reading file of path " + path + " ended after " + readLength + "b and not " + bufferLength);
                stream.Dispose();
            }, null);
            return tcs.Task;
        }

        public static Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            if (TaskUtils.GetGlobalMultithreading())
            {
                File.WriteAllBytes(path, bytes);
                return TaskUtils.MyFromResult(new object());
            }

            var tcs = new TaskCompletionSource<object>();

            FileStream stream = File.Open(@"C:\file.txt", FileMode.Open);
            stream.BeginWrite(bytes, 0, bytes.Length, asyncResult =>
            {
                tcs.SetResult(null);
                stream.EndWrite(asyncResult);
                stream.Dispose();
            }, null);

            return tcs.Task;
        }
    }
}
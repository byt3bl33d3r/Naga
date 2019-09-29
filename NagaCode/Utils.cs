using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Naga
{
    class Utils
    {

        public static byte[] Hex2Binary(string hex)
        {
            var chars = hex.ToCharArray();
            var bytes = new List<byte>();
            for (int index = 0; index < chars.Length; index += 2)
            {
                var chunk = new string(chars, index, 2);
                bytes.Add(byte.Parse(chunk, NumberStyles.AllowHexSpecifier));
            }
            return bytes.ToArray();
        }

        public static string GetDllName(string name)
        {
            var dllName = name + ".dll";
            if (name.IndexOf(',') > 0)
            {
                dllName = name.Substring(0, name.IndexOf(',')) + ".dll";
            }

            return dllName;
        }

        public static byte[] GetResourceFromZip(ZipStorer zip, string name)
        {
            foreach (var entry in zip.ReadCentralDir())
            {
                if (entry.FilenameInZip != name) continue;
                zip.ExtractFile(entry, out var data);
                return data;
            }

            return default;
        }

        internal static byte[] GetResourceByName(string resName)
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var resource = asm.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resName));
            using (var resourceStream = asm.GetManifestResourceStream(resource))
            {
                using (var memoryStream = new MemoryStream())
                {
                    resourceStream?.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
    public static class Retry
    {
        public static T Do<T>(Func<T> action, TimeSpan retryInterval,
            int maxAttempts = 3)
        {
            var exceptions = new List<Exception>();

            for (var attempts = 0; attempts < maxAttempts; attempts++)
            {
                try
                {
                    if (attempts > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }
#if DEBUG
                    Console.WriteLine($"[-] Attempt #{attempts + 1}");
#endif
                    return action();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine("\t[!] {0}", ex.Message);
#endif
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
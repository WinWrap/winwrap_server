using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace winwrap_edit_server
{
    public class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, object> defaults = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "help", false },
                { "debug", false },
                { "log", false },
                { "ip", "localhost" },
                { "port", 5000 },
                { "reset", false },
                { "sandboxed", false },
                { "scriptroot", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\WebEditServer" },
                { "start", "http://www.winwrap.com/webedit/index.html" },
                { "wwwroot", "" }
            };
            Dictionary<string, object> parameters = WWB.Util.GetParameters(args, defaults);

            bool help = (bool)parameters["help"];
            string error = VerifyWinWrapBasic();
            if (error != null)
                help = true;

            if (help)
            {
                if (error == null)
                    error = "";
                else
                    error = "\r\n" + error;

                parameters[".error"] = error;

                Console.Write(WWB.Util.ReadResourceTextFile("Messages.Help", parameters));
                Console.ReadKey();
                return;
            }

            bool reset = (bool)parameters["reset"];
            bool sandboxed = (bool)parameters["sandboxed"];
            string scriptroot = (string)parameters["scriptroot"];
            string wwwroot = (string)parameters["wwwroot"];

            string log_file = null;
            if ((bool)parameters["log"])
                log_file =
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                    WWB.Util.Replace("\\WebEditServer-{port}.txt", parameters);

            var hostBuilder = new WebHostBuilder()
                .UseKestrel();

            if (!string.IsNullOrEmpty(wwwroot))
                hostBuilder.UseContentRoot(wwwroot)
                    .UseWebRoot(wwwroot);

            var host = hostBuilder.ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    // comment the next line out to get full logging
                    logging.SetMinimumLevel(LogLevel.Critical);
                })
                .UseStartup<Startup>()
                .UseUrls(WWB.Util.Replace("http://{ip}:{port}", parameters))
                .Build();

            if ((string)parameters["start"] != "")
            {
                string prefix = null;
                if (!((string)parameters["start"]).StartsWith("http:"))
                    prefix = WWB.Util.Replace("http://{ip}:{port}/", parameters);

                System.Diagnostics.Process.Start(prefix + WWB.Util.Replace("{start}?serverip={ip}:{port}", parameters));
            }

            parameters[".log_file"] = log_file;

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            WinWrapBasicService.Singleton.Initialize(parameters, cts.Token);

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                WinWrapBasicService.Shutdown();
            }
        }

        static private string VerifyWinWrapBasic()
        {
            try
            {
                using (WinWrap.Basic.BasicNoUIObj basic = new WinWrap.Basic.BasicNoUIObj())
                {
                    basic.Secret = new Guid(Secret.MySecret);
                    basic.Initialize();
                    if (basic.IsInitialized())
                        return null;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return null;
        }
    }

    /*
     *   Azure Windows 10 Enterprise N 1709
     *   
     *   Windows Classic Desktop - Console App - x64 (maybe temporary)
     *   Console.WriteLine("Hello World!"); - Ctrl-F5
     *   NuGet settings PackageReference (maybe temporary)
     *   NuGet Microsoft.AspNetCore, Microsoft.AspNetCore.Server.Kestrel
     *   https://jonhilton.net/2016/07/18/your-first-net-core-web-application-using-nothing-but-the-command-line/
     *   http://localhost:5000
     *   https://jonhilton.net/2016/07/27/how-to-add-mvc-to-your-asp-net-core-web-application/
     *   NuGet Microsoft.AspNetCore.Mvc
     *   public class HomeController : Controller
     *   start - allow public access
     *   create inbound rule testtcp5000
     *   http://localhost:5000/home/index
     *   http://10.0.1.4:5000/home/index
     *   Azure VM Settings.Inbound 1010 Port_5000 5000 TCP Any Any
     *   router Service Name: winwrap5000, External Ports: 5000, same, ipaddress
     *     dmz doesn't work
     *   http://13.65.45.222:5000/home/index (ipaddress)
     *   http://192.168.1.203:5000/home/index
     *   
     *   for use with "statichtml" project
     *   app.UseCors(builder => 
     *   builder.AllowAnyHeader(); builder.AllowAnyMethod(); builder.AllowAnyOrigin();
     *   
     *   The referenced component 'System.Net.Http' could not be found
     *   install the "System.ComponentModel.Annotations" "4.3.0" nuget package
     *   didn't work
     *   above will not be needed when [NuGet] bug fixed ?
     *    
     *   git config --global user.email "you@example.com"
     *   git config --global user.name "Your Name"
    */
}

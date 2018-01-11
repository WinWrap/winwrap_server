using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace winwrap_edit_server
{
    public class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, object> parameters = GetParameters(args);

            if ((bool)parameters["help"])
            {
                Console.Write(Util.ReadResourceTextFile("Messages.Help"));
                Console.ReadKey();
                return;
            }

            Console.WriteLine(Util.ReadResourceTextFile("Messages.Startup", parameters));

            string wwwroot = (string)parameters["wwwroot"];

            string log_file = null;
            if ((bool)parameters["log"])
                log_file =
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                    Util.Replace("\\WebEditServer-{port}.txt", parameters);

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(wwwroot)
                .UseWebRoot(wwwroot)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    // comment the next line out to get full logging
                    logging.SetMinimumLevel(LogLevel.Critical);
                })
                .UseStartup<Startup>()
                .UseUrls(Util.Replace("http://{ip}:{port}", parameters))
                .Build();

            if ((string)parameters["start"] != "")
            {
                string prefix = null;
                if (!((string)parameters["start"]).StartsWith("http:"))
                    prefix = Util.Replace("http://{ip}:{port}/", parameters);

                System.Diagnostics.Process.Start(prefix + Util.Replace("{start}?serverip={ip}:{port}", parameters));
            }

            WinWrapBasicService.Singleton.Initialize((bool)parameters["reset"], log_file);

            host.Run();
        }

        static private Dictionary<string, object> GetParameters(string[] args)
        {
            // get the options from the command line
            Dictionary<string, object> default_parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "help", false },
                { "log", false },
                { "ip", "localhost" },
                { "port", 5000 },
                { "reset", false },
                { "start", "http://www.winwrap.com/webedit/index.html" },
                { "wwwroot", Directory.GetCurrentDirectory() }
            };

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (string arg in args)
            {
                string[] parts = arg.Split(new char[] { '=' }, 2);
                string key = parts[0];
                if (!default_parameters.ContainsKey(key))
                {
                    Console.Write(Util.ReadResourceTextFile("Messages.BadOption", key));
                    parameters["help"] = true;
                    continue;
                }

                object value = default_parameters[key];
                if (parts.Length == 1)
                {
                    if (default_parameters[key].GetType() != typeof(Boolean))
                    {
                        Console.Write(Util.ReadResourceTextFile("Messages.BadOptionNoValue", key));
                        parameters["help"] = true;
                        continue;
                    }

                    value = true;
                }
                else
                {
                    if (default_parameters[key].GetType() == typeof(Boolean))
                    {
                        Console.Write(Util.ReadResourceTextFile("Messages.BadOptionValue", key));
                        parameters["help"] = true;
                        continue;
                    }

                    value = parts[1];
                    try
                    {
                        value = Convert.ChangeType(parts[1], value.GetType());
                    }
                    catch (Exception ex)
                    {
                        Console.Write(Util.ReadResourceTextFile("Messages.BadOptionValue2", key, ex.Message));
                        parameters["help"] = true;
                        continue;
                    }
                }

                parameters[key] = value;
            }

            // establish values from defaults for missing parameters
            foreach (string key in default_parameters.Keys)
                if (!parameters.ContainsKey(key))
                    parameters[key] = default_parameters[key];

            return parameters;
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

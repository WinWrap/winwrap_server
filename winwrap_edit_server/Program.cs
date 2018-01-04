using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace winwrap_edit_server
{
    public class Program
    {
        //public static WWB.SharedWWB sharedWWB = new WWB.SharedWWB();
        public static WWB.WinWrapBasicService WinWrapBasicService = new WWB.WinWrapBasicService();

        static void Main(string[] args)
        {
            Console.WriteLine("winwrap_edit_server");

            var host = new WebHostBuilder()
    .UseKestrel()
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
        logging.AddConsole();
        logging.AddDebug();
        // comment the next line out to get full logging
        logging.SetMinimumLevel(LogLevel.Critical);
    })
    .UseStartup<Startup>()
    //.UseUrls("http://localhost:5000") // xyz 
    //.UseUrls("http://192.168.1.208:5000")
    .UseUrls("http://192.168.1.211:5000")
    .Build();

            host.Run();
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

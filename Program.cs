using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyCourse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            //WebHost.CreateDefaultBuilder(args)
            //.UseStartup<Startup>();
            //webHost sostituito dalla versione .Net 3.0 da un Generic Host
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webHostBuilder => {
                webHostBuilder.UseStartup<Startup>();
            });
            //se volessi configurare la dependency injection in un'applicazione console userei: 
            //.ConfigureServices            
    }
}

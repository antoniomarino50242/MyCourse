using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyCourse
{
    public class Program
    {
        //---CONFIGURAZIONE CON .NET5 o precedenti
        /*public static void Main(string[] args)
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
            */      

        //---CONFIGURAZIONE CON .NET6 CON MINIMAL HOSTING MODEL
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            Startup startup = new(builder.Configuration);

            //aggiunger i servizi per la dependency injection (metodo ConfigureServices)
            startup.ConfigureServices(builder.Services);

            WebApplication app = builder.Build();

            //usiamo i middleware (metodo Configure)
            startup.Configure(app);

            app.Run();
        }     
    }
}

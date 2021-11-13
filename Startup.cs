using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCourse.Models.Entities.Services.Infrastructure;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Application;
using MyCourse.Models.Services.Infrastructure;

namespace MyCourse
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCaching();

            services.AddMvc(options => {
                var homeProfile = new CacheProfile();
                homeProfile.Duration = Configuration.GetValue<int>("ReponseCache:Home:Duration");
                homeProfile.Location = Configuration.GetValue<ResponseCacheLocation>("ResponseCacheLocation:Home:Location");
                //homeProfile.VaryByQueryKeys() = new string[] {"page"};

                //In modo più succinto scritto come
                //Configuration.Bind("ReponseCache:Home", homeProfile);
                homeProfile.VaryByQueryKeys = Configuration.GetValue<string[]>("ReponseCache:Home:VaryByQueryKeys");
                options.CacheProfiles.Add("Home", homeProfile);
                
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddTransient<ICourseService, AdoNetCourseService>();
            //services.AddTransient<ICourseService, EfCoreCourseService>();
            services.AddTransient<IDatabaseAccessor, SqliteDatabaseAccessor>();
            services.AddTransient<ICachedCourseService, MemoryCachedCourseService>();

            //registro dbcontext 
            //services.AddScoped<MyCourseDbContext>();
            //services.AddDbContext<MyCourseDbContext>();
            services.AddDbContextPool<MyCourseDbContext>(optionsBuilder => 
            {
                string connetionString = Configuration.GetSection("ConnectionStrings").GetValue<String>("Default");
                optionsBuilder.UseSqlite(connetionString);
            });

            //options
            services.Configure<ConnectionStringOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<TimeOptions>(Configuration.GetSection("Time"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection("MemoryCache"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            //if (env.IsDevelopment())
            if (env.IsEnvironment("Development"))
            {
                app.UseDeveloperExceptionPage();

                //Aggiorniamo un file per notificare al BrowserSync che deve aggiornare la pagina
                lifetime.ApplicationStarted.Register(() =>
                {
                    string filePath = Path.Combine(env.ContentRootPath, "bin/reload.txt");
                    File.WriteAllText(filePath, DateTime.Now.ToString());
                });
            } else{
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            
            app.UseResponseCaching();

            //app.UseMvcWithDefaultRoute();
            app.UseMvc(routeBuilder => 
            {
                // Esempio di percorso conforme al template route: /courses/detail/5
                routeBuilder.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

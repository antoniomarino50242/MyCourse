using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCourse.Customization.Identity;
using MyCourse.Customization.ModelBinders;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.Services.Application.Lessons;
using MyCourse.Models.Enums;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.Entities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using AspNetCore.ReCaptcha;
using MyCourse.Models.Authorization;

namespace MyCourse
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddReCaptcha(Configuration.GetSection("ReCaptcha"));
            services.AddResponseCaching();
            //services.AddRazorPages();

            services.AddMvc(options =>
            {
                var homeProfile = new CacheProfile();
                //homeProfile.Duration = Configuration.GetValue<int>("ReponseCache:Home:Duration");
                //homeProfile.Location = Configuration.GetValue<ResponseCacheLocation>("ResponseCacheLocation:Home:Location");
                //homeProfile.VaryByQueryKeys() = new string[] {"page"};

                //In modo più succinto scritto come
                Configuration.Bind("ReponseCache:Home", homeProfile);
                //homeProfile.VaryByQueryKeys = Configuration.GetValue<string[]>("ReponseCache:Home:VaryByQueryKeys");
                options.CacheProfiles.Add("Home", homeProfile);

                //model binder personalizzati
                options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
            });

            services.AddRazorPages(options=> {
                options.Conventions.AllowAnonymousToPage("/Privacy");
                options.Conventions.AllowAnonymousToFolder("/Public");
                options.Conventions.AuthorizeFolder("/Admin");
            });

            var identityBuilder = services.AddDefaultIdentity<ApplicationUser>(options=>{
                        options.Password.RequireDigit = true;
                        options.Password.RequiredLength = 8;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = true;
                        options.Password.RequiredUniqueChars = 4;

                        //conferma account
                        options.SignIn.RequireConfirmedAccount = true;

                        //conferma password
                        options.Lockout.AllowedForNewUsers = true;
                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    })
                    .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>()
                    .AddPasswordValidator<CommonPasswordValidator<ApplicationUser>>();

            //Usiamo AdoNet o Entity Framework Core per l'accesso ai dati?
            var persistence = Persistence.EfCore;
            switch (persistence)
            {
                case Persistence.AdoNet:
                    services.AddTransient<ICourseService, AdoNetCourseService>();
                    services.AddTransient<ILessonService, AdoNetLessonService>();
                    services.AddTransient<IDatabaseAccessor, SqliteDatabaseAccessor>();

                    identityBuilder.AddUserStore<AdoNetUserStore>();
                break;

                case Persistence.EfCore:
                    
                    identityBuilder.AddEntityFrameworkStores<MyCourseDbContext>();

                    services.AddTransient<ICourseService, EfCoreCourseService>();
                    services.AddTransient<ILessonService, EfCoreLessonService>();
                    services.AddDbContextPool<MyCourseDbContext>(optionsBuilder =>
                    {
                        string connetionString = Configuration.GetSection("ConnectionStrings").GetValue<String>("Default");
                        optionsBuilder.UseSqlite(connetionString);
                    });
                break;
            }

            services.AddTransient<ICachedCourseService, MemoryCachedCourseService>();
            services.AddTransient<ICachedLessonService, MemoryCachedLessonService>();
            services.AddSingleton<IImagePersister, MagickNetImagePersister>();
            services.AddSingleton<IEmailSender, MailKitEmailSender>();
            services.AddSingleton<IEmailClient, MailKitEmailSender>();
            services.AddSingleton<IAuthorizationPolicyProvider, MultiAuthorizationPolicyProvider>();

            // Uso il ciclo di vita Scoped per registrare questi AuthorizationHandler perché
            // sfruttano un servizio (il DbContext) registrato con il ciclo di vita Scoped
            services.AddScoped<IAuthorizationHandler, CourseAuthorRequirementHandler>();
            services.AddScoped<IAuthorizationHandler, CourseLimitRequirementHandler>();
            services.AddScoped<IAuthorizationHandler, CourseSubscriberRequirementHandler>();

            //Policies
            services.AddAuthorization( options=> {
                options.AddPolicy(nameof(Policy.CourseAuthor), builder => 
                {
                    builder.Requirements.Add(new CourseAuthorRequirement());
                });

                options.AddPolicy(nameof(Policy.CourseLimit), builder => {
                    builder.Requirements.Add(new CourseLimitRequirement(limit : 5));
                });

                options.AddPolicy(nameof(Policy.CourseSubscriber), builder =>
                {
                    builder.Requirements.Add(new CourseSubscriberRequirement());
                });
            });

            //options
            services.Configure<ConnectionStringOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<TimeOptions>(Configuration.GetSection("Time"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection("MemoryCache"));
            services.Configure<ImageOptions>(Configuration.GetSection("ImageOption"));
            services.Configure<SmtpOptions>(Configuration.GetSection("Smtp"));
            services.Configure<UsersOptions>(Configuration.GetSection("Users"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
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
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            //Nel caso volessi impostare una Culture specifica...
            /*var appCulture = CultureInfo.InvariantCulture;
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(appCulture),
                SupportedCultures = new[] { appCulture }
            });*/

            //EndpointRoutingMiddleware
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseResponseCaching();

            //app.UseMvcWithDefaultRoute();
            /*app.UseMvc(routeBuilder => 
            {
                // Esempio di percorso conforme al template route: /courses/detail/5
                routeBuilder.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });*/

            //Endpoint middleware
            app.UseEndpoints(routeBuilder =>
            {
                routeBuilder.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").RequireAuthorization();
                routeBuilder.MapRazorPages().RequireAuthorization();
            });
        }
    }
}

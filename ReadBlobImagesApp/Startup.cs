using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Identity.Web;
using ReadBlobImagesApp.Controllers;
using static System.Net.Mime.MediaTypeNames;

namespace ReadBlobImagesApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                options.HandleSameSiteCookieCompatibility();
            });

            // Configuration to sign-in users with Azure AD B2C
            services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "AzureAdB2C");

            //Configuring appsettings section AzureAdB2C, into IOptions
            services.AddOptions();
            services.Configure<OpenIdConnectOptions>(Configuration.GetSection("AzureAdB2C"));

            AddSession(services);

            services.AddHttpClient<UploadController>();
            services.AddScoped<IAzureHelper, AzureHelper>();
            services.AddScoped<IConfigKeys, ConfigKeys>();

            services.AddScoped<IMessageHelper, MessageHelper>();

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        //public void Configure(WebApplication app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //if (!app.Environment.IsDevelopment())

                //app.UseExceptionHandler("/Home/Error");
                app.UseExceptionHandler(exceptionHandlerApp =>
                {
                    exceptionHandlerApp.Run(async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                        // using static System.Net.Mime.MediaTypeNames;
                        context.Response.ContentType = Text.Plain;

                        await context.Response.WriteAsync("An exception was thrown.");

                        var exceptionHandlerPathFeature =
                            context.Features.Get<IExceptionHandlerPathFeature>();

                        if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
                        {
                            await context.Response.WriteAsync(" The file was not found.");
                        }

                        if (exceptionHandlerPathFeature?.Path == "/")
                        {
                            await context.Response.WriteAsync(" Page: Home.");
                        }

                        await context.Response.WriteAsync(" Error: " + exceptionHandlerPathFeature?.Error.Message);
                    });
                });
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //enable session
            app.UseSession();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }


        private void AddSession(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);//You can set Time   
            });
        }
    }
}

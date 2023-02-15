using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            AddSession(services);
            //AddAuthentication(services);

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

            //app.UseAuthentication();
            //app.UseAuthorization();

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

        //void AddTranslationService(WebApplicationBuilder builder)
        //{
        //    var configLangCode = builder.Configuration.GetValue<string>("ConfigKeys:LanguageCode");
        //    Enum.TryParse(configLangCode, out LangCode langCode);

        //    builder.Services.AddSingleton<IMessageHelper>(s => new TranslationHelper(langCode));
        //}

        void AddAuthentication(IServiceCollection services)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration, "AzureAd");

            //services
            //        .AddAuthentication(AzureADDefaults.AuthenticationScheme)
            //        .AddAzureAD(options => Configuration.Bind("AzureAd", options));
        }
    }
}

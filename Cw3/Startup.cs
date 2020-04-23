using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cw3.DAL;
using Cw3.Middlewares;
using Cw3.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Cw3
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddSingleton<IDbService, MockDbService>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new
                        Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidIssuer = "Gakko",
                            ValidAudience = "Students",
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]))
                        };
                    });
            services.AddTransient<IStudentsDbService, SqlServerDbService>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IStudentsDbService service)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseMiddleware<LoggingMiddleware>();

            //app.Use(async (context, next) =>
            //{
            //    if (!context.Request.Headers.ContainsKey("Index"))
            //    {
            //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            //        await context.Response.WriteAsync("Nie poda³eœ indeksu");
            //        return;
            //    }
            //    else
            //    {
            //        string index = context.Request.Headers["Index"].ToString();
            //        if (service.checkStudentID(index))
            //        {
            //            await next();
            //        } else
            //        {
            //            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            //            await context.Response.WriteAsync("Niew³aœciwy index");
            //            return;
            //        }
            //    }


            //    //Custom code
            //    await next();
            //});
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

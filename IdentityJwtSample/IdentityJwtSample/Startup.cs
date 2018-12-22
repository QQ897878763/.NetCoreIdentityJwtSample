using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IdentityJwtSample.Helpers;
using IdentityJwtSample.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace IdentityJwtSample
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            //注入配置信息
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            //Jwt验证配置
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    //需注意的是如果用户的Token输入错误是不会进入到Validated事件中！ 
                    OnTokenValidated = context =>
                    {
                        //如果也可以从DI容器中获取校验服务                     
                        var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
                        var userId = int.Parse(context.Principal.Identity.Name);
                        var user = userService.GetById(userId);
                        if (user == null)//校验失败
                        { 
                            context.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    }
                };
                //获取权限是否需要HTTPS
                x.RequireHttpsMetadata = false;
                //在成功的授权之后令牌是否应该存储在Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties中
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    //是否验证秘钥
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            //注册服务到DI容器中
            services.AddScoped<IUserService, UserService>();
            AddSwagger(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();


            //引用Swagger服务
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityJwtSample"));
        }

        #region Swagger相关配置
        private void AddSwagger(IServiceCollection services)
        {
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(options =>
            {
                //
                options.SwaggerDoc("v1", new Info { Title = "IdentityJwtWebAPI", Version = "v1" });
                //注入WebAPI注释文件给Swagger       
                options.IgnoreObsoleteActions();
                // 类、方法标记 [Obsolete]，可以阻止【Swagger文档】生成
                options.DescribeAllEnumsAsStrings();
                //options.OperationFilter<FormDataOperationFilter>();
                // add a custom operation filter which sets default values
                //options.OperationFilter<SwaggerDefaultValues>();
                // integrate xml comments
                options.IncludeXmlComments(XmlCommentsFilePath);
            });
        }
        static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }
        #endregion
    }
}

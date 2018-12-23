using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
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
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            //AddAutoMapper(); //AutoMapper直接在DI初始化时候一并与服务一起注入
            AddAuthentication(services);
            AddSwagger(services);
            return RegisterIOC(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //UseAuthentication这里必需写在UseMvc()前面
            app.UseAuthentication();
            app.UseMvc();
            //引用Swagger服务
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityJwtSample"));
        }

        /// <summary>
        /// 注册IOC服务 
        /// 使用Autofac替换默认依赖注入组件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private IServiceProvider RegisterIOC(IServiceCollection services)
        {
            //注册服务到DI容器中,这里要将生命中期设置为单例,不然没办法测试删除、更新
            //services.AddSingleton<IUserService, UserService>();
            //这里改为用Autofac实现依赖注入
            List<Assembly> assemblies = new List<Assembly>();
            assemblies.Add(Assembly.GetExecutingAssembly());
            ContainerManager.Container = AutofacConfig.RegisterService(assemblies, services);

            return new AutofacServiceProvider(ContainerManager.Container);
        }

        /// <summary>
        /// 初始化AutoMapper
        /// </summary>
        private void AddAutoMapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfile());
            });
        }

        /// <summary>
        /// 新增身份验证
        /// </summary>
        /// <param name="services"></param>
        private void AddAuthentication(IServiceCollection services)
        {
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
                        //这里用微软默认的DI容器居然每次都是重新实例化了一次IUserService,明明我注入时候申明了服务生命周期为单例...
                        //这里改成用Autofac实现...
                        //var setviceProvider = services.BuildServiceProvider();
                        //var userService = setviceProvider.GetService<IUserService>();
                        IUserService userService = ContainerManager.Container.Resolve<IUserService>();
                        var userId = int.Parse(context.Principal.Identity.Name);
                        /*在生产环境里其实建议使用Redis或其他缓存服务中取(当然在获取授权时需要将与之对应的验证信息存入缓存)，减轻数据库压力
                        同时生产环境建议添加一个API提供给用户主动撤销Token，撤销的Token也存入缓存,这种Token称为黑名单，
                        黑名单仅用于使尚未过期的Token
                        */
                        var user = userService.GetById(userId);
                        if (user == null)//校验失败
                        {
                            context.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    },
                    
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

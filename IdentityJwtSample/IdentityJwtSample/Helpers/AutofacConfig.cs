using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using IdentityJwtSample.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
 

namespace IdentityJwtSample.Helpers
{
    public class AutofacConfig
    {
        public static List<Type> RetrieveProfiles(List<Assembly> assemblieLst)
        {
            var loadedProfiles = ExtractProfiles(assemblieLst);
            return loadedProfiles;
        }

        private static List<Type> ExtractProfiles(IEnumerable<Assembly> assemblies)
        {
            var profiles = new List<Type>();
            foreach (var assembly in assemblies)
            {
                var assemblyProfiles = assembly.ExportedTypes.Where(type => type.IsSubclassOf(typeof(Profile)));
                profiles.AddRange(assemblyProfiles);
            }
            return profiles;
        }


        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="assemblieLst"></param>
        public static IContainer RegisterService(List<Assembly> assemblieLst, IServiceCollection services)
        {
            var builder = new ContainerBuilder();
            builder.Populate(services);
            var baseType = typeof(IDependency);
            assemblieLst.ForEach((assem) =>
            {
                if (assem != null)
                {
                    builder.RegisterAssemblyTypes(assem)
                    .Where(type => baseType.IsAssignableFrom(type) && !type.IsAbstract)
                   .AsImplementedInterfaces().SingleInstance();
                }
            });
            List<Type> loadedProfiles = RetrieveProfiles(assemblieLst);
            builder.RegisterTypes(loadedProfiles.ToArray());
            IContainer container = builder.Build();
            RegisterAutoMapper(container, loadedProfiles);
            return container;
        }

        public static void RegisterAutoMapper(IContainer container, IEnumerable<Type> loadedProfiles)
        {
            Mapper.Initialize(cfg =>
            {
                cfg.ConstructServicesUsing(container.Resolve);
                foreach (var profile in loadedProfiles)
                {
                    var resolvedProfile = container.Resolve(profile) as Profile;
                    cfg.AddProfile(resolvedProfile);
                }
            });
        }

    }
}

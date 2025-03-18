using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeSpirit.Shared.Extensions
{
    public static class MapperExtension
    {
        /// <summary>
        /// 为实现IBaseCRUDIService的服务自动配置AutoMapper映射关系
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TDto">DTO类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
        /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
        /// <typeparam name="TBatchImportDto">批量导入DTO类型</typeparam>
        /// <param name="profile">AutoMapper配置文件</param>
        /// <returns>配置文件实例，支持链式调用</returns>
        public static Profile ConfigureBaseCRUDIMappings<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto>(
            this Profile profile)
            where TEntity : class
            where TDto : class
            where TKey : IEquatable<TKey>
            where TCreateDto : class
            where TUpdateDto : class
            where TBatchImportDto : class
        {
            // 实体 -> DTO
            profile.CreateMap<TEntity, TDto>();

            // 创建DTO -> 实体
            profile.CreateMap<TCreateDto, TEntity>();

            // 更新DTO -> 实体
            profile.CreateMap<TUpdateDto, TEntity>();

            // 批量导入DTO -> 实体
            profile.CreateMap<TBatchImportDto, TEntity>();

            // 分页列表 -> 分页列表DTO
            profile.CreateMap<PageList<TEntity>, PageList<TDto>>();

            return profile;
        }

        /// <summary>
        /// 自动扫描程序集并配置所有实现IBaseCRUDIService的服务映射
        /// </summary>
        /// <param name="configuration">AutoMapper配置表达式</param>
        /// <param name="assemblies">要扫描的程序集，如果为空则扫描当前应用程序域中的所有程序集</param>
        public static void ConfigureAllCRUDIMappings(this IMapperConfigurationExpression configuration, params Assembly[] assemblies)
        {
            // 如果没有指定程序集，则获取当前应用程序域中的所有程序集
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            // 获取泛型接口类型定义
            var crudInterfaceType = typeof(IBaseCRUDIService<,,,,,>);

            foreach (var assembly in assemblies)
            {
                // 获取程序集中的所有类型
                var types = assembly.GetTypes();

                // 查找实现了IBaseCRUDIService的服务类
                foreach (var type in types)
                {
                    // 跳过接口和抽象类
                    if (type.IsInterface || type.IsAbstract)
                        continue;

                    // 查找该类型实现的所有接口
                    var interfaceTypes = type.GetInterfaces();

                    foreach (var interfaceType in interfaceTypes)
                    {
                        // 检查是否是泛型接口
                        if (!interfaceType.IsGenericType)
                            continue;

                        // 获取泛型接口定义
                        var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();

                        // 检查是否是IBaseCRUDIService接口
                        if (genericTypeDefinition == crudInterfaceType)
                        {
                            // 获取泛型参数
                            var genericArgs = interfaceType.GetGenericArguments();
                            var entityType = genericArgs[0];
                            var dtoType = genericArgs[1];
                            var keyType = genericArgs[2];
                            var createDtoType = genericArgs[3];
                            var updateDtoType = genericArgs[4];
                            var batchImportDtoType = genericArgs[5];

                            // 创建实体到DTO的映射
                            CreateMap(configuration, entityType, dtoType);

                            // 创建DTO到实体的映射
                            CreateMap(configuration, createDtoType, entityType);
                            CreateMap(configuration, updateDtoType, entityType);
                            CreateMap(configuration, batchImportDtoType, entityType);

                            // 创建分页列表的映射
                            ConfigurePageListMapping(configuration, entityType, dtoType);
                        }
                    }
                }
            }
        }

        private static void CreateMap(IMapperConfigurationExpression configuration, Type sourceType, Type destinationType)
        {
            // 使用更灵活的方式获取CreateMap方法
            var methods = typeof(IMapperConfigurationExpression)
                .GetMethods()
                .Where(m => 
                    m.Name == "CreateMap" && 
                    m.IsGenericMethod)
                .ToList();

            var methodInfo = methods.FirstOrDefault();
            if (methodInfo == null)
            {
                throw new InvalidOperationException("无法找到CreateMap方法");
            }

            try
            {
                // 使用动态类型
                dynamic configurationDynamic = configuration;
                dynamic sourceTypeDynamic = sourceType;
                dynamic destinationTypeDynamic = destinationType;

                // 动态调用CreateMap
                configurationDynamic.CreateMap(sourceTypeDynamic, destinationTypeDynamic);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"调用CreateMap方法时出错: {ex.Message}", ex);
            }
        }

        private static void ConfigurePageListMapping(IMapperConfigurationExpression configuration, Type entityType, Type dtoType)
        {
            // 创建PageList<TEntity>到PageList<TDto>的映射
            var pageListEntityType = typeof(PageList<>).MakeGenericType(entityType);
            var pageListDtoType = typeof(PageList<>).MakeGenericType(dtoType);

            try
            {
                // 使用动态类型
                dynamic configurationDynamic = configuration;
                dynamic sourceTypeDynamic = pageListEntityType;
                dynamic destinationTypeDynamic = pageListDtoType;

                // 动态调用CreateMap
                configurationDynamic.CreateMap(sourceTypeDynamic, destinationTypeDynamic);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"配置PageList映射时出错: {ex.Message}", ex);
            }
        }
    }
}

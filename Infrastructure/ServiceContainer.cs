using System;
using System.Collections.Generic;
using AutoCAD_Layer_Manger.Services;

namespace AutoCAD_Layer_Manger.Infrastructure
{
    /// <summary>
    /// 簡單的依賴注入容器
    /// </summary>
    public interface IServiceContainer
    {
        void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
            where TInterface : class;

        void RegisterSingleton<T>(T instance) where T : class;

        void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
            where TInterface : class;

        T GetService<T>() where T : class;
        object GetService(Type serviceType);
        bool IsRegistered<T>() where T : class;
    }

    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new();
        private readonly Dictionary<Type, object> _singletonInstances = new();

        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
            where TInterface : class
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Singleton
            };
        }

        public void RegisterSingleton<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            
            _singletonInstances[typeof(T)] = instance;
            _services[typeof(T)] = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                ImplementationType = typeof(T),
                Lifetime = ServiceLifetime.Singleton
            };
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
            where TInterface : class
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Transient
            };
        }

        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }

        public object GetService(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
            }

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                if (_singletonInstances.TryGetValue(serviceType, out var instance))
                {
                    return instance;
                }

                instance = CreateInstance(descriptor.ImplementationType);
                _singletonInstances[serviceType] = instance;
                return instance;
            }

            return CreateInstance(descriptor.ImplementationType);
        }

        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        private object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors();
            
            // 尋找最適合的建構函式（參數最多的）
            var constructor = constructors[0];
            foreach (var ctor in constructors)
            {
                if (ctor.GetParameters().Length > constructor.GetParameters().Length)
                {
                    constructor = ctor;
                }
            }

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = GetService(parameters[i].ParameterType);
            }

            return Activator.CreateInstance(type, args)!;
        }

        private class ServiceDescriptor
        {
            public Type ServiceType { get; set; } = null!;
            public Type ImplementationType { get; set; } = null!;
            public ServiceLifetime Lifetime { get; set; }
        }

        private enum ServiceLifetime
        {
            Transient,
            Singleton
        }
    }

    /// <summary>
    /// 服務定位器
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceContainer? _container;

        public static void SetContainer(IServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public static T GetService<T>() where T : class
        {
            if (_container == null)
            {
                throw new InvalidOperationException("Service container is not initialized.");
            }

            return _container.GetService<T>();
        }

        public static bool IsInitialized => _container != null;
    }

    /// <summary>
    /// 服務配置
    /// </summary>
    public static class ServiceConfiguration
    {
        public static IServiceContainer ConfigureServices()
        {
            var container = new ServiceContainer();

            // 註冊服務
            container.RegisterSingleton<ISettingsManager, JsonSettingsManager>();
            container.RegisterSingleton<IEntityConverter, EntityConverter>();
            container.RegisterSingleton<ILayerService, LayerService>();
            container.RegisterSingleton<AppStateManager>(
                new AppStateManager(container.GetService<ISettingsManager>()));

            return container;
        }
    }
}
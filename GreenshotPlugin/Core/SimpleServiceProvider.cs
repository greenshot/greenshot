using System;
using System.Collections.Generic;
using System.Linq;
using GreenshotPlugin.Interfaces;

namespace GreenshotPlugin.Core
{
    /// <summary>
    /// A really cheap and simple DI system
    /// </summary>
    public class SimpleServiceProvider : IServiceLocator
    {
        private readonly Dictionary<Type, List<object>> _services = new Dictionary<Type, List<object>>();

        public static IServiceLocator Current { get; } = new SimpleServiceProvider();

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            var typeOfService = typeof(TService);
            if (!_services.TryGetValue(typeOfService, out var results))
            {
                yield break;
            }
            foreach (TService result in results)
            {
                yield return result;
            }
        }

        public TService GetInstance<TService>()
        {
            return GetAllInstances<TService>().SingleOrDefault();
        }

        public void AddService<TService>(IEnumerable<TService> services)
        {
            var serviceType = typeof(TService);
            if (!_services.TryGetValue(serviceType, out var currentServices))
            {
                currentServices = new List<object>();
                _services.Add(serviceType, currentServices);
            }

            foreach (var service in services)
            {
                currentServices.Add(service);
            }
        }

        public void AddService<TService>(params TService[] services)
        {
            AddService(services.AsEnumerable());
        }
    }
}

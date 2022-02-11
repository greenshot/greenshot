using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// A really cheap and simple DI system
    /// </summary>
    public class SimpleServiceProvider : IServiceLocator
    {
        private readonly Dictionary<Type, IList<object>> _services = new();

        public static IServiceLocator Current { get; } = new SimpleServiceProvider();

        public IReadOnlyList<TService> GetAllInstances<TService>()
        {
            var typeOfService = typeof(TService);
            if (!_services.TryGetValue(typeOfService, out var results))
            {
                return Array.Empty<TService>();
            }

            return results.Cast<TService>().ToArray();
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
                if (service == null)
                {
                    continue;
                }

                currentServices.Add(service);
            }
        }

        public void AddService<TService>(params TService[] services)
        {
            AddService(services.AsEnumerable());
        }
    }
}
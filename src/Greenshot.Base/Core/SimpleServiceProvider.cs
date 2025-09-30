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

        /// <summary>
        /// Gets the current instance of the service locator.
        /// </summary>
        public static IServiceLocator Current { get; } = new SimpleServiceProvider();

        /// <inheritdoc/>
        public IReadOnlyList<TService> GetAllInstances<TService>()
        {
            var typeOfService = typeof(TService);
            if (!_services.TryGetValue(typeOfService, out var results))
            {
                return Array.Empty<TService>();
            }

            return results.Cast<TService>().ToArray();
        }

        /// <inheritdoc/>
        public TService GetInstance<TService>()
        {
            return GetAllInstances<TService>().SingleOrDefault();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void AddService<TService>(params TService[] services)
        {
            AddService(services.AsEnumerable());
        }

        /// <inheritdoc/>
        public void RemoveService<TService>(IEnumerable<TService> services)
        {
            var serviceType = typeof(TService);
            if (!_services.TryGetValue(serviceType, out var currentServices))
            {
                return;
            }

            foreach (var service in services)
            {
                if (service == null)
                {
                    continue;
                }
                currentServices.Remove(service);
            }

            if (currentServices.Count == 0)
            {
                _services.Remove(serviceType);
            }
        }

        /// <inheritdoc/>
        public void RemoveService<TService>(params TService[] services)
        {
            RemoveService(services.AsEnumerable());
        }
    }
}
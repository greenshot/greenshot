using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// A really cheap and simple DI system (until the Generic Host of the .NET 10 move), thread safe.
    /// </summary>
    public class SimpleServiceProvider : IServiceProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SimpleServiceProvider));
        private readonly Dictionary<Type, List<object>> _services = new();
        private readonly object _lock = new();

        /// <summary>
        /// Gets the current instance of the service provider.
        /// </summary>
        public static SimpleServiceProvider Current { get; } = new SimpleServiceProvider();

        /// <summary>
        /// All registered instances of the service type
        /// </summary>
        public IReadOnlyList<TService> GetAllInstances<TService>()
        {
            lock (_lock)
            {
                if (!_services.TryGetValue(typeof(TService), out var results))
                {
                    return Array.Empty<TService>();
                }

                return results.Cast<TService>().ToArray();
            }
        }

        /// <summary>
        /// The one instance of the service type
        /// </summary>
        /// <param name="isOptional">false: throw when there is none</param>
        public TService GetInstance<TService>(bool isOptional = false)
        {
            try
            {
                var instances = GetAllInstances<TService>();

                if (instances.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Found {instances.Count} instances of {typeof(TService).FullName}, but expected only one."
                    );
                }

                var instance = instances.FirstOrDefault();

                if (!isOptional && instance is null)
                {
                    throw new InvalidOperationException(
                        $"No instance of {typeof(TService).FullName} found, but it is required."
                    );
                }

                return instance;
            }
            catch (Exception ex)
            {
                Log.Error($"GetInstance failed for {typeof(TService)}", ex);
                throw;
            }
        }

        /// <summary>
        /// IServiceProvider: the last registered instance of the type, null when there is none
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            lock (_lock)
            {
                return _services.TryGetValue(serviceType, out var results) && results.Count > 0 ? results[results.Count - 1] : null;
            }
        }

        /// <summary>
        /// Register the services
        /// </summary>
        public void AddService<TService>(IEnumerable<TService> services)
        {
            if (services == null)
            {
                return;
            }

            // Materialize outside the lock, the enumeration can run code
            var toAdd = services.Where(service => service != null).Cast<object>().ToList();
            lock (_lock)
            {
                if (!_services.TryGetValue(typeof(TService), out var currentServices))
                {
                    currentServices = new List<object>();
                    _services.Add(typeof(TService), currentServices);
                }

                currentServices.AddRange(toAdd);
            }
        }

        /// <summary>
        /// Register the services
        /// </summary>
        public void AddService<TService>(params TService[] services)
        {
            AddService(services.AsEnumerable());
        }

        /// <summary>
        /// Remove the services
        /// </summary>
        public void RemoveService<TService>(IEnumerable<TService> services)
        {
            if (services == null)
            {
                return;
            }

            var toRemove = services.Where(service => service != null).Cast<object>().ToList();
            lock (_lock)
            {
                if (!_services.TryGetValue(typeof(TService), out var currentServices))
                {
                    return;
                }

                foreach (var service in toRemove)
                {
                    currentServices.Remove(service);
                }

                if (currentServices.Count == 0)
                {
                    _services.Remove(typeof(TService));
                }
            }
        }

        /// <summary>
        /// Remove the services
        /// </summary>
        public void RemoveService<TService>(params TService[] services)
        {
            RemoveService(services.AsEnumerable());
        }
    }
}

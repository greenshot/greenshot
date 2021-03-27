using System.Collections.Generic;

namespace GreenshotPlugin.Interfaces
{
    public interface IServiceLocator
    {
        IEnumerable<TService> GetAllInstances<TService>();
        TService GetInstance<TService>();

        void AddService<TService>(params TService[] services);

        void AddService<TService>(IEnumerable<TService> services);
    }
}

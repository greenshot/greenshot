using System.Collections.Generic;

namespace Greenshot.Base.Interfaces
{
    public interface IServiceLocator
    {
        IEnumerable<TService> GetAllInstances<TService>();
        TService GetInstance<TService>();

        void AddService<TService>(params TService[] services);

        void AddService<TService>(IEnumerable<TService> services);
    }
}
using System.Collections.Generic;
using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DemandDomain;
using ZppForPrimitives;

namespace Zpp.ProviderDomain
{
    /**
     * wraps the collection with all providers
     */
    public interface IProviders
    {
        void Add(Provider provider);

        void AddAll(Providers providers);
        
        List<Provider> GetAll();
        
        List<T> GetAllAs<T>();

        bool ProvideMoreThan(Quantity quantity);
    }
}
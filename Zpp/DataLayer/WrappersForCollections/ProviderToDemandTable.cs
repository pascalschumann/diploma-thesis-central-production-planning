using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DataLayer.ProviderDomain;

namespace Zpp.DataLayer.WrappersForCollections
{
    public class ProviderToDemandTable : CollectionWrapperWithStackSet<T_ProviderToDemand>, IProviderToDemandTable
    {
        public ProviderToDemandTable(List<T_ProviderToDemand> list) : base(list)
        {
        }

        public ProviderToDemandTable()
        {
        }

        public void Add(Provider provider, Id demandId, Quantity quantity)
        {
            T_ProviderToDemand providerToDemand = new T_ProviderToDemand();
            providerToDemand.DemandId = demandId.GetValue();
            providerToDemand.ProviderId = provider.GetId().GetValue();
            providerToDemand.Quantity = quantity.GetValue();

            StackSet.Push(providerToDemand);
        }
        
    }
}
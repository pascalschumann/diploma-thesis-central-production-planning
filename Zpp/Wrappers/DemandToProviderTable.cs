using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.DemandDomain;
using Zpp.DemandToProviderDomain;
using Zpp.ProviderDomain;
using Zpp.Utils;

namespace Zpp
{
    /**
     * wraps T_DemandToProvider
     */
    public class DemandToProviderTable : IDemandToProviderTable
    {
        private readonly List<T_DemandToProvider> _demandToProviderEntities = new List<T_DemandToProvider>();

        public DemandToProviderTable(List<T_DemandToProvider> demandToProviderEntities)
        {
            _demandToProviderEntities = demandToProviderEntities;
        }

        public DemandToProviderTable()
        {
        }

        public DemandToProviderTable(IDemandToProviders demandToProviders)
        {
            _demandToProviderEntities.AddRange(demandToProviders.ToDemandToT_DemandToProvider());
        }

        public List<T_DemandToProvider> GetAll()
        {
            return _demandToProviderEntities;
        }

        public void AddAll(IDemandToProviders demandToProviders)
        {
            _demandToProviderEntities.AddRange(demandToProviders.ToDemandToT_DemandToProvider());
        }

        public IDemandToProviders ToDemandToProviders(IDbTransactionData dbTransactionData)
        {
            IDemandToProviders demandToProviders = new DemandToProviders();
            
            foreach (var demandToProviderEntity in _demandToProviderEntities)
            {
                Demand demand =
                    dbTransactionData.DemandsGetById(new Id(demandToProviderEntity.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(demandToProviderEntity.ProviderId));
                if (demand == null || provider == null)
                {
                    throw new MrpRunException("Could not find demand or provider.");
                }
                
                demandToProviders.AddProviderForDemand(demand, provider);
                    
            }

            return demandToProviders;
        }
    }
}
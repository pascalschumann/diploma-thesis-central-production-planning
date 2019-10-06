using System.Collections.Generic;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.DataLayer.DemandDomain.Wrappers;

namespace Zpp.DataLayer.DemandDomain.WrappersForCollections
{
    /**
     * wraps the collection with all stockExchangeDemands
     */
    public class StockExchangeDemands : Demands
    {
        public StockExchangeDemands(List<T_StockExchange> iDemands
            ) : base(ToDemands(iDemands))
        {
        }

        private static List<Demand> ToDemands(List<T_StockExchange> iDemands)
        {
            List<Demand> demands = new List<Demand>();
            foreach (var iDemand in iDemands)
            {
                if (iDemand.StockExchangeType.Equals(StockExchangeType.Provider))
                {
                    continue;
                }
                demands.Add(new StockExchangeDemand(iDemand));
            }

            return demands;
        }
    }
}
using System.Collections.Generic;
using Master40.DB.DataModel;
using Zpp.DataLayer.DemandDomain.Wrappers;

namespace Zpp.DataLayer.DemandDomain.WrappersForCollections
{
    /**
     * wraps collection with all customerOrderParts
     */
    public class CustomerOrderParts : Demands
    {
        public CustomerOrderParts(List<T_CustomerOrderPart> iDemands
            ) : base(ToDemands(iDemands))
        {
        }

        private static List<Demand> ToDemands(List<T_CustomerOrderPart> iDemands
            )
        {
            List<Demand> demands = new List<Demand>();
            foreach (var iDemand in iDemands)
            {
                demands.Add(new CustomerOrderPart(iDemand));
            }

            return demands;
        }
    }
}
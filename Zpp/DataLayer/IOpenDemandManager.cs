using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.Common.ProviderDomain;
using Zpp.DbCache;

namespace Zpp.Mrp.NodeManagement
{
    public interface IOpenDemandManager
    {
        void AddDemand(Demand oneDemand, Quantity reservedQuantity);

        ResponseWithDemands SatisfyProviderByOpenDemand(Provider provider, Quantity demandedQuantity);
        
        void Dispose();
    }
}
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;

namespace Zpp.Mrp2
{
    public interface IMrp2
    {
        void ManufacturingResourcePlanning(IDemands dbDemands);
        
        
        void StartMrp2();
    }
}
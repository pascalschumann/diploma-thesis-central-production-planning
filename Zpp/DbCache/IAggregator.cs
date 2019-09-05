using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Mrp.MachineManagement;
using Zpp.Simulation.Types;

namespace Zpp.DbCache
{
    /**
     * A layer over masterData/transactionData that provides aggregations of entities from masterData/transactionData
     */
    public interface IAggregator
    {
        ProductionOrderBoms GetProductionOrderBomsOfProductionOrder(ProductionOrder productionOrder);

        List<Machine> GetMachinesOfProductionOrderOperation(ProductionOrderOperation productionOrderOperation);

        List<ProductionOrderOperation> GetProductionOrderOperationsOfMachine(Machine machine);
        
        List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(ProductionOrder productionOrder);
        
        List<ProductionOrderOperation> GetProductionOrderOperationsOfProductionOrder(Id productionOrderId);

        Demands GetDemandsOfProvider(Provider provider);

        ProductionOrderBom GetAnyProductionOrderBomByProductionOrderOperation(ProductionOrderOperation productionOrderOperation);

        ProductionOrderBoms GetAllProductionOrderBomsBy(
            ProductionOrderOperation productionOrderOperation);

        Providers GetAllProvidersOf(Demand demand);

        Demands GetAllDemandsOf(Provider provider);
        
        List<Provider> GetProvidersForCurrent(SimulationInterval simulationInterval);
        
    }
}
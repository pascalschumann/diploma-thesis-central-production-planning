﻿using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.DbCache;
using Zpp.Mrp.MachineManagement;
using Zpp.Mrp.ProductionManagement.ProductionTypes;
using Zpp.OrderGraph;

namespace Zpp.Simulation.Agents.JobDistributor.Types
{
    public class OperationManager
    {
        private readonly IDbMasterDataCache _dbMasterDataCache;
        private readonly IDbTransactionData _dbTransactionData;
        private readonly IDirectedGraph<INode> _productionOrderGraph;
        private readonly ProductionOrderOperationGraphs _productionOrderOperationGraphs;
        


        public OperationManager(IDbMasterDataCache dbMasterDataCache, 
                              IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            _dbMasterDataCache = dbMasterDataCache;
            _productionOrderGraph = new ProductionOrderDirectedGraph(_dbTransactionData, false);
            _productionOrderOperationGraphs = new ProductionOrderOperationGraphs();
            Init();
        }

        private void Init()
        {
            foreach (var productionOrder in _dbTransactionData.ProductionOrderGetAll())
            {
                IDirectedGraph<INode> productionOrderOperationGraph =
                    new ProductionOrderOperationDirectedGraph(_dbTransactionData,
                        (ProductionOrder)productionOrder);
                _productionOrderOperationGraphs.Add((ProductionOrder)productionOrder,
                    productionOrderOperationGraph);
            }
        }

        /// <summary>
        /// returns the mature cherry's
        /// </summary>
        /// <returns></returns>
        public IStackSet<ProductionOrderOperation> GetLeafs()
        {
            var productionOrderOperations
                    = MachineManager.CreateS(_productionOrderGraph, _productionOrderOperationGraphs);
            return productionOrderOperations;
        }
    }
}
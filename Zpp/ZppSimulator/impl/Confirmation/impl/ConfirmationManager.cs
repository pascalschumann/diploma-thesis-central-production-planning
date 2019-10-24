using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrappersForCollections;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.ZppSimulator.impl.Confirmation.impl
{
    public class ConfirmationManager : IConfirmationManager
    {
        public void CreateConfirmations(SimulationInterval simulationInterval)
        {
            /*ISimulator simulator = new Simulator();
            simulator.ProcessCurrentInterval(simulationInterval, _orderGenerator);*/
            // --> does not work correctly, use trivial impl instead

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            // stockExchanges, purchaseOrderParts, operations(use PrBom instead):
            // set in progress when startTime is within interval
            DemandOrProviders demandOrProvidersToSetInProgress = new DemandOrProviders();
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.PurchaseOrderPartGetAll())));
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeDemandsGetAll())));
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeProvidersGetAll())));
            demandOrProvidersToSetInProgress.AddAll(
                aggregator.GetDemandsOrProvidersWhereStartTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.ProductionOrderBomGetAll())));

            foreach (var demandOrProvider in demandOrProvidersToSetInProgress)
            {
                demandOrProvider.SetInProgress();
            }

            // stockExchanges, purchaseOrderParts, operations(use PrBom instead):
            // set done when endTime is within interval
            DemandOrProviders demandOrProvidersToSetDone = new DemandOrProviders();
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.PurchaseOrderPartGetAll())));
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeDemandsGetAll())));
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.StockExchangeProvidersGetAll())));
            demandOrProvidersToSetDone.AddAll(
                aggregator.GetDemandsOrProvidersWhereEndTimeIsWithinInterval(simulationInterval,
                    new DemandOrProviders(dbTransactionData.ProductionOrderBomGetAll())));
            foreach (var demandOrProvider in demandOrProvidersToSetDone)
            {
                demandOrProvider.SetDone();
            }

            // customerOrderParts: set done if all childs are done
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
            INodes rootNodes = demandToProviderGraph.GetRootNodes();
            foreach (var rootNode in rootNodes)
            {
                bool isDone = processChilds(demandToProviderGraph.GetSuccessorNodes(rootNode),
                    demandToProviderGraph);
                if (isDone)
                {
                    CustomerOrderPart customerOrderPart = (CustomerOrderPart) rootNode.GetEntity();
                    customerOrderPart.SetDone();
                }
            }
        }

        /**
         * Top-down traversing demandToProviderGraph
         */
        private bool processChilds(INodes childs, DemandToProviderGraph demandToProviderGraph)
        {
            if (childs == null)
            {
                return true;
            }

            foreach (var child in childs)
            {
                IDemandOrProvider demandOrProvider = (IDemandOrProvider) child.GetEntity();
                if (demandOrProvider.IsDone())
                {
                    return processChilds(demandToProviderGraph.GetSuccessorNodes(child),
                        demandToProviderGraph);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public void ApplyConfirmations()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            // Lösche alle children der COPs (StockExchangeProvider) inclusive Pfeile auf und weg
            foreach (var customerOrderPart in dbTransactionData.T_CustomerOrderPartGetAll())
            {
                IProviders providers = aggregator.GetAllChildProvidersOf(customerOrderPart);
                if (providers.Count() > 1)
                {
                    throw new MrpRunException("A customerOrderPart can only have one provider.");
                }

                foreach (var provider in providers)
                {
                    IEnumerable<T_DemandToProvider> demandToProviders = dbTransactionData
                        .DemandToProviderGetAll().GetAll()
                        .Where(x => x.GetProviderId().Equals(provider.GetId()));
                    IEnumerable<T_ProviderToDemand> providerToDemands = dbTransactionData
                        .ProviderToDemandGetAll().GetAll()
                        .Where(x => x.GetProviderId().Equals(provider.GetId()));
                    dbTransactionData.DemandToProviderDeleteAll(demandToProviders);
                    dbTransactionData.ProviderToDemandDeleteAll(providerToDemands);
                    dbTransactionData.StockExchangeProvidersDelete(
                        (StockExchangeProvider) provider);
                }
            }

            // ProductionOrder: 3 Zustände siehe DA
            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                State state =
                    DetermineProductionOrderState((ProductionOrder) productionOrder, aggregator);
                switch (state)
                {
                    case State.Created:
                        ApplyProductionOrderIsInStateCreated((ProductionOrder) productionOrder,
                            aggregator, dbTransactionData);
                        break;
                    case State.InProgress:
                        ApplyProductionOrderIsInProgress();
                        break;
                    case State.Finished:
                        ApplyProductionOrderIsDone((ProductionOrder) productionOrder, aggregator,
                            dbTransactionData);
                        break;
                    default: throw new MrpRunException("This state is not expected.");
                }
            }
        }

        /**
         * Subgraph of a productionOrder includes:
         * - parent (StockExchangeDemand)
         * - childs (ProductionOrderBoms)
         * - childs of childs (StockExchangeProvider)
         */
        private List<IDemandOrProvider> GetDemandOrProvidersOfProductionOrderSubGraph(
            bool IncludeParentStockExchangeDemand, ProductionOrder productionOrder,
            IAggregator aggregator)
        {
            List<IDemandOrProvider> demandOrProvidersOfProductionOrderSubGraph =
                new List<IDemandOrProvider>();

            if (IncludeParentStockExchangeDemand)
            {
                IDemands stockExchangeDemands = aggregator.GetAllParentDemandsOf(productionOrder);
                if (stockExchangeDemands.Count() > 1)
                {
                    throw new MrpRunException(
                        "A productionOrder can only have one parentDemand (stockExchangeDemand).");
                }

                demandOrProvidersOfProductionOrderSubGraph.AddRange(stockExchangeDemands);
            }

            IDemands productionOrderBoms = aggregator.GetAllChildDemandsOf(productionOrder);
            demandOrProvidersOfProductionOrderSubGraph.AddRange(productionOrderBoms);
            foreach (var productionOrderBom in productionOrderBoms)
            {
                IProviders stockExchangeProvider =
                    aggregator.GetAllChildProvidersOf(productionOrderBom);
                if (stockExchangeProvider.Count() > 1)
                {
                    throw new MrpRunException(
                        "A ProductionOrderBom can only have one childProvider (stockExchangeProvider).");
                }

                demandOrProvidersOfProductionOrderSubGraph.AddRange(stockExchangeProvider);
            }

            return demandOrProvidersOfProductionOrderSubGraph;
        }

        private void ApplyProductionOrderIsInStateCreated(ProductionOrder productionOrder,
            IAggregator aggregator, IDbTransactionData dbTransactionData)
        {
            // delete all operations
            List<ProductionOrderOperation> operations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            dbTransactionData.ProductionOrderOperationDeleteAll(operations);

            // collect entities and demandToProviders/providerToDemands to delete
            List<IDemandOrProvider> demandOrProvidersToDelete =
                GetDemandOrProvidersOfProductionOrderSubGraph(true, productionOrder, aggregator);

            // delete all collected entities
            foreach (var demandOrProvider in demandOrProvidersToDelete)
            {
                List<ILinkDemandAndProvider> demandAndProviders =
                    aggregator.GetArrowsToAndFrom(demandOrProvider);
                dbTransactionData.DeleteAllFrom(demandAndProviders);

                dbTransactionData.DeleteA(demandOrProvider);
            }
        }

        private void ApplyProductionOrderIsInProgress()
        {
            // nothing to do here
            return;
        }

        private void ApplyProductionOrderIsDone(ProductionOrder productionOrder,
            IAggregator aggregator, IDbTransactionData dbTransactionData)
        {
            IDbTransactionData dbTransactionDataArchive =
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive();

            // archive operations
            List<ProductionOrderOperation> operations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            dbTransactionDataArchive.ProductionOrderOperationAddAll(operations);
            dbTransactionData.ProductionOrderOperationDeleteAll(operations);

            // archive demands Or providers
            List<IDemandOrProvider> demandOrProvidersToArchive =
                GetDemandOrProvidersOfProductionOrderSubGraph(false, productionOrder, aggregator);


            // delete all collected entities
            foreach (var demandOrProvider in demandOrProvidersToArchive)
            {
                List<ILinkDemandAndProvider> demandAndProviderLinks =
                    aggregator.GetArrowsToAndFrom(demandOrProvider);
                dbTransactionDataArchive.AddAllFrom(demandAndProviderLinks);
                dbTransactionData.DeleteAllFrom(demandAndProviderLinks);

                dbTransactionDataArchive.AddA(demandOrProvider);
                dbTransactionData.DeleteA(demandOrProvider);
            }
        }

        private State DetermineProductionOrderState(ProductionOrder productionOrder,
            IAggregator aggregator)
        {
            bool atLeastOneIsInProgress = false;
            bool atLeastOneIsDone = false;
            bool atLeastOneIsInStateCreated = false;
            var productionOrderOperations =
                aggregator.GetProductionOrderOperationsOfProductionOrder(productionOrder);
            foreach (var productionOrderOperation in productionOrderOperations)
            {
                if (productionOrderOperation.IsInProgress())
                {
                    atLeastOneIsInProgress = true;
                    break;
                }
                else if (productionOrderOperation.IsDone())
                {
                    atLeastOneIsDone = true;
                }
                else
                {
                    atLeastOneIsInStateCreated = true;
                }
            }

            if (atLeastOneIsInProgress || atLeastOneIsInStateCreated && atLeastOneIsDone)
            {
                return State.InProgress;
            }
            else if (atLeastOneIsInStateCreated && !atLeastOneIsInProgress && !atLeastOneIsDone)
            {
                return State.Created;
            }
            else if (atLeastOneIsDone && !atLeastOneIsInProgress && !atLeastOneIsInStateCreated)
            {
                return State.Created;
            }
            else
            {
                throw new MrpRunException("This state is not expected.");
            }
        }
    }
}
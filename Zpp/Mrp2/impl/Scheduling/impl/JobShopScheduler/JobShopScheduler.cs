using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.DataLayer.impl.WrapperForEntities;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.StackSet;

namespace Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler
{
    public class JobShopScheduler : IJobShopScheduler
    {
        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

        public void ScheduleWithGifflerThompsonAsZaepfel(IPriorityRule priorityRule)
        {
            IProductionOrderToOperationGraph<INode> productionOrderToOperationGraph =
                new ProductionOrderToOperationGraph();

            Dictionary<Id, List<Resource>> resourcesByResourceSkillId =
                new Dictionary<Id, List<Resource>>();
            foreach (var resourceSkill in _dbMasterDataCache.M_ResourceSkillGetAll())
            {
                resourcesByResourceSkillId.Add(resourceSkill.GetId(),
                    ZppConfiguration.CacheManager.GetAggregator()
                        .GetResourcesByResourceSkillId(resourceSkill.GetId()));
            }

            // set correct idleStartTimes in resources from operations in progress
            /*IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            foreach (var operation in dbTransactionData.ProductionOrderOperationGetAll())
            {
                if (operation.IsInProgress())
                {
                    foreach (var resource in resourcesByResourceSkillId[
                        operation.GetResourceSkillId()])
                    {
                        if (resource.GetValue().Id.Equals(operation.GetValue().ResourceId) &&
                            resource.GetIdleStartTime().GetValue() < operation.GetEndTime())
                        {
                            resource.SetIdleStartTime(new DueTime(operation.GetEndTime()));
                        }
                    }
                }
            }*/


            /*
            S: Menge der aktuell einplanbaren Arbeitsvorgänge
            a: Menge der technologisch an erster Stelle eines Fertigungsauftrags stehenden Arbeitsvorgänge
            N(o): Menge der technologisch direkt nachfolgenden Arbeitsoperationen von Arbeitsoperation o
            M(o): Maschine auf der die Arbeitsoperation o durchgeführt wird
            K: Konfliktmenge (die auf einer bestimmten Maschine gleichzeitig einplanbaren Arbeitsvorgänge)            
            p(o): Bearbeitungszeit von Arbeitsoperation o (=Duration)
            t(o): Startzeit der Operation o (=Start)
            d(o): Fertigstellungszeitpunkt von Arbeitsoperation o (=End)
            d_min: Minimum der Fertigstellungszeitpunkte
            o_min: Operation mit minimalem Fertigstellungszeitpunkt
            o1: beliebige Operation aus K (o_dach bei Zäpfel)
            */
            IStackSet<ProductionOrderOperation> S = new StackSet<ProductionOrderOperation>();

            // Bestimme initiale Menge: S = a
            S = CreateS(productionOrderToOperationGraph);

            // t(o) = 0 für alle o aus S
            foreach (var o in S)
            {
                int newStart = o.GetStartTimeBackward().GetValue();
                o.SetStartTime(newStart);
            }

            // while S not empty do
            while (S != null && S.Any())
            {
                int d_min = Int32.MaxValue;
                ProductionOrderOperation o_min = null;
                foreach (var o in S)
                {
                    // Berechne d(o) = t(o) + p(o) für alle o aus S
                    int newEnd = o.GetStartTime() + o.GetValue().Duration;
                    o.SetEndTime(newEnd);
                    // Bestimme d_min = min{ d(o) | o aus S }
                    if (o.GetEndTime() < d_min)
                    {
                        d_min = o.GetEndTime();
                        o_min = o;
                    }
                }

                // Bilde Konfliktmenge K = { o | o aus S UND M(o) == M(o_min) UND t(o) < d_min }
                IStackSet<ProductionOrderOperation> K = new StackSet<ProductionOrderOperation>();
                foreach (var o in S)
                {
                    if (o.GetValue().ResourceSkillId.Equals(o_min.GetValue().ResourceSkillId) &&
                        o.GetStartTime() < d_min)
                    {
                        K.Push(o);
                    }
                }

                // while K not empty do
                if (K.Any())
                {
                    // Entnehme Operation mit höchster Prio (o1) aus K und plane auf nächster freier Resource ein

                    List<ProductionOrderOperation> allO1 = new List<ProductionOrderOperation>();

                    foreach (var machine in resourcesByResourceSkillId[o_min.GetResourceSkillId()]
                        .OrderBy(x => x.GetIdleStartTime().GetValue()))
                    {
                        if (K.Any() == false)
                        {
                            break;
                        }

                        ProductionOrderOperation o1 = null;
                        o1 = priorityRule.GetHighestPriorityOperation(machine.GetIdleStartTime(),
                            K.GetAll());
                        if (o1 == null)
                        {
                            throw new MrpRunException("This is not possible if K.Any() is true.");
                        }

                        allO1.Add(o1);

                        K.Remove(o1);

                        o1.SetMachine(machine);
                        // correct op's start time if resource's idleTime is later
                        if (machine.GetIdleStartTime().GetValue() > o1.GetStartTime())
                        {
                            int newStart = machine.GetIdleStartTime().GetValue();
                            o1.SetStartTime(newStart);
                            int newEnd = o1.GetStartTime() + o1.GetValue().Duration;
                            o1.SetEndTime(newEnd);
                        }

                        // correct op's start time if op's material is later available
                        DueTime dueTimeOfOperationMaterial = o1.GetEarliestPossibleStartTime();
                        if (dueTimeOfOperationMaterial.GetValue() > o1.GetStartTime())
                        {
                            int newStart = dueTimeOfOperationMaterial.GetValue();
                            o1.SetStartTime(newStart) ;
                            int newEnd = o1.GetStartTime() + o1.GetValue().Duration;
                            o1.SetEndTime(newEnd);
                        }

                        machine.SetIdleStartTime(new DueTime(o1.GetEndTime()));
                    }


                    // t(o) = d(o1) für alle o aus K ohne alle o1 
                    foreach (var o in K)
                    {
                        o.SetStartTime(allO1[0].GetEndTime());
                    }

                    /*if N(o1) not empty then
                        S = S vereinigt N(o1) ohne alle o1
                     */
                    foreach (var o1 in allO1)
                    {
                        INode o1AsNode = new Node(o1);

                        IStackSet<ProductionOrderOperation> predecessorOperations =
                            new StackSet<ProductionOrderOperation>();
                        productionOrderToOperationGraph.DeterminePredecessorOperations(
                            predecessorOperations, o1AsNode);

                        IStackSet<ProductionOrderOperation> N = predecessorOperations
                            .As<ProductionOrderOperation>();

                        // t(o) = d(o1) für alle o aus N(o1)
                        if (N != null)
                        {
                            foreach (var n in N)
                            {
                                n.SetStartTime(o1.GetEndTime());
                            }
                        }

                        // prepare for next round
                        productionOrderToOperationGraph.RemoveNode(o1AsNode);
                    }

                    S = CreateS(productionOrderToOperationGraph);
                }
            }
        }

        /**
         * @return: all leafs of all operationGraphs
         */
        private IStackSet<ProductionOrderOperation> CreateS(
            IProductionOrderToOperationGraph<INode> productionOrderToOperationGraph)
        {
            IStackSet<ProductionOrderOperation> S = new StackSet<ProductionOrderOperation>();
            productionOrderToOperationGraph.GetLeafOperations(S);

            return S;
        }
    }
}
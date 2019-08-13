using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.DemandDomain;
using Zpp.GraphicalRepresentation;
using Zpp.MachineDomain;
using Zpp.ProviderDomain;
using Zpp.Utils;

namespace Zpp
{
    public class OrderGraph : IGraph<INode>
    {
        private readonly Dictionary<INode, List<IEdge>> _adjacencyList =
            new Dictionary<INode, List<IEdge>>();

        private readonly IDbTransactionData _dbTransactionData;

        public OrderGraph(IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            
            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll().GetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(demandToProvider.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(demandToProvider.ProviderId));
                Assert.True(demand != null || provider != null,
                    "Demand/Provider should not be null.");
                INode fromNode = new Node(demand, demandToProvider.GetDemandId());
                INode toNode = new Node(provider, demandToProvider.GetProviderId());
                AddEdge(fromNode, new Edge(demandToProvider, fromNode, toNode)
                );
            }

            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll().GetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(providerToDemand.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(providerToDemand.ProviderId));
                Assert.True(demand != null || provider != null,
                    "Demand/Provider should not be null.");

                INode fromNode = new Node(provider, providerToDemand.GetProviderId());
                INode toNode = new Node(demand, providerToDemand.GetDemandId());
                AddEdge(fromNode, new Edge(providerToDemand.ToDemandToProvider(), fromNode, toNode)
                );
            }
        }

        public List<INode> GetChildNodes(INode fromNode)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                return null;
            }

            return _adjacencyList[fromNode].Select(x => x.GetToNode()).ToList();
        }

        public void AddEdges(INode fromNode, List<IEdge> edges)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, edges);
                return;
            }

            _adjacencyList[fromNode].AddRange(edges);
        }

        public void AddEdge(INode fromNode, IEdge edge)
        {
            if (!_adjacencyList.ContainsKey(fromNode))
            {
                _adjacencyList.Add(fromNode, new List<IEdge>());
            }

            _adjacencyList[fromNode].Add(edge);
        }

        public int CountEdges()
        {
            return GetAllToNodes().Count;
        }

        public List<IEdge> GetAllEdgesForFromNode(INode fromNode)
        {
            return _adjacencyList[fromNode];
        }

        public override string ToString()
        {
            string mystring = "";
            foreach (var fromNode in GetAllFromNodes())
            {
                foreach (var edge in GetAllEdgesForFromNode(fromNode))
                {
                    // <Type>, <Menge>, <ItemName> and on edges: <Menge>
                    Quantity quantity = new Quantity(edge.GetDemandToProvider().Quantity);
                    mystring +=
                        $"\"{fromNode.GetId()};{fromNode.GetGraphizString(_dbTransactionData)}\" -> " +
                        $"\"{edge.GetToNode().GetId()};{edge.GetToNode().GetGraphizString(_dbTransactionData)}\"";
                    if (quantity.IsNull() == false)
                    {
                        mystring += $" [ label=\" {quantity}\" ]";    
                    }
                    mystring += ";" + Environment.NewLine;
                }
            }

            return mystring;
        }

        public List<INode> GetAllToNodes()
        {
            List<INode> toNodes = new List<INode>();

            foreach (var edges in _adjacencyList.Values.ToList())
            {
                foreach (var edge in edges)
                {
                    toNodes.Add(edge.GetToNode());
                }
            }

            return toNodes;
        }

        // 
        // TODO: Switch this to iterative depth search (with dfs limit default set to max depth of given truck examples)
        ///
        /// <summary>
        ///     A depth-first-search (DFS) traversal of given tree
        /// </summary>
        /// <param name="graph">to traverse</param>
        /// <returns>
        ///    The List of the traversed nodes in exact order
        /// </returns>
        public List<INode> TraverseDepthFirst(Action<INode, List<INode>, List<INode>> action, CustomerOrderPart startNode)
        {
            var stack = new Stack<INode>();

            Dictionary<INode, bool> discovered = new Dictionary<INode, bool>();
            List<INode> traversed = new List<INode>();

            stack.Push(startNode);
            INode parentNode;

            while (stack.Any())
            {
                INode poppedNode = stack.Pop();

                // init dict if node not yet exists
                if (!discovered.ContainsKey(poppedNode))
                {
                    discovered[poppedNode] = false;
                }

                // if node is not discovered
                if (!discovered[poppedNode])
                {
                    traversed.Add(poppedNode);
                    discovered[poppedNode] = true;
                    List<INode> childNodes = GetChildNodes(poppedNode);
                    action(poppedNode, childNodes, traversed);

                    if (childNodes != null)
                    {
                        foreach (INode node in childNodes)
                        {
                            stack.Push(node);
                        }
                    }
                }
            }

            return traversed;
        }

        public List<INode> GetAllFromNodes()
        {
            return _adjacencyList.Keys.ToList();
        }

        public List<INode> GetAllUniqueNode()
        {
            List<INode> fromNodes = GetAllFromNodes();
            List<INode> toNodes = GetAllToNodes();
            IStackSet<INode> uniqueNodes = new StackSet<INode>();
            uniqueNodes.PushAll(fromNodes);
            uniqueNodes.PushAll(toNodes);

            return uniqueNodes.GetAll();
        }

        public GanttChart GetAsGanttChart(IDbTransactionData dbTransactionData)
        {
            GanttChart ganttChart = new GanttChart();

                foreach (var node in GetAllUniqueNode())
                {
                    if (node.GetNodeType().Equals(NodeType.Demand))
                    {
                        Demand demand = (Demand) node.GetEntity();
                        GanttChartBar ganttChartBar = new GanttChartBar()
                        {
                            article = demand.GetArticle().Name,
                            articleId = demand.GetArticle().Id.ToString(),
                            end = demand.GetDueTime(dbTransactionData).ToString(),
                        };
                        if (demand.GetStartTime(dbTransactionData) != null)
                        {
                            ganttChartBar.start = demand.GetStartTime(dbTransactionData).ToString();
                        }

                        if (demand.GetType() == typeof(ProductionOrderBom))
                        {
                            ProductionOrderBom productionOrderBom =
                                (ProductionOrderBom) demand.GetEntity();
                            
                            ProductionOrderOperation productionOrderOperation =
                                productionOrderBom.GetProductionOrderOperation(dbTransactionData);
                            if (productionOrderOperation != null)
                            {
                                ganttChartBar.operation = productionOrderOperation.GetValue().Name;
                                ganttChartBar.operationId = productionOrderOperation.GetValue().Id.ToString();
                            }
                        }
                        
                        ganttChart.AddGanttChartBar(ganttChartBar);
                    }
                    else if (node.GetNodeType().Equals(NodeType.Provider))
                    {
                        Provider provider = (Provider) node.GetEntity();
                        GanttChartBar ganttChartBar = new GanttChartBar()
                        {
                            article = provider.GetArticle().Name,
                            articleId = provider.GetArticle().Id.ToString(),
                            end = provider.GetDueTime(dbTransactionData).ToString()
                        };
                        if (provider.GetStartTime(dbTransactionData) != null)
                        {
                            ganttChartBar.start = provider.GetStartTime(dbTransactionData).ToString();
                        }
                        
                        ganttChart.AddGanttChartBar(ganttChartBar);
                    }
                    
                    
                }

            return ganttChart;
        }
    }
}
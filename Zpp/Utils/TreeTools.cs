using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;

namespace Zpp.Utils
{
    public static class TreeTools<TNode>
    {
        // 
        // TODO 1: This must be revisited under following aspect: a article node can be existing multiple times in tree,
        // it must be ensured, that every multiple existing object have its own instance
        // TODO 2: Switch this to iterative depth search (with dfs limit default set to max depth of given truck examples)
        ///
        /// <summary>
        ///     A depth-first-search (DFS) traversal of given tree
        /// </summary>
        /// <param name="tree">to traverse</param>
        /// <returns>
        ///    The List of the traversed nodes in exact order
        /// </returns>
        public static List<TNode> traverseDepthFirst(ITree<TNode> tree, Action<TNode> action)
        {
            var stack = new Stack<TNode>();
            
            Dictionary<TNode, bool> discovered = new Dictionary<TNode, bool>();
            List<TNode> traversed = new List<TNode>();
            
            stack.Push(tree.getRootNode());
            while (stack.Any())
            {
                TNode poppedNode = stack.Pop();
                traversed.Add(poppedNode);
                
                // init dict if node not yet exists
                if (! discovered.ContainsKey(poppedNode) )
                {
                    discovered[poppedNode] = false;
                } 
                
                // if node is not discovered
                if (! discovered[poppedNode] )
                {
                    discovered[poppedNode] = true;
                    action(poppedNode);
                    
                    foreach (TNode node in tree.getChildNodes(poppedNode))
                    {
                        stack.Push(node);
                    }
                }
            }
            return traversed;
        }

        /**
         * prints the articleTree in following format (adjacencyList): parentId: child1, child2, ...
         */
        public static string AdjacencyListToString(Dictionary<int, IEnumerable<TNode>> adjacencyList)
        {
            string myString = "";
            foreach (int rowId in adjacencyList.Keys)
            {
                if (!adjacencyList[rowId].Any())
                {
                    continue;
                }
                myString += rowId + ": ";
                foreach (TNode node in adjacencyList[rowId])
                {
                    myString += node.ToString() + ", ";
                }
 
                myString = myString.Substring(0, myString.Length-2);
                myString += Environment.NewLine;
            }

            return myString;
        }
    }
}
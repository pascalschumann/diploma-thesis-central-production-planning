using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph.impl;

namespace Zpp.Util.Graph
{
    public interface INode
    {
        Id GetId();
        
        NodeType GetNodeType();

        INode GetEntity();
    }
}
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Simulation.impl.Types;
using Zpp.ZppSimulator.impl;

namespace Zpp.ZppSimulator
{
    public interface IZppSimulator
    {
        void StartOneCycle(SimulationInterval simulationInterval, Quantity customerOrderQuantity);

        void StartPerformanceStudy();

        void StartTestCycle();
    }
}
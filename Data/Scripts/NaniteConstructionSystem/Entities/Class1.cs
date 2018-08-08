using ParallelTasks;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Game.ModAPI;

namespace Ntech.Nanite.Entities
{
    public static class ConveyorWork
    {
        public static void DoWork(WorkData workData)
        {
            var data = workData as ConveyorWorkData;
            if (data == null)
                return;

            IMyTerminalBlock workBlock;
            if (!data.Blocks.TryDequeue(out workBlock))
                return;

            IMyInventory compareInventory = data.CompareBlock.GetInventory();
            IMyInventory workInventory = workBlock.GetInventory();

            if (compareInventory == null || workInventory == null)
                return;

            if (compareInventory.IsConnectedTo(workInventory))
                data.ConnectedInventories.Add(workBlock);
            else
                data.DisconnectedInventories.Add(workBlock);
        }

        public class ConveyorWorkData : WorkData
        {
            public IMyTerminalBlock CompareBlock;
            public MyConcurrentQueue<IMyTerminalBlock> Blocks;
            public MyConcurrentHashSet<IMyTerminalBlock> ConnectedInventories;
            public MyConcurrentHashSet<IMyTerminalBlock> DisconnectedInventories;

            public ConveyorWorkData(IMyTerminalBlock compare, MyConcurrentQueue<IMyTerminalBlock> blocks)
            {
                CompareBlock = compare;
                Blocks = blocks;
                ConnectedInventories = new MyConcurrentHashSet<IMyTerminalBlock>();
                DisconnectedInventories = new MyConcurrentHashSet<IMyTerminalBlock>();
            }
        }
    }

    public class Foo
    {
        private List<IMyTerminalBlock> _inventoryList;
        private IMyTerminalBlock _compareInventory;

        private void ProcessConveyors()
        {
            var blocks = new MyConcurrentQueue<IMyTerminalBlock>(_inventoryList.Count);
            foreach (IMyTerminalBlock b in _inventoryList)
                blocks.Enqueue(b);
            var data = new ConveyorWork.ConveyorWorkData(_compareInventory, blocks);
            for (var i = 0; i < _inventoryList.Count; i++)
                MyAPIGateway.Parallel.StartBackground(ConveyorWork.DoWork, Callback, data);
        }

        private void Callback(WorkData workData)
        {
            var data = workData as ConveyorWork.ConveyorWorkData;
            if (data == null)
                return;

            if (data.Blocks.Count > 0)
                return; //work isn't done

            MyConcurrentHashSet<IMyTerminalBlock> connected = data.ConnectedInventories;
            MyConcurrentHashSet<IMyTerminalBlock> disconnected = data.DisconnectedInventories;
            //process results
        }
    }
}

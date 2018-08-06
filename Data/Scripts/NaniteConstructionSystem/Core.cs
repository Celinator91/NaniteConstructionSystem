using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;

namespace Ntech.Nanite
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public const ushort PACKET_SYNC_CONFIG = 8956;

        public static Session Instance { get; private set; }

        internal List<LargeControlFacilityLogic> LargeControlFacilityLogics => largeControlFacilityLogics;

        private readonly List<LargeControlFacilityLogic> largeControlFacilityLogics = new List<LargeControlFacilityLogic>();

        #region Simulation / Init
        public override void BeforeStart()
        {
            base.BeforeStart();

            try
            {
                NaniteConstructionSystem.Logging.Instance.WriteLine($"Logging Started");
                //MyAPIGateway.Multiplayer.RegisterMessageHandler(PACKET_SYNC_CONFIG, PacketSettingsReceived);
            }
            catch (Exception ex) { NaniteConstructionSystem.Logging.Instance.WriteLine($"Exception in BeforeStart: {ex}"); }
        }
        #endregion

        #region Loading
        public override void LoadData()
        {
            Instance = this;
            NaniteConstructionSystem.Logging.Instance.WriteLine("Loaded");
        }

        protected override void UnloadData()
        {
            Instance = null;
            NaniteConstructionSystem.Logging.Instance.WriteLine("Unloaded");
        }
        #endregion
    }
}

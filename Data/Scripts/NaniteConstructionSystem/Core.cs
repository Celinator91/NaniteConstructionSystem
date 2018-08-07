using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Ntech.Nanite
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenFarm), true, "LargeNaniteBeaconProjection", "SmallNaniteBeaconProjection")]
    public class NaniteBeaconProjectionLogic : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("ADDING Projection Beacon: {0}", Entity.EntityId));
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return null;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        static bool _isInitialized = false;

        public static Session Instance { get; private set; }

        private static Config m_configuration;
        public static Config Configuration
        {
            get { return m_configuration; }
            private set
            {
                m_configuration = value;
            }
        }

        internal List<Entities.LargeControlFacilityLogic> LargeControlFacilityLogics => largeControlFacilityLogics;

        private readonly List<Entities.LargeControlFacilityLogic> largeControlFacilityLogics = new List<Entities.LargeControlFacilityLogic>();

        #region Simulation / Init
        public override void BeforeStart()
        {
            base.BeforeStart();

            try
            {
                NaniteConstructionSystem.Logging.Instance.WriteLine($"Logging Started");

                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    var cfg = new Config
                    {
                        test = "foo"
                    };
                    SetConfig(cfg);
                }

                MyAPIGateway.Multiplayer.RegisterMessageHandler(Extensions.MessageUtils.MessageId, Extensions.MessageUtils.HandleMessage);

                MyAPIGateway.Session.OnSessionReady += Session_OnSessionReady;
            }
            catch (Exception ex) { NaniteConstructionSystem.Logging.Instance.WriteLine($"Exception in BeforeStart: {ex}"); }
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isInitialized && MyAPIGateway.Session != null)
                Init();
        }

        private void Init()
        {
            _isInitialized = true;
            NaniteConstructionSystem.Logging.Instance.WriteLine("Session.Init()");
        }

        void Session_OnSessionReady()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                Extensions.MessageUtils.SendMessageToServer(new MessageClientConnected());
        }
        #endregion

        public static void SetConfig(Config config)
        {
            // The settings need to be merged into the existing client options
            var clientconfig = Configuration;   // current client settings
            Configuration = config;             // Replace client settings 

            NaniteConstructionSystem.Logging.Instance.WriteLine("Loading new settings from server");
            NaniteConstructionSystem.Logging.Instance.WriteLine(Configuration.test);
        }

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

    [ProtoContract]
    public class MessageConfig : Extensions.MessageBase
    {
        [ProtoMember(10)]
        public Config Configuration;

        public override void ProcessClient()
        {
            Session.SetConfig(Configuration);
        }

        public override void ProcessServer()
        {
        }
    }

    [ProtoContract]
    public class MessageClientConnected : Extensions.MessageBase
    {
        public override void ProcessClient()
        {
        }

        public override void ProcessServer()
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("Sending config to new client: {0}", SenderSteamId));
            // Send new clients the configuration
            Extensions.MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageConfig() { Configuration = Session.Configuration });
        }
    }

    [ProtoContract]
    public class MessageLargeControlFacilityStateChange : Extensions.MessageBase
    {
        [ProtoMember(10)]
        public long EntityId;

        [ProtoMember(11)]
        public Entities.LargeControlFacilityLogic.FactoryStates State;

        public override void ProcessClient()
        {
            foreach (var item in Session.Instance.LargeControlFacilityLogics)
            {
                if (item.Entity.EntityId == EntityId)
                    item.FactoryState = State;
            }
        }

        public override void ProcessServer()
        {
        }
    }

    [ProtoContract]
    public class Config
    {
        [ProtoMember(1)]
        public string test = "";
    }
}

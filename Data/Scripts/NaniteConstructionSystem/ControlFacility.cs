using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Ntech.Nanite
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipWelder), true, "LargeNaniteFactory")]
    class LargeControlFacilityLogic : MyGameLogicComponent
    {
        public IMyShipWelder Welder => (IMyShipWelder)Entity;

        private MySoundPair m_soundPair;
        private MyEntity3DSoundEmitter m_soundEmitter;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                Welder.AppendingCustomInfo += AppendingCustomInfo;

                m_soundPair = new MySoundPair("ArcParticleElectrical");
                m_soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity);
                m_soundEmitter.CustomMaxDistance = 30f;
                m_soundEmitter.CustomVolume = 2f;
            }
            catch (Exception ex) { NaniteConstructionSystem.Logging.Instance.WriteLine($"Exception in EntityInit: {ex}"); }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            try
            {
                if (Welder.CubeGrid.Physics == null) return;
                Session.Instance.LargeControlFacilityLogics.Add(this);
                if (MyAPIGateway.Multiplayer.IsServer)
                    Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
            catch (Exception ex) { NaniteConstructionSystem.Logging.Instance.WriteLine($"Exception in UpdateOnceBeforeFrame: {ex}"); }
        }

        public override void UpdateBeforeSimulation100()
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine("UpdateBeforeSimulation100 factory");
            base.UpdateBeforeSimulation100();

            if (MyAPIGateway.Multiplayer.IsServer)
                return;

            float emissivity = 1.0f;
            if (!Welder.Enabled || !Welder.IsFunctional)
            {
                m_soundEmitter.StopSound(true);
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.Red, Color.White);
            }

            if (MyAPIGateway.Gui?.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                Welder.RefreshCustomInfo();

                // Toggle to trigger UI update
                Welder.ShowInToolbarConfig = !Welder.ShowInToolbarConfig;
                Welder.ShowInToolbarConfig = !Welder.ShowInToolbarConfig;
            }

        }
        public override void Close()
        {
            if (Session.Instance != null)
            {
                Session.Instance.LargeControlFacilityLogics.Remove(this);
            }

            base.Close();
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            Random rnd = new Random();
            int rndnumber = rnd.Next();
            stringBuilder.Append(Session.Configuration.test + ": " + rndnumber.ToString());
        }
    }

    /// <summary>
    /// Class used to set emissives on a block dynamically
    /// </summary>
    public class MyCubeBlockEmissive : MyCubeBlock
    {
        public static void SetEmissiveParts(MyEntity entity, float emissivity, Color emissivePartColor, Color displayPartColor)
        {
            if (entity != null)
                UpdateEmissiveParts(entity.Render.RenderObjectIDs[0], emissivity, emissivePartColor, displayPartColor);
        }
    }
}

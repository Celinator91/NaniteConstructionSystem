using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Ntech.Nanite.Entities
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipWelder), false, "LargeNaniteFactory")]
    public class LargeControlFacilityLogic : MyGameLogicComponent
    {
        public enum FactoryStates
        {
            Disabled,
            Enabled,
            SpoolingUp,
            SpoolingDown,
            MissingParts,
            MissingPower,
            InvalidTargets,
            Active
        }

        public IMyShipWelder Welder => (IMyShipWelder)Entity;

        private FactoryStates m_factoryState = FactoryStates.Disabled;
        private FactoryStates m_lastState = FactoryStates.Disabled;
        public FactoryStates FactoryState
        {
            get { return m_factoryState; }
            set { m_factoryState = value; }
        }

        private Particles.NaniteParticleManager m_particleManager;
        public Particles.NaniteParticleManager ParticleManager
        {
            get { return m_particleManager; }
        }

        private const int m_spoolingTime = 3000;
        private int m_spoolPosition;
        private int m_updateCount;
        private MySoundPair m_soundPair;
        private MyEntity3DSoundEmitter m_soundEmitter;
        private List<Effects.NaniteBlockEffectBase> m_effects;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine("asdasdasd");
            try
            {
                base.Init(objectBuilder);

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                Welder.AppendingCustomInfo += AppendingCustomInfo;
                m_particleManager = new Particles.NaniteParticleManager(this);

                if (MyAPIGateway.Session.IsServer)
                    return;

                m_soundPair = new MySoundPair("ArcParticleElectrical");
                m_soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity);
                m_soundEmitter.CustomMaxDistance = 30f;
                m_soundEmitter.CustomVolume = 2f;

                m_effects = new List<Effects.NaniteBlockEffectBase>();
                m_effects.Add(new Effects.LightningBolt.LightningBoltEffect((MyCubeBlock)Entity));
                m_effects.Add(new Effects.CenterOrbEffect((MyCubeBlock)Entity));
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

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (MyAPIGateway.Session.IsServer)
            {
                ProcessState();
            }
            else
            {
                DrawEmissives();
                DrawEffects();
            }

            m_updateCount++;
        }

        public override void UpdateBeforeSimulation100()
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine("UpdateBeforeSimulation100 factory");
            base.UpdateBeforeSimulation100();

            if (MyAPIGateway.Multiplayer.IsServer)
                return;

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

        /// <summary>
        /// Check state of block (used to be just emissives, but emissive state has been turned into block state.  Will refactor names later)
        /// </summary>
        private void ProcessState()
        {
            IMyFunctionalBlock blockEntity = (IMyFunctionalBlock)Entity;
            //int totalTargets = m_targets.Sum(x => x.TargetList.Count);
            //int totalPotentialTargets = m_targets.Sum(x => x.PotentialTargetList.Count);
            //float totalPowerRequired = m_targets.Sum(x => x.TargetList.Count * x.GetPowerUsage());

            if (!blockEntity.Enabled || !blockEntity.IsFunctional)
            {
                m_factoryState = FactoryStates.Disabled;
            }
            if (true /*(totalTargets > 0)*/ && true /*NaniteConstructionPower.HasRequiredPower(blockEntity, totalPowerRequired)*/ || m_particleManager.Particles.Count > 0)
            {
                if (m_spoolPosition == m_spoolingTime)
                {
                    m_factoryState = FactoryStates.Active;
                }
                else
                {
                    m_factoryState = FactoryStates.SpoolingUp;
                }
            }
            //else if (totalTargets == 0 && totalPotentialTargets > 0 && !NaniteConstructionPower.HasRequiredPower(blockEntity, m_targets.Min(x => x.GetPowerUsage())))
            //{
            //    m_factoryState = FactoryStates.MissingPower;
            //}
            //else if (totalTargets == 0 && totalPotentialTargets > 0)
            //{
            //    m_factoryState = FactoryStates.InvalidTargets;

            //    foreach (var item in InventoryManager.ComponentsRequired.ToList())
            //    {
            //        if (item.Value <= 0)
            //            InventoryManager.ComponentsRequired.Remove(item.Key);
            //    }

            //    //if (InventoryManager.ComponentsRequired.Count > 0)
            //    //    m_factoryState = FactoryStates.MissingParts;
            //}
            else if (blockEntity.Enabled)
            {
                if (m_spoolPosition <= 0)
                {
                    m_spoolPosition = 0;
                    m_factoryState = FactoryStates.Enabled;
                }
                else
                    m_factoryState = FactoryStates.SpoolingDown;
            }
            else
            {
                m_factoryState = FactoryStates.Disabled;
            }

            if (m_factoryState != FactoryStates.Active && m_factoryState != FactoryStates.SpoolingUp && m_factoryState != FactoryStates.SpoolingDown)
            {
                if (m_spoolPosition > 0f)
                    m_factoryState = FactoryStates.SpoolingDown;
            }

            if (m_lastState != m_factoryState || m_updateCount % 120 == 0)
            {
                m_lastState = m_factoryState;
                Extensions.MessageUtils.SendMessageToAllPlayers(new MessageLargeControlFacilityStateChange()
                {
                    EntityId = Entity.EntityId,
                    State = m_factoryState
                });
            }
        }

        /// <summary>
        /// Change color of emissives on the block model to appropriate color
        /// </summary>
        private void DrawEmissives()
        {
            if (m_factoryState == FactoryStates.SpoolingUp)
            {
                m_spoolPosition += (int)(1000f / 60f);
                if (m_spoolPosition >= m_spoolingTime)
                    m_spoolPosition = m_spoolingTime;
            }
            else if (m_factoryState == FactoryStates.SpoolingDown)
            {
                m_spoolPosition -= (int)(1000f / 60f);
                if (m_spoolPosition <= 0)
                    m_spoolPosition = 0;
            }

            if (MyAPIGateway.Session.Player == null)
                return;

            float emissivity = 1.0f;
            IMyFunctionalBlock blockEntity = (IMyFunctionalBlock)Entity;
            if (!blockEntity.Enabled || !blockEntity.IsFunctional)
            {
                m_soundEmitter.StopSound(true);
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.Red, Color.White);
            }
            else if (m_factoryState == FactoryStates.Active)
            {
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.FromNonPremultiplied(new Vector4(0.05f, 0.05f, 0.35f, 0.75f)) * (((float)m_spoolPosition / m_spoolingTime) + 0.1f), Color.White);
            }
            else if (m_factoryState == FactoryStates.SpoolingUp)
            {
                if (m_spoolPosition >= m_spoolingTime)
                {
                    m_soundEmitter.PlaySound(m_soundPair, true);
                }

                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.FromNonPremultiplied(new Vector4(0.05f, 0.05f, 0.35f, 0.75f)) * (((float)m_spoolPosition / m_spoolingTime) + 0.1f), Color.White);
            }
            else if (m_factoryState == FactoryStates.SpoolingDown)
            {
                m_soundEmitter.StopSound(true);
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.FromNonPremultiplied(new Vector4(0.05f, 0.05f, 0.35f, 0.75f)) * (((float)m_spoolPosition / m_spoolingTime) + 0.1f), Color.White);
            }
            else if (m_factoryState == FactoryStates.MissingPower)
            {
                emissivity = (float)MathHelper.Clamp(0.5 * (1 + Math.Sin(2 * 3.14 * m_updateCount * 8)), 0.0, 1.0);
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.DarkGoldenrod * emissivity, Color.White);
            }
            else if (m_factoryState == FactoryStates.MissingParts)
            {
                emissivity = (float)MathHelper.Clamp(0.5 * (1 + Math.Sin(2 * 3.14 * m_updateCount * 8)), 0.0, 1.0);
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.DeepPink * emissivity, Color.White);
            }
            else if (m_factoryState == FactoryStates.InvalidTargets)
            {
                emissivity = (float)MathHelper.Clamp(0.5 * (1 + Math.Sin(2 * 3.14 * m_updateCount * 8)), 0.0, 1.0);
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.Lime * emissivity, Color.White);
            }
            else if (m_factoryState == FactoryStates.Enabled)
            {
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.Green, Color.White);
            }
            else
            {
                if (m_soundEmitter.IsPlaying)
                    m_soundEmitter.StopSound(true);

                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)Entity, emissivity, Color.Red, Color.White);
            }
        }


        /// <summary>
        /// Draws effects (lightning and center spinning orb)
        /// </summary>
        private void DrawEffects()
        {
            foreach (var item in m_effects)
            {
                if (m_factoryState == FactoryStates.Active)
                    item.ActiveUpdate();
                else if (m_factoryState == FactoryStates.SpoolingUp)
                    item.ActivatingUpdate(m_spoolPosition, m_spoolingTime);
                else if (m_factoryState == FactoryStates.SpoolingDown)
                    item.DeactivatingUpdate(m_spoolPosition, m_spoolingTime);
                else
                    item.InactiveUpdate();
            }
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
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Ntech.Nanite
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Welder), false, "LargeNaniteFactory")]
    class LargeControlFacilityLogic : MyGameLogicComponent
    {
        public IMyShipWelder Welder => (IMyShipWelder)Entity;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
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
    }
}

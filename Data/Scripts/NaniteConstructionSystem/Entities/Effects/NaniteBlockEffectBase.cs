using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ntech.Nanite.Entities.Effects
{
    public abstract class NaniteBlockEffectBase
    {
        protected static string m_modelsFolder;

        public NaniteBlockEffectBase()
        {
            if (m_modelsFolder == null)
                GetModelsFolder();
        }

        private void GetModelsFolder()
        {
            m_modelsFolder = Path.GetFullPath(Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, MyAPIGateway.Utilities.GamePaths.ModScopeName.Split('_')[0], "Models", "Cubes", "large") + Path.DirectorySeparatorChar);
        }

        public abstract void ActiveUpdate();
        public abstract void InactiveUpdate();
        public abstract void ActivatingUpdate(int position, int maxPosition);
        public abstract void DeactivatingUpdate(int position, int maxPosition);
        public abstract void Unload();
    }
}

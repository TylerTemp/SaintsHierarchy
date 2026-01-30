using System;
using System.Collections.Generic;
using UnityEditor;

namespace SaintsHierarchy.Editor
{
    public class RuntimeCacheConfig: ScriptableSingleton<RuntimeCacheConfig>
    {
        [Serializable]
        public struct RuntimeConfig
        {
            public int instanceId;
            public GameObjectConfig config;
        }

        public List<RuntimeConfig> configs = new List<RuntimeConfig>();

        public void Upsert(int instanceId, GameObjectConfig config)
        {
            int index = -1;
            for (int searchIndex = 0; searchIndex < configs.Count; searchIndex++)
            {
                // ReSharper disable once InvertIf
                if (instanceId == configs[searchIndex].instanceId)
                {
                    index = searchIndex;
                    break;
                }
            }

            RuntimeConfig newConfig = new RuntimeConfig
            {
                instanceId = instanceId,
                config = config,
            };

            if (index == -1)
            {
                configs.Add(newConfig);
            }
            else
            {
                configs[index] = newConfig;
            }
        }

        public (bool found, GameObjectConfig config) Search(int instanceId)
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (RuntimeConfig runtimeConfig in configs)
            {
                if (runtimeConfig.instanceId == instanceId)
                {
                    return (true, runtimeConfig.config);
                }
            }

            return (false, default);
        }
    }
}

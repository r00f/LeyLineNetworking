using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using UnityEngine;

namespace BlankProject
{
    public static class OneTimeInitialisation
    {
        private static bool initialized;
        private static uint faction = 1;

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            WorldsInitializationHelper.SetupInjectionHooks();
            PlayerLoopManager.RegisterDomainUnload(WorldsInitializationHelper.DomainUnloadShutdown, 1000);

            // Setup template to use for player on connecting client
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = LeyLineEntityTemplates.Player;
            faction++;

        }
    }
}

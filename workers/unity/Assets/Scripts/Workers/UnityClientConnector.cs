using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using UnityEngine;
using Unity.Entities;


namespace BlankProject
{
    public class UnityClientConnector : WorkerConnector
    {
        private const string AuthPlayer = "Prefabs/UnityClient/Authoritative/Player";
        private const string NonAuthPlayer = "Prefabs/UnityClient/NonAuthoritative/Player";
        #pragma warning disable 649
        [SerializeField] private bool UseExternalIp;
        [SerializeField] private GameObject level;
        #pragma warning restore 649

        public const string WorkerType = "UnityClient";

        private async void Start()
        {
            Application.targetFrameRate = 60;

            var connParams = CreateConnectionParameters(WorkerUtils.UnityClient);
            connParams.Network.UseExternalIp = UseExternalIp;

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityClient), initializer));
                        break;
                    case ConnectionService.Locator:
                        builder.SetConnectionFlow(new LocatorFlow(initializer));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityClient)));
            }

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worlds.ClientWorld = Worker.World.EntityManager;
            Worlds.DefaultWorld = World.AllWorlds[0].EntityManager;
            WorkerUtils.AddClientSystems(Worker.World);

            var fallback = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, new AdvancedEntityPipeline(Worker, AuthPlayer, NonAuthPlayer), gameObject);
            PlayerLifecycleHelper.AddClientSystems(Worker.World);
        }
    }
}

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

        public const string WorkerType = "UnityClient";

        private async void Start()
        {
            var connParams = CreateConnectionParameters(WorkerType);
            connParams.Network.ConnectionType = NetworkConnectionType.Kcp;

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType), initializer));
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
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType)));
            }

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worlds.ClientWorld = Worker.World.EntityManager;
            Worlds.DefaultWorld = World.AllWorlds[0].EntityManager;
            WorkerUtils.AddClientSystems(Worker.World);

            //Debug.Log(Worlds.DefaultWorld.World.Name);
            //FOR SOME REASON THIS HAS CEASED TO WORK NOW WE CHECK IF WORLDS IS INITIALIZED IN SYSTEMS INSTEAD
            //World.Active.GetOrCreateSystem<LeyLineHybridECS.MeshColorLerpSystem>();
            //World.Active.GetOrCreateSystem<ProjectileSystem>();
            //World.Active.GetOrCreateSystem<ClientCleanupSystem>();

            var fallback = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, new AdvancedEntityPipeline(Worker, AuthPlayer, NonAuthPlayer, fallback), gameObject);
            PlayerLifecycleHelper.AddClientSystems(Worker.World);
        }
    }
}

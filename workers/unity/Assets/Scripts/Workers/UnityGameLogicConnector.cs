using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Unity.Entities;
using UnityEngine;

namespace BlankProject
{
    public class UnityGameLogicConnector : WorkerConnector
    {
        public const string WorkerType = "UnityGameLogic";

        private async void Start()
        {
            Application.targetFrameRate = 60;
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = LeyLineEntityTemplates.Player;


            IConnectionFlow flow;
            ConnectionParameters connectionParameters;

            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityGameLogic));
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityGameLogic),
                    new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic,
                    new CommandLineConnectionParameterInitializer());
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worlds.GameLogicWorld = Worker.World.EntityManager;
            WorkerUtils.AddGameLogicSystems(Worker.World);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, gameObject);
            Worker.World.GetOrCreateSystem<MetricSendSystem>();
            PlayerLifecycleHelper.AddServerSystems(Worker.World);
        }
    }
}

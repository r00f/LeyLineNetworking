using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace BlankProject
{
    public class MapSpawnWorkerConnector : WorkerConnector
    {
        public const string WorkerType = "MapSpawn";
        [SerializeField] private EntityRepresentationMapping entityRepresentationMapping;

        private async void Start()
        {
            Application.targetFrameRate = 60;
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = LeyLineEntityTemplates.Player;

            IConnectionFlow flow;
            ConnectionParameters connectionParameters;

            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.MapSpawn));
                connectionParameters = CreateConnectionParameters(WorkerUtils.MapSpawn);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.MapSpawn),
                    new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerUtils.MapSpawn,
                    new CommandLineConnectionParameterInitializer());
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            WorkerUtils.AddMapSpawnSystems(Worker.World);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, entityRepresentationMapping, gameObject);
            Worker.World.GetOrCreateSystem<MetricSendSystem>();
            PlayerLifecycleHelper.AddServerSystems(Worker.World);
        }
    }
}

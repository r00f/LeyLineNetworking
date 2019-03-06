
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Gdk.GameObjectCreation;
using Unity.Entities;

namespace BlankProject
{
    public class UnityClientConnector : DefaultWorkerConnector
    {
        private const string AuthPlayer = "Prefabs/UnityClient/Authoritative/Player";
        private const string NonAuthPlayer = "Prefabs/UnityClient/NonAuthoritative/Player";

        public const string WorkerType = "UnityClient";

        private async void Start()
        {
            await Connect(WorkerType, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worlds.ClientWorld = Worker.World.GetOrCreateManager<EntityManager>();
            PlayerLifecycleHelper.AddClientSystems(Worker.World);
            GameObjectRepresentationHelper.AddSystems(Worker.World);
            WorkerUtils.AddClientSystems(Worker.World);
            World.Active.GetOrCreateManager<LeyLineHybridECS.MeshColorLerpSystem>();
            var fallback = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, new AdvancedEntityPipeline(Worker, AuthPlayer, NonAuthPlayer, fallback), gameObject);
        }

        protected override string SelectDeploymentName(DeploymentList deployments)
        {
            return deployments.Deployments[0].DeploymentName;
        }
    }
}
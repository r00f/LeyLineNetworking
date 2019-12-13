
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Worker.CInterop;
using UnityEngine;



public class EntityCreationBehaviour : MonoBehaviour
{
    //private WorldCommands.Requirable.WorldCommandRequestSender commandSender;
    //private WorldCommands.Requirable.WorldCommandResponseHandler responseHandler;

    void OnEnable()
    {
        // Register callback for listening to any incoming create entity command responses for this entity
        //if (responseHandler != null)
            //responseHandler.OnCreateEntityResponse += OnCreateEntityResponse;

    }

    private void Start()
    {

    }

    public void CreateExampleEntity(EntityTemplate entityTemplate)
    {
        //if(commandSender != null)
        //commandSender.CreateEntity(entityTemplate);
    }

    void OnCreateEntityResponse(WorldCommands.CreateEntity.ReceivedResponse response)

    {

        if (response.StatusCode == StatusCode.Success)

        {

            var createdEntityId = response.EntityId.Value;

            // handle success

        }

        else

        {

            // handle failure

        }

    }

}
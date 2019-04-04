using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using Unit;
using Generic;
using Improbable;

public class ClientMovementSystem : ComponentSystem
{

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Position.Component> Positions;
        public readonly ComponentDataArray<ServerPath.Component> ServerPaths;
        public ComponentArray<AnimatorComponent> AnimatorComponents;
        public ComponentArray<Transform> Transforms;
    }

    [Inject] private UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;


    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var serverPosition = m_UnitData.Positions[i];
            var transform = m_UnitData.Transforms[i];
            var animatorComponent = m_UnitData.AnimatorComponents[i];
            var serverPath = m_UnitData.ServerPaths[i];

            if (transform.position != serverPosition.Coords.ToUnityVector())
            {
                //move
                transform.position = Vector3.MoveTowards(transform.position, serverPosition.Coords.ToUnityVector(), Time.deltaTime);
                //rotate towards movement direction
                Vector3 targetDir = serverPosition.Coords.ToUnityVector() - animatorComponent.RotateTransform.position;
                float rotSpeed = Time.deltaTime * 3;
                Vector3 newDir = Vector3.RotateTowards(animatorComponent.RotateTransform.forward, targetDir, rotSpeed, 0.0f);
                animatorComponent.RotateTransform.rotation = Quaternion.LookRotation(newDir);
                animatorComponent.Animator.SetBool("Moving", true);
            }
            else
            {
                animatorComponent.Animator.SetBool("Moving", false);
            }
        }
    }

}

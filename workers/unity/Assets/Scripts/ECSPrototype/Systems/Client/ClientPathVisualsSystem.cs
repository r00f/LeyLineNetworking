using UnityEngine;
using System.Collections;
using Improbable.Gdk.Core;
using System.Collections.Generic;
using Cells;
using LeyLineHybridECS;
using Unity.Entities;
using Unit;
using Generic;
using Player;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class ClientPathVisualsSystem : ComponentSystem
{

    public struct ClientPathData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<ClientPath.Component> PathData;
    }

    [Inject] private ClientPathData m_ClientPathData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public ComponentDataArray<ClientPath.Component> Paths;
        public ComponentArray<LineRendererComponent> LineRenderers;
        public ComponentArray<UnitComponentReferences> ComponentReferences;
    }

    [Inject] private UnitData m_UnitData;

    public struct AuthoritativePlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
    }

    [Inject] private AuthoritativePlayerData m_AuthoritativePlayerData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;



    protected override void OnUpdate()
    {
        for (int li = 0; li < m_UnitData.Length; li++)
        {
            var lr = m_UnitData.LineRenderers[li];
            var unitWorldIndex = m_UnitData.WorldIndexData[li].Value;

            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                if (unitWorldIndex == gameStateWorldIndex)
                {
                    if (m_GameStateData.GameState[gi].CurrentState != GameStateEnum.planning)
                    {
                        if (m_GameStateData.GameState[gi].CurrentState == GameStateEnum.calculate_energy)
                        {
                            var path = m_UnitData.Paths[li];
                            if (path.Path.CellAttributes.Count > 0)
                            {
                                path.Path.CellAttributes.Clear();
                                m_UnitData.Paths[li] = path;
                            }
                        }
                        if (lr.lineRenderer.enabled)
                        {
                            lr.lineRenderer.enabled = false;
                        }
                    }
                }
            }
        }

        for (int pi = 0; pi < m_ClientPathData.Length; pi++)
        {
            var mouseState = m_ClientPathData.MouseStateData[pi];
            var path = m_ClientPathData.PathData[pi];
            var playerState = m_AuthoritativePlayerData.PlayerStateData[0];

            if (playerState.CurrentState == PlayerStateEnum.waiting_for_target)
            {
                for (int ci = 0; ci < m_CellData.Length; ci++)
                {
                    var cellMousestate = m_CellData.MouseStateData[ci];
                    var cellAttributes = m_CellData.CellAttributes[ci];

                    if (cellMousestate.CurrentState == MouseState.State.Hovered)
                    {
                        var cellsToMark = m_ClientPathData.CellsToMarkData[pi];
                        path.Path = FindPath(cellAttributes.CellAttributes.Cell, cellsToMark.CachedPaths);
                        m_ClientPathData.PathData[pi] = path;
                    }
                }
            }
        }

        //Update LineRenderers

        for (int li = 0; li < m_UnitData.Length; li++)
        {
            var mouseState = m_UnitData.MouseStateData[li];
            var lr = m_UnitData.LineRenderers[li];
            var playerState = m_AuthoritativePlayerData.PlayerStateData[0];
            var unitId = m_UnitData.EntityIds[li].EntityId.Id;
            
            if (playerState.SelectedUnitId == unitId)
            {
                var path = m_UnitData.Paths[li];
                var cellsToMark = m_UnitData.CellsToMarkData[li];
                //update Linerenderer
                lr.lineRenderer.enabled = true;
                lr.lineRenderer.positionCount = path.Path.CellAttributes.Count + 1;
                lr.lineRenderer.SetPosition(0, lr.transform.position + lr.offset);

                for (int pi = 1; pi <= path.Path.CellAttributes.Count; pi++)
                {
                    lr.lineRenderer.SetPosition(pi, path.Path.CellAttributes[pi - 1].Position.ToUnityVector() + lr.offset);
                }
            }
        }
    }

    public CellAttributeList FindPath(CellAttribute destination, Dictionary<CellAttribute, CellAttributeList> cachedPaths)
    {
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }
}

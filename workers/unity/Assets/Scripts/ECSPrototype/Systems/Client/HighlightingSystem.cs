using Cell;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using LeyLineHybridECS;
using Player;
using System;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(PlayerStateSystem)), UpdateAfter(typeof(SendActionRequestSystem)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(AddComponentsSystem))]
public class HighlightingSystem : ComponentSystem
{
    public struct ActiveUnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Unit_BaseDataSet> BaseDataSets;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public ComponentDataArray<MouseState> MouseStates;
        public ComponentArray<LineRendererComponent> LineRenderers;
    }

    [Inject]
    ActiveUnitData m_ActiveUnitData;

    public struct PlayerStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
        public readonly ComponentDataArray<FactionComponent.Component> Faction;
        public ComponentDataArray<HighlightingDataComponent> HighlightingData;
        public ComponentDataArray<PlayerState.Component> PlayerState;

    }

    [Inject]
    PlayerStateData m_PlayerStateData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
        public readonly ComponentDataArray<GameState.Component> GameState;

    }
    [Inject]
    GameStateData m_GameStateData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentArray<Transform> Transforms;
        public ComponentDataArray<MarkerState> MarkerStateData;
    }

    [Inject] CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<Health.Component> HealthAttributes;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentArray<LineRendererComponent> LineRenderers;
        public readonly ComponentArray<Transform> Transforms;
    }

    [Inject] UnitData m_UnitData;

    public struct MarkerStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Position.Component> Positions;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
        public ComponentDataArray<MarkerState> MarkerStates;
        public ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<SpatialEntityId> EntityIDs;
    }

    [Inject] MarkerStateData m_MarkerStateData;

    [Inject] HandleCellGridRequestsSystem m_CellGrid;

    [Inject] ResourceSystem m_ResourceSystem;

    protected override void OnUpdate()
    {
        if (m_PlayerStateData.Length == 0)
            return;

        var playerState = m_PlayerStateData.PlayerState[0];
        var playerWorldIndex = m_PlayerStateData.WorldIndex[0].Value;
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];
        var playerFaction = m_PlayerStateData.Faction[0].Faction;

        //used to increase performance - does not work for clearing old AoE when selecting new action yet
        //HashSet<Vector3f> extendedCoordsInRangeHash = new HashSet<Vector3f>(m_CellGrid.CircleDraw(playerState.SelectedUnitCoordinate, playerHighlightingData.Range + playerHighlightingData.AoERadius + playerHighlightingData.RingRadius));

        for (int j = 0; j < m_GameStateData.Length; j++)
        {
            var gameStateWorldIndex = m_GameStateData.WorldIndex[j].Value;
            var gameState = m_GameStateData.GameState[j].CurrentState;

            if (playerWorldIndex == gameStateWorldIndex)
            {
                if (gameState == GameStateEnum.planning)
                {
                    if (playerHighlightingData.HoveredCoordinate != playerHighlightingData.LastHoveredCoordinate)
                    {
                        if (playerState.CurrentState == PlayerStateEnum.waiting_for_target && playerHighlightingData.TargetRestrictionIndex != 2)
                        {
                            FillUnitTargetsList(playerHighlightingData);
                            SetNumberOfTargets();
                            UpdateSelectedUnit();
                        }
                        else
                        {
                            ResetHighlights();
                        }

                        playerHighlightingData.LastHoveredCoordinate = playerHighlightingData.HoveredCoordinate;
                        m_PlayerStateData.HighlightingData[0] = playerHighlightingData;
                    }
                }
                else
                {
                    for (int m = 0; m < m_MarkerStateData.Length; m++)
                    {
                        var markerState = m_MarkerStateData.MarkerStates[m];

                        if (markerState.NumberOfTargets > 0)
                        {
                            markerState.NumberOfTargets = 0;
                        }

                        m_MarkerStateData.MarkerStates[m] = markerState;
                    }

                    for (int u = 0; u < m_ActiveUnitData.Length; u++)
                    {

                        var lineRenderer = m_ActiveUnitData.LineRenderers[u];
                        lineRenderer.lineRenderer.positionCount = 0;
                    }
                }
            }
        }
    }

    public void FillUnitTargetsList(HighlightingDataComponent inHinghlightningData)
    {
        var playerState = m_PlayerStateData.PlayerState[0];

        if (inHinghlightningData.AoERadius > 0)
        {
            List<Vector3f> area = new List<Vector3f>();
            area = m_CellGrid.CircleDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.AoERadius);
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(area);

            if (cubeCoordList.CubeCoordinates.Count != 1 && inHinghlightningData.HoveredCoordinate != new Vector3f(999, 999, 999))
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;

            }
        }
        else if (inHinghlightningData.RingRadius > 0)
        {
            List<Vector3f> ring = new List<Vector3f>();
            ring = m_CellGrid.RingDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.RingRadius);
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(ring);

            //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
            if (cubeCoordList.CubeCoordinates.Count != 1 && inHinghlightningData.HoveredCoordinate != new Vector3f(999, 999, 999))
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;

            }
        }
        else if (inHinghlightningData.LineAoE == 1)
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
            cubeCoordList.CubeCoordinates = m_CellGrid.LineDraw(m_PlayerStateData.PlayerState[0].SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate);
            //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
            if (cubeCoordList.CubeCoordinates.Count != 1 && inHinghlightningData.HoveredCoordinate != new Vector3f(999, 999, 999))
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
        }
        else
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
            cubeCoordList.CubeCoordinates.Add(inHinghlightningData.HoveredCoordinate);

            HashSet<long> unitIds = new HashSet<long>(playerState.UnitTargets.Keys);
            //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
            if (inHinghlightningData.HoveredCoordinate != new Vector3f(999, 999, 999))
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;

            }
            else if (unitIds.Contains(playerState.SelectedUnitId) && inHinghlightningData.TargetRestrictionIndex != 2)
            {
                playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Clear();
            }
        }

        /*
        //loop over units to check if any effect restriction applies and remove unit if true
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var unitCoord = m_UnitData.Coords[i].CubeCoordinate;
            var unitFaction = m_UnitData.Factions[i].Faction;
            var unitId = m_UnitData.EntityIds[i].EntityId.Id;

            if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
            {
                if (playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Contains(unitCoord))
                {
                    //if target is not valid, remove it from UnitTargets Dict
                    if (!m_CellGrid.ValidateUnitTarget(unitId, playerState.SelectedUnitId, unitFaction, (UnitRequisitesEnum)inHinghlightningData.EffectRestrictionIndex))
                    {
                        playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Remove(unitCoord);
                    }
                }
            }
        }
        */
    }

    public void GatherHighlightingInformation(long unitID, int actionID)
    {
        UpdateInjectedComponentGroups();
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];
        ECSActionTarget target = null;
        ECSActionEffect effect = null;

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var iD = m_ActiveUnitData.EntityIds[i].EntityId.Id;
            var actions = m_ActiveUnitData.BaseDataSets[i];

            if (iD == unitID)
            {
                if (target == null)
                {
                    if (actionID < 0)
                    {
                        switch (actionID)
                        {
                            case -3:
                                break;
                            case -2:
                                target = actions.BasicMove.Targets[0];
                                effect = actions.BasicMove.Effects[0];
                                break;
                            case -1:
                                target = actions.BasicAttack.Targets[0];
                                effect = actions.BasicAttack.Effects[0];
                                break;
                        }
                    }
                    else
                    {
                        if (actionID < actions.Actions.Count)
                        {
                            target = actions.Actions[actionID].Targets[0];
                            effect = actions.Actions[actionID].Effects[0];
                        }
                        else
                        {
                            target = actions.SpawnActions[actionID - actions.Actions.Count].Targets[0];
                            effect = actions.SpawnActions[actionID - actions.Actions.Count].Effects[0];
                        }
                    }
                }

                playerHighlightingData.Range = (uint)target.targettingRange;

                switch (target.HighlighterToUse)
                {
                    case ECSActionTarget.HighlightDef.Radius:
                        playerHighlightingData.PathingRange = 0;
                        break;
                    case ECSActionTarget.HighlightDef.Path:
                        playerHighlightingData.PathingRange = 1;
                        break;
                }

                if (target is ECSATarget_Unit)
                {
                    ECSATarget_Unit unitTarget = target as ECSATarget_Unit;
                    playerHighlightingData.IsUnitTarget = 1;
                    playerHighlightingData.TargetRestrictionIndex = (uint)unitTarget.Restrictions;
                }
                else if (target is ECSATarget_Tile)
                {
                    playerHighlightingData.IsUnitTarget = 0;
                    playerHighlightingData.TargetRestrictionIndex = 999;
                }

                playerHighlightingData.EffectRestrictionIndex = (uint)effect.ApplyToRestrictions;

                HandleMods(target.SecondaryTargets, ref playerHighlightingData);

                m_PlayerStateData.HighlightingData[0] = playerHighlightingData;
            }
        }
    }

    /*
    public void UpdateHoveredCoordinates()
    {
        var playerState = m_PlayerStateData.PlayerState[0];
        var playerWorldIndex = m_PlayerStateData.WorldIndex[0].Value;
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];
        var playerFaction = m_PlayerStateData.Faction[0].Faction;

        HashSet<Vector3f> coordsInRangeHash = new HashSet<Vector3f>();

        foreach (CellAttribute c in playerState.CellsInRange)
        {
            coordsInRangeHash.Add(c.CubeCoordinate);
        }

        for (int m = 0; m < m_MarkerStateData.Length; m++)
        {
            var worldIndex = m_MarkerStateData.WorldIndex[m].Value;
            var coord = m_MarkerStateData.Coords[m].CubeCoordinate;
            var markerState = m_MarkerStateData.MarkerStates[m];
            var mouseState = m_MarkerStateData.MouseStates[m].CurrentState;
            var entityID = m_MarkerStateData.EntityIDs[m].EntityId.Id;
            var position = m_MarkerStateData.Positions[m].Coords;

            if (coordsInRangeHash.Contains(coord))
            {
                if (playerHighlightingData.IsUnitTarget == 1)
                {
                    if ((mouseState == MouseState.State.Hovered || mouseState == MouseState.State.Clicked) && coord != playerHighlightingData.HoveredCoordinate)
                    {
                        if (markerState.IsUnit == 1)
                        {
                            if (m_CellGrid.ValidateUnitTarget(entityID, playerState.SelectedUnitId, playerFaction, (UnitRequisitesEnum)playerHighlightingData.TargetRestrictionIndex))
                            {
                                playerHighlightingData.HoveredCoordinate = coord;
                                playerHighlightingData.HoveredPosition = position.ToUnityVector();
                            }
                        }
                        else if (playerHighlightingData.HoveredPosition != Vector3.zero)
                        {
                            playerHighlightingData.HoveredCoordinate = new Vector3f(999, 999, 999);
                            playerHighlightingData.HoveredPosition = new Vector3(0, 0, 0);
                        }
                    }
                }
                else
                {
                    if (markerState.IsUnit == 0)
                    {
                        if ((mouseState == MouseState.State.Hovered || mouseState == MouseState.State.Clicked) && coord != playerHighlightingData.HoveredCoordinate)
                        {
                            playerHighlightingData.HoveredCoordinate = coord;
                            playerHighlightingData.HoveredPosition = position.ToUnityVector();
                        }
                    }
                }
            }
        }

        if (playerHighlightingData.HoveredCoordinate != playerHighlightingData.LastHoveredCoordinate)
        {
            FillUnitTargetsList(playerHighlightingData);
            playerHighlightingData.LastHoveredCoordinate = playerHighlightingData.HoveredCoordinate;
        }

        m_PlayerStateData.HighlightingData[0] = playerHighlightingData;
        Debug.Log("update HoveredCoordinate");
    }
    */

    public void SetNumberOfTargets()
    {
        var playerState = m_PlayerStateData.PlayerState[0];

        for (int m = 0; m < m_MarkerStateData.Length; m++)
        {
            var markerState = m_MarkerStateData.MarkerStates[m];
            var coord = m_MarkerStateData.Coords[m];

            uint nOfTargets = 0;

            foreach (long l in playerState.UnitTargets.Keys)
            {
                HashSet<Vector3f> targetCoordsHash = new HashSet<Vector3f>(playerState.UnitTargets[l].CubeCoordinates);
                if (targetCoordsHash.Contains(coord.CubeCoordinate))
                {
                    nOfTargets += 1;
                }

                //start for setting different target markers depending on effectType - need to solve multiple targets problem
                /*
                for(int u = 0; u < m_ActiveUnitData.Length; u++)
                {
                    var unitId = m_ActiveUnitData.EntityIds[u].EntityId.Id;
                    var selectedAction = m_ActiveUnitData.ActionsData[u].CurrentSelected;

                    if(l == unitId && selectedAction.Index != -3)
                    {
                        markerState.CurrentTargetType = (MarkerState.TargetType)(int)selectedAction.Effects[0].EffectType;
                    }
                }
                */
            }

            markerState.NumberOfTargets = nOfTargets;
            m_MarkerStateData.MarkerStates[m] = markerState;
        }
    }

    public void UpdateSelectedUnit()
    {
        UpdateInjectedComponentGroups();

        var playerState = m_PlayerStateData.PlayerState[0];
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var iD = m_ActiveUnitData.EntityIds[i].EntityId.Id;
            var actions = m_ActiveUnitData.BaseDataSets[i];
            var occCoord = m_ActiveUnitData.Coords[i].CubeCoordinate;
            var worldIndex = m_ActiveUnitData.WorldIndexData[i].Value;
            var mouseState = m_ActiveUnitData.MouseStates[i];
            var lineRendererComp = m_ActiveUnitData.LineRenderers[i];
            var faction = m_ActiveUnitData.Factions[i];

            if (iD == playerState.SelectedUnitId)
            {
                if (mouseState.CurrentState == MouseState.State.Clicked)
                {
                    mouseState.CurrentState = MouseState.State.Neutral;
                    m_ActiveUnitData.MouseStates[i] = mouseState;
                }

                if (playerState.CellsInRange.Count == 0)
                {
                    if (playerHighlightingData.PathingRange == 1)
                    {
                        uint range = playerHighlightingData.Range;
                        if (playerHighlightingData.PathLine == 1)
                        {
                            if (m_ResourceSystem.CheckPlayerEnergy(faction.Faction, 0) < playerHighlightingData.Range)
                            {
                                range = (uint)m_ResourceSystem.CheckPlayerEnergy(faction.Faction, 0);
                            }
                        }

                        List<CellAttributes> go = m_CellGrid.GetRadius(occCoord, range, worldIndex);
                        playerState.CachedPaths = m_CellGrid.GetAllPathsInRadius(range, go, occCoord);

                        foreach (CellAttribute key in playerState.CachedPaths.Keys)
                        {
                            playerState.CellsInRange.Add(key);
                        }

                        playerState.CellsInRange = playerState.CellsInRange;
                        m_PlayerStateData.PlayerState[0] = playerState;

                        HighlightReachable();
                    }
                    else
                    {

                        List<CellAttributes> go = m_CellGrid.GetRadius(occCoord, playerHighlightingData.Range, worldIndex);
                        foreach (CellAttributes c in go)
                        {
                            playerState.CellsInRange.Add(c.Cell);
                        }
                        m_PlayerStateData.PlayerState[0] = playerState;
                        HighlightReachable();
                    }
                }

                if (playerHighlightingData.IsUnitTarget == 1)
                {
                    //if target wants a Unit, loop over units to find which one is hovered
                    for (int u = 0; u < m_UnitData.Length; u++)
                    {
                        var unitCoord = m_UnitData.Coords[u].CubeCoordinate;
                        var hoveredLineRendererComp = m_UnitData.LineRenderers[u];

                        if (unitCoord == playerHighlightingData.HoveredCoordinate)
                        {
                            playerHighlightingData.LineYOffset = hoveredLineRendererComp.arcOffset.y;
                        }
                    }
                }
                else
                    playerHighlightingData.LineYOffset = 0;

                if (playerHighlightingData.PathLine == 1)
                {
                    CellAttributeList Path = new CellAttributeList();
                    Path = m_CellGrid.FindPath(playerHighlightingData.HoveredCoordinate, m_PlayerStateData.PlayerState[0].CachedPaths);

                    if (Path.CellAttributes.Count > 0)
                    {
                        UpdatePathLineRenderer(Path, lineRendererComp);
                    }
                }
                else if (playerHighlightingData.LineAoE == 0)
                {
                    //use Arc Line
                    if (playerHighlightingData.HoveredPosition != Vector3.zero)
                    {
                        UpdateArcLineRenderer(playerHighlightingData.LineYOffset, playerHighlightingData.HoveredPosition, lineRendererComp);
                    }
                    else// if (mouseState.CurrentState != MouseState.State.Neutral)
                        lineRendererComp.lineRenderer.positionCount = 0;
                }
                else
                {
                    lineRendererComp.lineRenderer.positionCount = 0;
                }
            }
        }
    }

    void HandleMods(List<ECSActionSecondaryTargets> inMods, ref HighlightingDataComponent highLightingData)
    {
        highLightingData.LineAoE = 0;
        highLightingData.PathLine = 0;
        highLightingData.AoERadius = 0;
        highLightingData.RingRadius = 0;

        foreach (ECSActionSecondaryTargets t in inMods)
        {
            if (t is SecondaryPath)
            {
                highLightingData.PathLine = 1;
            }
            else if (t is SecondaryArea)
            {
                SecondaryArea secAoE = t as SecondaryArea;
                highLightingData.AoERadius = (uint)secAoE.areaSize;
            }
            else if (t is SecondaryLine)
            {
                highLightingData.LineAoE = 1;
            }
            else if (t is SecondaryRing)
            {
                SecondaryRing secRing = t as SecondaryRing;
                highLightingData.RingRadius = secRing.radius;
            }
        }
    }

    void UpdatePathLineRenderer(CellAttributeList inPath, LineRendererComponent inLineRendererComp)
    {
        Color pathColorFaded = new Color(inLineRendererComp.pathColor.r, inLineRendererComp.pathColor.g, inLineRendererComp.pathColor.b, 0.1f);
        inLineRendererComp.lineRenderer.startColor = inLineRendererComp.pathColor;
        inLineRendererComp.lineRenderer.endColor = pathColorFaded;

        inLineRendererComp.lineRenderer.positionCount = inPath.CellAttributes.Count + 1;
        inLineRendererComp.lineRenderer.SetPosition(0, inLineRendererComp.transform.position + inLineRendererComp.pathOffset);

        for (int pi = 1; pi <= inPath.CellAttributes.Count; pi++)
        {
            inLineRendererComp.lineRenderer.SetPosition(pi, inPath.CellAttributes[pi - 1].Position.ToUnityVector() + inLineRendererComp.pathOffset);
        }
    }

    void UpdateArcLineRenderer(float targetYOffset, Vector3 inTarget, LineRendererComponent inLineRendererComp)
    {
        Color arcColorFaded = new Color(inLineRendererComp.arcColor.r, inLineRendererComp.arcColor.g, inLineRendererComp.arcColor.b, 0.1f);
        inLineRendererComp.lineRenderer.startColor = arcColorFaded;
        inLineRendererComp.lineRenderer.endColor = inLineRendererComp.arcColor;

        Vector3 arcOrigin = inLineRendererComp.transform.position + inLineRendererComp.arcOffset;
        Vector3 arcTarget = new Vector3(inTarget.x, inTarget.y + targetYOffset, inTarget.z);

        Vector3[] arcPath = CalculateSinusPath(arcOrigin, arcTarget, 1);
        inLineRendererComp.lineRenderer.positionCount = arcPath.Length;

        inLineRendererComp.lineRenderer.SetPositions(arcPath);
    }

    public Vector3[] CalculateSinusPath(Vector3 origin, Vector3 target, float zenitHeight)
    {
        Vector3 distance = target - origin;
        int numberOfPositions = 8 + (2 * Mathf.RoundToInt(distance.magnitude) / 2);

        Vector3[] sinusPath = new Vector3[numberOfPositions + 1];
        float heightDifference = origin.y - target.y;
        float[] ypositions = CalculateSinusPoints(numberOfPositions);
        float xstep = 1.0f / numberOfPositions;

        for (int i = 0; i < ypositions.Length; i++)
        {
            float sinYpos = ypositions[i] * zenitHeight - heightDifference * (xstep * i);
            sinusPath[i] = origin + new Vector3(distance.x * (xstep * i), sinYpos, distance.z * (xstep * i));
        }

        sinusPath[numberOfPositions] = target;

        return sinusPath;
    }

    public float[] CalculateSinusPoints(int numberOfPositions)
    {
        float[] yPosArray = new float[numberOfPositions];
        float xStep = 1.0f / numberOfPositions;
        int i = 0;

        for (float x = 0.0f; x <= 1.0f; x += xStep)
        {
            if (i < yPosArray.Length)
            {
                yPosArray[i] = (float)Math.Sin(x * Math.PI);
                i++;
            }
        }

        return yPosArray;
    }

    public void ResetHighlights()
    {
        if (m_PlayerStateData.Length == 0)
            return;

        UpdateInjectedComponentGroups();
        var playerState = m_PlayerStateData.PlayerState[0];
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];

        var coordToReset = new Vector3f(999, 999, 999);
        HashSet<long> unitIdHash = new HashSet<long>(playerState.UnitTargets.Keys);

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var actions = m_ActiveUnitData.ActionsData[i];
            var unitId = m_ActiveUnitData.EntityIds[i].EntityId.Id;
            var lineRenderer = m_ActiveUnitData.LineRenderers[i].lineRenderer;

            if (actions.LockedAction.Index == -3 && actions.CurrentSelected.Index == -3)
            {
                if (unitIdHash.Contains(unitId) && playerHighlightingData.TargetRestrictionIndex != 2)
                {
                    lineRenderer.positionCount = 0;
                    if(playerState.UnitTargets[unitId].CubeCoordinates.Count != 0)
                        coordToReset = playerState.UnitTargets[unitId].CubeCoordinates[0];
                    playerState.UnitTargets.Remove(unitId);
                    playerState.UnitTargets = playerState.UnitTargets;
                    m_PlayerStateData.PlayerState[0] = playerState;
                }
            }
        }

        for (int i = 0; i < m_MarkerStateData.Length; i++)
        {
            var markerState = m_MarkerStateData.MarkerStates[i];
            var mouseState = m_MarkerStateData.MouseStates[i];
            var coords = m_MarkerStateData.Coords[i].CubeCoordinate;

            if (coords == coordToReset)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                m_MarkerStateData.MouseStates[i] = mouseState;
                markerState.NumberOfTargets = 0;
                m_MarkerStateData.MarkerStates[i] = markerState;
            }
            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                m_MarkerStateData.MouseStates[i] = mouseState;
            }
            if (markerState.CurrentState != (MarkerState.State)(int)mouseState.CurrentState)
            {
                markerState.CurrentState = (MarkerState.State)(int)mouseState.CurrentState;
                markerState.IsSet = 0;
                m_MarkerStateData.MarkerStates[i] = markerState;
            }
        }
    }

    public void HighlightReachable()
    {
        var playerState = m_PlayerStateData.PlayerState[0];

        //remove selected unit target from player UnitTargets Dict 
        if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
        {
            playerState.UnitTargets.Remove(playerState.SelectedUnitId);
            playerState.UnitTargets = playerState.UnitTargets;
            m_PlayerStateData.PlayerState[0] = playerState;
        }

        for (int i = 0; i < m_MarkerStateData.Length; i++)
        {
            var marker = m_MarkerStateData.MarkerStates[i];
            var coord = m_MarkerStateData.Coords[i].CubeCoordinate;

            if (marker.CurrentState == MarkerState.State.Hovered || marker.CurrentState == MarkerState.State.Clicked)
            {
                marker.CurrentState = MarkerState.State.Neutral;
                marker.IsSet = 0;
            }

            foreach (CellAttribute c in playerState.CellsInRange)
            {
                if (coord == c.CubeCoordinate)
                {
                    marker.CurrentState = MarkerState.State.Reachable;
                    marker.IsSet = 0;
                }
            }

            m_MarkerStateData.MarkerStates[i] = marker;
        }
    }

    public void ClearPlayerState()
    {
        UpdateInjectedComponentGroups();
        var playerstate = m_PlayerStateData.PlayerState[0];
        var playerHighlighting = m_PlayerStateData.HighlightingData[0];

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var iD = m_ActiveUnitData.EntityIds[i].EntityId.Id;
            var lineRendererComp = m_ActiveUnitData.LineRenderers[i];

            if (iD == playerstate.SelectedUnitId)
            {
                lineRendererComp.lineRenderer.positionCount = 0;
            }
        }
        playerstate.CellsInRange.Clear();
        playerstate.CachedPaths.Clear();
        m_PlayerStateData.PlayerState[0] = playerstate;
    }

    public void SetSelfTarget(long entityID)
    {
        UpdateInjectedComponentGroups();
        var playerState = m_PlayerStateData.PlayerState[0];
        var playerHighlighting = m_PlayerStateData.HighlightingData[0];
        playerState.UnitTargets.Remove(entityID);

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var iD = m_ActiveUnitData.EntityIds[i].EntityId.Id;
            var lineRendererComp = m_ActiveUnitData.LineRenderers[i];
            var coord = m_ActiveUnitData.Coords[i].CubeCoordinate;

            if (iD == entityID)
            {
                lineRendererComp.lineRenderer.positionCount = 0;
                CubeCoordinateList list = new CubeCoordinateList(new List<Vector3f>{coord});
                playerState.UnitTargets.Add(iD, list);
            }
        }

        playerState.CellsInRange.Clear();
        playerState.CachedPaths.Clear();
        playerState.UnitTargets = playerState.UnitTargets;
        m_PlayerStateData.PlayerState[0] = playerState;

        SetNumberOfTargets();
    }

}
using UnityEngine;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;
using Generic;
using Cell;
using Unit;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class HandleCellGridRequestsSystem : ComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    public struct SelectActionRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<Actions.CommandRequests.SelectActionCommand> ReceivedSelectActionRequests;
        public ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<FactionComponent.Component> Faction;
    }

    [Inject] SelectActionRequestData m_SelectActionRequestData;

    public struct SetTargetRequestData
    {
        public readonly int Length;
        public ComponentDataArray<Actions.CommandRequests.SetTargetCommand> ReceivedSetTargetRequests;
        public ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public readonly ComponentDataArray<FactionComponent.Component> Faction;
    }

    [Inject] SetTargetRequestData m_SetTargetRequestData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Health.Component> HealthData;
        public readonly ComponentDataArray<FactionComponent.Component> FactionData;
    }

    [Inject] UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] GameStateData m_GameStateData;

    [Inject] ResourceSystem m_ResourceSystem;

    [Inject] TimerSystem m_TimerSystem;

    protected override void OnUpdate()
    {
        #region select action

        for (int i = 0; i < m_SelectActionRequestData.Length; i++)
        {
            var actionData = m_SelectActionRequestData.ActionsData[i];
            var selectActionRequest = m_SelectActionRequestData.ReceivedSelectActionRequests[i];
            var cellsToMarkData = m_SelectActionRequestData.CellsToMarkData[i];
            var coord = m_SelectActionRequestData.CoordinateData[i].CubeCoordinate;
            var worldIndex = m_SelectActionRequestData.WorldIndexData[i].Value;
            var faction = m_SelectActionRequestData.Faction[i];
            var unitId = m_SelectActionRequestData.EntityIds[i].EntityId.Id;

            cellsToMarkData.SetClientRange = false;
            m_SelectActionRequestData.CellsToMarkData[i] = cellsToMarkData;

            m_ResourceSystem.AddEnergy(faction.Faction, actionData.LockedAction.CombinedCost);

            if(actionData.LockedAction.Effects.Count != 0)
            {
                if (actionData.LockedAction.Effects[0].EffectType == EffectTypeEnum.gain_armor)
                {
                    m_ResourceSystem.RemoveArmor(actionData.LockedAction.Targets[0].TargetId, actionData.LockedAction.Effects[0].GainArmorNested.ArmorAmount);
                }
            }

            actionData.LockedAction = actionData.NullAction;

            foreach (var sar in selectActionRequest.Requests)
            {
                int index = sar.Payload.ActionId;
                Action actionToSelect = actionData.NullAction;

                if(m_ResourceSystem.CheckPlayerEnergy(faction.Faction) > 0)
                {
                    if (index >= 0)
                    {
                        var a = actionData.OtherActions[index];
                        a.CombinedCost = CalculateCombinedCost(actionData.OtherActions[index].Targets[0]);
                        actionData.OtherActions[index] = a;

                        if (m_ResourceSystem.CheckPlayerEnergy(faction.Faction, actionData.OtherActions[index].CombinedCost) >= 0)
                        {
                            actionToSelect = actionData.OtherActions[index];
                        }
                    }
                    else
                    {
                        if (index == -2)
                        {
                            var a = actionData.BasicMove;
                            a.CombinedCost = CalculateCombinedCost(actionData.BasicMove.Targets[0]);
                            actionData.BasicMove = a;

                            if (m_ResourceSystem.CheckPlayerEnergy(faction.Faction, actionData.BasicMove.CombinedCost) >= 0)
                            {
                                actionToSelect = actionData.BasicMove;
                            }
                        }
                        else if (index == -1)
                        {
                            var a = actionData.BasicAttack;
                            a.CombinedCost = CalculateCombinedCost(actionData.BasicAttack.Targets[0]);
                            actionData.BasicAttack = a;

                            if (m_ResourceSystem.CheckPlayerEnergy(faction.Faction, actionData.BasicAttack.CombinedCost) >= 0)
                            {
                                actionToSelect = actionData.BasicAttack;
                            }
                        }
                    }
                }

                actionData.CurrentSelected = actionToSelect;
            }

            if (actionData.CurrentSelected.Targets.Count != 0)
            {
                if(actionData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                {
                    if(actionData.CurrentSelected.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                    {
                        //Set target instantly
                        actionData.LockedAction = SetLockedAction(actionData.CurrentSelected, coord, coord, unitId, faction.Faction);
                        actionData.CurrentSelected = actionData.NullAction;
                    }
                }
                else if (!actionData.CurrentSelected.Equals(actionData.LastSelected) || cellsToMarkData.CellsInRange.Count == 0)
                {
                    cellsToMarkData.CellsInRange = GetRadius(coord, (uint)actionData.CurrentSelected.Targets[0].Targettingrange, worldIndex);

                    switch (actionData.CurrentSelected.Targets[0].Higlighter)
                    {
                        case UseHighlighterEnum.pathing:
                            uint range = (uint)actionData.CurrentSelected.Targets[0].Targettingrange;
                            if(actionData.CurrentSelected.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                            {
                                if (m_ResourceSystem.CheckPlayerEnergy(faction.Faction, 0) < (uint)actionData.CurrentSelected.Targets[0].Targettingrange)
                                {
                                    range = (uint)m_ResourceSystem.CheckPlayerEnergy(faction.Faction, 0);
                                }
                            }
                            cellsToMarkData.CachedPaths = GetAllPathsInRadius(range, cellsToMarkData.CellsInRange, cellsToMarkData.CellsInRange[0].Cell);
                            break;
                        case UseHighlighterEnum.no_pathing:
                            break;
                    }
                }
            }

            actionData.LastSelected = actionData.CurrentSelected;
            m_SelectActionRequestData.ActionsData[i] = actionData;

            cellsToMarkData.SetClientRange = true;
            m_SelectActionRequestData.CellsToMarkData[i] = cellsToMarkData;
        }

        #endregion

        #region set target

        for (int i = 0; i < m_SetTargetRequestData.Length; i++)
        {
            var usingId = m_SetTargetRequestData.EntityIds[i].EntityId.Id;
            var actionData = m_SetTargetRequestData.ActionsData[i];
            var setTargetRequest = m_SetTargetRequestData.ReceivedSetTargetRequests[i];
            var unitWorldIndex = m_SetTargetRequestData.WorldIndexData[i].Value;
            var cellsToMark = m_SetTargetRequestData.CellsToMarkData[i];
            var faction = m_SetTargetRequestData.Faction[i];
            var originCoord = m_SetTargetRequestData.CoordinateData[i].CubeCoordinate;

            foreach (var str in setTargetRequest.Requests)
            {
                long id = str.Payload.TargetId;

                if (actionData.CurrentSelected.Index != -3 && actionData.LockedAction.Index == -3)
                {
                    switch (actionData.CurrentSelected.Targets[0].TargetType)
                    {
                        case TargetTypeEnum.cell:
                            for (int ci = 0; ci < m_CellData.Length; ci++)
                            {
                                var cellId = m_CellData.EntityIds[ci].EntityId.Id;
                                var cellAtts = m_CellData.CellAttributes[ci].CellAttributes;
                                var cell = m_CellData.CellAttributes[ci].CellAttributes.Cell;

                                if (cellId == id)
                                {
                                    bool isValidTarget = false;
                                    if(cellsToMark.CachedPaths.ContainsKey(cell) && cellsToMark.CachedPaths.Count != 0)
                                    {
                                        isValidTarget = true;
                                    }
                                    else if(cellsToMark.CellsInRange.Contains(cellAtts))
                                    {
                                        isValidTarget = true;
                                    }

                                    if(isValidTarget)
                                    {
                                        actionData.LockedAction = actionData.CurrentSelected;
                                        var locked = actionData.LockedAction;
                                        var t = actionData.LockedAction.Targets[0];
                                        t.TargetCoordinate = cell.CubeCoordinate;
                                        t.TargetId = id;
                                        actionData.LockedAction.Targets[0] = t;

                                        for (int mi = 0; mi < actionData.LockedAction.Targets[0].Mods.Count; mi++)
                                        {
                                            var modType = actionData.LockedAction.Targets[0].Mods[mi].ModType;
                                            var mod = actionData.LockedAction.Targets[0].Mods[0];
                                            switch (modType)
                                            {
                                                case ModTypeEnum.aoe:
                                                    mod.Coordinates.AddRange(CircleDraw(t.TargetCoordinate, (uint)mod.AoeNested.Radius));
                                                    break;
                                                case ModTypeEnum.path:
                                                    foreach (CellAttribute c in FindPath(cell, cellsToMark.CachedPaths).CellAttributes)
                                                    {
                                                        mod.Coordinates.Add(c.CubeCoordinate);
                                                    }
                                                    actionData.LockedAction.Targets[0].Mods[0] = mod;
                                                    locked.CombinedCost = CalculateCombinedCost(t);
                                                    break;
                                                case ModTypeEnum.line:
                                                    mod.Coordinates.AddRange(LineDraw(originCoord, t.TargetCoordinate));
                                                    break;
                                                case ModTypeEnum.ring:
                                                    mod.Coordinates.AddRange(RingDraw(t.TargetCoordinate, mod.RingNested.Radius));
                                                    break;
                                            }
                                        }
                                        actionData.LockedAction = locked;
                                        m_ResourceSystem.SubstactEnergy(faction.Faction, locked.CombinedCost);
                                    }
                                    else
                                    {
                                        actionData.LockedAction = actionData.NullAction;
                                    }
                                }
                            }
                            break;
                        case TargetTypeEnum.unit:
                            for (int ci = 0; ci < m_UnitData.Length; ci++)
                            {
                                var unitId = m_UnitData.EntityIds[ci].EntityId.Id;
                                var unitCoord = m_UnitData.CoordinateData[ci].CubeCoordinate;
                                
                                if (unitId == id)
                                {
                                    bool isValidTarget = false;
                                        foreach (CellAttributes c in cellsToMark.CellsInRange)
                                        {
                                            if (c.Cell.CubeCoordinate == unitCoord)
                                            {
                                                isValidTarget = ValidateUnitTarget(unitId, usingId, faction.Faction, actionData.CurrentSelected.Targets[0].UnitTargetNested.UnitReq);
                                            }
                                        }

                                    if (isValidTarget)
                                    {
                                        actionData.LockedAction = SetLockedAction(actionData.CurrentSelected, originCoord, unitCoord, unitId, faction.Faction);
                                    }
                                    else
                                    {
                                        actionData.LockedAction = actionData.NullAction;
                                    }
                                }
                            }
                            break;
                    }
                    actionData.CurrentSelected = actionData.NullAction;
                }
            }
            m_SetTargetRequestData.ActionsData[i] = actionData;
        }

        #endregion
    }

    public uint CalculateCombinedCost(ActionTarget inActionTarget)
    {
        uint combinedCost = 0;

        combinedCost += inActionTarget.EnergyCost;

        if (inActionTarget.Mods.Count != 0)
        {
            if(inActionTarget.Mods[0].ModType == ModTypeEnum.path)
            {
                combinedCost += (uint)inActionTarget.Mods[0].Coordinates.Count;
            }
        }

        return combinedCost;
    }

    public Action SetLockedAction(Action selectedAction, Vector3f originCoord, Vector3f unitCoord, long unitId, uint faction)
    {
        Action locked = selectedAction;
        var t = locked.Targets[0];
        t.TargetCoordinate = unitCoord;
        t.TargetId = unitId;
        locked.Targets[0] = t;

        for (int mi = 0; mi < locked.Targets[0].Mods.Count; mi++)
        {
            var modType = locked.Targets[0].Mods[mi].ModType;
            var mod = locked.Targets[0].Mods[0];
            switch (modType)
            {
                case ModTypeEnum.aoe:
                    mod.Coordinates.AddRange(CircleDraw(t.TargetCoordinate, (uint)mod.AoeNested.Radius));
                    break;
                case ModTypeEnum.path:

                    break;
                case ModTypeEnum.line:
                    mod.Coordinates.AddRange(LineDraw(originCoord, t.TargetCoordinate));
                    break;
                case ModTypeEnum.ring:
                    mod.Coordinates.AddRange(RingDraw(t.TargetCoordinate, mod.RingNested.Radius));
                    break;
            }
        }

        m_ResourceSystem.AddArmor(locked.Targets[0].TargetId, locked.Effects[0].GainArmorNested.ArmorAmount);
        m_ResourceSystem.SubstactEnergy(faction, locked.CombinedCost);

        return locked;
    }

    Vector3f[] DirectionsArray = new Vector3f[]{
          new Vector3f(+1, -1, 0), new Vector3f(+1, 0, -1), new Vector3f(0, +1, -1),
            new Vector3f(-1, +1, 0), new Vector3f(-1, 0, +1), new Vector3f(0, -1, +1)
    };

    Vector2 CubeToAxial(Vector3f cube)
    {
        return new Vector2(cube.X, cube.Y);
    }

    Vector3f AxialToCube(Vector2 axial)
    {
        return new Vector3f(axial.x, axial.y, -axial.x -axial.y);
    }

    //size equals width of a hexagon / 2
    public Vector2 CubeCoordToXZ(Vector3f coord)
    {
        Vector2 axial = CubeToAxial(coord);
        var x = 1.5f * (3 / 2 * axial.x);
        var y = 1.73f * ((axial.x * 0.5f) + axial.y);

        //center cell + coordinate offset = XZ coordinate in world space - offset X by (worldindex - 1) * 100?
        return new Vector2(50, 55.22f) + new Vector2(x, y);
    }


    Vector3f CubeDirection(uint direction)
    {
        if (direction < 6)
            return DirectionsArray[direction];
        else
            return new Vector3f();
    }
    
    Vector3f CubeNeighbour(Vector3f origin, uint direction)
    {
        return origin + CubeDirection(direction);
    }

    Vector3f CubeScale(Vector3f direction, uint scale)
    {
        return direction * scale;
    }

    public Vector3f CoordinateDirection(Vector3f origin, Vector3f destination)
    {
        var direction = destination - origin;
        return direction;
    }

    public List<Vector3f> RingDraw(Vector3f origin, uint radius)
    {
        var ring = new List<Vector3f>();
        var coord = origin + CubeScale(DirectionsArray[4], radius);

        for(int i = 0; i < 6; i++)
        {
            for(int j = 0; j < radius; j++)
            {
                ring.Add(coord);
                coord = CubeNeighbour(coord, (uint)i);
            }
        }
        return ring;
    }

    public int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }

    public float GetAngle(Vector3f originPos, Vector3f targetPos)
    {
        Vector3f dir = targetPos - originPos;
        float Angle = Mathf.Atan2(dir.X, dir.Z) * Mathf.Rad2Deg;
        return Angle;
    }

    public float LineLerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public Vector3f CubeLerp(Vector3f a, Vector3f b, float t)
    {
        return CubeRound(new Vector3f(LineLerp(a.X, b.X, t), LineLerp(a.Y, b.Y, t), LineLerp(a.Z, b.Z, t)));
    }

    public List<Vector3f> LineDraw(Vector3f origin, Vector3f destination)
    {
        List<Vector3f> line = new List<Vector3f>();
        var n = GetDistance(origin, destination);
        //nudge destination
        destination += new Vector3f(1e-6f, 2e-6f, -3e-6f);

        for(int i = 0; i <= n; i++)
        {
            line.Add(CubeLerp(origin, destination, 1f / n * i));
        }

        return line;
    }

    public Vector3f CubeRound(Vector3f cubeFloat)
    {

        var rx = Mathf.Round(cubeFloat.X);
        var ry = Mathf.Round(cubeFloat.Y);
        var rz = Mathf.Round(cubeFloat.Z);

        var x_diff = Mathf.Abs(rx - cubeFloat.X);
        var y_diff = Mathf.Abs(ry - cubeFloat.Y);
        var z_diff = Mathf.Abs(rz - cubeFloat.Z);

        if(x_diff > y_diff && x_diff > z_diff)
        {
            rx = -ry - rz;
        }
        else if(y_diff > z_diff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new Vector3f(rx, ry, rz);
    }

    public List<Vector3f> CircleDraw(Vector3f originCellCubeCoordinate, uint radius)
    {
        var results = new List<Vector3f>();
        results.Add(originCellCubeCoordinate);

        for (int x = (int)(-radius); x <= radius; x++)
        {
            for (int y = (int)(Mathf.Max(-(float)radius, -x - (float)radius)); y <= (int)(Mathf.Min((float)radius, -x + radius)); y++)
            {
                var z = -x - y;
                results.Add(originCellCubeCoordinate + new Vector3f(x, y, z));
            }
        }
        return results;
    }

    public List<CellAttributes> GetRadius(Vector3f originCellCubeCoordinate, uint radius, uint unitWorldIndex)
    {
        //returns a list of offsetCoordinates
        var cellsInRadius = new List<CellAttributes>();
        //reserve first index for origin
        cellsInRadius.Add(new CellAttributes{Neighbours = new CellAttributeList(new List<CellAttribute>())});

        //get all cubeCordinates within range
        var coordList = CircleDraw(originCellCubeCoordinate, radius);

        HashSet<Vector3f> coordHash = new HashSet<Vector3f>();

        foreach(Vector3f v in coordList)
        {
            coordHash.Add(v);
        }

        //use a hashset instead of a list to improve contains performance

        for (int i = 0; i < m_CellData.Length; i++)
        {
            uint cellWorldIndex = m_CellData.WorldIndexData[i].Value;
            Vector3f cubeCoordinate = m_CellData.CoordinateData[i].CubeCoordinate;
            var cellAttributes = m_CellData.CellAttributes[i].CellAttributes;

            if (cellWorldIndex == unitWorldIndex)
            {
                if (cubeCoordinate == originCellCubeCoordinate)
                {
                    cellsInRadius[0] = cellAttributes;
                }
                else if (coordHash.Contains(cubeCoordinate))
                {
                    cellsInRadius.Add(cellAttributes);
                }
            }
        }
        
        return cellsInRadius;
    }

    public Dictionary<CellAttribute, CellAttributeList> GetAllPathsInRadius(uint radius, List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        var paths = CachePaths(cellsInRange, origin);
        var cachedPaths = new Dictionary<CellAttribute, CellAttributeList>();

        foreach (var key in paths.Keys)
        {
            var path = paths[key];

            int pathCost;

            if (key.IsTaken)
                continue;

            pathCost = path.CellAttributes.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                path.CellAttributes.Reverse();
                cachedPaths.Add(key, path);
            }
        }
        
        return cachedPaths;

    }

    public Dictionary<CellAttribute, CellAttributeList> GetAllPathsInRadius(uint radius, List<CellAttributes> cellsInRange, Vector3f originCoord)
    {

        CellAttribute origin = new CellAttribute();
        for(int i = 0; i < m_CellData.Length; i++)
        {
            var coordinate = m_CellData.CoordinateData[i].CubeCoordinate;
            var cellAttribute = m_CellData.CellAttributes[i].CellAttributes.Cell;
            if(coordinate == originCoord)
            {
                origin = cellAttribute;
            }
        }

        var paths = CachePaths(cellsInRange, origin);
        var cachedPaths = new Dictionary<CellAttribute, CellAttributeList>();

        foreach (var key in paths.Keys)
        {
            var path = paths[key];

            int pathCost;

            if (key.IsTaken)
                continue;

            pathCost = path.CellAttributes.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                cachedPaths.Add(key, path);
            }

            path.CellAttributes.Reverse();
        }

        return cachedPaths;

    }

    public Dictionary<CellAttribute, CellAttributeList> CachePaths(List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        var edges = GetGraphEdges(cellsInRange, origin);
        var paths = pathfinder.FindAllPaths(edges, origin);
        return paths;
    }

    /// <summary>
    /// Method returns graph representation of cell grid for pathfinding.
    /// </summary>
    public Dictionary<CellAttribute, Dictionary<CellAttribute, int>> GetGraphEdges(List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        Dictionary<CellAttribute, Dictionary<CellAttribute, int>> ret = new Dictionary<CellAttribute, Dictionary<CellAttribute, int>>();

        //instead of looping over all cells in grid only loop over cells in CellsInMovementRange

        for (int i = 0; i < cellsInRange.Count; ++i)
        {
            CellAttributes cell = cellsInRange[i];
            
            var isTaken = cellsInRange[i].Cell.IsTaken;
            var movementCost = cellsInRange[i].Cell.MovementCost;
            var neighbours = cellsInRange[i].Neighbours.CellAttributes;
            

            ret[cell.Cell] = new Dictionary<CellAttribute, int>();


            if (!isTaken || cell.Cell.CubeCoordinate == origin.CubeCoordinate)
            {
                if(neighbours != null)
                {
                    foreach (var neighbour in neighbours)
                    {
                        ret[cell.Cell][neighbour] = neighbour.MovementCost;
                    }

                }

            }
        }
        return ret;
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

    public CellAttributeList FindPath(Vector3f inDestination, Dictionary<CellAttribute, CellAttributeList> cachedPaths)
    {
        CellAttribute destination = new CellAttribute();
        for (int i = 0; i < m_CellData.Length; i++)
        {
            var coordinate = m_CellData.CoordinateData[i].CubeCoordinate;
            var cellAttribute = m_CellData.CellAttributes[i].CellAttributes.Cell;
            if (coordinate == inDestination)
            {
                destination = cellAttribute;
            }
        }
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public CellAttributes SetCellAttributes(CellAttributes cellAttributes, bool isTaken, EntityId entityId, uint worldIndex)
    {
        var cell = cellAttributes.Cell;
        cell.IsTaken = isTaken;
        cell.UnitOnCellId = entityId;

        CellAttributes cellAtt = new CellAttributes
        {
            Neighbours = cellAttributes.Neighbours,
            Cell = cell
        };

        UpdateNeighbours(cellAtt.Cell, cellAtt.Neighbours, worldIndex);

        return cellAtt;
    }

    public void UpdateNeighbours(CellAttribute cell, CellAttributeList neighbours, uint worldIndex)
    {
        for (int ci = 0; ci < m_CellData.Length; ci++)
        {
            var cellWordlIndex = m_CellData.WorldIndexData[ci].Value;
            if(worldIndex == cellWordlIndex)
            {
                var cellAtt = m_CellData.CellAttributes[ci];
                for (int n = 0; n < neighbours.CellAttributes.Count; n++)
                {
                    if (neighbours.CellAttributes[n].CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate)
                    {
                        for (int cn = 0; cn < cellAtt.CellAttributes.Neighbours.CellAttributes.Count; cn++)
                        {
                            if (cellAtt.CellAttributes.Neighbours.CellAttributes[cn].CubeCoordinate == cell.CubeCoordinate)
                            {
                                cellAtt.CellAttributes.Neighbours.CellAttributes[cn] = cell;
                                cellAtt.CellAttributes = cellAtt.CellAttributes;
                                m_CellData.CellAttributes[ci] = cellAtt;
                            }
                        }
                    }
                }
            }
        }
    }

    public bool ValidateUnitTarget(long targetUnitId, long usingUnitId, uint inFaction, UnitRequisitesEnum restrictions)
    {
        UpdateInjectedComponentGroups();
        bool valid = false;

        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var unitId = m_UnitData.EntityIds[i].EntityId.Id;
            var faction = m_UnitData.FactionData[i].Faction;

            if(targetUnitId == unitId)
            {
                switch (restrictions)
                {
                    case UnitRequisitesEnum.any:
                        valid = true;
                        break;
                    case UnitRequisitesEnum.enemy:
                        if(faction != inFaction)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.friendly:
                        if (faction == inFaction)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.friendly_other:
                        if(faction == inFaction && usingUnitId != unitId)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.other:
                        if(usingUnitId != unitId)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.self:
                        //maybe selfstate becomes irrelevant once a self-target is implemented.
                        if (usingUnitId == unitId)
                        {
                            valid = true;
                        }
                        break;
                        
                    default:
                        break;
                }
            }

        }
        return valid;
    }

}

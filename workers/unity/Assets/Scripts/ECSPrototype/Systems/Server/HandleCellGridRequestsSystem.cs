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
using Unity.Collections;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class HandleCellGridRequestsSystem : ComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    CommandSystem m_CommandSystem;
    TimerSystem m_TimerSystem;
    ResourceSystem m_ResourceSystem;

    EntityQuery m_GameStateData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    EntityQuery m_SetTargetRequestData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<GameState.Component>()
        );

        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<Actions.Component>(),
            ComponentType.ReadWrite<CellsToMark.Component>()
            );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
    }

    protected override void OnUpdate()
    {
        #region select action

        var selectActionRequests = m_CommandSystem.GetRequests<Actions.SelectActionCommand.ReceivedRequest>();

        for (int i = 0; i < selectActionRequests.Count; i++)
        {
            var selectActionRequest = selectActionRequests[i];

            Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitEntityId, ref WorldIndex.Component unitWorldIndex, ref Actions.Component unitActions, ref CubeCoordinate.Component unitCoord, ref FactionComponent.Component unitFaction, ref CellsToMark.Component unitCellsToMark) =>
            {
                //if this unit has sent a selectActionCommand
                if (unitEntityId.EntityId.Id == selectActionRequest.EntityId.Id)
                {
                    unitCellsToMark.SetClientRange = false;

                    m_ResourceSystem.AddEnergy(unitFaction.Faction, unitActions.LockedAction.CombinedCost);

                    if (unitActions.LockedAction.Effects.Count != 0)
                    {
                        if (unitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.gain_armor)
                        {
                            m_ResourceSystem.RemoveArmor(unitActions.LockedAction.Targets[0].TargetId, unitActions.LockedAction.Effects[0].GainArmorNested.ArmorAmount);
                        }
                    }

                    unitActions.LockedAction = unitActions.NullAction;

                    int index = selectActionRequest.Payload.ActionId;
                    Action actionToSelect = unitActions.NullAction;

                    if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction) > 0)
                    {
                        if (index >= 0)
                        {
                            var a = unitActions.OtherActions[index];
                            a.CombinedCost = CalculateCombinedCost(unitActions.OtherActions[index].Targets[0]);
                            unitActions.OtherActions[index] = a;

                            if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.OtherActions[index].CombinedCost) >= 0)
                            {
                                actionToSelect = unitActions.OtherActions[index];
                            }
                        }
                        else
                        {
                            if (index == -2)
                            {
                                var a = unitActions.BasicMove;
                                a.CombinedCost = CalculateCombinedCost(unitActions.BasicMove.Targets[0]);
                                unitActions.BasicMove = a;

                                if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.BasicMove.CombinedCost) >= 0)
                                {
                                    actionToSelect = unitActions.BasicMove;
                                }
                            }
                            else if (index == -1)
                            {
                                var a = unitActions.BasicAttack;
                                a.CombinedCost = CalculateCombinedCost(unitActions.BasicAttack.Targets[0]);
                                unitActions.BasicAttack = a;

                                if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.BasicAttack.CombinedCost) >= 0)
                                {
                                    actionToSelect = unitActions.BasicAttack;
                                }
                            }
                        }
                    }

                    unitActions.CurrentSelected = actionToSelect;

                    if (unitActions.CurrentSelected.Targets.Count != 0)
                    {
                        bool self = false;
                        if (unitActions.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                        {
                            if (unitActions.CurrentSelected.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                            {
                                //Set target instantly
                                self = true;
                                unitActions.LockedAction = SetLockedAction(unitActions.CurrentSelected, unitCoord.CubeCoordinate, unitCoord.CubeCoordinate, unitEntityId.EntityId.Id, unitFaction.Faction);
                                unitActions.CurrentSelected = unitActions.NullAction;
                            }
                        }
                        if ((!unitActions.CurrentSelected.Equals(unitActions.LastSelected) || unitCellsToMark.CellsInRange.Count == 0) && !self)
                        {
                            unitCellsToMark.CellsInRange = GetRadius(unitCoord.CubeCoordinate, (uint)unitActions.CurrentSelected.Targets[0].Targettingrange, unitWorldIndex.Value);
                            unitCellsToMark.CachedPaths.Clear();


                            switch (unitActions.CurrentSelected.Targets[0].Higlighter)
                            {
                                case UseHighlighterEnum.pathing:
                                    uint range = (uint)unitActions.CurrentSelected.Targets[0].Targettingrange;
                                    if (unitActions.CurrentSelected.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                                    {
                                        if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, 0) < (uint)unitActions.CurrentSelected.Targets[0].Targettingrange)
                                        {
                                            range = (uint)m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, 0);
                                        }
                                    }
                                    unitCellsToMark.CachedPaths = GetAllPathsInRadius(range, unitCellsToMark.CellsInRange, unitCellsToMark.CellsInRange[0].Cell);

                                    break;
                                case UseHighlighterEnum.no_pathing:
                                    break;
                            }
                        }
                    }

                    unitActions.LastSelected = unitActions.CurrentSelected;
                    unitCellsToMark.SetClientRange = true;
                    unitCellsToMark.CachedPaths = unitCellsToMark.CachedPaths;
                }
            });
        }

        #endregion

        #region set target
        var setTargetRequests = m_CommandSystem.GetRequests<Actions.SetTargetCommand.ReceivedRequest>();

        for (int i = 0; i < setTargetRequests.Count; i++)
        {
            var setTargetRequest = setTargetRequests[i];

            Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitEntityId, ref WorldIndex.Component unitWorldIndex, ref Actions.Component unitActions, ref CubeCoordinate.Component unitCoord, ref FactionComponent.Component unitFaction, ref CellsToMark.Component unitCellsToMark) =>
            {
                var requestingUnitId = unitEntityId;
                var requestingUnitActions = unitActions;
                var requestingUnitCoord = unitCoord;
                var requestingUnitFaction = unitFaction;
                var requestingUnitCellsToMark = unitCellsToMark;

                if (unitEntityId.EntityId.Id == setTargetRequest.EntityId.Id)
                {
                    long id = setTargetRequest.Payload.TargetId;

                    if (unitActions.CurrentSelected.Index != -3 && unitActions.LockedAction.Index == -3)
                    {
                        switch (unitActions.CurrentSelected.Targets[0].TargetType)
                        {
                            case TargetTypeEnum.cell:

                                Entities.With(m_CellData).ForEach((ref SpatialEntityId cellId, ref CellAttributesComponent.Component cellAtts) =>
                                {
                                    var cell = cellAtts.CellAttributes.Cell;

                                    if (cellId.EntityId.Id == id)
                                    {
                                        bool isValidTarget = false;
                                        if (requestingUnitCellsToMark.CachedPaths.Count != 0)
                                        {
                                            if (requestingUnitCellsToMark.CachedPaths.ContainsKey(cell))
                                            {
                                                isValidTarget = true;
                                            }
                                        }
                                        else
                                        {
                                            bool valid = false;
                                            foreach (CellAttributes ca in requestingUnitCellsToMark.CellsInRange)
                                            {
                                                if (Vector3fext.ToUnityVector(ca.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(cell.CubeCoordinate))
                                                {
                                                    if (requestingUnitActions.CurrentSelected.Targets[0].CellTargetNested.RequireEmpty)
                                                    {
                                                        if (!cell.IsTaken) valid = true;
                                                    }
                                                    else
                                                    {
                                                        valid = true;
                                                    }
                                                }

                                            }

                                            isValidTarget = valid;

                                        }

                                        if (isValidTarget)
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.CurrentSelected;
                                            var locked = requestingUnitActions.LockedAction;
                                            var t = requestingUnitActions.LockedAction.Targets[0];
                                            t.TargetCoordinate = cell.CubeCoordinate;
                                            t.TargetId = id;
                                            requestingUnitActions.LockedAction.Targets[0] = t;

                                            for (int mi = 0; mi < requestingUnitActions.LockedAction.Targets[0].Mods.Count; mi++)
                                            {
                                                var modType = requestingUnitActions.LockedAction.Targets[0].Mods[mi].ModType;
                                                var mod = requestingUnitActions.LockedAction.Targets[0].Mods[0];
                                                switch (modType)
                                                {
                                                    case ModTypeEnum.aoe:
                                                        foreach (Vector3f v in CircleDraw(t.TargetCoordinate, (uint)mod.AoeNested.Radius))
                                                        {
                                                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                                                        }
                                                        break;
                                                    case ModTypeEnum.path:
                                                        foreach (CellAttribute c in FindPath(cell, requestingUnitCellsToMark.CachedPaths).CellAttributes)
                                                        {
                                                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(c.CubeCoordinate, c.Position));
                                                        }
                                                        requestingUnitActions.LockedAction.Targets[0].Mods[0] = mod;
                                                        locked.CombinedCost = CalculateCombinedCost(t);
                                                        break;
                                                    case ModTypeEnum.line:
                                                        foreach (Vector3f v in LineDraw(requestingUnitCoord.CubeCoordinate, t.TargetCoordinate))
                                                        {
                                                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                                                        }
                                                        break;
                                                    case ModTypeEnum.ring:
                                                        foreach (Vector3f v in RingDraw(t.TargetCoordinate, mod.RingNested.Radius))
                                                        {
                                                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                                                        }
                                                        break;
                                                }
                                            }
                                            requestingUnitActions.LockedAction = locked;
                                            m_ResourceSystem.SubstactEnergy(requestingUnitFaction.Faction, locked.CombinedCost);
                                        }
                                        else
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                                        }
                                    }
                                });
                                break;
                            case TargetTypeEnum.unit:
                                Entities.With(m_UnitData).ForEach((ref SpatialEntityId targetUnitId, ref CubeCoordinate.Component targetUnitCoord) =>
                                {
                                    if (targetUnitId.EntityId.Id == id)
                                    {
                                        bool isValidTarget = false;
                                        foreach (CellAttributes c in requestingUnitCellsToMark.CellsInRange)
                                        {
                                            if (Vector3fext.ToUnityVector(c.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate))
                                            {
                                                isValidTarget = ValidateUnitTarget(targetUnitId.EntityId.Id, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction, requestingUnitActions.CurrentSelected.Targets[0].UnitTargetNested.UnitReq);
                                            }
                                        }
                                        if (isValidTarget)
                                        {
                                            requestingUnitActions.LockedAction = SetLockedAction(requestingUnitActions.CurrentSelected, requestingUnitCoord.CubeCoordinate, targetUnitCoord.CubeCoordinate, targetUnitId.EntityId.Id, requestingUnitFaction.Faction);
                                        }
                                        else
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                                        }
                                    }
                                });
                                break;
                        }

                        unitActions = requestingUnitActions;
                        unitActions.CurrentSelected = unitActions.NullAction;
                    }
                }
            });

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
                combinedCost += (uint)inActionTarget.Mods[0].CoordinatePositionPairs.Count;
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
                    foreach (Vector3f v in CircleDraw(t.TargetCoordinate, (uint)mod.AoeNested.Radius))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                    }
                    break;
                case ModTypeEnum.path:

                    break;
                case ModTypeEnum.line:
                    foreach (Vector3f v in LineDraw(originCoord, t.TargetCoordinate))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                    }
                    break;
                case ModTypeEnum.ring:
                    foreach (Vector3f v in RingDraw(t.TargetCoordinate, mod.RingNested.Radius))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                    }
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
    /*
    public Vector2 CubeCoordToXZ(Vector3f coord)
    {
        Vector2 axial = CubeToAxial(coord);
        var x = 1.5f * (3 / 2 * axial.x);
        var y = 1.73f * ((axial.x * 0.5f) + axial.y);

        //center cell + coordinate offset = XZ coordinate in world space - offset X by (worldindex - 1) * 100?
        return new Vector2(50, 55.22f) + new Vector2(x, y);
    }
    */

    Vector3f CubeDirection(uint direction)
    {
        if (direction < 6)
            return DirectionsArray[direction];
        else
            return new Vector3f();
    }
    
    Vector3f CubeNeighbour(Vector3f origin, uint direction)
    {
        var cubeDirection = CubeDirection(direction);
        return new Vector3f(origin.X + cubeDirection.X, origin.Y + cubeDirection.Y, origin.Z + cubeDirection.Z);
    }

    Vector3f CubeScale(Vector3f direction, uint scale)
    {
        return new Vector3f(direction.X * scale, direction.Y * scale, direction.Z * scale);
    }

    public Vector3f CoordinateDirection(Vector3f origin, Vector3f destination)
    {
        return new Vector3f(destination.X - origin.X, destination.Y - origin.Y, destination.Z - origin.Z);
    }

    public List<Vector3f> RingDraw(Vector3f origin, uint radius)
    {
        var ring = new List<Vector3f>();
        var cubeScale = CubeScale(DirectionsArray[4], radius);
        //Debug.Log("OriginCoord = " + origin.X + ", " + origin.Y + ", " + origin.Z);
        //Debug.Log("CubeScale = " + cubeScale.X + ", " + cubeScale.Y + ", " + cubeScale.Z);
        var coord = new Vector3f(origin.X + cubeScale.X, origin.Y + cubeScale.Y, origin.Z + cubeScale.Z);

        for(int i = 0; i < 6; i++)
        {
            for(int j = 0; j < radius; j++)
            {
                //Debug.Log("RingCoord = " + coord.X + coord.Y + coord.Z);
                ring.Add(coord);
                coord = CubeNeighbour(coord, (uint)i);
            }
        }

        //Debug.Log("RingCount = " + ring.Count);
        return ring;
    }

    public int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }

    public float GetAngle(Vector3f originPos, Vector3f targetPos)
    {
        Vector3f dir = CoordinateDirection(originPos, targetPos);
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
        //destination = new Vector3f(destination.X + 1e - 6f, destination.Y + 2e-6f, destination.Z + 3e-6f);

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
                results.Add(new Vector3f(originCellCubeCoordinate.X + x, originCellCubeCoordinate.Y + y, originCellCubeCoordinate.Z + z));
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

        Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWorldIndex, ref CubeCoordinate.Component cubeCoordinate, ref CellAttributesComponent.Component cellAttributes) =>
        {
            if (cellWorldIndex.Value == unitWorldIndex)
            {
                if (Vector3fext.ToUnityVector(cubeCoordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCellCubeCoordinate))
                {
                    cellsInRadius[0] = cellAttributes.CellAttributes;
                }
                else if (coordHash.Contains(cubeCoordinate.CubeCoordinate))
                {
                    cellsInRadius.Add(cellAttributes.CellAttributes);
                }
            }
        });
        
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
        Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component coordinate, ref CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCoord))
            {
                origin = cellAttribute.CellAttributes.Cell;
            }
        });

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


            if (!isTaken || Vector3fext.ToUnityVector(cell.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(origin.CubeCoordinate))
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

        Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component coordinate, ref CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(inDestination))
            {
                destination = cellAttribute.CellAttributes.Cell;
            }
        });

        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public CellAttributes SetCellAttributes(CellAttributes cellAttributes, bool isTaken, long entityId, uint worldIndex)
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
        Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWordlIndex, ref CellAttributesComponent.Component cellAtt) =>
        {
            if (worldIndex == cellWordlIndex.Value)
            {
                for (int n = 0; n < neighbours.CellAttributes.Count; n++)
                {
                    if (Vector3fext.ToUnityVector(neighbours.CellAttributes[n].CubeCoordinate) == Vector3fext.ToUnityVector(cellAtt.CellAttributes.Cell.CubeCoordinate))
                    {
                        for (int cn = 0; cn < cellAtt.CellAttributes.Neighbours.CellAttributes.Count; cn++)
                        {
                            if (Vector3fext.ToUnityVector(cellAtt.CellAttributes.Neighbours.CellAttributes[cn].CubeCoordinate) == Vector3fext.ToUnityVector(cell.CubeCoordinate))
                            {
                                cellAtt.CellAttributes.Neighbours.CellAttributes[cn] = cell;
                                cellAtt.CellAttributes = cellAtt.CellAttributes;
                            }
                        }
                    }
                }
            }
        });
    }

    public bool ValidateUnitTarget(long targetUnitId, long usingUnitId, uint inFaction, UnitRequisitesEnum restrictions)
    {
        bool valid = false;

        Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component faction) =>
        {
            if (targetUnitId == unitId.EntityId.Id)
            {
                switch (restrictions)
                {
                    case UnitRequisitesEnum.any:
                        valid = true;
                        break;
                    case UnitRequisitesEnum.enemy:
                        if (faction.Faction != inFaction)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.friendly:
                        if (faction.Faction == inFaction)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.friendly_other:
                        if (faction.Faction == inFaction && usingUnitId != unitId.EntityId.Id)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.other:
                        if (usingUnitId != unitId.EntityId.Id)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.self:
                        //maybe selfstate becomes irrelevant once a self-target is implemented.
                        if (usingUnitId == unitId.EntityId.Id)
                        {
                            valid = true;
                        }
                        break;

                    default:
                        break;
                }
            }
        });

        return valid;
    }

}

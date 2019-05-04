using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using System;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cell;
using Player;
using Improbable.Gdk.ReactiveComponents;

public class HighlightingSystem : ComponentSystem
{
    public struct ActiveUnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Actions.Component> ActionsData;

    }
    [Inject]
    ActiveUnitData m_ActiveUnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameState;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
    }
    [Inject]
    GameStateData m_GameStateData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject]
    private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Health.Component> HealthAttributes;
    }

    [Inject]
    private UnitData m_UnitData;
    

    public struct ActiveHightlighter
    {
        public enum UsePath
        {
            None,
            toCellPath,
            toCellArc,
            toUnitPath,
            toUnitArc
        }
        public UsePath usePath;
        public Color pathColor;
        public List<CircleAoeHighlighter> CircleAoEList;


    }

    public struct CircleAoeHighlighter
    {
        public int radius;
        public enum HandleOverlap
        {
            add,
            multiply,
            subtract
        }
        public HandleOverlap overlap;
        public enum AoEAround
        {
            target,
            self
        }
        public AoEAround aoeAround;
    }
    public ActiveHightlighter Highlighter = new ActiveHightlighter();
    // Start is called before the first frame update
    protected override void OnUpdate()
    {
        throw new NotImplementedException();
    }

    public void SetHighlighter(Unit.Action inAction)
    {

    }
}

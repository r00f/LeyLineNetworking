using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;
using Generic;
using Player;
using Improbable.Gdk.ReactiveComponents;
using Cell;
using Improbable;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HighlightingSystem))]
    public class CellMarkerSystem : ComponentSystem
    {
        struct CellData
        {
            public readonly int Length;
            public ComponentDataArray<MarkerState> MarkerStateData;
            public ComponentArray<MarkerGameObjects> MarkerGameObjectsData;
        }

        [Inject] CellData m_CellData;

        struct NewCellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntityData;
            public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
            public ComponentArray<MarkerGameObjects> MarkerGameObjectsData;
        }
        [Inject] NewCellData m_NewCellData;

        struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public ComponentDataArray<MarkerState> MarkerStateData;
            public ComponentArray<UnitMarkerGameObjects> MarkerGameObjectsData;
        }

        [Inject] UnitData m_UnitData;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
            public readonly ComponentDataArray<GameState.Component> GameState;

        }
        [Inject] GameStateData m_GameStateData;

        public struct PlayerStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
        }

        [Inject] PlayerStateData m_PlayerStateData;

        Settings settings;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            settings = Resources.Load<Settings>("Settings");

        }

        protected override void OnUpdate()
        {
            
            for (int i = 0; i < m_NewCellData.Length; i++)
            {
                MarkerGameObjects markerGameObject = m_NewCellData.MarkerGameObjectsData[i];
                var cellAtt = m_NewCellData.CellAttributes[i];
                int colorIndex = cellAtt.CellAttributes.CellMapColorIndex;
                markerGameObject.MapMarkerRenderer.material.color = settings.MapCellColors[colorIndex];
                //Debug.Log("AssignMapColor");
            }
            

            var playerWorldIndex = m_PlayerStateData.WorldIndex[0].Value;

            for (int j = 0; j < m_GameStateData.Length; j++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndex[j].Value;
                var gameState = m_GameStateData.GameState[j].CurrentState;

                if(gameStateWorldIndex == playerWorldIndex)
                {
                    if (gameState != GameStateEnum.planning)
                    {
                        for (int i = 0; i < m_CellData.Length; i++)
                        {
                            MarkerGameObjects markerGameObject = m_CellData.MarkerGameObjectsData[i];
                            markerGameObject.TargetMarker.SetActive(false);
                            markerGameObject.ClickedMarker.SetActive(false);
                            markerGameObject.HoveredMarker.SetActive(false);
                        }
                        for (int i = 0; i < m_UnitData.Length; i++)
                        {
                            UnitMarkerGameObjects markerGameObject = m_UnitData.MarkerGameObjectsData[i];
                            markerGameObject.AttackTargetMarker.SetActive(false);
                            markerGameObject.DefenseMarker.SetActive(false);
                            markerGameObject.HealTargetMarker.SetActive(false);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < m_CellData.Length; i++)
                        {
                            int isSet = m_CellData.MarkerStateData[i].IsSet;
                            MarkerState markerState = m_CellData.MarkerStateData[i];
                            MarkerGameObjects markerGameObject = m_CellData.MarkerGameObjectsData[i];

                            /*
                            if (markerState.IsTarget == 1)
                            {
                                if (!markerGameObject.TargetMarker.activeSelf)
                                    markerGameObject.TargetMarker.SetActive(true);
                            }
                            else
                            {
                                if (markerGameObject.TargetMarker.activeSelf)
                                    markerGameObject.TargetMarker.SetActive(false);
                            }
                            */

                            switch(markerState.NumberOfTargets)
                            {
                                case 0:
                                    if (markerGameObject.TargetMarker.activeSelf)
                                        markerGameObject.TargetMarker.SetActive(false);
                                    break;
                                case 1:
                                    if (!markerGameObject.TargetMarker.activeSelf)
                                        markerGameObject.TargetMarker.SetActive(true);

                                    markerGameObject.TargetMarkerRenderer.material.color = markerGameObject.TargetColors[0];
                                    break;
                                case 2:
                                    markerGameObject.TargetMarkerRenderer.material.color = markerGameObject.TargetColors[1];
                                    break;
                                case 3:
                                    markerGameObject.TargetMarkerRenderer.material.color = markerGameObject.TargetColors[2];
                                    break;
                                case 4:
                                    markerGameObject.TargetMarkerRenderer.material.color = markerGameObject.TargetColors[3];
                                    break;
                            }

                            if (isSet == 0)
                            {
                                switch (markerState.CurrentState)
                                {
                                    case MarkerState.State.Neutral:
                                        markerGameObject.ClickedMarker.SetActive(false);
                                        markerGameObject.HoveredMarker.SetActive(false);
                                        markerGameObject.ReachableMarker.SetActive(false);
                                        break;
                                    case MarkerState.State.Clicked:
                                        if (markerState.NumberOfTargets == 0)
                                            markerGameObject.ClickedMarker.SetActive(true);
                                        markerGameObject.HoveredMarker.SetActive(false);
                                        markerGameObject.ReachableMarker.SetActive(false);
                                        break;
                                    case MarkerState.State.Hovered:
                                        markerGameObject.ClickedMarker.SetActive(false);
                                        markerGameObject.HoveredMarker.SetActive(true);
                                        markerGameObject.ReachableMarker.SetActive(false);
                                        break;
                                    case MarkerState.State.Reachable:
                                        markerGameObject.ClickedMarker.SetActive(false);
                                        markerGameObject.HoveredMarker.SetActive(false);
                                        markerGameObject.ReachableMarker.SetActive(true);
                                        break;
                                }

                                markerState.IsSet = 1;
                                m_CellData.MarkerStateData[i] = markerState;
                            }
                        }

                        for (int i = 0; i < m_UnitData.Length; i++)
                        {
                            int isSet = m_UnitData.MarkerStateData[i].IsSet;
                            MarkerState markerState = m_UnitData.MarkerStateData[i];
                            MouseState mouseState = m_UnitData.MouseStateData[i];
                            UnitMarkerGameObjects markerGameObject = m_UnitData.MarkerGameObjectsData[i];
                            var teamColor = m_UnitData.FactionData[i].TeamColor;
                            int colorIndex = 0;

                            if (teamColor == TeamColorEnum.blue)
                            {
                                colorIndex = 1;
                            }
                            else if (teamColor == TeamColorEnum.red)
                            {
                                colorIndex = 2;
                            }

                            if (markerState.NumberOfTargets > 0 && markerState.TargetTypeSet == 0)
                            {
                                switch (markerState.CurrentTargetType)
                                {
                                    case MarkerState.TargetType.Neutral:
                                        //change when we implement other target types
                                        markerGameObject.AttackTargetMarker.SetActive(true);
                                        markerGameObject.DefenseMarker.SetActive(false);
                                        markerGameObject.HealTargetMarker.SetActive(false);
                                        break;
                                    case MarkerState.TargetType.AttackTarget:
                                        markerGameObject.AttackTargetMarker.SetActive(true);
                                        markerGameObject.DefenseMarker.SetActive(false);
                                        markerGameObject.HealTargetMarker.SetActive(false);
                                        break;
                                    case MarkerState.TargetType.DefenseTarget:
                                        markerGameObject.AttackTargetMarker.SetActive(false);
                                        markerGameObject.DefenseMarker.SetActive(true);
                                        markerGameObject.HealTargetMarker.SetActive(false);
                                        break;
                                    case MarkerState.TargetType.HealTarget:
                                        markerGameObject.AttackTargetMarker.SetActive(false);
                                        markerGameObject.DefenseMarker.SetActive(false);
                                        markerGameObject.HealTargetMarker.SetActive(true);
                                        break;
                                }
                                markerState.TargetTypeSet = 1;
                            }
                            else if (markerState.NumberOfTargets == 0)
                            {
                                if (markerState.TargetTypeSet == 1)
                                {
                                    markerState.TargetTypeSet = 0;
                                }
                                markerGameObject.AttackTargetMarker.SetActive(false);
                                markerGameObject.DefenseMarker.SetActive(false);
                                markerGameObject.HealTargetMarker.SetActive(false);
                            }

                            if (isSet == 0)
                            {
                                switch (markerState.CurrentState)
                                {
                                    case MarkerState.State.Neutral:
                                        markerGameObject.Outline.enabled = false;
                                        markerState.IsSet = 1;
                                        break;
                                    case MarkerState.State.Clicked:
                                        markerGameObject.Outline.enabled = false;
                                        //markerGameObject.Outline.enabled = true;
                                        //markerGameObject.Outline.color = colorIndex;
                                        markerState.IsSet = 1;
                                        break;
                                    case MarkerState.State.Hovered:
                                        markerGameObject.Outline.enabled = true;
                                        markerGameObject.Outline.color = colorIndex;
                                        markerState.IsSet = 1;
                                        break;
                                    case MarkerState.State.Reachable:
                                        markerGameObject.Outline.enabled = true;
                                        if(mouseState.CurrentState == MouseState.State.Hovered)
                                        {
                                            markerGameObject.Outline.color = colorIndex;
                                        }
                                        else
                                        {
                                            markerGameObject.Outline.color = 0;
                                        }
                                        break;
                                }
                            }
                            m_UnitData.MarkerStateData[i] = markerState;
                        }
                    }
                }
            }
        }
    }
}


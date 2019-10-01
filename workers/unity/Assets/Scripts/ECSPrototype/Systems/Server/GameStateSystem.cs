using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Player;
using Cell;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using System.Collections.Generic;
using Improbable;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class GameStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
            public ComponentDataArray<GameState.Component> GameStateData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] Data m_Data;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<PlayerAttributes.Component> AttributesData;
            public ComponentDataArray<PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<SpatialEntityId> EntityIds;
            public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] UnitData m_UnitData;

        /*
        public struct SpawnCellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
            public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
            public readonly ComponentDataArray<Authoritative<CellAttributesComponent.Component>> AuthorativeData;
            public readonly ComponentDataArray<IsSpawn.Component> IsSpawnData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<UnitToSpawn.Component> UnitToSpawnData;
        }

        [Inject] SpawnCellData m_SpawnCellData;
        */

        
        public struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<CubeCoordinate.Component> CubeCoordinateData;
            public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        }

        [Inject] CellData m_CellData;
        

        [Inject] HandleCellGridRequestsSystem m_CellGridSystem;

        [Inject] CleanupSystem m_CleanUpSystem;

        [Inject] SpawnUnitsSystem m_SpawnSystem;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Data.Length; i++)
            {
                var gameState = m_Data.GameStateData[i];
                var gameStateWorldIndex = m_Data.WorldIndexData[i].Value;

                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
#if UNITY_EDITOR
                        if (gameState.PlayersOnMapCount == 1)
                        {
                            gameState.CurrentState = GameStateEnum.planning;
                            m_Data.GameStateData[i] = gameState;
                        }
#else
                        if (gameState.PlayersOnMapCount == 2)
                        {
                            gameState.CurrentState = GameStateEnum.planning;
                            m_Data.GameStateData[i] = gameState;
                        }
#endif
                        break;
                    case GameStateEnum.planning:
                        if (AllPlayersReady(gameStateWorldIndex) || gameState.CurrentPlanningTime <= 0)
                        {
                            gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                            gameState.CurrentState = GameStateEnum.interrupt;
                            m_Data.GameStateData[i] = gameState;
                        }
                        else if(AnyPlayerReady(gameStateWorldIndex))
                        {
                            gameState.CurrentPlanningTime -= Time.deltaTime;
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.interrupt:
                        
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.interrupt, gameStateWorldIndex);
                            m_Data.GameStateData[i] = gameState;
                        }

                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if(gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.CurrentState = GameStateEnum.attack;
                                gameState.HighestExecuteTime = 0;
                            }
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.attack:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.attack, gameStateWorldIndex);
                            m_Data.GameStateData[i] = gameState;
                        }

                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateIsTaken(gameStateWorldIndex);
                                gameState.CurrentState = GameStateEnum.move;
                                gameState.HighestExecuteTime = 0;
                            }
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.move:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.move, gameStateWorldIndex);
                            m_Data.GameStateData[i] = gameState;
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateIsTaken(gameStateWorldIndex);
                                gameState.CurrentState = GameStateEnum.skillshot;
                                gameState.HighestExecuteTime = 0;
                            }
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.skillshot:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.skillshot, gameStateWorldIndex);
                            m_Data.GameStateData[i] = gameState;
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.CurrentState = GameStateEnum.calculate_energy;
                                gameState.HighestExecuteTime = 0;
                            }
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.calculate_energy:
                        
                        gameState.CurrentPlanningTime = gameState.PlanningTime;
                        gameState.CurrentState = GameStateEnum.cleanup;
                        m_Data.GameStateData[i] = gameState;
                        break;
                    case GameStateEnum.cleanup:
                            //check if any hero is dead to go into gameOver
                            m_CleanUpSystem.DeleteDeadUnits(gameStateWorldIndex);
                            //UpdateIsTaken(gameStateWorldIndex);
                            gameState.CurrentState = GameStateEnum.planning;
                            m_Data.GameStateData[i] = gameState;
                        break;
                    case GameStateEnum.game_over:
                        break;
                }

                #if UNITY_EDITOR

                #else

                if(gameState.PlayersOnMapCount < 2 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    gameState.CurrentState = GameStateEnum.waiting_for_players;
                    m_Data.GameStateData[i] = gameState;
                }

                #endif
            }
        }

        private bool AnyPlayerReady(uint gameStateWorldIndex)
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var playerWorldIndex = m_PlayerData.WorldIndexData[i].Value;

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    var playerState = m_PlayerData.PlayerStateData[i].CurrentState;
                    if (playerState == PlayerStateEnum.ready)
                        return true;
                }
            }
            return false;
        }

        private float HighestExecuteTime(GameStateEnum step, uint gameStateWorldIndex)
        {
            float highestTime = 0.3f;
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
                var lockedAction = m_UnitData.ActionsData[i].LockedAction;

                if(unitWorldIndex == gameStateWorldIndex)
                {
                    if ((int)lockedAction.ActionExecuteStep == (int)step - 2)
                    {
                        if (step == GameStateEnum.move)
                        {
                            //make sure move has enough time
                            float addedTime = 2.5f;
                            float unitMoveTime = addedTime + (lockedAction.Effects[0].MoveAlongPathNested.TimePerCell * (lockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count + 1));
                            if (unitMoveTime > highestTime)
                                highestTime = unitMoveTime;
                        }
                        else
                        {
                            if (lockedAction.TimeToExecute > highestTime)
                                highestTime = lockedAction.TimeToExecute;
                        }
                    }
                }
            }
            return highestTime;
        }

        private bool AllPlayersReady(uint gameStateWorldIndex)
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var playerWorldIndex = m_PlayerData.WorldIndexData[i].Value;

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    var playerState = m_PlayerData.PlayerStateData[i].CurrentState;
                    if (playerState != PlayerStateEnum.ready)
                        return false;
                }
            }
            return true;
        }

        private void UpdateIsTaken(uint gameStateWorldIndex)
        {
            Dictionary<Vector3f, long> unitDict = new Dictionary<Vector3f, long>();
            HashSet<Vector3f> unitCoordHash = new HashSet<Vector3f>();

            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var worldIndex = m_UnitData.WorldIndexData[i].Value;
                var cubeCoord = m_UnitData.CoordinateData[i].CubeCoordinate;
                var entityId = m_UnitData.EntityIds[i].EntityId.Id;
                var actions = m_UnitData.ActionsData[i];

                if (gameStateWorldIndex == worldIndex)
                {
                    if(actions.LockedAction.Index != -3 && actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                    {
                        if (!unitDict.ContainsKey(cubeCoord))
                            unitDict.Add(cubeCoord, entityId);
                        if (!unitCoordHash.Contains(cubeCoord))
                            unitCoordHash.Add(cubeCoord);
                    }
                }
            }

            for (int i = 0; i < m_CellData.Length; i++)
            {
                var worldIndex = m_CellData.WorldIndexData[i].Value;
                var cellCubeCoordinate = m_CellData.CubeCoordinateData[i];
                var cellWorldIndex = m_CellData.WorldIndexData[i].Value;
                var cellAtt = m_CellData.CellAttributes[i];

                if (gameStateWorldIndex == worldIndex)
                {
                    //unitCoordHash is filled with unitCoords that have a move planned died
                    if (unitCoordHash.Contains(cellCubeCoordinate.CubeCoordinate))
                    {
                        long id = unitDict[cellCubeCoordinate.CubeCoordinate];

                        if(cellAtt.CellAttributes.Cell.UnitOnCellId != id)
                        {
                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, id, cellWorldIndex);
                            m_CellData.CellAttributes[i] = cellAtt;
                            //Debug.Log("SetTaken: " + m_CellData.CellAttributes[i].CellAttributes.Cell.UnitOnCellId);
                        }
                        else if (cellAtt.CellAttributes.Cell.IsTaken && !cellAtt.CellAttributes.Cell.ObstructVision)
                        {
                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, 0, cellWorldIndex);
                            m_CellData.CellAttributes[i] = cellAtt;
                            //Debug.Log("ReSetTaken: " + m_CellData.CellAttributes[i].CellAttributes.Cell.UnitOnCellId);
                        }
                    }
                }
            }
        }
        /*
        private bool NoUnitMoving(uint gameStateWorldIndex)
        {
            //loop through all Units to check if idle
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;

                if (unitWorldIndex == gameStateWorldIndex)
                {
                    var lockedAction = m_UnitData.ActionsData[i].LockedAction;

                    if(lockedAction.Index == -2)
                    {
                        if (lockedAction.Targets[0].Mods[0].Coordinates.Count != 0)
                            return false;
                    }
                }
            }
            return true;
        }
    */

        /*
private bool AllPlayersEndTurnReady(uint gameStateWorldIndex)
{
    for (int i = 0; i < m_PlayerData.Length; i++)
    {
        var playerWorldIndex = m_PlayerData.WorldIndexData[i].Value;

        if (playerWorldIndex == gameStateWorldIndex)
        {
            var playerState = m_PlayerData.PlayerStateData[i];
            if (!playerState.EndStepReady)
                return false;
        }
    }
    return true;
}
*/

        /*
private bool AllSpawnsInitialized(uint gameStateWorldIndex)
{
    for (int i = 0; i < m_SpawnCellData.Length; i++)
    {
        var cellWorldIndex = m_SpawnCellData.WorldIndexData[i].Value;
        var unitToSpawn = m_SpawnCellData.UnitToSpawnData[i];
        var coord = m_SpawnCellData.CoordinateData[i];
        var cellAtt = m_SpawnCellData.CellAttributes[i].CellAttributes.Cell;

        if (cellWorldIndex == gameStateWorldIndex)
        {
            if (unitToSpawn.IsSpawn && !cellAtt.IsTaken)
                return false;
        }
    }
    return true;
}
*/

    }
}



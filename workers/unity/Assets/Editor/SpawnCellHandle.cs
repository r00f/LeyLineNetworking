using LeyLineHybridECS;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnitToSpawnEditor))]
class SpawnCellHandle : Editor
{
    protected virtual void OnSceneGUI()
    {
        UnitToSpawnEditor handleExample = (UnitToSpawnEditor) target;

        if (handleExample == null)
        {
            return;
        }

        Handles.color = Color.yellow;

        if(handleExample.IsUnitSpawn)
            Handles.ArrowHandleCap(0, handleExample.transform.position, Quaternion.Euler(new Vector3(0, handleExample.StartRotation, 0)), 1f, EventType.Repaint);
    }
}

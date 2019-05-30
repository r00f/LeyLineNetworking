using UnityEngine;
using UnityEditor;

namespace LeyLineHybridECS
{
    [CustomEditor(typeof(ECSATarget_Tile))]
    public class Action_TargetCell_Editor : Editor
{
    ECSATarget_Tile myActionTarget;
    public override void OnInspectorGUI()
    {

        myActionTarget = target as ECSATarget_Tile;
        base.OnInspectorGUI();
        if (GUILayout.Button("AddModifier"))
        {
            if (myActionTarget.ModToAdd != null)
            {
                ECSActionSecondaryTargets go = Instantiate(myActionTarget.ModToAdd);
                myActionTarget.SecondaryTargets.Add(go);
                AssetDatabase.AddObjectToAsset(go, AssetDatabase.GetAssetPath(myActionTarget));
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(myActionTarget));
                myActionTarget.ModToAdd = null;
            }
        }


    }
    // Start is called before the first frame update

}
}
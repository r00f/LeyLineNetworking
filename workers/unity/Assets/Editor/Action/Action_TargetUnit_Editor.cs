using UnityEngine;
using UnityEditor;

namespace LeyLineHybridECS
{
    [CustomEditor(typeof(ECSATarget_Unit))]
    public class Action_TargetUnit_Editor : Editor
    {


        ECSATarget_Unit myActionTarget;
        public override void OnInspectorGUI()
        {

            myActionTarget = target as ECSATarget_Unit;
            base.OnInspectorGUI();
            if (GUILayout.Button("AddModifier"))
            {
                if(myActionTarget.ModToAdd != null)
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
using UnityEngine;
using UnityEditor;

namespace LeyLineHybridECS
{
    [CustomEditor(typeof(ECSAction))]
    public class ActionEditorScript : Editor
    {
#if UNITY_EDITOR
        ECSAction myAction;
        public override void OnInspectorGUI()
        {
            
            myAction = target as ECSAction;
            base.OnInspectorGUI();
            if (GUILayout.Button("AddTarget"))
            {
                if(myAction.TargetToAdd != null)
                {
                    ECSActionTarget go = Instantiate(myAction.TargetToAdd);
                    myAction.Targets.Add(go);
                    AssetDatabase.AddObjectToAsset(go, AssetDatabase.GetAssetPath(myAction));
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(myAction));
                    myAction.TargetToAdd = null;
                }
            }
            if (GUILayout.Button("AddEffect"))
            {
                if (myAction.EffectToAdd != null)
                {
                    ECSActionEffect go = Instantiate(myAction.EffectToAdd);
                    myAction.Effects.Add(go);
                    AssetDatabase.AddObjectToAsset(go, AssetDatabase.GetAssetPath(myAction));
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(myAction));
                    myAction.EffectToAdd = null;
                }
            }

        }
        // Start is called before the first frame update
#endif
    }
}
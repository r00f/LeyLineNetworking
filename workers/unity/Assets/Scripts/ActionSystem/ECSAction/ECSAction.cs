using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Improbable;


namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    [System.Serializable]
    public class ECSAction : ScriptableObject
    {
        /*
    public enum MultiTarget
    {
        No,
        Or,
        All,
        Any
    }
    public MultiTarget MultiTargetting = MultiTarget.No;
    */
        public ECSActionTarget TargetToAdd;
        [HideInInspector]
        public List<ECSActionTarget> Targets;
        public ECSActionEffect EffectToAdd;
        [HideInInspector]
        public List<ECSActionEffect> Effects;
        public string ActionName;
        public Sprite ActionIcon;
    }
}
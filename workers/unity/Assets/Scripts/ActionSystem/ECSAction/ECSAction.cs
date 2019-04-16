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
        public List<ECSActionTarget> Targets;
        public List<ECSActionEffect> Effects;
        public Vector3f UnitUsedByCoordinate;
        public int baseEneryCost;
        public int currentEnergyCost;
        public string ButtonName;
    }
}
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
        public enum ExecuteStep
        {
            Interrupt,
            Attack,
            Move,
            Skillshot,
            Cleanup
        }

        public ExecuteStep ActionExecuteStep = ExecuteStep.Attack;

        [Header("Action Information")]
        public Sprite ActionIcon;
        public Projectile ProjectileFab;
        public string ActionName;
        public string Description;
        public float TimeToExecute;
        public bool HasWindup;

        [Header("Add Targets/Effects")]
        public ECSActionTarget TargetToAdd;
        public ECSActionEffect EffectToAdd;
        public List<ECSActionTarget> Targets;
        public List<ECSActionEffect> Effects;
    }
}
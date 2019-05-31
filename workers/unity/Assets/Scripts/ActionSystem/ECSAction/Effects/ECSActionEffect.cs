using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{
    [System.Serializable]
    public class ECSActionEffect : ScriptableObject
    {
        public enum ApplyTo
        {
            AllPrimary,
            AllSecondary,
            All
        }
        public ApplyTo ApplyToTargets;
        public int specificTargetIdentifier = 0;
        public enum applyRestrictions
        {
            Friendly,
            FriendlyOther,
            Self,
            Enemy,
            Other,
            Any
        }
        public applyRestrictions ApplyToRestrictions;
    }

}
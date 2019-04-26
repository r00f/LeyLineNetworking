using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LeyLineHybridECS
{
    [System.Serializable]
    public class ECSActionTarget : ScriptableObject
    {
        public enum HighlightDef
        {
            Path,
            Radius,
            Path_Visible,
            Radius_Visible
        }
        public HighlightDef HighlighterToUse;
        public int targettingRange;
        public uint energyCost;
        public List<ECSActionSecondaryTargets> SecondaryTargets = new List<ECSActionSecondaryTargets>();

    }
}
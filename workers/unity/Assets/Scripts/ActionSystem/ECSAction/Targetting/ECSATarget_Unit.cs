using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
namespace LeyLineHybridECS
{
    [System.Serializable]
    [CreateAssetMenu]
    public class ECSATarget_Unit : ECSActionTarget
    {
        public enum UnitRestrictions
        {
            Friendly,
            FriendlyOther,
            Self,
            Enemy,
            Other,
            Any            
        }
        //public Unit MainTarget;
        public UnitRestrictions Restrictions;
        public Vector3f Coordinate;

    }
}


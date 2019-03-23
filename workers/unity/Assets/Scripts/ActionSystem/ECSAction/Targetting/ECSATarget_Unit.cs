using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    public class ECSATarget_Unit : ECSActionTarget
    {
        public enum UnitRestrictions
        {
            Friendly,
            FriendlyOther,
            Self,
            Enemy,
            Any            
        }
        //public Unit MainTarget;
        public UnitRestrictions Restrictions;
    }
}


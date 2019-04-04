﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{
    public class ECSActionEffect : ScriptableObject
    {
        public enum ApplyTo
        {
            AllPrimary,
            AllSecondary,
            All,
            Specific
        }
        public ApplyTo applyTo;
        public int specificTargetIdentifier = 0;
    }
}
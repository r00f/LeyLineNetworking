﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Unit_BaseDataSet : MonoBehaviour
{
    [Range (0,20)]
    public uint VisionRange;
    public uint BaseHealth;
    public uint upkeepCost;
}
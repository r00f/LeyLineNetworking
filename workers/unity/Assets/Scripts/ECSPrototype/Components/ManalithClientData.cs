using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManalithClientData : MonoBehaviour
{
    public ManalithInfoComponent IngameIconRef { get; set; }
    public long ManalithEntityID = 0;
    public Vector3 WorldPos;
    public Image TooltipBackgroundImage;
    public string NodeName;
}

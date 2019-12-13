using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Unit_Library : ScriptableObject
{
    public Unit_Ref Hero = new Unit_Ref();
    public List<Unit_Ref> Units = new List<Unit_Ref>();
}

[System.Serializable]
public struct Unit_Ref
{
    public string Identifier;
    public GameObject Prefab;
    
}
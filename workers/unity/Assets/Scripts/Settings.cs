using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu, System.Serializable]
public class Settings : ScriptableObject
{
    public List<Color> FactionColors;
    public List<Color> MapCellColors;
    public Mesh TestMesh;
    public Material TestMat;
    
}

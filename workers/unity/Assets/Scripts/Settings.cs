using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu, System.Serializable]
public class Settings : ScriptableObject
{
    public List<Color> FactionColors;
    public List<Color> MapCellColors;
    public List<Color> TurnStepColors;
    public List<Color> TurnStepBgColors;
    public Color UIEnergyIncomeColor;
    public Mesh TestMesh;
    public Material TestMat;
    public List<Texture2D> ParialColorMasks;
    public GameObject MouseClickPS;
    public GameObject ExplosionDebugSphere;
    
}

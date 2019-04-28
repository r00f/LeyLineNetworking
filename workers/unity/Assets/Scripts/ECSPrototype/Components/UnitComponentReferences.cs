using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitComponentReferences : MonoBehaviour
{
    [Header("PlayerColorMeshRenderers")]
    public List<MeshRenderer> MeshRenderers;
    public Color PlayerColor;

    [Header("HealthBar")]
    public GameObject HealthbarPrefab;
    public GameObject HealthBarInstance;

    [Header("SelectionCircle")]
    public GameObject SelectionCircleGO;
    public MeshRenderer SelectionMeshRenderer;



}

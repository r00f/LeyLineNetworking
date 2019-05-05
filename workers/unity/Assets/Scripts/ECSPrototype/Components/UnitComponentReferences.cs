using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;
    
public class UnitComponentReferences : MonoBehaviour
{
    [Header("UnitVisuals")]
    public GameObject RotationParent;
    public Animator Animator;

    [Header("PlayerColorMeshRenderers")]
    public List<MeshRenderer> MeshRenderers;
    public Color PlayerColor;

    [Header("HealthBar")]
    public GameObject HealthbarPrefab;
    public GameObject HealthBarInstance;

    [Header("UnitVIsuals")]
    public GameObject SelectionCircleGO;
    public MeshRenderer SelectionMeshRenderer;
    public Outline Outline;
}

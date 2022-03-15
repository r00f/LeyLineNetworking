using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshMaterialComponent : MonoBehaviour
{
    public Animator Animator;
    public List<Renderer> AllMesheRenderers;
    public List<List<Material>> AllMeshMaterials = new List<List<Material>>();

    public int CurrentMoveIndex;
    public float CurrentMoveTime;
}

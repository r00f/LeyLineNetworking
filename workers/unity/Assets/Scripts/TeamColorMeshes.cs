using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColorMeshes : MonoBehaviour
{
    [SerializeField]
    public List<MeshRenderer> fullColorMeshes = new List<MeshRenderer>();
    [SerializeField]
    public Color color;
    [SerializeField]
    public List<Renderer> detailColorMeshes = new List<Renderer>();
}

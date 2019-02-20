using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColorMeshes : MonoBehaviour
{
    [SerializeField]
    public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    [SerializeField]
    public Color color;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColorMeshes : MonoBehaviour
{
    [SerializeField]
    public List<Renderer> fullColorMeshes = new List<Renderer>();
    [SerializeField]
    public Color color;
    [SerializeField]
    public List<Renderer> detailColorMeshes = new List<Renderer>();
}

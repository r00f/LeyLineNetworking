using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DetailColorTest : MonoBehaviour
{
    [SerializeField]
    Vector2 colorTextureOffset;

    [SerializeField]
    MeshRenderer renderer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if(renderer.material.GetTextureOffset("_DetailAlbedoMap") != colorTextureOffset)
            renderer.material.SetTextureOffset("_DetailAlbedoMap", colorTextureOffset);
    }
}

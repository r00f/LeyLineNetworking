using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlitTexture : MonoBehaviour
{
    [SerializeField]
    RenderTexture sourceTexture;
    [SerializeField]
    RenderTexture destTex;
    [SerializeField]
    Material mat;

    void Start()
    {
        if (!sourceTexture || !destTex)
        {
            Debug.LogError("A texture or a render texture are missing, assign them.");
        }
    }

    void Update()
    {
        
        //Graphics.Blit(sourceTexture, destTex, );
    }
}

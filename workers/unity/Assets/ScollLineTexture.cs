using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ScollLineTexture : MonoBehaviour
{
    [SerializeField]
    bool scroll;
    [SerializeField]
    LineRenderer lineRenderer;
    [SerializeField]
    float scrollSpeed;
    float offset = 0;


    void Update()
    {
        if (!scroll)
            return;

        if (offset <= 1 - Time.deltaTime * scrollSpeed)
        {
            offset += Time.deltaTime * scrollSpeed;
        }
        else
            offset = 0;

        lineRenderer.material.SetTextureOffset("_UnlitColorMap", new Vector2(0, offset));
    }
}

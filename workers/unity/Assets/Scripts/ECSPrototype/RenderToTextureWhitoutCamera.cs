
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderToTextureWhitoutCamera : MonoBehaviour
{
    public Material material;
    public RenderTexture rt;
    public int Pass = 0;

    void Blit(RenderTexture destination, Material mat, int pass)
    {
        RenderTexture.active = destination;
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.invertCulling = true;
        mat.SetPass(pass);
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 0.0f);
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 0.0f);
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 0.0f);
        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);
        GL.End();
        GL.invertCulling = false;
        GL.PopMatrix();
    }

    void Start()
    {
        Pass = material.passCount - 1;
    }

    void Update()
    {
        Blit(rt, material, Pass);
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
public class FogTextureModifier : MonoBehaviour
{
    [SerializeField]
    Texture3D fogTexture;
    [SerializeField]
    RenderTexture visibilityRenderTexture;
    Texture2D visibilityStorage;
    RenderTexture currentlyActiveRenderTexture;

    [SerializeField]
    Texture2D testTexture2D;

    [SerializeField]
    DensityVolume densityVolume;

    private void OnEnable()
    {
        visibilityStorage = new Texture2D(32, 32);
    }

    // Update is called once per frame
    void Update()
    {
        if (!fogTexture || !visibilityRenderTexture)
            return;

        currentlyActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = visibilityRenderTexture;
        visibilityStorage.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
        RenderTexture.active = currentlyActiveRenderTexture;

        // Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
        int size = fogTexture.width;
        Color[] colors = new Color[size * size * size];
        float inverseResolution = 1.0f / (size - 1.0f);

        //store XZ colour array and copy it 32x on the y axis
        for (int y = 0; y < size; y++)
        {
            int yOffset = y * size * size;
            for (int z = 0; z < size; z++)
            {
                int zOffset = z * size;
                for (int x = 0; x < size; x++)
                {
                    if(!testTexture2D)
                        colors[x + yOffset + zOffset] = visibilityStorage.GetPixel(x, z);
                    else
                        colors[x + yOffset + zOffset] = testTexture2D.GetPixel(x, z);
                }
            }
        }

        // Copy the color values to the texture
        fogTexture.SetPixels(colors);

        // Apply the changes to the texture and upload the updated texture to the GPU
        fogTexture.Apply();

        //fogTexture.IncrementUpdateCount();

        densityVolume.OnTextureUpdated();
    }
}

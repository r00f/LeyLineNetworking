using Generic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureWriterPrototype : MonoBehaviour
{
    [SerializeField]
    Vector3 CubeCoord;

    [SerializeField]
    Vector2 Offset;

    [SerializeField]
    float squareSize;

    [SerializeField]
    Image Image;


    Texture2D texture;

    [SerializeField]
    Color32 colorA;

    [SerializeField]
    Color32 colorB;

    Vector2 Centerpixel;

    void Start()
    {
       texture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
       Centerpixel = new Vector2(texture.width / 2, texture.height / 2);
       Image.material.SetTexture("_MainTex", texture);
    }


    private void Update()
    {
        /*
        if (texture)
        {
            DrawSquareAtOffset();
            
            var data = texture.GetRawTextureData<Color32>();

            // fill texture data with a simple pattern
            int index = 0;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if(index < data.Length)
                        data[index++] = ((x & y) == 0 ? colorA : colorB);
                }
            }

            // upload to the GPU
            texture.Apply();
            

    }
    */

    }



    public void DrawSquareAtOffset()
    {
        //loop over Writable Texture and Paint 1 Hexagon White
        var data = texture.GetRawTextureData<Color32>();

        int index = 0;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                index++;
                if (index < data.Length)
                {
                    if (x >= Centerpixel.x - squareSize && x <= Centerpixel.x + squareSize && y >= Centerpixel.y - squareSize && y <= Centerpixel.y + squareSize)
                        data[index] = colorA;
                    else
                        data[index] = colorB;
                }
            }
        }

        texture.Apply();
    }

    public void DrawHexAtCoordinate()
    {

        //loop over Writable Texture and Paint 1 Hexagon White
        var data = texture.GetRawTextureData<Color32>();

        int index = 0;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                index++;
                if (index < data.Length)
                {


                    data[index] = colorA;


                }
            }
        }

        texture.Apply();

    }
}

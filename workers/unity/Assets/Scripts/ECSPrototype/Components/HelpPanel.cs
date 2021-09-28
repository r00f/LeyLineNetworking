using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : MonoBehaviour
{
    public RenderTextures RenderTextures;
    public List<MainMenuTabButton> TabButtons;
    public List<HelpSection> HelpSections;

    [HideInInspector]
    public HelpSection ActiveHelpSection;

    public void FindComponents()
    {
        HelpSections.Clear();
        TabButtons.Clear();

        foreach (MainMenuTabButton b in GetComponentsInChildren<MainMenuTabButton>())
        {
            TabButtons.Add(b);
        }

        foreach (HelpSection h in GetComponentsInChildren<HelpSection>())
        {
            HelpSections.Add(h);

            h.VideoPlayerHandlers.Clear();
            foreach (VideoPlayerHandler v in h.GetComponentsInChildren<VideoPlayerHandler>())
            {
                h.VideoPlayerHandlers.Add(v);
            }

            for(int i = 0; i < h.VideoPlayerHandlers.Count; i++)
            {
                if (i < RenderTextures.RenderTex.Count)
                {
                    h.VideoPlayerHandlers[i].VideoPlayer.targetTexture = RenderTextures.RenderTex[i];
                    h.VideoPlayerHandlers[i].RawImage.texture = RenderTextures.RenderTex[i];
                }
                else
                    Debug.Log("Not Enough RenderTextures! Assign more RenderTextures in Scriptable object");
            }
        }

        for (int i = 0; i < HelpSections.Count; i++)
        {
            if (i != 0)
                HelpSections[i].gameObject.SetActive(false);
        }
    }
}

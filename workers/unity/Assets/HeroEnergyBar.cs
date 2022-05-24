using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HeroEnergyBar : MonoBehaviour
{
    public float MaxFillAmount = 1;
    public Image HeroEnergyIncomeFill;
    public Image HeroCurrentEnergyFill;
    public float lerpTime = 1f;
    public Color baseEnergyColor;
    public Color baseIncomeColor;
    float lerptime = 0f;
    public Image Backgroundflareup;

    private void Update()
    {
        if (lerptime >= 0f)
        {
            lerptime -= Time.deltaTime / lerptime;

            Backgroundflareup.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.white, lerptime);
            HeroCurrentEnergyFill.color = Color.Lerp(baseEnergyColor, Color.white, lerptime);
            HeroEnergyIncomeFill.color = Color.Lerp(baseIncomeColor, Color.white, lerptime);

        }
    }


    public void NotEnoughEnergy()
    {
        lerptime = 1f;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public float IncomingDamage;
    public GameObject HealthBarGo;
    public GameObject PlayerColorGo;
    public Image HoveredImage;
    public Image PlayerColorImage;
    public Image HealthFill;
    public Image ArmorFill;
    public Image DamageFill;
    public Image Parts;
    public RectTransform HealthBarRect;
    public RectTransform DamageRect;
    public Texture HealthSectionsSmall;
    public Texture HealthSectionsBig;

    public void Start()
    {
        Parts.material = Instantiate(Parts.material);
    }
}

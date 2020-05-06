using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public float IncomingDamage;
    public Image PlayerColor;
    public Image HealthFill;
    public Image ArmorFill;
    public Image DamageFill;
    public RectTransform HealthBarRect;
    public RectTransform DamageRect;
}

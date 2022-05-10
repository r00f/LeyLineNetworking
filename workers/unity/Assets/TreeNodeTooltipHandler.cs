using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof (RectTransform))]
public class TreeNodeTooltipHandler : MonoBehaviour
{
    // Start is called before the first frame update
    public Text Name;
    public Text Cost;
    public Text DescriptionText;
    public Image DisplayImage;
    public RectTransform rect;
}

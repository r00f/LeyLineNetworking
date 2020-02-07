﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitGroupUI : MonoBehaviour
{

    public GameObject UnitsPanel;
    public Image UnitTypeImage;
    public Image EnergyFill;
    public Image EnergyGainFill;
    public RectTransform Arrow;
    public Text UnitCountText;
    public Text EnergyChangeText;
    public List<long> ExistingUnitIds = new List<long>();
    public List<SelectUnitButton> SelectUnitButtons;

    public void SwitchPanelActive()
    {
        UnitsPanel.SetActive(!UnitsPanel.activeSelf);
        Arrow.localScale = new Vector3(-Arrow.localScale.x, Arrow.localScale.y, Arrow.localScale.z);
    }
}

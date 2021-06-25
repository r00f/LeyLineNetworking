using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    public Button ExitGameButton;
    public Button ConcedeButton;


    [Header("Cam Settings")]
    public GameObject CamSettingsPanel;
    //public InputField CamSpeedInputField;
    public Slider CamSpeedSlider;
    public Slider CamRotationSlider;
    public Slider CamZoomDistSlider;
    public Slider CamZoomSpeedSlider;
    public Toggle EdgeHoverToggle;
    public InputField TurnOverrideInputField;

    [Header("Sound Settings")]
    public Slider MasterVolumeSlider;
    public Slider SFXVolumeSlider;
    public Slider MusicVolumeSlider;

    [Header("Menu Panels")]
    public List<MainMenuTabButton> PanelButtons;
    public List<GameObject> Panels;

}

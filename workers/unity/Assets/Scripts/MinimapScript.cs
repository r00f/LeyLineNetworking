using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(RectTransform))]
public class MinimapScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsFullscreenMap;
    public bool isHovered;
    public float scale;
    public float MapSize;
    public RectTransform Map;
    public Vector3 MapCenter;
    public Moba_Camera MobaCamera;
    //public Camera MapCam;
    public HeroTransform h_Transform;

    public int ManalithCapturePingSize;
    public int BecomeVisiblePingSize;
    public int DeathPingSize;

    //public int MapUnitOutlineOffset;
    public Vector2 MapUnitPixelSize;
    public Vector2 MapCellPixelSize;
    //public Vector2 MapCellDarknessPixelSize;

    public Vector2 UnitColorOffsetMin;
    public Vector2 UnitColorOffsetMax;


    [Header("Panels")]
    public GameObject MiniMapCellTilesPanel;
    //public GameObject MiniMapDarknessTilesPanel;
    public GameObject MiniMapUnitTilesPanel;
    public GameObject MiniMapEffectsPanel;
    public GameObject MiniMapManalithTilesPanel;
    public GameObject MiniMapPlayerTilePanel;



    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    void Start()
    {
        Map = GetComponent<RectTransform>();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire1") && isHovered)
        {
            Vector2 MouseScreenPos = Input.mousePosition;
            Vector2 RectPos = Map.position;
            Vector2 Dir = (MouseScreenPos - RectPos) / (Screen.width / Map.rect.width);
            h_Transform.MoveCameraToMapClickPosition = true;
            h_Transform.TargetMapPosition = new Vector3(MapCenter.x + (scale * Dir.x), 0, MapCenter.z + (scale * Dir.y));
        }
    }
}

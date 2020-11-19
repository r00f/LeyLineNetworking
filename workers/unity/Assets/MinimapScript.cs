using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(RectTransform))]
public class MinimapScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovered;
    public float scale;
    public float MapSize;
    public RectTransform Map;
    public Vector3 MapCenter;
    //public Camera MapCam;
    public HeroTransform h_Transform;

    [Header("Panels")]
    public GameObject MiniMapCellTilesPanel;
    public GameObject MiniMapUnitTilesPanel;
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

        if (Input.GetButton("Fire1") && isHovered) { 
            Vector2 MouseScreenPos = Input.mousePosition;
            Vector2 RectPos = Map.position;
            Vector2 Dir = (MouseScreenPos - RectPos) / (Screen.width / Map.rect.width); //* scale);
            Vector3 PlanePosition = new Vector3(MapCenter.x + (scale * Dir.x), 0, MapCenter.z + (scale * Dir.y));

            if(h_Transform != null)
            {
                h_Transform.Transform = null;
                h_Transform.Position = PlanePosition;
                h_Transform.requireUpdate = true;
            }
        }
    }
}

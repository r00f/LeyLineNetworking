using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(RectTransform))]
public class MinimapScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovered;
    public RectTransform Map;
    public float scale;
    public Camera MapCam;
    public HeroTransform h_Transform;


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

        if (Input.GetButtonDown("Fire1") && isHovered) { 
            Vector2 MouseScreenPos = Input.mousePosition;
            Vector2 RectPos = Map.position;
            Vector2 Dir = (MouseScreenPos - RectPos)/100;
            Vector3 PlanePosition = new Vector3(MapCam.transform.position.x + (MapCam.orthographicSize * Dir.x), 0 , MapCam.transform.position.z + (MapCam.orthographicSize * Dir.y));

            if(h_Transform != null)
            {
                h_Transform.Transform = null;
                h_Transform.Position = PlanePosition;
                h_Transform.requireUpdate = true;
            }

            Debug.Log("Minimap Clicked" + Dir);
        }
    }
}

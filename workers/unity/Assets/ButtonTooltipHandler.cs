using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    ButtonTooltip toolTip;

    private void Update()
    {
        toolTip.Text.text = gameObject.name;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        toolTip.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        toolTip.gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        toolTip.gameObject.SetActive(false);
    }
}

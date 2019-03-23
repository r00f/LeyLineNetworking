using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeyLineHybridECS
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class ActionButtons : MonoBehaviour
    {
        // Start is called before the first frame update
        public Actions ActionsSelectedUnit;
        Button ButtonRef;
        public ECSAction AssignedAction;
        Text MyText;
        Image MyImage;

        void Start()
        {
            ButtonRef = GetComponent<Button>();
            MyImage = GetComponent<Image>();
            if(GetComponentInChildren<Text>() != null)
            {
                Debug.Log("filled Text");
                MyText = GetComponentInChildren<Text>();
            }
        }

        public void OnButtonPressed()
        {
            if(AssignedAction != null && ActionsSelectedUnit != null)
            {
                //ActionsSelectedUnit.CurrentActiveAction = AssignedAction;
            }
        }
        public void ClearFields()
        {
            MyImage.enabled = false;
            MyText.text = "";
            ButtonRef.interactable = false;
            ActionsSelectedUnit = null;
            AssignedAction = null;
            //hide button
        }
        public void Appear()
        {
            if(AssignedAction != null)
            {
                MyImage.enabled = true;
                ButtonRef.interactable = true;
                MyText.text = AssignedAction.ButtonName;
            }
        }

    }
}
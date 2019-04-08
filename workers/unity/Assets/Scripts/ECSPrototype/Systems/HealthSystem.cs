using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UI;
using Unit;

namespace LeyLineHybridECS
{

    public class HealthSystem : ComponentSystem
    {
        struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<Position3D> PositionData;
            public readonly ComponentArray<BoxCollider> ColliderData;
            public readonly ComponentDataArray<IsVisible> IsVisibleData;
            //public ComponentArray<Health> HealthData;
        }

        [Inject] private Data m_Data;

        Canvas canvas;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            canvas = Object.FindObjectOfType<Canvas>();
        }

        protected override void OnUpdate()
        {
            /*
            for (int i = 0; i < m_Data.Length; i++)
            {
                var position = m_Data.PositionData[i];
                var health = m_Data.HealthData[i];
                var collider = m_Data.ColliderData[i];
                var isVisible = m_Data.IsVisibleData[i];


                //if there is no healthbar, instantiate it into healthBarParent
                if(!health.HealthBarInstance)
                {
                    health.CurrentHealth = health.TotalHealth;
                    health.HealthBarInstance = Object.Instantiate(health.HealthBarPrefab, position.Value, Quaternion.identity, GameObject.FindGameObjectWithTag("HealthBarParent").transform);
                }
                else
                {
                    if (isVisible.Value == 1)
                    {
                        health.HealthBarInstance.SetActive(true);
                    }
                    else
                    {
                        health.HealthBarInstance.SetActive(false);
                    }
                    health.HealthBarInstance.transform.position = WorldToUISpace(canvas, position.Value + new float3(0, collider.bounds.size.y + 1f, 0));
                    health.HealthBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount = Mathf.Lerp(health.HealthBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount, (float)health.CurrentHealth / (float)health.TotalHealth, 0.1f);
                }
            }
            */
        }

        public Vector3 WorldToUISpace(Canvas parentCanvas, Vector3 worldPos)
        {
            //Convert the world for screen point so that it can be used with ScreenPointToLocalPointInRectangle function
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            Vector2 movePos;

            //Convert the screenpoint to ui rectangle local point
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out movePos);
            //Convert the local point to world point
            return parentCanvas.transform.TransformPoint(movePos);
        }

    }

}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class DollyCamTrailerMovement : MonoBehaviour
{

    public CinemachineVirtualCamera VirtualCamera;
    CinemachineTrackedDolly trackedDolly;
    public float Speed;
    public bool Move;

    public void StartMoving()
    {
        trackedDolly.m_PathPosition = 0;
        Move = true;
    }

    private void Start()
    {
        trackedDolly = VirtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineTrackedDolly;

    }

    void Update()
    {
        if(Move && trackedDolly.m_PathPosition < 1 - Time.deltaTime * Speed)
        {
            trackedDolly.m_PathPosition += Time.deltaTime * Speed;
        }
    }
}

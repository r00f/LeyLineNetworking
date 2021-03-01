using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using Generic;
using Unity.Entities;
using UnityEngine.Rendering.HighDefinition;

public class DollyCameraComponent : MonoBehaviour
{
    [SerializeField]
    CinemachineVirtualCamera dollyCam;
    [SerializeField]
    CinemachineBrain cameraBrain;
    [SerializeField]
    float UIDisplayDelatTime;
    //[SerializeField]
    //float decalProjectorLerpInSpeed;
    [SerializeField]
    float mapTitleLerpSpeed;
    //[SerializeField]
    //DecalProjector decalProjector;
    [SerializeField]
    UIReferences UIRef;
    [SerializeField]
    AnimationCurve speedCurve;
    [SerializeField]
    uint pathEndOffset;
    [SerializeField]
    Vector2 mapTitleStartEndPoints;
    [SerializeField]
    TextMesh mapTitleTextMesh;
    [SerializeField]
    float cameraSpeed;
    float distancePercentage;
    uint playerFaction = 0;
    CinemachineTrackedDolly trackedDolly;
    CinemachineSmoothPath smoothPath;
    List<CinemachineSmoothPath.Waypoint> pathWaypoints;
    Moba_Camera playerCam;
    bool directionSet;
    [SerializeField]
    bool endPath;
    public bool RevealVisionTrigger;

    // Start is called before the first frame update
    void Start()
    {
        trackedDolly = dollyCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineTrackedDolly;
        smoothPath = trackedDolly.m_Path as CinemachineSmoothPath;
        pathWaypoints = smoothPath.m_Waypoints.ToList();
        trackedDolly.m_PathPosition = 0;
        dollyCam.Priority = 11;
        mapTitleTextMesh.color = new Color(mapTitleTextMesh.color.r, mapTitleTextMesh.color.g, mapTitleTextMesh.color.b, 0);
        UIRef.UIActive = false;
        UIRef.UIMainPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.FindGameObjectWithTag("Player"))
        {
            playerCam = GameObject.FindGameObjectWithTag("Player").transform.GetComponent<Moba_Camera>();
        }
        if (!playerCam || UIRef.StartupPanel.activeSelf)
            return;

        if (playerCam.playerFaction != 0 && playerFaction == 0)
        {
            if (pathWaypoints.Count != 0)
            {
                //if faction is odd, reverse path
                if ((playerCam.playerFaction & 1) == 0)
                {
                    //Debug.Log("REVERSECAMPATH");
                    mapTitleTextMesh.transform.eulerAngles = mapTitleTextMesh.transform.eulerAngles + new Vector3(0, 180, 0);
                    pathWaypoints.Reverse();
                    smoothPath.m_Waypoints = pathWaypoints.ToArray();
                    smoothPath.InvalidateDistanceCache();
                }

                playerFaction = playerCam.playerFaction;

                directionSet = true;
            }

        }

        if(directionSet)
        {
            if (smoothPath.m_Waypoints.Length != 0)
            {
                if ((trackedDolly.m_PathPosition >= smoothPath.m_Waypoints.Length - pathEndOffset || Input.anyKeyDown) && !endPath)
                {
                    RevealVisionTrigger = true;
                    endPath = true;
                }
            }

            if (!endPath)
            {
                MoveCameraAlongDollyPath();
            }
            else
            {
                TransitionToPlayerCamera();
            }
        }
    }

    void MoveCameraAlongDollyPath()
    {
        if (trackedDolly.m_PathPosition >= mapTitleStartEndPoints.x && trackedDolly.m_PathPosition <= mapTitleStartEndPoints.y)
        {
            mapTitleTextMesh.color += new Color(0, 0, 0, Time.deltaTime * mapTitleLerpSpeed);
        }
        else if (mapTitleTextMesh.color.a != 0)
        {
            mapTitleTextMesh.color -= new Color(0, 0, 0, Time.deltaTime * mapTitleLerpSpeed);
        }

        //path movement lock player cam
        distancePercentage = trackedDolly.m_PathPosition / smoothPath.m_Waypoints.Length;
        cameraSpeed = speedCurve.Evaluate(distancePercentage);
        UIRef.UIMainPanel.SetActive(false);
        playerCam.settings.cameraLocked = true;
        trackedDolly.m_PathPosition += cameraSpeed * Time.deltaTime;
    }

    void TransitionToPlayerCamera()
    {
        if (mapTitleTextMesh.color.a != 0)
        {
            mapTitleTextMesh.color -= new Color(0, 0, 0, Time.deltaTime * mapTitleLerpSpeed);
        }
        //reach end of path start transition to mobaCam
        dollyCam.Priority = 9;
        //decalProjector.fadeFactor += decalProjectorLerpInSpeed * Time.deltaTime;

        if (UIDisplayDelatTime > 0)
            UIDisplayDelatTime -= Time.deltaTime;


        else if (!cameraBrain.IsBlending && !UIRef.UIActive)
        {
            UIRef.UIMainPanel.SetActive(true);
            UIRef.UIActive = true;
            Destroy(gameObject);
        }
    }
}

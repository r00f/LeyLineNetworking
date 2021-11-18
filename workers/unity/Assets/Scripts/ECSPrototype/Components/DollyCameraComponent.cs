using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using Generic;
using Unity.Entities;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using FMODUnity;
using LeyLineHybridECS;

public class DollyCameraComponent : MonoBehaviour
{

    [SerializeField]
    CinemachineVirtualCamera dollyCam;
    [SerializeField]
    CinemachineBrain cameraBrain;
    [SerializeField]
    float UIDisplayDelatTime;
    [SerializeField]
    float mapTitleLerpSpeed;
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
    bool skipPath;
    bool endPath;
    [HideInInspector]
    public bool RevealVisionTrigger = true;
    [SerializeField]
    VolumeProfile volumeProfile;
    Fog fog;
    [SerializeField]
    float fogDistanceLerpSpeed;
    GameObject playerGO;
    StudioListener playerListener;
    [SerializeField]
    StudioListener dollyCamListener;
    [SerializeField]
    GameObject AreaEmitter;

    //[SerializeField]
    //StudioListener playerCamStudioListener;

    // Start is called before the first frame update
    void Start()
    {
        if(volumeProfile.TryGet(out Fog f))
        {
            fog = f;
        }
        UIRef = FindObjectOfType<UIReferences>();
        UIRef.dollyCam = this;
        trackedDolly = dollyCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineTrackedDolly;
        smoothPath = trackedDolly.m_Path as CinemachineSmoothPath;
        pathWaypoints = smoothPath.m_Waypoints.ToList();
        trackedDolly.m_PathPosition = 0;
        dollyCam.Priority = 11;
        mapTitleTextMesh.color = new Color(mapTitleTextMesh.color.r, mapTitleTextMesh.color.g, mapTitleTextMesh.color.b, 0);
        UIRef.UIActive = false;
        SetUserInterfaceEnabled(false);
        UIRef.DollyPathCameraActive = true;
        fog.depthExtent.value = 64;
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerGO)
        {
            if(GameObject.FindGameObjectWithTag("Player"))
            {
                playerGO = GameObject.FindGameObjectWithTag("Player");
                playerCam = playerGO.transform.GetComponent<Moba_Camera>();
                playerListener = playerGO.transform.GetComponentInChildren<StudioListener>();
            }
        }
        else if (playerCam.playerFaction != 0 && !directionSet && !UIRef.StartupPanel.activeSelf)
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

        if(directionSet)
        {
            if (smoothPath.m_Waypoints.Length != 0)
            {
                if ((trackedDolly.m_PathPosition >= smoothPath.m_Waypoints.Length - pathEndOffset || Input.anyKeyDown || skipPath) && !endPath)
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
        //SetUserInterfaceEnabled(false);
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

        if (fog.depthExtent.value > 32)
            fog.depthExtent.value -= Time.deltaTime * fogDistanceLerpSpeed;

        if (UIDisplayDelatTime > 0)
            UIDisplayDelatTime -= Time.deltaTime;


        else if (!cameraBrain.IsBlending && !UIRef.UIActive)
        {
            //UIRef.EnvironmentBus.setPaused(false);
            //UIRef.SFXBus.setPaused(false);
            UIRef.DollyPathCameraActive = false;
            AreaEmitter.transform.SetParent(playerGO.transform);
            AreaEmitter.transform.localPosition = Vector3.zero;
            dollyCamListener.enabled = false;
            playerListener.enabled = true;
            mapTitleTextMesh.color = new Color(0, 0, 0, 0);
            SetUserInterfaceEnabled(true);
            UIRef.UIActive = true;
            Destroy(gameObject);
        }
    }

    public void SetUserInterfaceEnabled(bool enabled)
    {
        for (int i = 0; i < UIRef.Canvases.Count - 1; i++)
        {
            UIRef.Canvases[i].enabled = enabled;
        }
    }
}

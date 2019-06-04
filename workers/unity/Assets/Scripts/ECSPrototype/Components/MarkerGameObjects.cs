using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    public class MarkerGameObjects : MonoBehaviour
    {
        public GameObject HoveredMarker;
        public GameObject ClickedMarker;
        public GameObject ReachableMarker;
        public GameObject TargetMarker;
        public MeshRenderer TargetMarkerRenderer;
        public List<Color> TargetColors;
    }

}


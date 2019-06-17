using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unit;

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
        public EffectTypeEnum EffectType;
    }

}


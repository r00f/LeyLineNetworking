/// Credit Alastair Aitchison
/// Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/issues/123/uilinerenderer-issues-with-specifying

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/UI Line Connector")]
    [RequireComponent(typeof(UILineRenderer))]
    [ExecuteInEditMode]
    public class UILineConnector : MonoBehaviour
    {

        // The elements between which line segments should be drawn
        public RectTransform[] transforms;
        private Vector2[] previousPositions;
        private UILineRenderer lr;

        private void Awake()
        {
            lr = GetComponent<UILineRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            if (transforms == null || transforms.Length < 1)
            {
                return;
            }
            //Performance check to only redraw when the child transforms move
            if (previousPositions != null && previousPositions.Length == transforms.Length)
            {
                bool updateLine = false;
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (!updateLine && previousPositions[i] != transforms[i].anchoredPosition)
                    {
                        updateLine = true;
                    }
                }
                if (!updateLine) return;
            }

            Vector2[] points = new Vector2[transforms.Length];

            for (int i = 0; i < transforms.Length; i++)
            {
                points[i] = new Vector2(transforms[i].anchoredPosition.x, transforms[i].anchoredPosition.y);
            }

            // And assign the converted points to the line renderer
            lr.Points = points;
            lr.RelativeSize = false;
            lr.drivenExternally = true;

            previousPositions = new Vector2[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                previousPositions[i] = transforms[i].anchoredPosition;
            }
        }
    }
}

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeyLineHybridECS;
using UnityEditor;

public class ManalithGroup : MonoBehaviour
{
    [SerializeField]
    TerrainController terrainController;
    public List<ManalithInitializer> ManalithInitializers = new List<ManalithInitializer>();
    public List<Vector2> ManalithPairs = new List<Vector2>();

    public void ConnectManalithInitializerScripts()
    {
        for(int i = 0; i < ManalithPairs.Count; i++)
        {
            if(ManalithInitializers[(int)ManalithPairs[i].x] && ManalithInitializers[(int)ManalithPairs[i].y])
            {
                var manalithInitializer = ManalithInitializers[(int)ManalithPairs[i].x];
                var manalithInitializerToConnect = ManalithInitializers[(int)ManalithPairs[i].y];
                manalithInitializer.connectedManaLith = manalithInitializerToConnect;
            }
        }
    }

    public void ConnectManaliths()
    {
        if (!terrainController)
            terrainController = FindObjectOfType<TerrainController>();

        terrainController.leyLineCrackPositions.Clear();
        EditorUtility.SetDirty(terrainController);

        for (int i = 0; i < ManalithInitializers.Count; i++)
        {
            ManalithInitializers[i].UpdateLeyLineCircle();
        }

        for (int i = 0; i < ManalithInitializers.Count; i++)
        {
            ManalithInitializers[i].GeneratePathAndFinalize();
        }

        terrainController.UpdateLeyLineCracks();


    }

}
#endif

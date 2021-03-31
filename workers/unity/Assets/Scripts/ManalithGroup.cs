using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeyLineHybridECS;

public class ManalithGroup : MonoBehaviour
{
    [HideInInspector]
    public List<ManalithInitializer> ManalithInitializers = new List<ManalithInitializer>();

    [SerializeField]
    Transform ManalithObjects;
    

    public List<Vector2> ManalithPairs = new List<Vector2>();

    #if UNITY_EDITOR

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
        for(int i = 0; i < ManalithInitializers.Count; i++)
        {
            ManalithInitializers[i].ConnectManaLith();

            //only generate when first pressing connect
            if(!ManalithInitializers[i].meshGenerated)
            {
                ManalithInitializers[i].GenerateMeshes();
                ManalithInitializers[i].meshGenerated = true;
            }


            if(ManalithObjects)
            {
                ManalithInitializers[i].GetComponent<MeshColor>().ManaLithObject = ManalithObjects.GetChild(i).transform.GetComponent<ManalithObject>();
                ManalithInitializers[i].GetComponent<ManalithClientData>().NodeName = ManalithObjects.GetChild(i).transform.GetComponent<ManalithObject>().Name;
            }

        }
        /*
        foreach(ManalithInitializer init in ManalithInitializers)
        {
            init.ConnectManaLith();
            init.GenerateMeshes();
        }
        */
    }
    #endif
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateDisabled : MonoBehaviour
{

    [SerializeField]
    GameObject prefab;
    // Start is called before the first frame update
    void Start()
    {
        GameObject go = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        go.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

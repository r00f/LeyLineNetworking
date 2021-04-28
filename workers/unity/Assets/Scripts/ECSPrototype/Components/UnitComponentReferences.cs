using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider), typeof(LineRendererComponent), typeof(AnimatorComponent)),
RequireComponent(typeof(TeamColorMeshes), typeof(IsVisibleReferences), typeof(UnitDataSet)),
RequireComponent(typeof(UnitHeadUIReferences), typeof(AnimatedPortraitReference), typeof(UnitEffects))]
public class UnitComponentReferences : MonoBehaviour
{
    public CapsuleCollider CapsuleCollider;
    public LineRendererComponent LinerendererComp;
    public AnimatorComponent AnimatorComp;
    public TeamColorMeshes TeamColorMeshesComp;
    public IsVisibleReferences IsVisibleRefComp;
    public UnitDataSet BaseDataSetComp;
    public UnitHeadUIReferences HeadUIReferencesComp;
    public AnimatedPortraitReference AnimPortraitComp;
    public UnitEffects UnitEffectsComp;
    
    [Header("HealthBar")]
    public GameObject HealthbarPrefab;
    public GameObject HealthBarInstance;

    [Header("UnitVIsuals")]
    public GameObject SelectionCircleGO;
    public MeshRenderer SelectionMeshRenderer;

    public List<GameObject> SelectionGameObjects;

    public void InitializeComponentReferences()
    {
        CapsuleCollider = GetComponent<CapsuleCollider>();
        LinerendererComp = GetComponent<LineRendererComponent>();
        AnimatorComp = GetComponent<AnimatorComponent>();
        TeamColorMeshesComp = GetComponent<TeamColorMeshes>();
        IsVisibleRefComp = GetComponent<IsVisibleReferences>();
        BaseDataSetComp = GetComponent<UnitDataSet>();
        HeadUIReferencesComp = GetComponent<UnitHeadUIReferences>();
        AnimPortraitComp = GetComponent<AnimatedPortraitReference>();
        UnitEffectsComp = GetComponent<UnitEffects>();

        SelectionGameObjects.Clear();

        foreach(SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>())
            SelectionGameObjects.Add(s.gameObject);

        foreach (MeshRenderer m in GetComponentsInChildren<MeshRenderer>())
            SelectionGameObjects.Add(m.gameObject);

        if(SelectionCircleGO && SelectionGameObjects.Contains(SelectionCircleGO))
        {
            SelectionGameObjects.Remove(SelectionCircleGO);
        }

    }
}

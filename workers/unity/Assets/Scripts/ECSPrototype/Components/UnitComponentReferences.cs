using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

[RequireComponent(typeof(AnimatorComponent), typeof(UnitHeadUIReferences), typeof(TeamColorMeshes))]
public class UnitComponentReferences : MonoBehaviour
{
    public CapsuleCollider CapsuleCollider;
    public LineRendererComponent LinerendererComp;
    public AnimatorComponent AnimatorComp;
    public TeamColorMeshes TeamColorMeshesComp;
    public IsVisibleReferences IsVisibleRefComp;
    public Unit_BaseDataSet BaseDataSetComp;
    public UnitHeadUIReferences HeadUIReferencesComp;
    public AnimatedPortraitReference AnimPortraitComp;
    public UnitEffects UnitEffectsComp;
    

    [Header("HealthBar")]
    public GameObject HealthbarPrefab;
    public GameObject HealthBarInstance;

    [Header("UnitVIsuals")]
    public GameObject SelectionCircleGO;
    public MeshRenderer SelectionMeshRenderer;


    public void InitializeComponentReferences()
    {
        CapsuleCollider = GetComponent<CapsuleCollider>();
        LinerendererComp = GetComponent<LineRendererComponent>();
        AnimatorComp = GetComponent<AnimatorComponent>();
        TeamColorMeshesComp = GetComponent<TeamColorMeshes>();
        IsVisibleRefComp = GetComponent<IsVisibleReferences>();
        BaseDataSetComp = GetComponent<Unit_BaseDataSet>();
        HeadUIReferencesComp = GetComponent<UnitHeadUIReferences>();
        AnimPortraitComp = GetComponent<AnimatedPortraitReference>();
        UnitEffectsComp = GetComponent<UnitEffects>();
    }
}

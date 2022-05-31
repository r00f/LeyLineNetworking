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
    public UnitHeadUIReferences HeadUIRef;
    public AnimatedPortraitReference AnimPortraitComp;
    public UnitEffects UnitEffectsComp;

    [HideInInspector]
    public float CurrentMoveTime;
    [HideInInspector]
    public int CurrentMoveIndex;

    public Vector3 LastStationaryPosition;

    [Header("HealthBar")]
    public GameObject HealthbarPrefab;
    public GameObject HealthBarInstance;

    [Header("UnitVIsuals")]
    public GameObject SelectionCircleGO;
    public MeshRenderer SelectionMeshRenderer;

    public List<GameObject> SelectionGameObjects;
    public MeshMaterialComponent MeshMatComponent;

    public void InitializeComponentReferences()
    {
        CapsuleCollider = GetComponent<CapsuleCollider>();
        LinerendererComp = GetComponent<LineRendererComponent>();
        AnimatorComp = GetComponent<AnimatorComponent>();
        TeamColorMeshesComp = GetComponent<TeamColorMeshes>();
        IsVisibleRefComp = GetComponent<IsVisibleReferences>();
        BaseDataSetComp = GetComponent<UnitDataSet>();
        HeadUIRef = GetComponent<UnitHeadUIReferences>();
        AnimPortraitComp = GetComponent<AnimatedPortraitReference>();
        UnitEffectsComp = GetComponent<UnitEffects>();

        MeshMatComponent = GetComponentInChildren<MeshMaterialComponent>();

        if(MeshMatComponent)
        {
            MeshMatComponent.AllMesheRenderers.Clear();
            MeshMatComponent.Animator = AnimatorComp.Animator;
            foreach (SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                MeshMatComponent.AllMesheRenderers.Add(s);
            }

            foreach (MeshRenderer m in GetComponentsInChildren<MeshRenderer>())
            {
                MeshMatComponent.AllMesheRenderers.Add(m);
            }
        }

        SelectionGameObjects.Clear();

        foreach (SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            SelectionGameObjects.Add(s.gameObject);
        }

        foreach (MeshRenderer m in GetComponentsInChildren<MeshRenderer>())
        {
            SelectionGameObjects.Add(m.gameObject);
        }

        if(SelectionCircleGO && SelectionGameObjects.Contains(SelectionCircleGO))
        {
            SelectionGameObjects.Remove(SelectionCircleGO);
        }
    }
}

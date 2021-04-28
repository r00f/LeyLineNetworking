using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeyLineHybridECS;

public class AIUnitDataSet : MonoBehaviour
{
    public List<Vector2> MoveActionPrioList = new List<Vector2>();
    public List<Vector2> AttackActionPrioList = new List<Vector2>();
    public List<Vector2> UtitlityActionPrioList = new List<Vector2>();

    public AiUnitStateEnum AIUnitStateEnum;

    public List<Vector3> ActionTypeWeightsList = new List<Vector3>();


    public enum AiUnitStateEnum
    {
        idle = 0,
        aggroed = 1,
        chasing = 2,
        inAttackRange = 3,
        inDanger = 4,
        returning = 5
    }
}

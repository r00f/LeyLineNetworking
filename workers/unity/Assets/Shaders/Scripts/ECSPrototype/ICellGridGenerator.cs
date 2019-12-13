using System.Collections.Generic;
using UnityEngine;
namespace LeyLineHybridECS
{
    public abstract class ICellGridGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
        public Transform CellsParent;
        public abstract List<Cell> GenerateGrid();
#endif
    }
}


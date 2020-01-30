using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    public class PathLists : MonoBehaviour
    {
        public LineRenderer LineRenderer;
        public List<Cell> CurrentPath = new List<Cell>();
        public Dictionary<Cell, List<Cell>> CachedPaths = new Dictionary<Cell, List<Cell>>();
        public HashSet<Cell> PathsInRange = new HashSet<Cell>();
        public List<Cell> CellsInMovementRange = new List<Cell>();

        public bool EnergyRemoved;
    }
}

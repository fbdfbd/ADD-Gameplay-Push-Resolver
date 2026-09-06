using System.Collections.Generic;
using System.Numerics;
namespace ADD.Gameplay.PushResolver
{
    internal sealed class SpatialGrid
    {
        private readonly Dictionary<long, List<int>> _cells = new(512);
        private readonly Dictionary<int, CellRange> _occupiedCellsByItem = new(512);
        private readonly Stack<List<int>> _cellPool = new();
        private int[] _candidateStamps = new int[512];
        private int _candidateStamp;
        private float _cellSize = 1f;
        public void Configure(float cellSize, int itemCapacity)
        {
            _cellSize = cellSize > ScalarMath.Epsilon ? cellSize : 1f;
            EnsureCandidateCapacity(itemCapacity);
        }
        public void Clear()
        {
            foreach (var pair in _cells)
            {
                pair.Value.Clear();
                _cellPool.Push(pair.Value);
            }
            _cells.Clear();
            _occupiedCellsByItem.Clear();
        }
        public void Insert(int index, Vector2 min, Vector2 max)
        {
            Remove(index);
            EnsureCandidateCapacity(index + 1);
            var minX = ScalarMath.FloorToInt(min.X / _cellSize);
            var minY = ScalarMath.FloorToInt(min.Y / _cellSize);
            var maxX = ScalarMath.FloorToInt(max.X / _cellSize);
            var maxY = ScalarMath.FloorToInt(max.Y / _cellSize);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var key = PackCell(x, y);
                    GetCell(key).Add(index);
                }
            }
            _occupiedCellsByItem[index] = new CellRange(minX, minY, maxX, maxY);
        }
        public void Remove(int index)
        {
            if (!_occupiedCellsByItem.TryGetValue(index, out var occupiedCells))
                return;
            for (var y = occupiedCells.MinY; y <= occupiedCells.MaxY; y++)
            {
                for (var x = occupiedCells.MinX; x <= occupiedCells.MaxX; x++)
                {
                    var key = PackCell(x, y);
                    if (!_cells.TryGetValue(key, out var cell))
                        continue;
                    var slot = cell.IndexOf(index);
                    if (slot >= 0)
                    {
                        var lastSlot = cell.Count - 1;
                        cell[slot] = cell[lastSlot];
                        cell.RemoveAt(lastSlot);
                    }
                    if (cell.Count != 0)
                        continue;
                    _cells.Remove(key);
                    _cellPool.Push(cell);
                }
            }
            _occupiedCellsByItem.Remove(index);
        }
        public void CollectCandidates(
            int sourceIndex,
            Vector2 min,
            Vector2 max,
            List<int> results)
        {
            results.Clear();
            BeginCandidateQuery();
            var minX = ScalarMath.FloorToInt(min.X / _cellSize);
            var minY = ScalarMath.FloorToInt(min.Y / _cellSize);
            var maxX = ScalarMath.FloorToInt(max.X / _cellSize);
            var maxY = ScalarMath.FloorToInt(max.Y / _cellSize);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (!_cells.TryGetValue(PackCell(x, y), out var cell))
                        continue;
                    for (var i = 0; i < cell.Count; i++)
                    {
                        var candidateIndex = cell[i];
                        if (candidateIndex == sourceIndex
                            || _candidateStamps[candidateIndex] == _candidateStamp)
                        {
                            continue;
                        }
                        _candidateStamps[candidateIndex] = _candidateStamp;
                        results.Add(candidateIndex);
                    }
                }
            }
        }
        private List<int> GetCell(long key)
        {
            if (_cells.TryGetValue(key, out var cell))
                return cell;
            cell = _cellPool.Count > 0 ? _cellPool.Pop() : new List<int>(4);
            _cells[key] = cell;
            return cell;
        }
        private void BeginCandidateQuery()
        {
            _candidateStamp++;
            if (_candidateStamp != int.MaxValue)
                return;
            System.Array.Clear(_candidateStamps, 0, _candidateStamps.Length);
            _candidateStamp = 1;
        }
        private void EnsureCandidateCapacity(int requiredCapacity)
        {
            if (_candidateStamps.Length >= requiredCapacity)
                return;
            var capacity = _candidateStamps.Length;
            while (capacity < requiredCapacity)
                capacity *= 2;
            System.Array.Resize(ref _candidateStamps, capacity);
        }
        private static long PackCell(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
        private readonly struct CellRange
        {
            public CellRange(int minX, int minY, int maxX, int maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }
            public int MinX { get; }
            public int MinY { get; }
            public int MaxX { get; }
            public int MaxY { get; }
        }
    }
}

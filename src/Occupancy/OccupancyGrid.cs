using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal sealed class OccupancyGrid
    {
        internal const int RouteCount = 4;
        internal const int MaxCellCount = 262_144;

        internal bool[] _blockedCells = System.Array.Empty<bool>();
        internal int[] _visitedStamps = System.Array.Empty<int>();
        internal int[] _routeParentCells = System.Array.Empty<int>();
        internal int[] _routePathNextCells = System.Array.Empty<int>();
        internal int[] _routePathStamps = System.Array.Empty<int>();
        internal readonly int[] _preparedPathStamps = new int[RouteCount];
        internal int[] _bfsQueue = System.Array.Empty<int>();
        internal int[] _blockingItemByCell = System.Array.Empty<int>();
        internal int[] _blockingCountByCell = System.Array.Empty<int>();
        internal float[] _blockingDistanceByCell = System.Array.Empty<float>();
        internal Vector2[] _positionByItem = System.Array.Empty<Vector2>();
        internal Vector2[] _halfSizeByItem = System.Array.Empty<Vector2>();
        internal Vector2[] _marginByItem = System.Array.Empty<Vector2>();
        internal int[] _priorityByItem = System.Array.Empty<int>();
        internal bool[] _activeItems = System.Array.Empty<bool>();
        internal Vector2 _origin;
        internal Vector2 _pitch = Vector2.One;
        internal int _width;
        internal int _height;
        internal int _itemCount;
        internal int _visitStamp;
        internal int _routeCellCapacity;
        internal Vector2 _movingHalfSize;

        internal int CellCount => _width * _height;

        internal bool Configure(
            Vector2 minCenter,
            Vector2 maxCenter,
            Vector2 anchor,
            Vector2 pitch,
            int itemCapacity)
        {
            if (pitch.X <= ScalarMath.Epsilon
                || pitch.Y <= ScalarMath.Epsilon
                || minCenter.X > maxCenter.X
                || minCenter.Y > maxCenter.Y)
            {
                return false;
            }

            _pitch = pitch;
            var minGridX = ScalarMath.CeilToInt((minCenter.X - anchor.X) / pitch.X);
            var maxGridX = ScalarMath.FloorToInt((maxCenter.X - anchor.X) / pitch.X);
            var minGridY = ScalarMath.CeilToInt((minCenter.Y - anchor.Y) / pitch.Y);
            var maxGridY = ScalarMath.FloorToInt((maxCenter.Y - anchor.Y) / pitch.Y);
            var width = (long)maxGridX - minGridX + 1;
            var height = (long)maxGridY - minGridY + 1;
            if (width <= 0
                || height <= 0
                || width > MaxCellCount
                || height > MaxCellCount)
            {
                return false;
            }

            var cellCount = width * height;
            if (cellCount > MaxCellCount)
                return false;

            _width = (int)width;
            _height = (int)height;

            _origin = new Vector2(
                anchor.X + minGridX * pitch.X,
                anchor.Y + minGridY * pitch.Y);
            _itemCount = itemCapacity;
            EnsureCellCapacity(CellCount);
            EnsureItemCapacity(itemCapacity);
            System.Array.Clear(
                _preparedPathStamps,
                0,
                _preparedPathStamps.Length);
            System.Array.Clear(_blockedCells, 0, CellCount);
            System.Array.Clear(_blockingCountByCell, 0, CellCount);
            for (var i = 0; i < CellCount; i++)
            {
                _blockingItemByCell[i] = -1;
                _blockingDistanceByCell[i] = float.PositiveInfinity;
            }
            System.Array.Clear(_activeItems, 0, _activeItems.Length);
            for (var i = 0; i < itemCapacity; i++)
            {
                _activeItems[i] = false;
            }
            return true;
        }

        internal void SetMovingHalfSize(Vector2 halfSize)
        {
            _movingHalfSize = halfSize;
        }

        internal int GetNearestCell(Vector2 position)
        {
            if (_width <= 0 || _height <= 0)
                return -1;

            var x = ScalarMath.Clamp(
                ScalarMath.RoundToInt((position.X - _origin.X) / _pitch.X),
                0,
                _width - 1);
            var y = ScalarMath.Clamp(
                ScalarMath.RoundToInt((position.Y - _origin.Y) / _pitch.Y),
                0,
                _height - 1);
            return ToCell(x, y);
        }

        internal Vector2 GetCellCenter(int cell)
        {
            var x = cell % _width;
            var y = cell / _width;
            return new Vector2(
                _origin.X + x * _pitch.X,
                _origin.Y + y * _pitch.Y);
        }

        internal void InsertItem(
            int itemIndex,
            Vector2 position,
            Vector2 halfSize,
            Vector2 margin,
            int priority)
        {
            _positionByItem[itemIndex] = position;
            _halfSizeByItem[itemIndex] = halfSize;
            _marginByItem[itemIndex] = margin;
            _priorityByItem[itemIndex] = priority;
            _activeItems[itemIndex] = true;
            AddMovableBlockedCells(itemIndex);
        }

        internal void MarkBlocked(
            Vector2 obstacleCenter,
            Vector2 obstacleHalfSize,
            Vector2 movingHalfSize,
            Vector2 margin)
        {
            var extent = GetBlockingExtent(
                obstacleHalfSize,
                movingHalfSize,
                margin);
            var minX = ScalarMath.Clamp(
                ScalarMath.CeilToInt(
                    (obstacleCenter.X - extent.X - _origin.X) / _pitch.X),
                0,
                _width - 1);
            var maxX = ScalarMath.Clamp(
                ScalarMath.FloorToInt(
                    (obstacleCenter.X + extent.X - _origin.X) / _pitch.X),
                0,
                _width - 1);
            var minY = ScalarMath.Clamp(
                ScalarMath.CeilToInt(
                    (obstacleCenter.Y - extent.Y - _origin.Y) / _pitch.Y),
                0,
                _height - 1);
            var maxY = ScalarMath.Clamp(
                ScalarMath.FloorToInt(
                    (obstacleCenter.Y + extent.Y - _origin.Y) / _pitch.Y),
                0,
                _height - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                    _blockedCells[ToCell(x, y)] = true;
            }
        }

        internal bool IsFixedBlocked(int cell)
        {
            return cell >= 0
                   && cell < CellCount
                   && _blockedCells[cell];
        }

        private void AddMovableBlockedCells(int itemIndex)
        {
            var extent = GetBlockingExtent(
                _halfSizeByItem[itemIndex],
                _movingHalfSize,
                _marginByItem[itemIndex]);
            var position = _positionByItem[itemIndex];
            var minX = ScalarMath.Clamp(
                ScalarMath.CeilToInt((position.X - extent.X - _origin.X) / _pitch.X),
                0,
                _width - 1);
            var maxX = ScalarMath.Clamp(
                ScalarMath.FloorToInt((position.X + extent.X - _origin.X) / _pitch.X),
                0,
                _width - 1);
            var minY = ScalarMath.Clamp(
                ScalarMath.CeilToInt((position.Y - extent.Y - _origin.Y) / _pitch.Y),
                0,
                _height - 1);
            var maxY = ScalarMath.Clamp(
                ScalarMath.FloorToInt((position.Y + extent.Y - _origin.Y) / _pitch.Y),
                0,
                _height - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var cell = ToCell(x, y);
                    _blockingCountByCell[cell]++;
                    var distance = (GetCellCenter(cell) - position).LengthSquared();
                    var currentItem = _blockingItemByCell[cell];
                    if (distance < _blockingDistanceByCell[cell]
                        || ScalarMath.Approximately(
                            distance,
                            _blockingDistanceByCell[cell])
                        && (currentItem < 0
                            || _priorityByItem[itemIndex]
                            < _priorityByItem[currentItem]))
                    {
                        _blockingDistanceByCell[cell] = distance;
                        _blockingItemByCell[cell] = itemIndex;
                    }
                }
            }
        }

        internal int ToCell(int x, int y) => y * _width + x;

        private static Vector2 GetBlockingExtent(
            Vector2 obstacleHalfSize,
            Vector2 movingHalfSize,
            Vector2 margin)
        {
            return Vector2.Max(
                Vector2.Zero,
                obstacleHalfSize + movingHalfSize + margin
                - Vector2.One * 0.0001f);
        }

        private void EnsureCellCapacity(int required)
        {
            if (_blockedCells.Length >= required)
                return;

            var capacity = ScalarMath.NextPowerOfTwo(ScalarMath.Max(4, required));
            System.Array.Resize(ref _blockedCells, capacity);
            System.Array.Resize(ref _visitedStamps, capacity);
            _routeCellCapacity = capacity;
            System.Array.Resize(
                ref _routeParentCells,
                capacity * RouteCount);
            System.Array.Resize(
                ref _routePathNextCells,
                capacity * RouteCount);
            System.Array.Resize(
                ref _routePathStamps,
                capacity * RouteCount);
            System.Array.Resize(ref _bfsQueue, capacity);
            System.Array.Resize(ref _blockingItemByCell, capacity);
            System.Array.Resize(ref _blockingCountByCell, capacity);
            System.Array.Resize(ref _blockingDistanceByCell, capacity);
        }

        private void EnsureItemCapacity(int required)
        {
            if (_positionByItem.Length >= required)
                return;

            var capacity = ScalarMath.NextPowerOfTwo(ScalarMath.Max(4, required));
            System.Array.Resize(ref _positionByItem, capacity);
            System.Array.Resize(ref _halfSizeByItem, capacity);
            System.Array.Resize(ref _marginByItem, capacity);
            System.Array.Resize(ref _priorityByItem, capacity);
            System.Array.Resize(ref _activeItems, capacity);
        }
    }
}

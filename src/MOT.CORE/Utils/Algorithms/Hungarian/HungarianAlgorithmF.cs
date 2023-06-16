using MOT.CORE.Utils.DataStructs;
using System;

namespace MOT.CORE.Utils.Algorithms.Hungarian
{
    public class HungarianAlgorithmF : IHungarianAlgorithm
    {
        private readonly float[,] _costsMatrix;
        private readonly MaskValue[,] _masks;
        private readonly bool[] _crossedRows;
        private readonly bool[] _crossedColumns;
        private readonly int _height;
        private readonly int _width;

        private Point2 _pathStart;
        private Point2[] _path;
        private bool _resized;

        public HungarianAlgorithmF(float[,] costsMatrix)
        {
            if (costsMatrix == null)
                throw new ArgumentNullException();

            _costsMatrix = InitializeCostsMatrix(costsMatrix);
            _height = _costsMatrix.GetLength(0);
            _width = _costsMatrix.GetLength(1);
            _masks = new MaskValue[_height, _width];
            _crossedRows = new bool[_height];
            _crossedColumns = new bool[_width];
            _path = new Point2[_height * _width];
            _pathStart = new Point2(0, 0);
        }

        public int[] Solve()
        {
            for (int i = 0; i < _height; i++)
            {
                float minValue = float.MaxValue;

                for (int j = 0; j < _width; j++)
                    minValue = float.Min(minValue, _costsMatrix[i, j]);

                for (int j = 0; j < _width; j++)
                {
                    _costsMatrix[i, j] -= minValue;

                    if (_costsMatrix[i, j] == 0.0f && !_crossedRows[i] && !_crossedColumns[j])
                    {
                        _masks[i, j] = MaskValue.Star;
                        _crossedRows[i] = true;
                        _crossedColumns[j] = true;
                    }
                }
            }

            UncrossAll();

            State nextState = State.First;

            while (nextState != State.Final)
            {
                nextState = nextState switch
                {
                    State.First => ExecuteFirst(),
                    State.Second => ExecuteSecond(),
                    State.Third => ExecuteThird(),
                    State.Fourth => ExecuteFourth(),
                    _ => State.Final
                };
            }

            if (_resized)
                return AssignResized();

            return Assign();
        }

        private State ExecuteFirst()
        {
            CrossColumnsByMask();

            if (GetCrossedColumnsCount() == _height)
                return State.Final;

            return State.Second;
        }

        private State ExecuteSecond()
        {
            while (true)
            {
                Point2 zeroPoint = FindZero();

                if (zeroPoint == Point2.Undefined)
                    return State.Fourth;

                _masks[zeroPoint.X, zeroPoint.Y] = MaskValue.Prime;

                int starColumn = FindMaskValueColumn(zeroPoint.X, MaskValue.Star);

                if (starColumn == -1)
                {
                    _pathStart = zeroPoint;
                    return State.Third;
                }

                _crossedRows[zeroPoint.X] = true;
                _crossedColumns[starColumn] = false;
            }
        }

        private State ExecuteThird()
        {
            int pathIndex = 0;
            _path[0] = _pathStart;

            while (true)
            {
                int starRow = FindMaskValueRow(_path[pathIndex].Y, MaskValue.Star);

                if (starRow == -1)
                    break;

                pathIndex++;
                _path[pathIndex] = new Point2(starRow, _path[pathIndex - 1].Y);

                int primeColumn = FindMaskValueColumn(_path[pathIndex].X, MaskValue.Prime);

                pathIndex++;
                _path[pathIndex] = new Point2(_path[pathIndex - 1].X, primeColumn);
            }

            ReducePathMaskValues(pathIndex + 1);
            UncrossAll();
            UndefinePrimes();

            return State.First;
        }

        private State ExecuteFourth()
        {
            float minValue = CurrentCostMinimum();

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (_crossedRows[i])
                        _costsMatrix[i, j] += minValue;

                    if (!_crossedColumns[j])
                        _costsMatrix[i, j] -= minValue;
                }
            }

            return State.Second;
        }

        private float[,] InitializeCostsMatrix(float[,] baseCostsMatrix)
        {
            int height = baseCostsMatrix.GetLength(0);
            int width = baseCostsMatrix.GetLength(1);

            if (height > width)
            {
                float[,] costsMatrix = new float[width, height];

                for (int i = 0; i < height; i++)
                    for (int j = 0; j < width; j++)
                        costsMatrix[j, i] = baseCostsMatrix[i, j];

                _resized = true;

                return costsMatrix;
            }

            _resized = false;

            return (float[,])baseCostsMatrix.Clone();
        }

        private int[] Assign()
        {
            int[] assigned = new int[_height];

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (_masks[i, j] == MaskValue.Star)
                    {
                        assigned[i] = j;
                        break;
                    }
                }
            }

            return assigned;
        }

        private int[] AssignResized()
        {
            int[] assigned = new int[_width];

            for (int i = 0; i < assigned.Length; i++)
                assigned[i] = -1;

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (_masks[i, j] == MaskValue.Star)
                    {
                        assigned[j] = i;
                        break;
                    }
                }
            }

            return assigned;
        }

        private int GetCrossedColumnsCount()
        {
            int crossedColumntCount = 0;

            for (int i = 0; i < _crossedColumns.Length; i++)
                if (_crossedColumns[i])
                    crossedColumntCount++;

            return crossedColumntCount;
        }

        private void CrossColumnsByMask()
        {
            for (int i = 0; i < _height; i++)
                for (int j = 0; j < _width; j++)
                    if (_masks[i, j] == MaskValue.Star)
                        _crossedColumns[j] = true;
        }

        private Point2 FindZero()
        {
            for (int i = 0; i < _height; i++)
                for (int j = 0; j < _width; j++)
                    if (_costsMatrix[i, j] == 0.0f && !_crossedRows[i] && !_crossedColumns[j])
                        return new Point2(i, j);

            return Point2.Undefined;
        }

        private int FindMaskValueColumn(int row, MaskValue maskValue)
        {
            for (int i = 0; i < _width; i++)
                if (_masks[row, i] == maskValue)
                    return i;

            return -1;
        }

        private int FindMaskValueRow(int column, MaskValue maskValue)
        {
            for (int i = 0; i < _height; i++)
                if (_masks[i, column] == maskValue)
                    return i;

            return -1;
        }

        private float CurrentCostMinimum()
        {
            float minValue = float.MaxValue;

            for (int i = 0; i < _height; i++)
                for (int j = 0; j < _width; j++)
                    if (!_crossedRows[i] && !_crossedColumns[j])
                        minValue = float.Min(minValue, _costsMatrix[i, j]);

            return minValue;
        }

        private void ReducePathMaskValues(int pathLength)
        {
            for (int i = 0; i < pathLength; i++)
            {
                _masks[_path[i].X, _path[i].Y] = _masks[_path[i].X, _path[i].Y] switch
                {
                    MaskValue.Star => MaskValue.Undefined,
                    MaskValue.Prime => MaskValue.Star,
                    _ => MaskValue.Undefined
                };
            }
        }

        private void UncrossAll()
        {
            int minHeightWidth = int.Min(_height, _width);

            int i = 0;

            for (; i < minHeightWidth; i++)
            {
                _crossedRows[i] = false;
                _crossedColumns[i] = false;
            }

            bool[] lastCrossed = _crossedColumns;

            if (minHeightWidth == _width)
                lastCrossed = _crossedRows;

            int lastLength = int.Max(_height, _width);

            for (; i < lastLength; i++)
                lastCrossed[i] = false;
        }

        private void UndefinePrimes()
        {
            for (int i = 0; i < _height; i++)
                for (int j = 0; j < _width; j++)
                    if (_masks[i, j] == MaskValue.Prime)
                        _masks[i, j] = MaskValue.Undefined;
        }

        private enum State : byte
        {
            First,
            Second,
            Third,
            Fourth,
            Final
        }

        private enum MaskValue : byte
        {
            Undefined = 0,
            Star,
            Prime
        }
    }
}

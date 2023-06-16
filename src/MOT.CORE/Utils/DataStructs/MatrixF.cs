using System;
using System.Linq;

namespace MOT.CORE.Utils.DataStructs
{
    public class MatrixF
    {
        private readonly float[,] _values;

        public MatrixF(float[,] values)
        {
            _values = values;
            Rows = _values.GetLength(0);
            Columns = _values.GetLength(1);
        }

        public MatrixF(int[,] values) : this(values.GetLength(0), values.GetLength(1))
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Columns; col++)
                {
                    _values[row, col] = values[row, col];
                }
            }
        }

        public MatrixF(int rows, int columns) : this(new float[rows, columns]) { }

        public int Rows { get; private set; }

        public int Columns { get; private set; }

        public MatrixF Transposed
        {
            get
            {
                var result = new float[Columns, Rows];

                for (var row = 0; row < Rows; row++)
                {
                    for (var col = 0; col < Columns; col++)
                    {
                        result[col, row] = _values[row, col];
                    }
                }

                return new MatrixF(result);
            }
        }

        public MatrixF Inverted
        {
            get
            {
                var (lu, indices, d) = GetDecomposition();
                var result = new float[Rows, Columns];

                for (var col = 0; col < Columns; col++)
                {
                    var column = new float[Columns];

                    column[col] = 1.0f;

                    var x = BackSubstition(lu, indices, column);

                    for (var row = 0; row < Rows; row++)
                    {
                        result[row, col] = x[row];
                    }
                }

                return new MatrixF(result);
            }
        }

        public static MatrixF operator +(MatrixF first, MatrixF second)
        {
            var result = new float[first.Rows, first.Columns];

            for (var row = 0; row < first.Rows; row++)
            {
                for (var col = 0; col < first.Columns; col++)
                {
                    result[row, col] = first._values[row, col] + second._values[row, col];
                }
            }

            return new MatrixF(result);
        }

        public static MatrixF operator -(MatrixF first, MatrixF second)
        {
            var result = new float[first.Rows, first.Columns];

            for (var row = 0; row < first.Rows; row++)
            {
                for (var col = 0; col < first.Columns; col++)
                {
                    result[row, col] = first._values[row, col] - second._values[row, col];
                }
            }

            return new MatrixF(result);
        }

        public static MatrixF operator *(float scalar, MatrixF matrix)
        {
            var result = new float[matrix.Rows, matrix.Columns];

            for (var row = 0; row < matrix.Rows; row++)
            {
                for (var col = 0; col < matrix.Columns; col++)
                {
                    result[row, col] = matrix._values[row, col] * scalar;
                }
            }

            return new MatrixF(result);
        }

        public static MatrixF operator *(MatrixF matrix, float scalar)
        {
            return scalar * matrix;
        }

        public static MatrixF operator *(MatrixF first, MatrixF second)
        {
            var result = new float[first.Rows, second.Columns];
            var rows = result.GetLength(0);
            var cols = result.GetLength(1);

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    result[row, col] = first.Row(row).Dot(second.Column(col));
                }
            }

            return new MatrixF(result);
        }

        public static MatrixF Identity(int size)
        {
            var identity = new float[size, size];

            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    identity[row, col] = row == col ? 1.0f : 0f;
                }
            }

            return new MatrixF(identity);
        }

        public static bool TryCopyValues(MatrixF from, MatrixF target)
        {
            if (from.Rows != target.Rows || from.Columns != target.Columns)
                return false;

            for (int i = 0; i < from.Rows; i++)
                for (int j = 0; j < from.Columns; j++)
                    target._values[i, j] = from._values[i, j];

            return true;
        }

        public Vector Dot(Vector vector)
        {
            return new Vector(Enumerable.Range(0, Rows).Select(Row).Select(row => row.Dot(vector)).ToArray());
        }

        public Vector Row(int index)
        {
            return new Vector(Enumerable.Range(0, Columns).Select(col => _values[index, col]).ToArray());
        }

        public Vector Column(int index)
        {
            return new Vector(Enumerable.Range(0, Rows).Select(row => _values[row, index]).ToArray());
        }

        private float[] BackSubstition(float[,] lu, int[] indices, float[] b)
        {
            var x = (float[])b.Clone();
            var ii = 0;
            for (var row = 0; row < Rows; row++)
            {
                var ip = indices[row];
                var sum = x[ip];

                x[ip] = x[row];

                if (ii == 0)
                {
                    for (var col = ii; col <= row - 1; col++)
                    {
                        sum -= lu[row, col] * x[col];
                    }
                }
                else if (sum == 0)
                {
                    ii = row;
                }

                x[row] = sum;
            }

            for (var row = Rows - 1; row >= 0; row--)
            {
                var sum = x[row];
                for (var col = row + 1; col < Columns; col++)
                {
                    sum -= lu[row, col] * x[col];
                }

                x[row] = sum / lu[row, row];
            }

            return x;
        }

        private (float[,] Result, int[] Indices, float D) GetDecomposition()
        {
            var max_row = 0;
            var vv = Enumerable.Range(0, Rows).Select(row => 1.0d / Enumerable.Range(0, Columns).Select(col => Math.Abs(_values[row, col])).Max()).ToArray();
            var result = (float[,])_values.Clone();
            var index = new int[Rows];
            var d = 1.0f;

            for (var col = 0; col < Columns; col++)
            {
                for (var row = 0; row < col; row++)
                {
                    var sum = result[row, col];
                    for (var k = 0; k < row; k++)
                    {
                        sum -= result[row, k] * result[k, col];
                    }

                    result[row, col] = sum;
                }

                var max = 0d;
                for (var row = col; row < Rows; row++)
                {
                    var sum = result[row, col];
                    for (var k = 0; k < col; k++)
                    {
                        sum -= result[row, k] * result[k, col];
                    }

                    result[row, col] = sum;

                    var tmp = vv[row] * Math.Abs(sum);

                    if (tmp >= max)
                    {
                        max = tmp;
                        max_row = row;
                    }
                }

                if (col != max_row)
                {
                    for (var k = 0; k < Rows; k++)
                    {
                        var tmp = result[max_row, k];
                        result[max_row, k] = result[col, k];
                        result[col, k] = tmp;
                    }

                    d = -d;
                    vv[max_row] = vv[col];
                }

                index[col] = max_row;

                if (col != Rows - 1)
                {
                    var tmp = 1.0f / result[col, col];
                    for (var row = col + 1; row < Rows; row++)
                    {
                        result[row, col] *= tmp;
                    }
                }
            }

            return (result, index, d);
        }
    }
}

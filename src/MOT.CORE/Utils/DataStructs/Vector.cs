using System;
using System.Drawing;
using System.Linq;

namespace MOT.CORE.Utils.DataStructs
{
    public class Vector
    {
        private readonly float[] _values;
        private float _magnitude;

        public static Vector Empty = new Vector(new float[0]);

        public Vector(params float[] values)
        {
            _values = values;
            _magnitude = GetMagnitude();
        }

        public Vector(int size)
        {
            _values = new float[size];
        }

        public Vector(ref float[] data)
        {
            _values = data;
            _magnitude = GetMagnitude();
        }

        public int Length => _values.Length;

        public float Magnitude => _magnitude;

        public float this[int index] => _values[index];

        public static Vector operator -(Vector first, Vector second)
        {
            return new Vector(first._values.Zip(second._values, (a, b) => a - b).ToArray());
        }

        public static Vector operator +(Vector first, Vector second)
        {
            return new Vector(first._values.Zip(second._values, (a, b) => a + b).ToArray());
        }

        public static Vector operator /(Vector first, float second)
        {
            return new Vector(first._values.Select(p => p / second).ToArray());
        }

        public static Vector operator *(Vector first, float second)
        {
            return new Vector(first._values.Select(p => p * second).ToArray());
        }

        public float Dot(Vector other)
        {
            return _values.Zip(other._values, (a, b) => a * b).Sum();
        }

        public PointF AsPointF()
        {
            const int pointDimensions = 2;

            if (_values.Length > pointDimensions)
                throw new Exception("Vector must be two dimensional.");

            return new PointF(_values[0], _values[1]);
        }

        public void Normalize()
        {
            for (int i = 0; i < _values.Length; i++)
                _values[i] = _values[i] / _magnitude;

            _magnitude = GetMagnitude();
        }

        public Vector Append(params float[] extraElements)
        {
            return new Vector(_values.Concat(extraElements).ToArray());
        }

        public float[] ToArray()
        {
            return _values.ToArray();
        }

        private float GetMagnitude()
        {
            double length = 0;

            foreach (float value in _values)
                length += value * value;

            return (float)Math.Sqrt(length);
        }
    }
}

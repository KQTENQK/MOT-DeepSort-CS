using System;

namespace MOT.CORE.Utils.DataStructs
{
    public struct Point2
    {
        public readonly int X;
        public readonly int Y;

        public static readonly Point2 Undefined = new Point2(-1, -1);

        public Point2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(Point2 first, Point2 second)
        {
            return first.X == second.X && first.Y == second.Y;
        }

        public static bool operator !=(Point2 first, Point2 second)
        {
            return first.X != second.X || first.Y != second.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is Point2 point && X == point.X && Y == point.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}

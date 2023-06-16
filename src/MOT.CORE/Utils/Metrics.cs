using MOT.CORE.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace MOT.CORE.Utils
{
    public static class Metrics
    {
        public static float OverlapArea(RectangleF first, RectangleF second) => RectangleF.Intersect(first, second).Area();

        public static float UnionArea(RectangleF first, RectangleF second) => first.Area() + second.Area() - OverlapArea(first, second);

        public static float IntersectionOverUnionLoss(RectangleF first, RectangleF second) => 1 - IntersectionOverUnion(first, second);

        public static float GeneralizedIntersectionOverUnionLoss(RectangleF first, RectangleF second) => 1 - GeneralizedIntersectionOverUnion(first, second);

        public static float CosineSimilarity(DataStructs.Vector first, DataStructs.Vector second) => first.Dot(second) / (first.Magnitude * second.Magnitude);

        public static float CosineDistance(DataStructs.Vector first, DataStructs.Vector second) => 1 - CosineSimilarity(first, second);

        public static float IntersectionOverUnionDistanceLoss(RectangleF first, RectangleF second) => 1 - IntersectionOverUnionDistance(first, second);

        public static DataStructs.Vector BoundsDeltaVelocity(RectangleF first, RectangleF second)
        {
            DataStructs.Vector firstCenter = new DataStructs.Vector(first.X + first.Width / 2, first.Y + first.Height / 2);
            DataStructs.Vector secondCenter = new DataStructs.Vector(second.X + second.Width / 2, second.Y + second.Height / 2);
            DataStructs.Vector delta = firstCenter - secondCenter;
            float magnitude = delta.Magnitude + float.Epsilon;

            return delta / magnitude;
        }

        public static float MedianCosineDistance(DataStructs.Vector[] knownAppearances, int appearanceLength, DataStructs.Vector appearance)
        {
            float medianAppearance = 0;

            for (int i = 0; i < appearanceLength; i++)
                medianAppearance += CosineDistance(knownAppearances[i], appearance);

            return medianAppearance / knownAppearances.Length;
        }

        public static float IntersectionOverUnionDistance(RectangleF first, RectangleF second)
        {
            Vector2 topLeft = new Vector2(float.Min(first.X, second.X), float.Min(first.Y, second.Y));
            Vector2 bottomRight = new Vector2(float.Max(first.X, second.X), float.Max(first.Y, second.Y));

            float crossedDistance = Vector2.DistanceSquared(topLeft, bottomRight);

            Vector2 firstCenter = new Vector2(first.X + first.Width / 2, first.Y + first.Height / 2);
            Vector2 secondCenter = new Vector2(second.X + second.Width / 2, second.Y + second.Height / 2);

            float centerDistance = Vector2.DistanceSquared(firstCenter, secondCenter);

            float overlapArea = RectangleF.Intersect(first, second).Area();
            float unionArea = first.Area() + second.Area() - overlapArea;

            if (unionArea < float.Epsilon)
                return 0;

            if (crossedDistance == 0)
                return overlapArea / unionArea;

            return overlapArea / unionArea - centerDistance / crossedDistance;
        }

        public static float IntersectionOverUnion(RectangleF first, RectangleF second)
        {
            float overlapArea = RectangleF.Intersect(first, second).Area();
            float unionArea = first.Area() + second.Area() - overlapArea;

            if (unionArea < float.Epsilon)
                return 0;

            return overlapArea / unionArea;
        }

        public static float GeneralizedIntersectionOverUnion(RectangleF first, RectangleF second)
        {
            float IoU = IntersectionOverUnion(first, second);
            float encompassingArea = RectangleF.Union(first, second).Area();
            float unionArea = UnionArea(first, second);

            if (unionArea < float.Epsilon)
                return IoU;

            return IoU - ((encompassingArea - unionArea) / encompassingArea);
        }
    }
}

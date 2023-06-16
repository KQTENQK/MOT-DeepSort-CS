using System;

namespace MOT.CORE.Utils.Algorithms.Hungarian
{
    public sealed class HungarianAlgorithm<T>
    {
        private readonly IHungarianAlgorithm _hungarialAlgorithm;

        public HungarianAlgorithm(T[,] costsMatrix)
        {
            if (costsMatrix == null)
                throw new ArgumentNullException();

            Type TType = costsMatrix.GetType();

            if (TType == typeof(float[,]))
                _hungarialAlgorithm = new HungarianAlgorithmF(costsMatrix as float[,]);
            else if (TType == typeof(double[,]))
                _hungarialAlgorithm = new HungarianAlgorithmD(costsMatrix as double[,]);
            else if (TType == typeof(int[,]))
                _hungarialAlgorithm = new HungarianAlgorithmInt(costsMatrix as int[,]);
            else
                throw new Exception("Only double, float, int cost matrix values available.");
        }

        public int[] Solve()
        {
            return _hungarialAlgorithm.Solve();
        }
    }
}

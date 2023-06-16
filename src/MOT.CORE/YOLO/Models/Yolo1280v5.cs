namespace MOT.CORE.YOLO.Models
{
    public class Yolo1280v5 : IYoloModel
    {
        public int Width { get; } = 1280;
        public int Height { get; } = 1280;
        public int Depth { get; } = 3;
        public int Dimensions { get; } = 85;
        public int[] Strides { get; } = new int[] { 8, 16, 32, 64 };

        public int[][][] Anchors { get; } = new int[][][]
        {
            new int[][] { new int[] { 019, 027 }, new int[] { 044, 040 }, new int[] { 038, 094 } },
            new int[][] { new int[] { 096, 068 }, new int[] { 086, 152 }, new int[] { 180, 137 } },
            new int[][] { new int[] { 140, 301 }, new int[] { 303, 264 }, new int[] { 238, 542 } },
            new int[][] { new int[] { 436, 615 }, new int[] { 739, 380 }, new int[] { 925, 792 } }
        };

        public int[] Shapes { get; } = new int[] { 160, 80, 40, 20 };
        public float Confidence { get; } = 0.20f;
        public float MulConfidence { get; } = 0.25f;
        public float Overlap { get; } = 0.45f;
        public int Channels { get; } = 3;
        public int BatchSize { get; } = 1;
        public string[] Outputs { get; set; } = new[] { "output" };
        public string Input { get; } = "images";
    }
}

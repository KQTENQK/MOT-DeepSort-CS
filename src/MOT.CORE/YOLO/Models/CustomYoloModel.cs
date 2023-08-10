namespace MOT.CORE.YOLO.Models
{
    public class CustomYoloModel : IYoloModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public int Dimensions { get; set; }
        public int[] Strides { get; set; }
        public int[][][] Anchors { get; set; }
        public int[] Shapes { get; set; }
        public float Confidence { get; set; }
        public float MulConfidence { get; set; }
        public float Overlap { get; set; }
        public int Channels { get; set; }
        public int BatchSize { get; set; }
        public string[] Outputs { get; set; }
        public string Input { get; set; }
    }
}

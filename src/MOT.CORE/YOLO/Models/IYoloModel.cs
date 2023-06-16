namespace MOT.CORE.YOLO.Models
{
    public interface IYoloModel
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int Depth { get; }
        public abstract int Dimensions { get; }
        public abstract int[] Strides { get; }
        public abstract int[][][] Anchors { get; }
        public abstract int[] Shapes { get; }
        public abstract float Confidence { get; }
        public abstract float MulConfidence { get; }
        public abstract float Overlap { get; }
        public abstract int Channels { get; }
        public abstract int BatchSize { get; }
        public abstract string[] Outputs { get; }
        public abstract string Input { get; }
    }
}

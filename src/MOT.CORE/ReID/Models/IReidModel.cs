namespace MOT.CORE.ReID.Models
{
    public interface IReidModel
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int BatchSize { get; }
        public abstract int Channels { get; }
        public abstract int OutputVectorSize { get; }
        public abstract string[] Outputs { get; set; }
        public abstract string Input { get; }
    }
}

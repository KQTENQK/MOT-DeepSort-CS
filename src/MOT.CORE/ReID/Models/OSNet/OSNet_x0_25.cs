namespace MOT.CORE.ReID.Models.OSNet
{
    public class OSNet_x0_25 : IReidModel
    {
        public int Width { get; } = 128;
        public int Height { get; } = 256;
        public int BatchSize { get; } = 16;
        public int Channels { get; } = 3;
        public int OutputVectorSize { get; } = 512;
        public string[] Outputs { get; set; } = new[] { "output" };
        public string Input { get; } = "input";
    }
}

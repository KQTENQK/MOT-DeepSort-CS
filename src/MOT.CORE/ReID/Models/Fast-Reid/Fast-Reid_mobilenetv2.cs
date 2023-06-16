namespace MOT.CORE.ReID.Models.Fast_Reid
{
    public class Fast_Reid_mobilenetv2 : IReidModel
    {
        public int Width { get; } = 128;
        public int Height { get; } = 256;
        public int BatchSize { get; } = 32;
        public int Channels { get; } = 3;
        public int OutputVectorSize { get; } = 1280;
        public string[] Outputs { get; set; } = new[] { "485" };
        public string Input { get; } = "batched_inputs.1";
    }
}

namespace MOT.CORE.ReID.Models
{
    public class CustomReidModel : IReidModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BatchSize { get; set; }
        public int Channels { get; set; }
        public int OutputVectorSize { get; set; }
        public string[] Outputs { get; set; }
        public string Input { get; set; }
    }
}

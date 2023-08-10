using CommandLine;

namespace MOT
{
    public sealed class CommandLineOptions
    {
        [Option('s', "source", HelpText = "Source video file path.", Required = true)]
        public string? SourceFilePath { get; set; }

        [Option('t', "target", HelpText = "Target video file path.", Required = true)]
        public string? TargetFilePath { get; set; }

        [Option('d', "detector", HelpText = "Detector net file path.", Required = true)]
        public string? DetectorFilePath { get; set; }

        [Option('m', "matcher", HelpText = "Matcher type:\n\t0 for DeepSort\n\t1 for Sort\n\t2 for Deep", Required = true)]
        public MatcherType? MatcherType { get; set; }

        [Option('y', "yver", HelpText = "Yolo model:\n\t0 for 1280 resolution\n\t1 for 640 resolution", Required = true)]
        public YoloVersion? YoloVersion { get; set; }

        [Option('v', "aver", HelpText = "Appearance model:\n\t0 for OSNet\n\t1 Fast-Reid", Required = false)]
        public AppearanceExtractorVersion? AppearanceExtractorVersion { get; set; }

        [Option('a', "appearance", HelpText = "Appearance extractor net file path.", Required = false)]
        public string? AppearanceExtractorFilePath { get; set; }

        [Option("fps", HelpText = "Target video fps.", Required = false)]
        public float? FPS { get; set; }

        [Option("threshold", HelpText = "Defines treshold for matcher.", Required = false)]
        public float? Threshold { get; set; }

        [Option("aweight", HelpText = "Defines appearance weight for deepsort matcher.", Required = false)]
        public float? AppearanceWeight { get; set; }

        [Option("asmooth", HelpText = "Defines appearance smooth weight for deepsort matcher.", Required = false)]
        public float? SmoothAppearanceWeight { get; set; }

        [Option("streak", HelpText = "Defines min streak to reidentify person.", Required = false)]
        public int? MinStreak { get; set; }

        [Option("misses", HelpText = "Defines max misses to lose indentification.", Required = false)]
        public int? MaxMisses { get; set; }

        [Option("fsmooth", HelpText = "Defines passed frames for smooth weight to be applied.", Required = false)]
        public int? FramesToAppearanceSmooth { get; set; }

        [Option("acount", HelpText = "Defines appearance extractors in memory count.", Required = false)]
        public int? ExtractorsInMemoryCount { get; set; }

        [Option('a', "conf", HelpText = "Defines target people detection confidence([0-1]).", Required = false)]
        public float? TargetConfidence { get; set; }
    }

    public enum MatcherType : byte
    {
        DeepSort = 0,
        Sort,
        Deep
    }

    public enum YoloVersion : byte
    {
        Yolo1280 = 0,
        Yolo640
    }

    public enum AppearanceExtractorVersion : byte
    {
        OSNet = 0,
        FastReid
    }
}

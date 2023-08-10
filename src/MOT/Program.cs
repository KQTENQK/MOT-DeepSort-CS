#pragma warning disable CA1416

using Emgu.CV;
using MOT.CORE.Matchers.Abstract;
using MOT.CORE.ReID;
using MOT.CORE.ReID.Models.OSNet;
using MOT.CORE.YOLO;
using MOT.CORE.YOLO.Models;
using System.Drawing;
using MOT.CORE.Matchers.SORT;
using MOT.CORE.Matchers.Deep;
using CommandLine;
using MOT.CORE.ReID.Models.Fast_Reid;

namespace MOT
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            CommandLineOptions options = result.Value;

            if (options is null ||
                string.IsNullOrEmpty(options.SourceFilePath) ||
                string.IsNullOrEmpty(options.TargetFilePath) ||
                string.IsNullOrEmpty(options.DetectorFilePath) ||
                options.MatcherType is null ||
                options.YoloVersion is null)
            {
                Console.WriteLine("Required arguments were not passed.");

                return;
            }

            VideoCapture videoCapture = new VideoCapture(options.SourceFilePath);

            double targetFps = options.FPS ?? videoCapture.Get(Emgu.CV.CvEnum.CapProp.Fps);
            int width = videoCapture.Width;
            int height = videoCapture.Height;

            VideoWriter videoWriter = new VideoWriter(options.TargetFilePath, -1, targetFps, new Size(width, height), true);

            Matcher matcher = ConstructMatcherFromOptions(options);
            float targetConfidence = float.Clamp(options.TargetConfidence ?? 0.0f, 0.0f, 1.0f);

            Mat readBuffer = new Mat();

            videoCapture.Read(readBuffer);

            while (readBuffer.IsEmpty == false)
            {
                Bitmap frame = readBuffer.ToBitmap();

                IReadOnlyList<ITrack> tracks = matcher.Run(frame, targetConfidence, DetectionObjectType.Person);

                DrawTracks(frame, tracks);

                videoWriter.Write(frame.ToImage<Emgu.CV.Structure.Bgr, byte>());
                videoCapture.Read(readBuffer);
            }

            matcher.Dispose();
            videoWriter.Dispose();
        }

        private static Matcher ConstructMatcherFromOptions(CommandLineOptions options)
        {
            IPredictor predictor = ConstructPredictorFromOptions(options);

            Matcher matcher = options.MatcherType switch
            { 
                MatcherType.DeepSort => new DeepSortMatcher(predictor,
                    ConstructAppearanceExtractorFromOptions(options),
                    options.AppearanceWeight ?? 0.775f,
                    options.Threshold ?? 0.5f,
                    options.MaxMisses ?? 50,
                    options.FramesToAppearanceSmooth ?? 40,
                    options.SmoothAppearanceWeight ?? 0.875f,
                    options.MinStreak ?? 8),

                MatcherType.Sort => new SortMatcher(predictor,
                    options.Threshold ?? 0.3f,
                    options.MaxMisses ?? 15,
                    options.MinStreak ?? 3),

                MatcherType.Deep => new DeepMatcher(predictor,
                    ConstructAppearanceExtractorFromOptions(options),
                    options.Threshold ?? 0.875f,
                    options.MaxMisses ?? 10,
                    options.MinStreak ?? 4),

                _ => throw new Exception("Matcher cannot be constructed.")
            };

            return matcher;
        }

        private static IPredictor ConstructPredictorFromOptions(CommandLineOptions options)
        {
            if (string.IsNullOrEmpty(options.DetectorFilePath))
                throw new ArgumentNullException($"{nameof(options.DetectorFilePath)} was undefined.");

            IPredictor predictor = options.YoloVersion switch
            {
                YoloVersion.Yolo640 => new YoloScorer<Yolo640v5>(File.ReadAllBytes(options.DetectorFilePath)),
                YoloVersion.Yolo1280 => new YoloScorer<Yolo1280v5>(File.ReadAllBytes(options.DetectorFilePath)),
                _ => throw new Exception("Yolo predictor cannot be constructed.")
            };

            return predictor;
        }

        private static IAppearanceExtractor ConstructAppearanceExtractorFromOptions(CommandLineOptions options)
        {
            if (string.IsNullOrEmpty(options.AppearanceExtractorFilePath))
                throw new ArgumentNullException($"{nameof(options.AppearanceExtractorFilePath)} was undefined.");

            if (options.AppearanceExtractorVersion == null)
                throw new ArgumentNullException($"{nameof(options.AppearanceExtractorVersion)} was undefined.");

            const int DefaultExtractorsCount = 4;

            IAppearanceExtractor appearanceExtractor = options.AppearanceExtractorVersion switch
            {
                AppearanceExtractorVersion.OSNet => new ReidScorer<OSNet_x1_0>(File.ReadAllBytes(options.AppearanceExtractorFilePath),
                    options.ExtractorsInMemoryCount ?? DefaultExtractorsCount),
                AppearanceExtractorVersion.FastReid => new ReidScorer<Fast_Reid_mobilenetv2>(File.ReadAllBytes(options.AppearanceExtractorFilePath),
                    options.ExtractorsInMemoryCount ?? DefaultExtractorsCount),
                _ => throw new Exception("Appearance extractor cannot be constructed.")
            };

            return appearanceExtractor;
        }

        private static void DrawTracks(Bitmap frame, IReadOnlyList<ITrack> tracks)
        {
            Graphics graphics = Graphics.FromImage(frame);

            foreach (ITrack track in tracks)
            {
                const int penSize = 4;
                const float yBoundingBoxIntent = 45f;
                const float xNumberIntent = 4f;
                const int fontSize = 44;

                graphics.DrawRectangles(new Pen(track.Color, penSize),
                    new[] { track.CurrentBoundingBox });

                graphics.FillRectangle(new SolidBrush(track.Color),
                    new RectangleF(track.CurrentBoundingBox.X - (penSize / 2), track.CurrentBoundingBox.Y - yBoundingBoxIntent,
                        track.CurrentBoundingBox.Width + penSize, yBoundingBoxIntent - (penSize / 2)));

                (float x, float y) = (track.CurrentBoundingBox.X - xNumberIntent, track.CurrentBoundingBox.Y - yBoundingBoxIntent);

                graphics.DrawString($"{track.Id}",
                    new Font("Consolas", fontSize, GraphicsUnit.Pixel), new SolidBrush(Color.FromArgb((0xFF << 24) | 0xDDDDDD)),
                    new PointF(x, y));
            }

            graphics.Dispose();
        }
    }
}
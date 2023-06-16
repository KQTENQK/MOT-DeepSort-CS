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

namespace MOT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            VideoCapture videoCapture = new VideoCapture("../../../Assets/Input/3.mp4");

            double targetFps = videoCapture.Get(Emgu.CV.CvEnum.CapProp.Fps);
            int width = videoCapture.Width;
            int height = videoCapture.Height;

            VideoWriter videoWriter = new VideoWriter("../../../Assets/Output/test3.mp4", -1, targetFps, new Size(width, height), true);

            var predictor = new YoloScorer<Yolo640v5>(File.ReadAllBytes("../../../Assets/Models/Yolo/yolo640v5.onnx"));
            var appearanceExtractor = new ReidScorer<OSNet_x1_0>(File.ReadAllBytes("../../../Assets/Models/Reid/osnet_x1_0_msmt17.onnx"), 3);

            var matcher = new DeepSortMatcher(predictor, appearanceExtractor);

            Mat readBuffer = new Mat();
            videoCapture.Read(readBuffer);

            while (readBuffer.IsEmpty == false)
            {
                Bitmap frame = readBuffer.ToBitmap();

                IReadOnlyList<ITrack> tracks = matcher.Run(frame, DetectionObjectType.Person);

                DrawTracks(frame, tracks);

                videoWriter.Write(frame.ToImage<Emgu.CV.Structure.Bgr, byte>());
                videoCapture.Read(readBuffer);
            }

            predictor.Dispose();
            appearanceExtractor.Dispose();
            videoWriter.Dispose();
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
                    new Font("Consolas", fontSize, GraphicsUnit.Pixel), new SolidBrush(Color.FromArgb((0xFF << 24) | 0xDBDBDB)),
                    new PointF(x, y));
            }

            graphics.Dispose();
        }
    }
}
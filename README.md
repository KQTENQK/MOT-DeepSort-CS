<h1 align="center">
Multiple object tracking
</h1>

This is the C# implementation of SoRT and DeepSoRT trackers using YOLO network as person predictor and OSNet as appearance extractor in the frame.

## Examples
<h3>SoRT example</h3>
<div align="center">
<img src="./GitAssets/sort.gif" width=640/>
</br>
</div>
<h3>DeepSoRT example</h3>
<div align="center">
<img src="./GitAssets/deepsort.gif" width=640/>
</br>
</div>

## Code example of using
<h3>Used file hierarchy:</h3>
</br>
<pre>
. 
└ Assets
    ├ Input
    │   └ test.mp4
    ├ Output
    └ Models
        ├ Yolo
        │   └ yolo640v5.onnx
        └ Reid
            └ osnet_x1_0_msmt17.onnx
</pre>
</br>

<i>Some .onnx models are in src/MOT/ directory.</i>

</br>

<h3>Initializing predictor and extractor</h3>

```cs
var predictor = new YoloScorer<Yolo640v5>(File.ReadAllBytes("../../../Assets/Models/Yolo/yolo640v5.onnx"));

int extractorsInMemoryCount = 3;
var appearanceExtractor = new ReidScorer<OSNet_x1_0>(File.ReadAllBytes("../../../Assets/Models/Reid/osnet_x1_0_msmt17.onnx"),
    extractorsInMemoryCount);
```

<h3 align="left">Initializing SoRT matcher</h3>

```cs
var matcher = new SortMatcher(predictor);
```

<h3 align="left">Initializing DeepSoRT matcher</h3>

```cs
var matcher = new DeepSortMatcher(predictor, appearanceExtractor);
```

</br>
<h3 align="left">Drawing people bounding boxed in the frame.</h3>

```cs
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
            new RectangleF(track.CurrentBoundingBox.X - (penSize / 2), 
                track.CurrentBoundingBox.Y - yBoundingBoxIntent,
                track.CurrentBoundingBox.Width + penSize, 
                yBoundingBoxIntent - (penSize / 2)));

        (float x, float y) = (track.CurrentBoundingBox.X - xNumberIntent, 
                            track.CurrentBoundingBox.Y - yBoundingBoxIntent);

        graphics.DrawString($"{track.Id}",
            new Font("Consolas", fontSize, GraphicsUnit.Pixel), 
            new SolidBrush(Color.FromArgb((0xFF << 24) | 0xDDDDDD)),
            new PointF(x, y));
    }

    graphics.Dispose();
}
```

<h2>Getting video frame using Emgu.CV, handling frame and drawing bounding boxes</h2>

```cs
VideoCapture videoCapture = new VideoCapture("../../../Assets/Input/test.mp4");

double targetFps = videoCapture.Get(Emgu.CV.CvEnum.CapProp.Fps);
int width = videoCapture.Width;
int height = videoCapture.Height;

VideoWriter videoWriter = new VideoWriter("../../../Assets/Output/test.mp4", -1, 
                                    targetFps, new Size(width, height), true);

var predictor = new YoloScorer<Yolo640v5>(File.ReadAllBytes("../../../Assets/Models/Yolo/yolo640v5.onnx"));

int extractorsInMemoryCount = 3;
var appearanceExtractor = new ReidScorer<OSNet_x1_0>(File.ReadAllBytes("../../../Assets/Models/Reid/osnet_x1_0_msmt17.onnx"),
    extractorsInMemoryCount);

var matcher = new DeepSortMatcher(predictor, appearanceExtractor);
float targetConfidence = 0.4f;

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

predictor.Dispose();
appearanceExtractor.Dispose();
videoWriter.Dispose();
```
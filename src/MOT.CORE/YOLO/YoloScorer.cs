using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MOT.CORE.Utils;
using MOT.CORE.YOLO.Models;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MOT.CORE.YOLO
{
    public class YoloScorer<TYoloModel> : IPredictor where TYoloModel : IYoloModel
    {
        private readonly InferenceSession _inferenceSession;
        private readonly TYoloModel _yoloModel;

        private YoloScorer()
        {
            _yoloModel = Activator.CreateInstance<TYoloModel>();
        }

        public YoloScorer(byte[] model, SessionOptions sessionOptions = null) : this()
        {
            _inferenceSession = new InferenceSession(model,
                sessionOptions ?? new SessionOptions());
        }

        public IReadOnlyList<IPrediction> Predict(Bitmap image, float targetConfidence, params DetectionObjectType[] targetDetectionTypes)
        {
            Bitmap resized = image;

            if (image.Width != _yoloModel.Width || image.Height != _yoloModel.Height)
            {
                resized = ResizeBitmap(image);
            }

            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_yoloModel.Input, ExtractPixels(resized))
            };

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> onnxOutput = _inferenceSession.Run(inputs, _yoloModel.Outputs);
            List<YoloPrediction> predictions = Suppress(ParseOutput(onnxOutput.First().Value as DenseTensor<float>,
                                                        targetConfidence, image, targetDetectionTypes));

            onnxOutput.Dispose();

            return predictions;
        }

        public void Dispose()
        {
            _inferenceSession.Dispose();
        }

        private YoloPrediction[] ParseOutput(DenseTensor<float> output, float targetConfidence, Image image, params DetectionObjectType[] targetDetectionTypes)
        {
            unsafe
            {
                List<YoloPrediction> result = new List<YoloPrediction>();

                (int width, int height) = (image.Width, image.Height);
                (float xGain, float yGain) = (_yoloModel.Width / (float)width, _yoloModel.Height / (float)height);
                float gain = Math.Min(xGain, yGain);
                (float xPad, float yPad) = ((_yoloModel.Width - width * gain) / 2, (_yoloModel.Height - height * gain) / 2);
                var spanOutput = output.Buffer.Span;

                for (int i = 0; i < (int)output.Length / _yoloModel.Dimensions; i++)
                {
                    int iOffset = i * _yoloModel.Dimensions;

                    if (spanOutput[iOffset + 4] <= _yoloModel.Confidence)
                        continue;

                    for (int j = 5; j < _yoloModel.Dimensions; j++)
                    {
                        spanOutput[i * _yoloModel.Dimensions + j] *= spanOutput[i * _yoloModel.Dimensions + 4];
                        DetectionObjectType objectType = (DetectionObjectType)(j);

                        if ((targetDetectionTypes.Length != 0 
                            && !targetDetectionTypes.Any(p => p == objectType))
                            || spanOutput[iOffset + j] < targetConfidence)
                            continue;

                        if (spanOutput[i * _yoloModel.Dimensions + j] <= _yoloModel.MulConfidence)
                            continue;

                        float xMin = ((spanOutput[iOffset + 0] - spanOutput[iOffset + 2] / 2) - xPad) / gain; // Unpad bbox top-left-x to original
                        float yMin = ((spanOutput[iOffset + 1] - spanOutput[iOffset + 3] / 2) - yPad) / gain; // Unpad bbox top-left-y to original
                        float xMax = ((spanOutput[iOffset + 0] + spanOutput[iOffset + 2] / 2) - xPad) / gain; // Unpad bbox bottom-right-x to original
                        float yMax = ((spanOutput[iOffset + 1] + spanOutput[iOffset + 3] / 2) - yPad) / gain; // Unpad bbox bottom-right-y to original

                        xMin = Clamp(xMin, 0, width); // Clip bbox top-left-x to boundaries
                        yMin = Clamp(yMin, 0, height); // Clip bbox top-left-y to boundaries
                        xMax = Clamp(xMax, 0, width - 1); // Clip bbox bottom-right-x to boundaries
                        yMax = Clamp(yMax, 0, height - 1); // Clip bbox bottom-right-y to boundaries

                        YoloPrediction prediction = new YoloPrediction(objectType, spanOutput[iOffset + j], new Rectangle((int)xMin, (int)yMin, (int)(xMax - xMin), (int)(yMax - yMin)));

                        result.Add(prediction);
                    }
                }

                return result.ToArray();
            }
        }

        private List<YoloPrediction> Suppress(YoloPrediction[] predictions)
        {
            List<YoloPrediction> result = new List<YoloPrediction>(predictions);

            foreach (YoloPrediction prediction in predictions)
            {
                foreach (YoloPrediction current in result.ToArray())
                {
                    if (current == prediction)
                        continue;

                    if (Metrics.IntersectionOverUnion(prediction.CurrentBoundingBox, current.CurrentBoundingBox) >= _yoloModel.Overlap)
                    {
                        if (prediction.Confidence >= current.Confidence)
                        {
                            result.Remove(current);
                        }
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float Clamp(float value, float min, float max) => (value < min) ? min : (value > max) ? max : value;

        private Bitmap ResizeBitmap(Bitmap image)
        {
            PixelFormat format = image.PixelFormat;

            Bitmap output = new Bitmap(_yoloModel.Width, _yoloModel.Height, format);

            (float xRatio, float yRatio) = (_yoloModel.Width / (float)image.Width, _yoloModel.Height / (float)image.Height);
            float ratio = float.Min(xRatio, yRatio);
            (int targetWidth, int targetHeight) = ((int)(image.Width * ratio), (int)(image.Height * ratio));
            (int x, int y) = ((_yoloModel.Width / 2) - (targetWidth / 2), (_yoloModel.Height / 2) - (targetHeight / 2));

            Rectangle roi = new Rectangle(x, y, targetWidth, targetHeight);

            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.FromArgb(0, 0, 0, 0));

                graphics.SmoothingMode = SmoothingMode.None;
                graphics.InterpolationMode = InterpolationMode.Bilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;

                graphics.DrawImage(image, roi);
            }

            return output;
        }

        private Tensor<float> ExtractPixels(Bitmap image)
        {
#if DEBUG
            if (image.Width != _yoloModel.Width || image.Height != _yoloModel.Height)
                throw new Exception("Image width and height is not equal to model width and height.");
#endif
            Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bitmapData = image.LockBits(rectangle, ImageLockMode.ReadOnly, image.PixelFormat);
            int width = bitmapData.Width;
            int height = bitmapData.Height;
            int bytesPerPixel = Image.GetPixelFormatSize(image.PixelFormat) / 8;

            DenseTensor<float> tensor = new DenseTensor<float>(new[] { _yoloModel.BatchSize, _yoloModel.Channels, _yoloModel.Height, _yoloModel.Width });

            unsafe
            {
                Span<float> rTensorSpan = tensor.Buffer.Span;
                Span<float> gTensorSpan = rTensorSpan.Slice(height * width, height * width);
                Span<float> bTensorSpan = rTensorSpan.Slice(height * width * 2, height * width);

                byte* scan0 = (byte*)bitmapData.Scan0;
                int stride = bitmapData.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = scan0 + (y * stride);
                    int rowOffset = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        int bIndex = x * bytesPerPixel;
                        int point = rowOffset + x;
                        rTensorSpan[point] = row[bIndex + 2] / 255.0f; //R
                        gTensorSpan[point] = row[bIndex + 1] / 255.0f; //G
                        bTensorSpan[point] = row[bIndex] / 255.0f; //B
                    }
                }

                image.UnlockBits(bitmapData);
            }

            return tensor;
        }

        private Tensor<float> ExtractPixelsParallel(Bitmap image)
        {
#if DEBUG
            if (image.Width != _yoloModel.Width || image.Height != _yoloModel.Height)
                throw new Exception("Image width and height is not equal to model width and height.");
#endif
            Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bitmapData = image.LockBits(rectangle, ImageLockMode.ReadOnly, image.PixelFormat);
            int width = bitmapData.Width;
            int height = bitmapData.Height;
            int bytesPerPixel = Image.GetPixelFormatSize(image.PixelFormat) / 8;

            DenseTensor<float> tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

            unsafe
            {
                MemoryHandle memoryHandle = tensor.Buffer.Pin();
                float* rOffset = (float*)memoryHandle.Pointer;
                float* gOffset = rOffset + (height * width);
                float* bOffset = rOffset + (2 * height * width);
                byte* scan0 = (byte*)bitmapData.Scan0;
                int stride = bitmapData.Stride;

                Parallel.For(0, height, (y) =>
                {
                    byte* row = scan0 + (y * stride);

                    Parallel.For(0, width, (x) =>
                    {
                        int bIndex = x * bytesPerPixel;
                        rOffset[y * height + x] = row[bIndex + 2] / 255.0f; //R
                        gOffset[y * height + x] = row[bIndex + 1] / 255.0f; //G
                        bOffset[y * height + x] = row[bIndex] / 255.0f; //B
                    });
                });

                memoryHandle.Dispose();
                image.UnlockBits(bitmapData);
            }

            return tensor;
        }

        private YoloPrediction[] ParseOutputParallel(DenseTensor<float> output, Image image, params DetectionObjectType[] targetDetectionTypes)
        {
            ConcurrentBag<YoloPrediction> result = new ConcurrentBag<YoloPrediction>();

            unsafe
            {
                MemoryHandle outputHandle = output.Buffer.Pin();
                float* outputOffset = (float*)outputHandle.Pointer;
                (int width, int height) = (image.Width, image.Height);
                (float xGain, float yGain) = (_yoloModel.Width / (float)width, _yoloModel.Height / (float)height);
                float gain = Math.Min(xGain, yGain);
                (float xPad, float yPad) = ((_yoloModel.Width - width * gain) / 2, (_yoloModel.Height - height * gain) / 2);

                Parallel.For(0, (int)output.Length / _yoloModel.Dimensions, (i) =>
                {
                    float* iOffset = outputOffset + (i * _yoloModel.Dimensions);

                    if (iOffset[4] <= _yoloModel.Confidence)
                        return;

                    Parallel.For(5, _yoloModel.Dimensions, (j) =>
                    {
                        iOffset[j] = iOffset[j] * iOffset[4]; // MulConfidence = ObjConfidence * ClsConfidence
                    });

                    Parallel.For(5, _yoloModel.Dimensions, (k) =>
                    {
                        DetectionObjectType objectType = (DetectionObjectType)(k);

                        if (targetDetectionTypes.Length != 0 && !targetDetectionTypes.Any(p => p == objectType))
                            return;

                        if (outputOffset[i * _yoloModel.Dimensions + k] <= _yoloModel.MulConfidence)
                            return;

                        float xMin = ((iOffset[0] - iOffset[2] / 2) - xPad) / gain; // Unpad bbox top-left-x to original
                        float yMin = ((iOffset[1] - iOffset[3] / 2) - yPad) / gain; // Unpad bbox top-left-y to original
                        float xMax = ((iOffset[0] + iOffset[2] / 2) - xPad) / gain; // Unpad bbox bottom-right-x to original
                        float yMax = ((iOffset[1] + iOffset[3] / 2) - yPad) / gain; // Unpad bbox bottom-right-y to original

                        xMin = Clamp(xMin, 0, width); // Clip bbox top-left-x to boundaries
                        yMin = Clamp(yMin, 0, height); // Clip bbox top-left-y to boundaries
                        xMax = Clamp(xMax, 0, width - 1); // Clip bbox bottom-right-x to boundaries
                        yMax = Clamp(yMax, 0, height - 1); // Clip bbox bottom-right-y to boundaries

                        YoloPrediction prediction = new YoloPrediction(objectType, iOffset[k], new Rectangle((int)xMin, (int)yMin, (int)(xMax - xMin), (int)(yMax - yMin)));

                        result.Add(prediction);
                    });
                });

                outputHandle.Dispose();
            }

            return result.ToArray();
        }
    }
}

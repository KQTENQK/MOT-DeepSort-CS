using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using MOT.CORE.ReID.Models;
using MOT.CORE.YOLO;
using MOT.CORE.Utils.DataStructs;
using System.Threading.Tasks;

namespace MOT.CORE.ReID
{
    public class ReidScorer<TReidModel> : IDisposable, IAppearanceExtractor where TReidModel : IReidModel
    {
        private readonly IReidModel _reidModel;
        private readonly List<InferenceSession> _inferenceSessions;

        private SessionOptions _currentSessionOptions;
        private byte[] _currentModel;

        private ReidScorer()
        {
            _reidModel = Activator.CreateInstance<TReidModel>();
        }

        public ReidScorer(byte[] model, int startSessionsCount = 1, SessionOptions sessionOptions = null) : this()
        {
            _inferenceSessions = new List<InferenceSession>();
            _currentSessionOptions = sessionOptions ?? new SessionOptions();
            _currentModel = model;

            for (int i = 0; i < startSessionsCount; i++)
                _inferenceSessions.Add(new InferenceSession(_currentModel, _currentSessionOptions));
        }

        public void Dispose()
        {
            for (int i = 0; i < _inferenceSessions.Count; i++)
                _inferenceSessions[i].Dispose();
        }

        public IReadOnlyList<Vector> Predict(Bitmap image, IPrediction[] detectedBounds)
        {
            int batchCount = detectedBounds.Length / _reidModel.BatchSize;
            batchCount = detectedBounds.Length % _reidModel.BatchSize == 0 ? batchCount : batchCount + 1;

            for (int i = _inferenceSessions.Count; i < batchCount; i++)
                _inferenceSessions.Add(new InferenceSession(_currentModel, _currentSessionOptions));

            DenseTensor<float>[] extracted = ExtractSubImages(image, detectedBounds, batchCount);
            List<Vector> appearances = new List<Vector>();
            float[][] modelOutputs = new float[batchCount][];

            RunInferencesAsync(extracted, batchCount, modelOutputs);

            for (int i = 0; i < batchCount - 1; i++)
            {
                for (int k = 0; k < _reidModel.BatchSize; k++)
                {
                    float[] parsing = modelOutputs[i].AsSpan<float>().Slice(k * _reidModel.OutputVectorSize, _reidModel.OutputVectorSize).ToArray();
                    Vector appearance = new Vector(ref parsing);
                    appearance.Normalize();
                    appearances.Add(appearance);
                }
            }

            int lastBatchAppearancesCount = detectedBounds.Length % _reidModel.BatchSize;

            if (lastBatchAppearancesCount == 0)
                lastBatchAppearancesCount = _reidModel.BatchSize;

            for (int k = 0; k < lastBatchAppearancesCount; k++)
            {
                float[] parsing = modelOutputs[batchCount - 1].AsSpan<float>().Slice(k * _reidModel.OutputVectorSize, _reidModel.OutputVectorSize).ToArray();
                Vector appearance = new Vector(ref parsing);
                appearance.Normalize();
                appearances.Add(appearance);
            }

            return appearances;
        }

        private async void RunInferencesAsync(DenseTensor<float>[] extracted, int batchCount, float[][] modelOutputs)
        {
            Task[] inferenceTasks = new Task[batchCount];

            for (int k = 0; k < batchCount; k++)
                inferenceTasks[k] = RunInference(extracted, k, modelOutputs);

            await Task.WhenAll(inferenceTasks);
        }

        private Task RunInference(DenseTensor<float>[] extracted, int iterationIndex, float[][] modelOutputs)
        {
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(_reidModel.Input, extracted[iterationIndex])
            };

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> onnxOutput = _inferenceSessions[iterationIndex].Run(inputs, _reidModel.Outputs);
            modelOutputs[iterationIndex] = onnxOutput.First().AsEnumerable<float>().ToArray();

            onnxOutput.Dispose();

            return Task.CompletedTask;
        }

        private DenseTensor<float>[] ExtractSubImages(Bitmap image, IPrediction[] detectedBoundingBoxes, int batchCount)
        {
            DenseTensor<float>[] subImagesData = new DenseTensor<float>[batchCount];

            unsafe
            {
                for (int i = 0; i < subImagesData.Length; i++)
                {
                    subImagesData[i] = new DenseTensor<float>(new[] { _reidModel.BatchSize, _reidModel.Channels, _reidModel.Height, _reidModel.Width });
                    Bitmap[] bitmaps = new Bitmap[batchCount];
                    int targetIterationsCount = i == batchCount - 1 ? detectedBoundingBoxes.Length % _reidModel.BatchSize : _reidModel.BatchSize;

                    for (int j = 0; j < targetIterationsCount; j++)
                    {
                        Bitmap bitmap = FragmentBitmap(image, detectedBoundingBoxes[i * _reidModel.BatchSize + j].CurrentBoundingBox, _reidModel.Width, _reidModel.Height);
                        Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        BitmapData bitmapData = image.LockBits(rectangle, ImageLockMode.ReadOnly, bitmap.PixelFormat);
                        int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                        Span<float> rTensorSpan = subImagesData[i].Buffer.Span.Slice(_reidModel.Channels * _reidModel.Height * _reidModel.Width * j,
                            _reidModel.Height * _reidModel.Width);
                        Span<float> gTensorSpan = subImagesData[i].Buffer.Span.Slice(_reidModel.Channels * _reidModel.Height * _reidModel.Width * j + _reidModel.Height * _reidModel.Width,
                            _reidModel.Height * _reidModel.Width);
                        Span<float> bTensorSpan = subImagesData[i].Buffer.Span.Slice(_reidModel.Channels * _reidModel.Height * _reidModel.Width * j + _reidModel.Height * _reidModel.Width * 2,
                            _reidModel.Height * _reidModel.Width);

                        byte* scan0 = (byte*)bitmapData.Scan0;
                        int stride = bitmapData.Stride;

                        for (int y = 0; y < bitmapData.Height; y++)
                        {
                            byte* row = scan0 + (y * stride);
                            int rowOffset = y * bitmapData.Width;

                            for (int x = 0; x < bitmapData.Width; x++)
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
                }
            }

            return subImagesData;
        }

        private Bitmap FragmentBitmap(Bitmap image, RectangleF boundingBox, int modelWidth, int modelHeight)
        {
            PixelFormat format = image.PixelFormat;

            Bitmap output = new Bitmap((int)boundingBox.Width, (int)boundingBox.Height, format);

            using (var graphics = Graphics.FromImage(output))
            {
                graphics.DrawImage(image, 0, 0, boundingBox, GraphicsUnit.Pixel);
            }

            return new Bitmap(output, modelWidth, modelHeight);
        }
    }
}

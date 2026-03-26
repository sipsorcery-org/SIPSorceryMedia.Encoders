//-----------------------------------------------------------------------------
// Filename: VpxVideoEncoder.cs
//
// Description: Implements a VP8 video encoder.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 20 Aug 2020  Aaron Clauson	Created, Dublin, Ireland.
// 17 Dec 2020  Aaron Clauson   Renamed from VideoEncoder to VpxVideoEncoder.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders.Codecs;

namespace SIPSorceryMedia.Encoders
{
    public class VpxVideoEncoder : IVideoEncoder, IDisposable
    {
        public const int VP8_FORMATID = 96;

        private ILogger logger = SIPSorcery.LogFactory.CreateLogger<VpxVideoEncoder>();

        private static readonly List<VideoFormat> _supportedFormats = new List<VideoFormat>
        {
            new VideoFormat(VideoCodecsEnum.VP8, VP8_FORMATID)
        };

        public List<VideoFormat> SupportedFormats
        {
            get => _supportedFormats;
        }

        uint? _targetKbps;
        public uint? TargetKbps
        {
            get => _targetKbps;
            set
            {
                lock (_encoderLock)
                {
                    if (_vp8Encoder != null)
                    {
                        _vp8Encoder.Dispose();
                        _vp8Encoder = null;
                    }

                    _forceKeyFrame = true;
                    _targetKbps = value;
                }
            }
        }

        private Vp8Codec _vp8Encoder;
        private Vp8Codec _vp8Decoder;
        private bool _forceKeyFrame = false;
        private Object _decoderLock = new object();
        private Object _encoderLock = new object();

        /// <summary>
        /// Creates a new video encoder can encode and decode samples.
        /// </summary>
        public VpxVideoEncoder()
        { }

        public void ForceKeyFrame() => _forceKeyFrame = true;

        public byte[] EncodeVideo(int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat, VideoCodecsEnum codec)
        {
            lock (_encoderLock)
            {
                if (_vp8Encoder == null)
                {
                    _vp8Encoder = new Vp8Codec();
                    _vp8Encoder.InitialiseEncoder((uint)width, (uint)height, targetKbps: _targetKbps);
                }

                byte[] encodedBuffer = null;

                if (pixelFormat == VideoPixelFormatsEnum.NV12)
                {
                    encodedBuffer = _vp8Encoder.Encode(sample, vpxmd.VpxImgFmt.VPX_IMG_FMT_NV12, _forceKeyFrame);
                }
                else if (pixelFormat == VideoPixelFormatsEnum.I420)
                {
                    encodedBuffer = _vp8Encoder.Encode(sample, vpxmd.VpxImgFmt.VPX_IMG_FMT_I420, _forceKeyFrame);
                }
                else
                {
                    int stride = GetStride(width, pixelFormat);
                    var i420Buffer = PixelConverter.ToI420(width, height, stride, sample, pixelFormat);
                    encodedBuffer = _vp8Encoder.Encode(i420Buffer, vpxmd.VpxImgFmt.VPX_IMG_FMT_I420, _forceKeyFrame);
                }

                if (_forceKeyFrame)
                {
                    _forceKeyFrame = false;
                }

                return encodedBuffer;
            }
        }

        public IEnumerable<VideoSample> DecodeVideo(byte[] frame, VideoPixelFormatsEnum pixelFormat, VideoCodecsEnum codec)
        {
            lock (_decoderLock)
            {
                if (_vp8Decoder == null)
                {
                    _vp8Decoder = new Vp8Codec();
                    _vp8Decoder.InitialiseDecoder();
                }

                List<byte[]> decodedFrames = _vp8Decoder.Decode(frame, frame.Length, out var width, out var height);

                if (decodedFrames == null)
                {
                    logger.LogWarning("VPX decode of video sample failed.");
                }
                else
                {
                    foreach (var decodedFrame in decodedFrames)
                    {
                        byte[] rgb = PixelConverter.I420toBGR(decodedFrame, (int)width, (int)height, out _);
                        yield return new VideoSample { Width = width, Height = height, Sample = rgb };
                    }
                }
            }
        }

        private static int RoundUp(int value, int to) => (value + to - 1) / to * to;

        private static int BytesPerPixel(VideoPixelFormatsEnum pixelFormat) => pixelFormat switch
        {
            VideoPixelFormatsEnum.Bgra => 4,
            VideoPixelFormatsEnum.Rgba => 4,
            VideoPixelFormatsEnum.Rgb  => 3,
            VideoPixelFormatsEnum.Bgr  => 3,
            _ => throw new ArgumentException($"Unsupported pixel format for bytes-per-pixel calculation: {pixelFormat}", nameof(pixelFormat))
        };

        /// <summary>
        /// Calculates the row stride in bytes for the given width and pixel format,
        /// rounding up to the nearest 4-byte boundary as required by most bitmap formats.
        /// </summary>
        private static int GetStride(int width, VideoPixelFormatsEnum pixelFormat) =>
            RoundUp(value: width * BytesPerPixel(pixelFormat), to: 4);

        public void Dispose()
        {
            _vp8Encoder?.Dispose();
            _vp8Decoder?.Dispose();
        }

        public byte[] EncodeVideoFaster(RawImage rawImage, VideoCodecsEnum codec)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RawImage> DecodeVideoFaster(byte[] encodedSample, VideoPixelFormatsEnum pixelFormat, VideoCodecsEnum codec)
        {
            throw new NotImplementedException();
        }
    }
}

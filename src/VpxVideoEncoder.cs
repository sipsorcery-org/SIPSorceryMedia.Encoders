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
                    _vp8Encoder.InitialiseEncoder((uint)width, (uint)height);
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
                    int stride = pixelFormat == VideoPixelFormatsEnum.Bgra ? width * 4 : width * 3;
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

        public void Dispose()
        {
            _vp8Encoder?.Dispose();
            _vp8Decoder?.Dispose();
        }
    }
}

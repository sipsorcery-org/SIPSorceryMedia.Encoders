//-----------------------------------------------------------------------------
// Filename: VideoEncoder.cs
//
// Description: Implements a VP8 video encoder.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 20 Aug 2020  Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Abstractions.V1;
using SIPSorceryMedia.Encoders.Codecs;

namespace SIPSorceryMedia.Encoders
{
    public class VideoEncoder : IVideoEncoder, IDisposable
    {
        private ILogger logger = SIPSorcery.LogFactory.CreateLogger<VideoEncoder>();

        public static readonly List<VideoCodecsEnum> SupportedCodecs = new List<VideoCodecsEnum>
        {
            VideoCodecsEnum.VP8
        };

        private Vp8Codec _vp8Encoder;
        private Vp8Codec _vp8Decoder;
        private bool _forceKeyFrame = false;
        private Object _decoderLock = new object();
        private Object _encoderLock = new object();

        /// <summary>
        /// Creates a new video encoder can encode and decode samples.
        /// </summary>
        public VideoEncoder()
        { }

        public void ForceKeyFrame() => _forceKeyFrame = true;
        public bool IsSupported(VideoCodecsEnum codec) => codec == VideoCodecsEnum.VP8;

        public byte[] EncodeVideo(int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat, VideoCodecsEnum codec)
        {
            lock (_encoderLock)
            {
                if (_vp8Encoder == null)
                {
                    _vp8Encoder = new Vp8Codec();
                    _vp8Encoder.InitialiseEncoder((uint)width, (uint)height);
                }

                var i420Buffer = PixelConverter.ToI420(width, height, sample, pixelFormat);
                var encodedBuffer = _vp8Encoder.Encode(i420Buffer, _forceKeyFrame);

                //SetBitmapData(sample, _encodeBmp, pixelFormat);

                //var nv12bmp = SoftwareBitmap.Convert(_encodeBmp, BitmapPixelFormat.Nv12);
                //byte[] nv12Buffer = null;

                //using (BitmapBuffer buffer = nv12bmp.LockBuffer(BitmapBufferAccessMode.Read))
                //{
                //    using (var reference = buffer.CreateReference())
                //    {
                //        unsafe
                //        {
                //            byte* dataInBytes;
                //            uint capacity;
                //            ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                //            nv12Buffer = new byte[capacity];
                //            Marshal.Copy((IntPtr)dataInBytes, nv12Buffer, 0, (int)capacity);
                //        }
                //    }
                //}

                //byte[] encodedBuffer = _vp8Encoder.Encode(nv12Buffer, _forceKeyFrame);

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
                    //DateTime startTime = DateTime.Now;
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
                        byte[] rgb = PixelConverter.I420toBGR(decodedFrame, (int)width, (int)height);
                        //Console.WriteLine($"VP8 decode took {DateTime.Now.Subtract(startTime).TotalMilliseconds}ms.");
                        //OnVideoSinkDecodedSample(rgb, width, height, (int)(width * 3), VideoPixelFormatsEnum.Bgr);
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

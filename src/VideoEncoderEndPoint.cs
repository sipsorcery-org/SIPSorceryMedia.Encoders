//-----------------------------------------------------------------------------
// Filename: VideoEncoderEndPoint.cs
//
// Description: Implements a video source and sink that is for encode/decode
// only, i.e. no hooks for system audio or video devices.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 20 Aug 2020  Aaron Clauson	Created, Dublin, Ireland.
// 29 Sep 2020  Aaron Clauson   Moved from SIPSorceryMedia.Windows assembly into
//                              a new dedicated project for x-platform support.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;

namespace SIPSorceryMedia.Encoders
{
    public class VideoEncoderEndPoint : IVideoSource, IVideoSink, IDisposable
    {
        private const int VIDEO_SAMPLING_RATE = 90000;
        private const int DEFAULT_FRAMES_PER_SECOND = 30;
        private const int VP8_FORMAT_ID = 96;

        private ILogger logger = SIPSorcery.LogFactory.CreateLogger<VideoEncoderEndPoint>();

        public static readonly List<VideoFormat> SupportedFormats = new List<VideoFormat>
        {
            new VideoFormat(VideoCodecsEnum.VP8, VP8_FORMAT_ID, VIDEO_SAMPLING_RATE)
        };

        private MediaFormatManager<VideoFormat> _formatManager;
        private VpxVideoEncoder _videoEncoder;
        private bool _isClosed;

        /// <summary>
        /// This video source DOES NOT generate raw samples. Subscribe to the encoded samples event
        /// to get samples ready for passing to the RTP transport layer.
        /// </summary>
        [Obsolete("This video source only generates encoded samples. No raw video samples will be supplied to this event.")]
        public event RawVideoSampleDelegate OnVideoSourceRawSample { add { } remove { } }

        /// <summary>
        /// This event will be fired whenever a video sample is encoded and is ready to transmit to the remote party.
        /// </summary>
        public event EncodedSampleDelegate OnVideoSourceEncodedSample;

        /// <summary>
        /// This event is fired after the sink decodes a video frame from the remote party.
        /// </summary>
        public event VideoSinkSampleDecodedDelegate OnVideoSinkDecodedSample;

#pragma warning disable CS0067
        public event SourceErrorDelegate OnVideoSourceError;
        public event RawVideoSampleFasterDelegate OnVideoSourceRawSampleFaster;
        public event VideoSinkSampleDecodedFasterDelegate OnVideoSinkDecodedSampleFaster;
#pragma warning restore CS0067

        /// <summary>
        /// Creates a new video source that can encode and decode samples.
        /// </summary>
        public VideoEncoderEndPoint()
        {
            _formatManager = new MediaFormatManager<VideoFormat>(SupportedFormats);
            _videoEncoder = new VpxVideoEncoder();
        }

        public void RestrictFormats(Func<VideoFormat, bool> filter) => _formatManager.RestrictFormats(filter);
        public List<VideoFormat> GetVideoSourceFormats() => _formatManager.GetSourceFormats();
        public void SetVideoSourceFormat(VideoFormat videoFormat) => _formatManager.SetSelectedFormat(videoFormat);
        public List<VideoFormat> GetVideoSinkFormats() => _formatManager.GetSourceFormats();
        public void SetVideoSinkFormat(VideoFormat videoFormat) => _formatManager.SetSelectedFormat(videoFormat);

        public void ForceKeyFrame() => _videoEncoder.ForceKeyFrame();
        public void GotVideoRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload) =>
            throw new ApplicationException("The Windows Video End Point requires full video frames rather than individual RTP packets.");
        public bool HasEncodedVideoSubscribers() => OnVideoSourceEncodedSample != null;
        public bool IsVideoSourcePaused() => false;
        public Task PauseVideo() => Task.CompletedTask;
        public Task ResumeVideo() => Task.CompletedTask;
        public Task StartVideo() => Task.CompletedTask;
        public Task CloseVideoSink() => Task.CompletedTask;
        public Task PauseVideoSink() => Task.CompletedTask;
        public Task ResumeVideoSink() => Task.CompletedTask;
        public Task StartVideoSink() => Task.CompletedTask;

        public MediaEndPoints ToMediaEndPoints()
        {
            return new MediaEndPoints
            {
                VideoSource = this,
                VideoSink = this
            };
        }

        public void ExternalVideoSourceRawSample(uint durationMilliseconds, int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat)
        {
            if (!_isClosed)
            {
                if (OnVideoSourceEncodedSample != null)
                {
                    var encodedBuffer = _videoEncoder.EncodeVideo(width, height, sample, pixelFormat, VideoCodecsEnum.VP8);

                    if (encodedBuffer != null)
                    {
                        uint fps = (durationMilliseconds > 0) ? 1000 / durationMilliseconds : DEFAULT_FRAMES_PER_SECOND;
                        uint durationRtpTS = VIDEO_SAMPLING_RATE / fps;
                        OnVideoSourceEncodedSample.Invoke(durationRtpTS, encodedBuffer);
                    }
                }
            }
        }

        public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] frame, VideoFormat format)
        {
            if (!_isClosed)
            {
                foreach (var decoded in _videoEncoder.DecodeVideo(frame, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8))
                {
                    OnVideoSinkDecodedSample(decoded.Sample, decoded.Width, decoded.Height, (int)(decoded.Width * 3), VideoPixelFormatsEnum.Bgr);
                }
            }
        }

        public Task CloseVideo()
        {
            if (!_isClosed)
            {
                _isClosed = true;
                Dispose();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _videoEncoder?.Dispose();
        }

        public void ExternalVideoSourceRawSampleFaster(uint durationMilliseconds, RawImage rawImage)
        {
            throw new NotImplementedException();
        }
    }
}

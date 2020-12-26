//-----------------------------------------------------------------------------
// Filename: VpxVideoEncoderUnitTest.cs
//
// Description: Unit tests for logic in VP8Codec.cs.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 19 Dec 2020	Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SIPSorceryMedia.Abstractions;
using Xunit;

namespace SIPSorceryMedia.Encoders.UnitTest
{
    public class VpxVideoEncoderUnitTest
    {
        private Microsoft.Extensions.Logging.ILogger logger = null;

        public VpxVideoEncoderUnitTest(Xunit.Abstractions.ITestOutputHelper output)
        {
            logger = TestLogger.GetLogger(output).CreateLogger(this.GetType().Name);
        }

        /// <summary>
        /// Tests that an I420 640x480 buffer can be encoded.
        /// </summary>
        [Fact]
        public void Encode_I420_640x480()
        {
            VpxVideoEncoder vpxEncoder = new VpxVideoEncoder();

            using (StreamReader sr = new StreamReader("img/testpattern_640x480.i420"))
            {
                byte[] buffer = new byte[sr.BaseStream.Length];
                sr.BaseStream.Read(buffer, 0, buffer.Length);
                byte[] encodedSample = vpxEncoder.EncodeVideo(640, 480, buffer, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

                Assert.NotNull(encodedSample);
                Assert.Equal(15399, encodedSample.Length);
            }
        }

        /// <summary>
        /// Tests that a VP8 encoded key frame can be decoded.
        /// </summary>
        [Fact]
        public void DecodeKeyFrameUnitTest()
        {
            VpxVideoEncoder vpxEncoder = new VpxVideoEncoder();

            string hexKeyFrame = File.ReadAllText("img/testpattern_keyframe_640x480.vp8");
            byte[] buffer = HexStr.ParseHexStr(hexKeyFrame.Trim());

            var frame = vpxEncoder.DecodeVideo(buffer, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8).First();

            Assert.NotNull(frame.Sample);
            Assert.Equal(921600, frame.Sample.Length);
            Assert.Equal(640U, frame.Width);
            Assert.Equal(480U, frame.Height);

            //fixed (byte* bmpPtr = encodedSample.Sample)
            //{
            //    Bitmap bmp = new Bitmap((int)encodedSample.Width, (int)encodedSample.Height, (int)encodedSample.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(bmpPtr));
            //    bmp.Save("decodetestpattern.bmp");
            //    bmp.Dispose();
            //}
        }

        /// <summary>
        /// Tests that a 640x480 test pattern I420 buffer can be encoded and decoded successfully.
        /// </summary>
        [Fact]
        public unsafe void Roundtrip_I420_640x480()
        {
            VpxVideoEncoder vpxEncoder = new VpxVideoEncoder();

            using (StreamReader sr = new StreamReader("img/testpattern_640x480.i420"))
            {
                byte[] buffer = new byte[sr.BaseStream.Length];
                sr.BaseStream.Read(buffer, 0, buffer.Length);

                var encodedFrame = vpxEncoder.EncodeVideo(640, 480, buffer, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

                Assert.NotNull(encodedFrame);
                Assert.Equal(15399, encodedFrame.Length);

                var decodedFrame = vpxEncoder.DecodeVideo(encodedFrame, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8).First();

                Assert.NotNull(decodedFrame.Sample);
                Assert.Equal(921600, decodedFrame.Sample.Length);
                Assert.Equal(640U, decodedFrame.Width);
                Assert.Equal(480U, decodedFrame.Height);

                fixed (byte* pBgr = decodedFrame.Sample)
                {
                    Bitmap bmp = new Bitmap((int)decodedFrame.Width, (int)decodedFrame.Height, (int)decodedFrame.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(pBgr));
                    bmp.Save("roundtrip_i420_640x480.bmp");
                    bmp.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that a 640x480 test pattern bitmap can be encoded and decoded successfully.
        /// </summary>
        [Fact]
        public unsafe void Roundtrip_Bitmap_640x480()
        {
            VpxVideoEncoder vpxEncoder = new VpxVideoEncoder();

            using (Bitmap bmp = new Bitmap("img/testpattern_640x480.bmp"))
            {
                byte[] i420 = BitmapToI420(bmp);

                var encodedFrame = vpxEncoder.EncodeVideo(640, 480, i420, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

                Assert.NotNull(encodedFrame);
                Assert.Equal(14207, encodedFrame.Length);

                var decodedFrame = vpxEncoder.DecodeVideo(encodedFrame, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8).First();

                Assert.NotNull(decodedFrame.Sample);
                Assert.Equal(921600, decodedFrame.Sample.Length);
                Assert.Equal(640U, decodedFrame.Width);
                Assert.Equal(480U, decodedFrame.Height);

                fixed (byte* pBgr = decodedFrame.Sample)
                {
                    Bitmap rtBmp = new Bitmap((int)decodedFrame.Width, (int)decodedFrame.Height, (int)decodedFrame.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(pBgr));
                    rtBmp.Save("roundtrip_bitmap_640x480.bmp");
                    rtBmp.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that an image with an uneven dimension can be encoded and decoded successfully.
        /// </summary>
        [Fact]
        public unsafe void Roundtrip_Bitmap_720x405()
        {
            VpxVideoEncoder vpxEncoder = new VpxVideoEncoder();

            using (Bitmap bmp = new Bitmap("img/testpattern_720x405.bmp"))
            {
                byte[] i420 = BitmapToI420(bmp);

                var encodedFrame = vpxEncoder.EncodeVideo(720, 405, i420, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

                Assert.NotNull(encodedFrame);
                //Assert.Equal(14207, encodedFrame.Length);

                var decodedFrame = vpxEncoder.DecodeVideo(encodedFrame, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8).First();

                Assert.NotNull(decodedFrame.Sample);
                //Assert.Equal(921600, decodedFrame.Sample.Length);
                Assert.Equal(720U, decodedFrame.Width);
                Assert.Equal(405U, decodedFrame.Height);

                fixed (byte* pBgr = decodedFrame.Sample)
                {
                    Bitmap rtBmp = new Bitmap((int)decodedFrame.Width, (int)decodedFrame.Height, (int)decodedFrame.Width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(pBgr));
                    rtBmp.Save("roundtrip_bitmap_720x405.bmp");
                    rtBmp.Dispose();
                }
            }
        }

        private static byte[] BitmapToByteArray(Bitmap bitmap, out int stride)
        {
            BitmapData bmpdata = null;

            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                stride = bmpdata.Stride;
                int numbytes = stride * bitmap.Height;
                byte[] bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                {
                    bitmap.UnlockBits(bmpdata);
                }
            }
        }

        private static byte[] BitmapToI420(Bitmap bmp)
        {
            var buffer = BitmapToByteArray(bmp, out int stride);
            return PixelConverter.BGRtoI420(buffer, bmp.Width, bmp.Height, stride);
        }
    }
}

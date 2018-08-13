using System.Threading.Tasks;
using Greenshot.Core;
using Greenshot.Core.Sources;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// Test the capture code
    /// </summary>
    public class CaptureTests
    {
        /// <summary>
        /// Test if a capture with the screen works
        /// </summary>
        [Fact]
        public async Task Test_CaptureFlow_ScreenSource()
        {
            var captureFlow = new CaptureFlow
            {
                Sources = {new ScreenSource()}
            };
            var capture = await captureFlow.Execute();
            Assert.NotNull(capture);
            Assert.NotNull(capture.CaptureElements);
            Assert.Equal(1, capture.CaptureElements.Count);
        }

        /// <summary>
        /// Test if a capture with the screen and mouse works
        /// </summary>
        [Fact]
        public async Task Test_CaptureFlow_ScreenSource_MouseSource()
        {
            var captureFlow = new CaptureFlow
            {
                Sources = { new ScreenSource() , new MouseSource()}
            };
            var capture = await captureFlow.Execute();
            Assert.NotNull(capture);
            Assert.NotNull(capture.CaptureElements);
            Assert.Equal(2, capture.CaptureElements.Count);
        }
    }
}

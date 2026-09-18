using System;
using Greenshot.Base.Core;
using Greenshot.UI.ViewModels;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class ExceptionHelperTests
    {
        public ExceptionHelperTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void NormalizeStackTrace_HandlesEnglishLocale()
        {
            string enStack = @"Exception: System.NullReferenceException: Object reference not set to an instance of an object.
   at Greenshot.Forms.MainForm.CaptureRegion() in D:\code\greenshot\src\Greenshot\Forms\MainForm.cs:line 42
   at Greenshot.Base.Core.CaptureHelper.Capture() in D:\code\greenshot\src\Greenshot\Forms\CaptureHelper.cs:line 100";

            string normalized = ExceptionHelper.NormalizeStackTrace(enStack);
            string expected = "System.NullReferenceException\nat Greenshot.Forms.MainForm.CaptureRegion()\nat Greenshot.Base.Core.CaptureHelper.Capture()";

            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void NormalizeStackTrace_HandlesGermanLocale()
        {
            string deStack = @"Exception: System.NullReferenceException: Der Objektverweis wurde nicht auf eine Objektinstanz festgelegt.
   bei Greenshot.Forms.MainForm.CaptureRegion() in D:\code\greenshot\src\Greenshot\Forms\MainForm.cs:Zeile 42
   bei Greenshot.Base.Core.CaptureHelper.Capture() in D:\code\greenshot\src\Greenshot\Forms\CaptureHelper.cs:Zeile 100";

            string normalized = ExceptionHelper.NormalizeStackTrace(deStack);
            string expected = "System.NullReferenceException\nat Greenshot.Forms.MainForm.CaptureRegion()\nat Greenshot.Base.Core.CaptureHelper.Capture()";

            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void NormalizeStackTrace_HandlesFrenchAndSpanishLocales()
        {
            string frStack = @"Exception: System.NullReferenceException: La référence d'objet n'est pas définie.
   à Greenshot.Forms.MainForm.CaptureRegion() dans D:\code\greenshot\src\Greenshot\Forms\MainForm.cs:ligne 42
   à Greenshot.Base.Core.CaptureHelper.Capture() dans D:\code\greenshot\src\Greenshot\Forms\CaptureHelper.cs:ligne 100";

            string esStack = @"Exception: System.NullReferenceException: Referencia a objeto no establecida.
   en Greenshot.Forms.MainForm.CaptureRegion() en D:\code\greenshot\src\Greenshot\Forms\MainForm.cs:línea 42
   en Greenshot.Base.Core.CaptureHelper.Capture() en D:\code\greenshot\src\Greenshot\Forms\CaptureHelper.cs:línea 100";

            string frNorm = ExceptionHelper.NormalizeStackTrace(frStack);
            string esNorm = ExceptionHelper.NormalizeStackTrace(esStack);

            Assert.Equal(frNorm, esNorm);
        }

        [Fact]
        public void ComputeHash_ProducesConsistentHashAcrossLocales()
        {
            string enStack = @"Exception: System.NullReferenceException: Object reference not set to an instance of an object.
   at Greenshot.Forms.MainForm.CaptureRegion() in D:\code\greenshot\src\Greenshot\Forms\MainForm.cs:line 42
   at Greenshot.Base.Core.CaptureHelper.Capture() in D:\code\greenshot\src\Greenshot\Forms\CaptureHelper.cs:line 100";

            string deStack = @"Exception: System.NullReferenceException: Der Objektverweis wurde nicht auf eine Objektinstanz festgelegt.
   bei Greenshot.Forms.MainForm.CaptureRegion() in D:\code\greenshot\src\Greenshot\Forms\MainForm.cs:Zeile 42
   bei Greenshot.Base.Core.CaptureHelper.Capture() in D:\code\greenshot\src\Greenshot\Forms\CaptureHelper.cs:Zeile 100";

            string hashEn = ExceptionHelper.ComputeHash(ExceptionHelper.NormalizeStackTrace(enStack));
            string hashDe = ExceptionHelper.ComputeHash(ExceptionHelper.NormalizeStackTrace(deStack));

            Assert.Equal(12, hashEn.Length);
            Assert.Equal(hashEn, hashDe);
            Assert.Equal("bda64fc43a57", hashEn);
        }

        [Fact]
        public void GetGitHubSearchUrl_ContainsFormattedMarkerAndHash()
        {
            string hash = "bda64fc43a57";
            string url = ExceptionHelper.GetGitHubSearchUrl(hash);

            Assert.Contains("github.com/greenshot/greenshot/issues", url);
            Assert.Contains(hash, url);
        }

        [Fact]
        public void BugReportViewModel_InitializesAndComputesHash()
        {
            var ex = new InvalidOperationException("Test message");
            var vm = new BugReportViewModel(ex);

            Assert.Equal("System.InvalidOperationException", vm.ExceptionType);
            Assert.Equal("Test message", vm.ExceptionMessage);
            Assert.True(vm.HasStackTraceHash);
            Assert.Equal(12, vm.StackTraceHash.Length);
            Assert.False(vm.IsDetailsExpanded);
            Assert.Equal("▼ Show Details", vm.ToggleDetailsText);

            vm.ToggleDetails();
            Assert.True(vm.IsDetailsExpanded);
            Assert.Equal("▲ Hide Details", vm.ToggleDetailsText);
        }
    }
}

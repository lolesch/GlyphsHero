using FluentAssertions;
using NUnit.Framework;
using Submodules.Utility.Tools.Timer;

namespace Code.Tests.EditMode.Utility
{
    /// <summary>
    /// Pins the intended Timer contract:
    ///  - Start() schedules; it must NOT perform the action synchronously.
    ///  - A non-repeat timer reports completion via OnComplete only when it elapses.
    ///  - A repeat timer fires OnRewind once per elapsed interval, never on Start.
    ///  - Stop() is a cancel and must stay silent (no OnComplete).
    ///
    /// RED on current code: Start() invokes OnRewind synchronously, and Stop() invokes
    /// OnComplete. These tests are written first to drive the fix (see KNOWN_ISSUES).
    /// </summary>
    [TestFixture]
    public sealed class TimerTests
    {
        [Test]
        public void Start_DoesNotFireOnRewindSynchronously()
        {
            var timer   = new Timer(1f, repeat: false);
            var rewinds = 0;
            timer.OnRewind += () => rewinds++;

            timer.Start();

            rewinds.Should().Be(0, "a freshly started timer must wait its duration before acting");
        }

        [Test]
        public void NonRepeatTimer_FiresOnCompleteOnlyAfterDurationElapses()
        {
            var timer     = new Timer(1f, repeat: false);
            var completed = 0;
            timer.OnComplete += () => completed++;

            timer.Start();
            completed.Should().Be(0, "nothing has elapsed yet");

            timer.Tick(1f);
            completed.Should().Be(1, "the full duration has now elapsed");
        }

        [Test]
        public void RepeatTimer_FiresOnRewindEachInterval_NeverOnStart()
        {
            var timer   = new Timer(1f, repeat: true);
            var rewinds = 0;
            timer.OnRewind += () => rewinds++;

            timer.Start();
            rewinds.Should().Be(0, "the first interval has not elapsed yet");

            timer.Tick(1f);
            rewinds.Should().Be(1);

            timer.Tick(1f);
            rewinds.Should().Be(2);
        }

        [Test]
        public void Stop_IsACancel_AndDoesNotFireOnComplete()
        {
            var timer     = new Timer(1f, repeat: false);
            var completed = 0;
            timer.OnComplete += () => completed++;

            timer.Start();
            timer.Stop();

            completed.Should().Be(0, "Stop cancels the timer; only natural elapse completes it");
        }
    }
}

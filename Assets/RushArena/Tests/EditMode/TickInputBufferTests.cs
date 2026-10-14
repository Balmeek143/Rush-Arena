using NUnit.Framework;
using RushArena.Core;

namespace RushArena.Tests
{
    public class TickInputBufferTests
    {
        [Test]
        public void TapBetweenTicksSurvivesReleaseButFiresOnlyOnce()
        {
            var buffer = new TickInputBuffer();
            buffer.Capture(new PlayerCommand { JumpPressed = true });
            buffer.Capture(default);
            buffer.Capture(default);
            Assert.That(buffer.Consume().JumpPressed, Is.True);
            Assert.That(buffer.Consume().JumpPressed, Is.False);
        }

        [Test]
        public void CatchUpTicksKeepHeldControlsWithoutRepeatingLook()
        {
            var buffer = new TickInputBuffer();
            buffer.Capture(new PlayerCommand { MoveY = 1, Sprint = true, LookX = 12, LookY = -3 });
            var first = buffer.Consume();
            var second = buffer.Consume();
            Assert.That(first.LookX, Is.EqualTo(12));
            Assert.That(first.LookY, Is.EqualTo(-3));
            Assert.That(second.LookX, Is.Zero);
            Assert.That(second.LookY, Is.Zero);
            Assert.That(second.MoveY, Is.EqualTo(1));
            Assert.That(second.Sprint, Is.True);
            buffer.Capture(default);
            Assert.That(buffer.Consume().MoveY, Is.Zero);
            Assert.That(buffer.Consume().Sprint, Is.False);
        }

        [TestCase(30, 60)]
        [TestCase(60, 60)]
        [TestCase(144, 60)]
        [TestCase(144, 30)]
        public void IndependentRenderAndTickSchedulesPreserveTotalLook(int fps, int tickRate)
        {
            var buffer = new TickInputBuffer();
            int frame = 0, tick = 0;
            float yaw = 0, pitch = 0;
            // One second, deliberately mixing frames with zero ticks and multiple ticks.
            while (frame < fps || tick < tickRate)
            {
                if (frame < fps && (tick == tickRate || (frame + 1.0) / fps <= (tick + 1.0) / tickRate))
                {
                    buffer.Capture(new PlayerCommand { LookX = 90f / fps, LookY = -24f / fps });
                    frame++;
                }
                else
                {
                    var command = buffer.Consume();
                    yaw += command.LookX; pitch += command.LookY; tick++;
                }
            }
            var tail = buffer.Consume(); yaw += tail.LookX; pitch += tail.LookY;
            Assert.That(yaw, Is.EqualTo(90).Within(0.001f));
            Assert.That(pitch, Is.EqualTo(-24).Within(0.001f));
            Assert.That(buffer.Consume().LookX, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FocusOrLifecycleClearDiscardsAllPendingInput(bool inactiveFrame)
        {
            var buffer = new TickInputBuffer();
            var stale = new PlayerCommand { MoveX = 1, MoveY = 1, Sprint = true, JumpPressed = true, LookX = 90, LookY = 50 };
            buffer.Capture(stale);
            if (inactiveFrame) buffer.Capture(stale, false);
            else buffer.Clear();
            var command = buffer.Consume();
            Assert.That(command.MoveX, Is.Zero); Assert.That(command.MoveY, Is.Zero);
            Assert.That(command.LookX, Is.Zero); Assert.That(command.LookY, Is.Zero);
            Assert.That(command.Sprint, Is.False); Assert.That(command.JumpPressed, Is.False);
        }
    }
}

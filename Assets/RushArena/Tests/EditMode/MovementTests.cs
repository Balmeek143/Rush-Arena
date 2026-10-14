using NUnit.Framework;
using RushArena.Core;

namespace RushArena.Tests
{
    public class MovementTests
    {
        [Test]
        public void DiagonalCannotExceedStraightSpeed()
        {
            var model = new MovementModel(new MovementTuning());
            for (int i=0;i<120;i++) model.Step(new PlayerCommand { MoveX=1, MoveY=1 },1f/120f,true,false,0);
            Assert.That(model.HorizontalSpeed,Is.EqualTo(7f).Within(0.001f));
        }
        [Test]
        public void JumpEdgeCannotDoubleJump()
        {
            var model = new MovementModel(new MovementTuning());
            model.Step(default,1f/120f,true,false,0);
            model.Step(new PlayerCommand { JumpPressed=true },1f/120f,true,false,0);
            float first = model.VerticalSpeed;
            model.Step(new PlayerCommand { JumpPressed=true },1f/120f,false,false,0);
            Assert.That(model.JumpedThisStep,Is.False);
            Assert.That(model.VerticalSpeed,Is.LessThan(first));
        }
        [Test]
        public void CoyoteJumpSucceedsAfterWalkingOffEdge()
        {
            var model = new MovementModel(new MovementTuning());
            model.Step(default,1f/120f,true,false,0);
            for (int i=0;i<6;i++) model.Step(default,1f/120f,false,false,0);
            model.Step(new PlayerCommand { JumpPressed=true },1f/120f,false,false,0);
            Assert.That(model.JumpedThisStep,Is.True);
        }
        [Test]
        public void BufferedJumpTriggersOnLanding()
        {
            var model = new MovementModel(new MovementTuning());
            model.Step(new PlayerCommand { JumpPressed=true },1f/120f,false,false,0);
            Assert.That(model.JumpedThisStep,Is.False);
            model.Step(default,1f/120f,true,false,0);
            Assert.That(model.JumpedThisStep,Is.True);
        }
        [Test]
        public void PitchStaysWithinComfortableBounds()
        {
            Assert.That(LookModel.Pitch(0,1000),Is.EqualTo(-85));
            Assert.That(LookModel.Pitch(0,-1000),Is.EqualTo(85));
        }
    }
}

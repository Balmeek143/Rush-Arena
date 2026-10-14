using NUnit.Framework;
using RushArena.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace RushArena.Tests
{
    // Real Input System events with synthetic devices. These are not physical mobile tests.
    public class MobileInputTests
    {
        private Touchscreen screen;
        private MobileInput source;
        [SetUp]
        public void SetUp()
        {
            EnhancedTouchSupport.Enable();
            screen=InputSystem.AddDevice<Touchscreen>();
            source=new MobileInput(); source.UpdateLayout();
        }
        [TearDown]
        public void TearDown()
        {
            InputSystem.RemoveDevice(screen);
            EnhancedTouchSupport.Disable();
        }
        private void Send(int id,Vector2 position,TouchPhase phase)
        {
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,position=position,phase=phase});
        }
        private Vector2 Left => source.SafeArea.position+new Vector2(source.SafeArea.width*0.2f,source.SafeArea.height*0.6f);
        private Vector2 Right => source.SafeArea.position+new Vector2(source.SafeArea.width*0.8f,source.SafeArea.height*0.6f);
        [Test]
        public void SimultaneousMoveLookSprintAndJumpAreIndependent()
        {
            Send(1,Left,TouchPhase.Began); Send(2,Right,TouchPhase.Began);
            Send(3,source.SprintArea.center,TouchPhase.Began); Send(4,source.JumpArea.center,TouchPhase.Began);
            InputSystem.Update();
            var first=source.SampleWithFocus(true);
            Assert.That(first.JumpPressed,Is.True); Assert.That(first.Sprint,Is.True);
            Send(1,Left+Vector2.up*source.JoystickRadius,TouchPhase.Moved);
            Send(2,Right+Vector2.right*40f,TouchPhase.Moved);
            InputSystem.Update();
            var command=source.SampleWithFocus(true);
            Assert.That(command.MoveY,Is.EqualTo(1f).Within(0.001f));
            Assert.That(command.LookX,Is.GreaterThan(0f));
            Assert.That(command.Sprint,Is.True); Assert.That(command.JumpPressed,Is.False);
        }
        [Test]
        public void CancelledFingersReleaseMovementAndSprint()
        {
            Send(1,Left,TouchPhase.Began); Send(2,source.SprintArea.center,TouchPhase.Began);
            InputSystem.Update(); source.SampleWithFocus(true);
            Send(1,Left+Vector2.up*source.JoystickRadius,TouchPhase.Moved);
            InputSystem.Update(); Assert.That(source.SampleWithFocus(true).MoveY,Is.GreaterThan(0f));
            Send(1,Left,TouchPhase.Canceled); Send(2,source.SprintArea.center,TouchPhase.Canceled);
            InputSystem.Update();
            var command=source.SampleWithFocus(true);
            Assert.That(command.MoveX,Is.Zero); Assert.That(command.MoveY,Is.Zero); Assert.That(command.Sprint,Is.False);
        }
        [Test]
        public void FocusLossRequiresReleaseBeforeAcceptingNewGestures()
        {
            Send(1,Left,TouchPhase.Began); InputSystem.Update(); source.SampleWithFocus(true);
            source.SampleWithFocus(false);
            Send(1,Left+Vector2.up*source.JoystickRadius,TouchPhase.Moved);
            InputSystem.Update(); Assert.That(source.SampleWithFocus(true).MoveY,Is.Zero);
            Send(1,Left,TouchPhase.Ended); InputSystem.Update(); source.SampleWithFocus(true);
            InputSystem.Update(); source.SampleWithFocus(true);
            Send(2,Left,TouchPhase.Began); InputSystem.Update(); source.SampleWithFocus(true);
            Send(2,Left+Vector2.up*source.JoystickRadius,TouchPhase.Moved);
            InputSystem.Update(); Assert.That(source.SampleWithFocus(true).MoveY,Is.GreaterThan(0f));
        }
    }
}

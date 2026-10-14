using System.Collections;
using NUnit.Framework;
using RushArena.Core;
using RushArena.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace RushArena.Tests
{
    public class MotorPhysicsTests
    {
        private GameObject floor, player, wall, ceiling;
        private FirstPersonMotor motor;
        private readonly Vector3 origin = new Vector3(1000,0,1000);
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Isolate test geometry from any currently open arena scene.
            floor.transform.position = origin + new Vector3(0,-0.5f,0); floor.transform.localScale = new Vector3(100,1,100);
            player = new GameObject("Test player"); player.SetActive(false); player.transform.position = origin + new Vector3(0,0.05f,0);
            var controller = player.AddComponent<CharacterController>();
            controller.height=1.8f; controller.radius=0.35f; controller.center=new Vector3(0,0.9f,0);
            controller.skinWidth=0.035f; controller.minMoveDistance=0; controller.stepOffset=0.3f;
            var pivot = new GameObject("Test eye"); pivot.transform.SetParent(player.transform,false);
            motor = player.AddComponent<FirstPersonMotor>(); motor.Configure(pivot.transform);
            player.SetActive(true);
            motor.enabled=false; // Tests explicitly advance simulation; no hardware or Update dependence.
            Physics.SyncTransforms();
            yield return null;
            Advance(default,60);
        }
        private void Advance(PlayerCommand command,int frames,float seconds=1f/120f)
        {
            for (int i=0;i<frames;i++) { motor.Simulate(command,seconds); command.JumpPressed=false; }
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(player); Object.Destroy(floor); Object.Destroy(wall); Object.Destroy(ceiling);
            yield return null;
        }
        [UnityTest]
        public IEnumerator GroundAndWallCollisionPreventPenetration()
        {
            Assert.That(motor.Grounded,Is.True);
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=origin+new Vector3(0,2,3); wall.transform.localScale=new Vector3(20,4,0.5f);
            Physics.SyncTransforms();
            Advance(new PlayerCommand { MoveY=1, Sprint=true },300);
            Assert.That(player.transform.position.z-origin.z,Is.LessThan(2.5f));
            Assert.That(player.transform.position.y,Is.GreaterThan(-0.1f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator JumpReturnsToGroundWithoutAutomaticRepeat()
        {
            Advance(new PlayerCommand { JumpPressed=true },1);
            float apex=0;
            for(int i=0;i<180;i++) { motor.Simulate(default,1f/120f); apex=Mathf.Max(apex,player.transform.position.y); }
            Assert.That(apex,Is.InRange(1.25f,1.55f));
            Assert.That(motor.Grounded,Is.True);
            Assert.That(player.transform.position.y,Is.LessThan(0.1f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator CeilingStopsJumpAndAllowsLanding()
        {
            ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position=origin+new Vector3(0,2.4f,0); ceiling.transform.localScale=new Vector3(10,0.2f,10);
            Physics.SyncTransforms();
            float max=0;
            for(int i=0;i<180;i++) { motor.Simulate(new PlayerCommand{JumpPressed=i==0},1f/120f); max=Mathf.Max(max,player.transform.position.y); }
            Assert.That(max,Is.LessThan(0.6f));
            Assert.That(motor.Grounded,Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator FrameRateAndLookDeltaRemainStable()
        {
            Advance(new PlayerCommand{MoveY=1},60,1f/30f);
            float at30=player.transform.position.z;
            motor.Respawn(); Advance(default,60);
            Advance(new PlayerCommand{MoveY=1},288,1f/144f);
            Assert.That(player.transform.position.z,Is.EqualTo(at30).Within(0.12f));
            motor.Simulate(new PlayerCommand{LookX=90},1f/30f);
            Assert.That(player.transform.eulerAngles.y,Is.EqualTo(90).Within(0.01f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator SmallStepCanBeClimbed()
        {
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=origin+new Vector3(0,0.1f,2); wall.transform.localScale=new Vector3(4,0.2f,2);
            Physics.SyncTransforms();
            Advance(new PlayerCommand{MoveY=1},35,1f/120f);
            Assert.That(player.transform.position.z-origin.z,Is.GreaterThan(1f));
            Assert.That(player.transform.position.y,Is.GreaterThan(0.1f));
            yield return null;
        }
    }
}

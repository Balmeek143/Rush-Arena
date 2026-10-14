#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using RushArena.Core;
using RushArena.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RushArena.Diagnostics
{
    // Explicit opt-in for a built-player check; never enabled in normal gameplay.
    public sealed class PlayerSmokeProbe : MonoBehaviour
    {
        private void Awake() { Application.runInBackground = true; }
        private IEnumerator Start()
        {
            var motor = GetComponent<FirstPersonMotor>();
            motor.enabled = false;
            float startZ = transform.position.z;
            yield return Drive(motor,default,5f);
            yield return Drive(motor,new PlayerCommand{MoveY=1},1f);
            float walkDistance = transform.position.z-startZ;
            float sprintStart = transform.position.z;
            yield return Drive(motor,new PlayerCommand{MoveY=1,Sprint=true},0.5f);
            float sprintDistance = transform.position.z-sprintStart;
            motor.Simulate(new PlayerCommand{JumpPressed=true},1f/120f);
            float apex = transform.position.y;
            float end = Time.unscaledTime+1.5f;
            while(Time.unscaledTime<end)
            {
                motor.Simulate(default,Time.unscaledDeltaTime);
                apex=Mathf.Max(apex,transform.position.y);
                yield return null;
            }
            motor.Simulate(new PlayerCommand{LookX=90},1f/120f);
            bool passed = walkDistance>5.5f && walkDistance<8.0f && sprintDistance>3.5f && sprintDistance<6.0f &&
                apex>1.2f && apex<1.6f && motor.Grounded && Mathf.Abs(transform.eulerAngles.y-90f)<0.1f;
            string directory = Path.Combine(Application.persistentDataPath,"M1Smoke");
            Directory.CreateDirectory(directory);
            string screenshot = Path.Combine(directory,"arena.png");
            if(File.Exists(screenshot)) File.Delete(screenshot);
            // Hidden windows can produce an all-black backbuffer capture on Windows.
            // Render the actual scene through URP into a texture and verify its pixels.
            var target = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            target.Create();
            var camera = Camera.main;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            RenderTexture.active = previous;
            Color32[] pixels = image.GetPixels32();
            int visible = 0, magenta = 0, minimum = 765, maximum = 0;
            foreach(var pixel in pixels)
            {
                int brightness = pixel.r+pixel.g+pixel.b;
                minimum = Math.Min(minimum,brightness); maximum = Math.Max(maximum,brightness);
                if(brightness>30) visible++;
                if(pixel.r>220 && pixel.b>220 && pixel.g<50) magenta++;
            }
            bool rendered = visible>pixels.Length/4 && magenta<pixels.Length/100 && maximum-minimum>30;
            File.WriteAllBytes(screenshot,image.EncodeToPNG());
            Destroy(image); target.Release(); Destroy(target);
            passed = passed && rendered;
            string report=$"Result: {(passed?"PASS":"FAIL")}\nUnity: {Application.unityVersion}\nGPU: {SystemInfo.graphicsDeviceName}\n"+
                $"Walk 1s: {walkDistance:F3}m\nSprint 0.5s: {sprintDistance:F3}m\nJump apex: {apex:F3}m\nGrounded: {motor.Grounded}\n"+
                $"Yaw: {transform.eulerAngles.y:F2}\nRendered pixels: {visible}/{pixels.Length}; magenta: {magenta}\n"+
                $"Screenshot: {screenshot}\nURP offscreen scene capture; overlay HUD excluded. Scripted smoke only; human feel and physical mobile tests are pending.";
            File.WriteAllText(Path.Combine(directory,"result.txt"),report);
            GetComponent<PerformanceHud>().ExportReport();
            Debug.Log("RUSH built-player smoke: "+report);
            yield return null;
            Application.Quit(passed?0:1);
        }
        private IEnumerator Drive(FirstPersonMotor motor,PlayerCommand command,float duration)
        {
            float until=Time.unscaledTime+duration;
            while(Time.unscaledTime<until) { motor.Simulate(command,Time.unscaledDeltaTime); yield return null; }
        }
        public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(),"-rush-smoke")>=0;
    }
}
#endif

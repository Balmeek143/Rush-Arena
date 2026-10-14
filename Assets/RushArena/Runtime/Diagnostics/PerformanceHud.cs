using System;
using System.Globalization;
using System.IO;
using RushArena.Input;
using RushArena.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RushArena.Diagnostics
{
    public sealed class PerformanceHud : MonoBehaviour
    {
        private FirstPersonMotor motor;
        private PlayerInputSource input;
        private Text text;
        private GameObject canvasObject;
        private RectTransform jumpRect, sprintRect;
        private readonly float[] frameTimes = new float[600];
        private readonly float[] sortedTimes = new float[600];
        private int count, next;
        private float refreshAt;
        private float recordAfter;
        private float nextMobileExport = 60f;
        private ProfilerRecorder gc, mainThread;
        private string lastReport = "Collecting frame samples...";
        public void Configure(FirstPersonMotor player, PlayerInputSource source) { motor = player; input = source; }
        private void OnEnable()
        {
            recordAfter = Time.realtimeSinceStartup+3f;
            // Invalid counters are reported as unavailable, never as zero.
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        }
        private void Start()
        {
            canvasObject = new GameObject("Milestone diagnostics", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            text = CreateText("Performance", 15, TextAnchor.UpperLeft);
            text.rectTransform.anchorMin = new Vector2(0,1); text.rectTransform.anchorMax = new Vector2(0,1);
            text.rectTransform.pivot = new Vector2(0,1); text.rectTransform.sizeDelta = new Vector2(620,220);
            var crosshair = CreateText("Crosshair", 24, TextAnchor.MiddleCenter); crosshair.text = "+";
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f,0.5f);
            crosshair.rectTransform.sizeDelta = new Vector2(30,30);
            if (input != null && input.Mobile != null)
            {
                jumpRect = CreateButtonLabel("JUMP"); sprintRect = CreateButtonLabel("SPRINT");
                var moveLabel = CreateText("Touch move instruction", 18, TextAnchor.LowerLeft);
                moveLabel.text = "LEFT: MOVE      RIGHT: LOOK";
                moveLabel.rectTransform.anchorMin = moveLabel.rectTransform.anchorMax = Vector2.zero;
                moveLabel.rectTransform.pivot = Vector2.zero;
                moveLabel.rectTransform.sizeDelta = new Vector2(450,45);
                moveLabel.rectTransform.anchoredPosition = new Vector2(Screen.safeArea.xMin+18, Screen.safeArea.yMin+10);
            }
        }
        private Text CreateText(string label, int size, TextAnchor alignment)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(canvasObject.transform, false);
            var value = obj.GetComponent<Text>(); value.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            value.fontSize = size; value.alignment = alignment; value.color = Color.white; value.raycastTarget = false;
            return value;
        }
        private RectTransform CreateButtonLabel(string label)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(canvasObject.transform, false);
            obj.GetComponent<Image>().color = new Color(0.07f,0.35f,0.35f,0.8f); obj.GetComponent<Image>().raycastTarget = false;
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            var labelText = CreateText(label+" label", 18, TextAnchor.MiddleCenter); labelText.text = label;
            labelText.transform.SetParent(obj.transform, false);
            labelText.rectTransform.anchorMin = Vector2.zero; labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = labelText.rectTransform.offsetMax = Vector2.zero;
            return rect;
        }
        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            if(Time.realtimeSinceStartup >= recordAfter)
            {
                frameTimes[next] = ms; next = (next+1)%frameTimes.Length; count = Mathf.Min(count+1,frameTimes.Length);
            }
            if (text == null) return;
            Rect safe = Screen.safeArea;
            text.rectTransform.anchoredPosition = new Vector2(safe.xMin+14, -(Screen.height-safe.yMax+14));
            if (input != null && input.Mobile != null)
            {
                input.Mobile.UpdateLayout();
                Layout(jumpRect,input.Mobile.JumpArea); Layout(sprintRect,input.Mobile.SprintArea);
            }
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime+0.25f;
                if(count==0)
                {
                    text.text = "RUSH ARENA | M1 LOCAL\nWarming up performance metrics (3 seconds)...";
                    return;
                }
                Array.Copy(frameTimes,sortedTimes,count); Array.Sort(sortedTimes,0,count);
                float sum=0; for (int i=0;i<count;i++) sum+=frameTimes[i];
                float mean=sum/count, p95=sortedTimes[Mathf.Clamp(Mathf.CeilToInt(count*0.95f)-1,0,count-1)];
                string allocation = gc.Valid ? gc.LastValue.ToString(CultureInfo.InvariantCulture)+" B" : "unavailable";
                string cpu = mainThread.Valid ? (mainThread.LastValue/1000000.0).ToString("F2",CultureInfo.InvariantCulture)+" ms" : "unavailable";
                lastReport = $"RUSH ARENA | M1 LOCAL | {SystemInfo.graphicsDeviceType}\n"+
                    $"{1000f/Mathf.Max(0.001f,mean):F0} FPS | mean {mean:F2} ms | p95 {p95:F2} ms | samples {count}\n"+
                    $"Main thread (last): {cpu} | GC (last): {allocation}\n"+
                    $"Speed {motor?.Speed:F1} m/s | grounded {motor?.Grounded} | target {(Application.isMobilePlatform?60:144)} FPS\n"+
                    (input?.Mobile != null ? "Left drag move | right drag look | hold sprint | tap jump" : "Click to capture | WASD | Shift sprint | Space jump | Esc release | F8 report");
                text.text = lastReport;
            }
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) ExportReport();
            if (Application.isMobilePlatform && Time.unscaledTime >= nextMobileExport)
            {
                nextMobileExport = Time.unscaledTime+60f;
                ExportReport();
            }
        }
        private static void Layout(RectTransform target, Rect area)
        {
            if (target == null) return;
            target.anchoredPosition = area.position; target.sizeDelta = area.size;
        }
        [ContextMenu("Export performance report")]
        public void ExportReport()
        {
            string report = $"UTC: {DateTime.UtcNow:O}\nUnity: {Application.unityVersion}\nDevice: {SystemInfo.deviceModel}\n"+
                $"OS: {SystemInfo.operatingSystem}\nResolution: {Screen.width}x{Screen.height}\nEditor: {Application.isEditor}\n"+
                "Rolling window; HUD and refresh allocations included. Not an input latency or network measurement.\n"+lastReport;
            string path = Path.Combine(Application.persistentDataPath,$"rush-m1-performance-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.txt");
            File.WriteAllText(path,report); Debug.Log("RUSH performance report saved: "+path);
        }
        private void OnDisable() { gc.Dispose(); mainThread.Dispose(); }
        private void OnApplicationPause(bool paused) { if (paused && Application.isMobilePlatform) ExportReport(); }
        private void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
    }
}

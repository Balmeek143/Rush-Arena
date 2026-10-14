using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RushArena.Runtime
{
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string RushGetUrlQueryParam(string paramName);

        [DllImport("__Internal")]
        private static extern void RushCopyTextToClipboard(string text);
#endif

        public static string GetRoomQueryParam()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string val = RushGetUrlQueryParam("room");
                if (!string.IsNullOrEmpty(val)) return val;
            }
            catch (Exception) {}
#endif
            try
            {
                string url = Application.absoluteURL;
                if (!string.IsNullOrEmpty(url) && url.Contains("?"))
                {
                    string query = url.Substring(url.IndexOf('?') + 1);
                    string[] pairs = query.Split('&');
                    foreach (var pair in pairs)
                    {
                        string[] kv = pair.Split('=');
                        if (kv.Length == 2 && kv[0].Trim().Equals("room", StringComparison.OrdinalIgnoreCase))
                        {
                            return UnityEngine.Networking.UnityWebRequest.UnEscapeURL(kv[1].Trim());
                        }
                    }
                }
            }
            catch (Exception) {}
            return null;
        }

        public static string CopyInviteLink(string roomName)
        {
            if (string.IsNullOrEmpty(roomName)) roomName = "rush-dev-room";
            string baseUrl = "https://rush-arena.pages.dev/";
            try
            {
                string abs = Application.absoluteURL;
                if (!string.IsNullOrEmpty(abs))
                {
                    int qIdx = abs.IndexOf('?');
                    baseUrl = qIdx > 0 ? abs.Substring(0, qIdx) : abs;
                }
            }
            catch (Exception) {}

            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            string inviteUrl = $"{baseUrl}?room={Uri.EscapeDataString(roomName)}";

            // Universal clipboard
            GUIUtility.systemCopyBuffer = inviteUrl;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                RushCopyTextToClipboard(inviteUrl);
            }
            catch (Exception) {}
#endif
            return inviteUrl;
        }
    }
}

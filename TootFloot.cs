using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Timers;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace FrogtownRepoMods
{
    [BepInPlugin(TootFloot.PLUGIN_GUID, TootFloot.PLUGIN_NAME, TootFloot.PLUGIN_VERSION)]
    public class TootFloot : BaseUnityPlugin
    {
        public static TootFloot instance;

        public const string PLUGIN_GUID = "tootfloot.frogtown.me";
        public const string PLUGIN_NAME = "Toot Floot";
        public const string PLUGIN_VERSION = "0.1";
        private static ManualLogSource L;
        private static DateTime _lastRegenAttempt;
        // For some reason void crew keeps destroying the primary mod object, so this class gets regenerated and forwards the update event to the RUE.
        [Obsolete]
        public static void RegenInstance(object sender, ElapsedEventArgs e)
        {
            if (_lastRegenAttempt.AddSeconds(5) > DateTime.Now) { return; }
            _lastRegenAttempt = DateTime.Now;
            if (UpdateRunner.instance) { return; }
            var go = new GameObject("UpdateRunner");
            go.AddComponent<UpdateRunner>();
            L.LogInfo("Regenerated UpdateRunner");
        }
        public static void StartRegenThread()
        {
            var timer = new Timer(100);
            timer.Elapsed += RegenInstance;
            timer.AutoReset = true;
            timer.Enabled = true;
        }
        private void Awake()
        {
            if (TootFloot.instance != null) { return; }
            L = Logger;
            instance = this;
            StartRegenThread();
            Harmony.CreateAndPatchAll(typeof(PatchPhysGrabberUpdate));
        }
        class PatchPhysGrabberUpdate {
            [HarmonyPatch(typeof(PhysGrabber), "Update")]
            [HarmonyPostfix()]
            private static void Update(PhysGrabber __instance) {
                if (!UpdateRunner.instance || __instance != UpdateRunner.instance.trackedGrabber) {
                    return;
                }
                float tSinceRelease = Time.time - UpdateRunner.instance.lastGrabRelease;
                if (tSinceRelease < .25f) {
                    var pt = UpdateRunner.instance.lastGrabbedItem;
                    if (pt) {
                        pt.position = new Vector3(pt.position.x, UpdateRunner.instance.grabHeight, pt.position.z);
                    }
                }
            }
        }
        class UpdateRunner : MonoBehaviour {
            public static UpdateRunner instance;
            void Awake() {
                L.LogInfo("Awake");
                instance = this;
            }
            void OnDestroy() {
                L.LogInfo("OnDestroy");
                instance = null;
            }
            private float lastTimeChange;
            public bool playing;
            public bool tilted;
            public float storedXRot;
            public PhysGrabber trackedGrabber;
            public float grabHeight;
            public Transform lastGrabbedItem;
            public float lastGrabRelease;
            void Update() {
                try {
                    if (!SemiFunc.IsMultiplayer()) {
                        return;
                    }
                    var cameraAim = PlayerController.instance.cameraAim;
                    var playerAvatar = PlayerController.instance.playerAvatarScript;
                    float newAng = -1;
                    KeyCode[] keys = new [] { KeyCode.G, KeyCode.H, KeyCode.U, KeyCode.J, KeyCode.I, KeyCode.K, KeyCode.L, KeyCode.P, KeyCode.Semicolon, KeyCode.LeftBracket, KeyCode.Quote };
                    float[] angs = new [] { 80, 56.3f, 45.7f, 37.16f, 28.06f, 20.18f, 11.08f, 2.44f, 353.64f, 342.77f, 330f };

                    bool anyDown = false;
                    trackedGrabber = playerAvatar.physGrabber;
                    if (trackedGrabber.grabbedObjectTransform && trackedGrabber.grabbedObjectTransform.name == "Valuable Ocarina(Clone)") {
                        for (int i = 0; i < keys.Length; i++) {
                            if (UnityEngine.Input.GetKeyDown(keys[i])) { newAng = angs[i]; }
                            if (UnityEngine.Input.GetKey(keys[i])) {
                                anyDown = true;
                                if (newAng == -1) { newAng = angs[i]; }
                            }
                        }
                    }

                    if (newAng > 0) {
                        if (!playing) {
                            grabHeight = trackedGrabber.grabbedObjectTransform.position.y;
                            lastGrabbedItem = trackedGrabber.grabbedObjectTransform;
                        }
                        if (!tilted) {
                            storedXRot = cameraAim.transform.rotation.eulerAngles.x;
                        }
                        tilted = true;
                        lastGrabRelease = Time.time;
                        cameraAim.transform.rotation = Quaternion.Euler(newAng, cameraAim.transform.rotation.eulerAngles.y, cameraAim.transform.rotation.eulerAngles.z);
                        MethodInfo resetPlayerAim = cameraAim.GetType().GetMethod("ResetPlayerAim", BindingFlags.NonPublic | BindingFlags.Instance);
                        resetPlayerAim.Invoke(cameraAim, new object[] { cameraAim.transform.rotation });

                        if (!playing && Time.time - lastTimeChange > .2) {
                            playing = true;
                            playerAvatar.ChatMessageSend("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false);
                            lastTimeChange = Time.time;
                        }
                    }
                    
                    if (!anyDown && playing && Time.time - lastTimeChange > .2){
                        playing = false;
                        playerAvatar.ChatMessageSend(".", false);
                        lastTimeChange = Time.time;
                    }
                    
                    if (!anyDown && tilted){
                        tilted = false;
                        cameraAim.transform.rotation = Quaternion.Euler(storedXRot, cameraAim.transform.rotation.eulerAngles.y, cameraAim.transform.rotation.eulerAngles.z);
                        MethodInfo resetPlayerAim = cameraAim.GetType().GetMethod("ResetPlayerAim", BindingFlags.NonPublic | BindingFlags.Instance);
                        resetPlayerAim.Invoke(cameraAim, new object[] { cameraAim.transform.rotation });
                    }
                } catch (Exception _) {}
            }
        }
    }
}

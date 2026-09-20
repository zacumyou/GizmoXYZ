using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.InputSystem;

namespace GizmoXYZ {
public sealed class Mod : IMod {
    internal const string Version = "0.6.11";
    internal static Settings Options;
    internal static bool Ready;
    internal static Game.Input.ProxyAction HoldAction;
    private static readonly HoldInputGate HoldGate = new HoldInputGate();
    private static bool hookSeen;
    private object harmony;
    private Type harmonyType;
    private Locale en, ko;
    public void OnLoad(UpdateSystem updates) {
        Diagnostics.Start();
        try {
            string hash;
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(typeof(ObjectToolSystem).Assembly.Location))
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            Diagnostics.Event("compatibility.check", hash);
            if (hash != "AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A")
                throw new InvalidOperationException("Game.dll changed: Gizmo XYZ requires API review before enabling.");
            Native.Check();
            Options = new Settings(this);
            AssetDatabase.global.LoadSettings("GizmoXYZ", Options, new Settings(this));
            Options.RegisterKeyBindings(); Options.RegisterInOptionsUI();
            HoldAction = Options.GetAction("Hold");
            en = new Locale(Options, false); ko = new Locale(Options, true);
            GameManager.instance.localizationManager.AddSource("en-US", en);
            GameManager.instance.localizationManager.AddSource("ko-KR", ko);
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "0Harmony");
            if (assembly == null) {
                if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset) || string.IsNullOrEmpty(asset.path))
                    throw new InvalidOperationException("Mod installation path unavailable");
                assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(asset.path), "0Harmony.dll"));
            }
            harmonyType = assembly.GetType("HarmonyLib.Harmony", true);
            harmony = Activator.CreateInstance(harmonyType, "zacum.GizmoXYZ");
            var hm = assembly.GetType("HarmonyLib.HarmonyMethod", true);
            var patch = harmonyType.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5);
            foreach (var entry in new[] { (Native.Update, nameof(UpdatePrefix)) }) {
                var prefix = Activator.CreateInstance(hm, typeof(Mod).GetMethod(entry.Item2, BindingFlags.Static | BindingFlags.NonPublic));
                patch.Invoke(harmony, new object[] { entry.Item1, prefix, null, null, null });
            }
            updates.UpdateAt<GizmoEditTool>(SystemUpdatePhase.ToolUpdate);
            updates.UpdateAt<GizmoSelectionTool>(SystemUpdatePhase.ToolUpdate);
            updates.UpdateBefore<GizmoSelectionDefinitionSystem,Game.Tools.GenerateObjectsSystem>(SystemUpdatePhase.Modification1);
            updates.UpdateAt<GizmoUI>(SystemUpdatePhase.UIUpdate);
            updates.UpdateBefore<GizmoDefinitionSystem, GenerateObjectsSystem>(SystemUpdatePhase.Modification1);
            Ready = true;
            Diagnostics.Event("mod.loaded", "Gizmo XYZ " + Version + " | pre-dispatch Shift-click | Game + Editor | native preview | 30 Hz drag preview");
        } catch (Exception e) { Diagnostics.Failure("mod.load.failed", e); OnDispose(); throw; }
    }
    private static bool UpdatePrefix(ObjectToolSystem __instance, JobHandle __0, ref JobHandle __result) {
        if (!Ready) return true;
        var session = __instance.World.GetOrCreateSystemManaged<GizmoSession>();
        try {
            if (!hookSeen) { hookSeen=true; Diagnostics.Event("tool.hook.seen", __instance.GetType().FullName); }
            var input = Game.Input.InputManager.instance;
            bool active = __instance.World.GetOrCreateSystemManaged<ToolSystem>().activeTool == __instance;
            bool create = __instance.actualMode == ObjectToolSystem.Mode.Create;
            bool focused = UnityEngine.Application.isFocused;
            HoldAction.shouldBeEnabled = active && create && !session.Held && focused && !input.hasInputFieldFocus && !input.overlayActive && input.controlOverWorld;
            // Registered mod binding is the primary path. The exact physical chord is also
            // sampled here so native modifier filtering cannot swallow Shift + left click.
            bool raw = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            bool bound = HoldAction.WasPressedThisFrame();
            if (raw || bound) Diagnostics.Event("input.hold.request", $"frame={UnityEngine.Time.frameCount} raw={raw} binding={bound} enabled={HoldAction.enabled} active={active} mode={__instance.actualMode} focused={focused} field={input.hasInputFieldFocus} overlay={input.overlayActive} world={input.controlOverWorld} held={session.Held}");
            if (HoldGate.TryConsume(UnityEngine.Time.frameCount,raw||bound,Ready,active,create,focused,input.hasInputFieldFocus,input.overlayActive,input.controlOverWorld,session.Held)) {
                __0.Complete(); session.Hold(__instance);
                Native.SetApply(__instance, ApplyMode.None);
                __result=__0; return false; // Never run native placement for this same press.
            }
            if (!session.Held) return true;
            __0.Complete();
            if (!session.Matches(__instance)) { session.Release("prefab or tool mode changed"); return true; }
            __result = session.Tick(__instance, __0); return false;
        } catch (Exception e) {
            Diagnostics.Failure("tool.exception", e); session.Release("tool exception");
            Native.SetApply(__instance, ApplyMode.Clear); __result = __0; return false;
        }
    }
    public void OnDispose() {
        Ready = false;
        if (HoldAction != null) HoldAction.shouldBeEnabled=false;
        var w = World.DefaultGameObjectInjectionWorld;
        if (w != null && w.IsCreated) w.GetExistingSystemManaged<GizmoSession>()?.Release("mod disposed");
        harmonyType?.GetMethod("UnpatchAll")?.Invoke(harmony, new object[] { "zacum.GizmoXYZ" });
        harmony = null;
        Options?.UnregisterInOptionsUI();
        if (en != null) GameManager.instance.localizationManager.RemoveSource("en-US", en);
        if (ko != null) GameManager.instance.localizationManager.RemoveSource("ko-KR", ko);
        Options = null; Diagnostics.Stop();
    }
}
internal static class Native {
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly MethodInfo Update = typeof(ObjectToolSystem).GetMethod("OnUpdate", Flags, null, new[] { typeof(JobHandle) }, null);
    internal static readonly MethodInfo Definitions = typeof(ObjectToolSystem).GetMethod("UpdateDefinitions", Flags);
    internal static readonly MethodInfo Allow = typeof(ObjectToolSystem).GetMethod("GetAllowApply", Flags);
    private static readonly MethodInfo Destroy = typeof(ToolBaseSystem).GetMethod("DestroyDefinitions", Flags);
    private static readonly FieldInfo Query = typeof(ObjectToolSystem).GetField("m_DefinitionQuery", Flags);
    private static readonly FieldInfo Barrier = typeof(ObjectToolBaseSystem).GetField("m_ToolOutputBarrier", Flags);
    private static readonly FieldInfo ForceUpdate = typeof(ToolBaseSystem).GetField("m_ForceUpdate", Flags);
    private static readonly PropertyInfo ApplyModeProperty = typeof(ToolBaseSystem).GetProperty("applyMode", Flags);
    internal static void Check() {
        if (Update == null || Definitions == null || Allow == null || Destroy == null || Query == null || Barrier == null || ForceUpdate == null || ApplyModeProperty?.GetSetMethod(true) == null)
            throw new MissingMemberException("Native placement contract changed");
    }
    internal static void SetApply(ObjectToolSystem tool, ApplyMode mode) => ApplyModeProperty.SetValue(tool, mode);
    internal static void Resume(ObjectToolSystem tool) => ForceUpdate.SetValue(tool, true);
    internal static JobHandle Rebuild(ObjectToolSystem tool, JobHandle deps) => (JobHandle)Definitions.Invoke(tool, new object[] { deps });
    internal static bool CanApply(ObjectToolSystem tool) => (bool)Allow.Invoke(tool, null);
    internal static JobHandle ClearDefinitions(ObjectToolSystem tool, JobHandle deps) => (JobHandle)Destroy.Invoke(tool, new[] { Query.GetValue(tool), Barrier.GetValue(tool), (object)deps });
}
}





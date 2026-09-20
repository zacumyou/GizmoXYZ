using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

namespace GizmoXYZ {
[FileLocation("GizmoXYZ")]
[SettingsUIMouseAction("Hold", ActionType.Button, usages: new[] { "GizmoXYZ.Placement" }, modifierOptions: ModifierOptions.Allow)]
[SettingsUIKeyboardAction("Confirm", ActionType.Button, usages: new[] { "GizmoXYZ.Held" })]
[SettingsUIKeyboardAction("Cancel", ActionType.Button, usages: new[] { "GizmoXYZ.Held" })]
[SettingsUIKeyboardAction("Space", ActionType.Button, usages: new[] { "GizmoXYZ.Held" })]
[SettingsUIKeyboardAction("Ground", ActionType.Button, usages: new[] { "GizmoXYZ.Held" })]
[SettingsUIKeyboardAction("Rotate", ActionType.Button, usages: new[] { "GizmoXYZ.Held" })]
[SettingsUIKeyboardAction("EditPanel", ActionType.Button, usages: new[] { "GizmoXYZ.Selected" })]
[SettingsUIKeyboardAction("Edit", ActionType.Button, usages: new[] { "GizmoXYZ.Selected" })]
[SettingsUIKeyboardAction("Duplicate", ActionType.Button, usages: new[] { "GizmoXYZ.CopyIt" })]
public sealed class Settings : ModSetting {
    public Settings(IMod mod) : base(mod) { }
    [SettingsUIHidden] public bool LastLocal {get;set;}=true;
    [SettingsUIKeyboardBinding(BindingKeyboard.T, "Space")]
    [SettingsUISection("General")] public ProxyBinding SpaceKey {get;set;}
    [SettingsUIKeyboardBinding(BindingKeyboard.G, "Ground")]
    [SettingsUISection("General")] public ProxyBinding GroundKey {get;set;}
    [SettingsUIMouseBinding(BindingMouse.Left, "Hold", shift: true)]
    [SettingsUISection("General")]
    public ProxyBinding HoldKey { get; set; }
    [SettingsUIKeyboardBinding(BindingKeyboard.Enter, "Confirm")]
    [SettingsUISection("General")]
    public ProxyBinding ConfirmKey { get; set; }
    [SettingsUIKeyboardBinding(BindingKeyboard.Escape, "Cancel")]
    [SettingsUISection("General")]
    public ProxyBinding CancelKey { get; set; }
    [SettingsUIKeyboardBinding(BindingKeyboard.R, "Rotate")]
    [SettingsUISection("General")]
    public ProxyBinding RotateKey { get; set; }
    [SettingsUIKeyboardBinding(BindingKeyboard.M, "Edit", shift: true)]
    [SettingsUISection("General")]
    public ProxyBinding EditKey { get; set; }
    [SettingsUIKeyboardBinding(BindingKeyboard.T, "EditPanel", ctrl: true)]
    [SettingsUISection("General")]
    public ProxyBinding EditPanelKey {get;set;}
    [SettingsUIKeyboardBinding(BindingKeyboard.D, "Duplicate", ctrl: true)]
    [SettingsUISection("General")]
    public ProxyBinding DuplicateKey {get;set;}
    [SettingsUISection("General")]
    public bool DiagnosticDetail { get; set; } = true;
    [SettingsUISection("General")]
    public string EventLog => Diagnostics.PathName ?? "Logs/GizmoXYZ";
    public override void SetDefaults() { DiagnosticDetail = true; LastLocal=true; }
}
internal sealed class Locale : IDictionarySource {
    private readonly Settings s; private readonly bool ko;
    internal Locale(Settings settings, bool korean) { s = settings; ko = korean; }
    public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts) => new Dictionary<string, string> {
        [s.GetBindingKeyLocaleID("EditPanel")] = ko?"선택 창에서 기즈모 편집":"Edit selection and close panel",
        [s.GetOptionLabelLocaleID(nameof(Settings.EditPanelKey))] = ko?"선택 창에서 기즈모 편집":"Edit selection and close panel",
        [s.GetOptionDescLocaleID(nameof(Settings.EditPanelKey))] = "Ctrl + T",
        [s.GetBindingKeyLocaleID("Space")] = ko?"로컬·글로벌 전환":"Toggle local/global axes",
        [s.GetOptionLabelLocaleID(nameof(Settings.SpaceKey))] = ko?"로컬·글로벌 전환":"Toggle local/global axes",
        [s.GetOptionDescLocaleID(nameof(Settings.SpaceKey))] = "T",
        [s.GetBindingKeyLocaleID("Ground")] = ko?"지면 높이에 맞추기":"Snap to ground",
        [s.GetOptionLabelLocaleID(nameof(Settings.GroundKey))] = ko?"지면 높이에 맞추기":"Snap to ground",
        [s.GetOptionDescLocaleID(nameof(Settings.GroundKey))] = "G",
        [s.GetSettingsLocaleID()] = "Gizmo XYZ",
        [s.GetBindingMapLocaleID()] = "Gizmo XYZ",
        [s.GetBindingKeyLocaleID("Duplicate")] = ko?"선택 오브젝트·그룹 복제":"Duplicate selected object or group",
        [s.GetOptionLabelLocaleID(nameof(Settings.DuplicateKey))] = ko?"선택 오브젝트·그룹 복제":"Duplicate selected object or group",
        [s.GetOptionDescLocaleID(nameof(Settings.DuplicateKey))] = "Ctrl+D",
        [s.GetBindingKeyLocaleID("Rotate")] = ko ? "이동·회전 전환" : "Toggle rotation",
        [s.GetOptionLabelLocaleID(nameof(Settings.RotateKey))] = ko ? "이동·회전 전환" : "Toggle rotation",
        [s.GetOptionDescLocaleID(nameof(Settings.RotateKey))] = ko ? "R · Shift 드래그: 15° 간격" : "R · Shift drag: 15°",
        [s.GetBindingKeyLocaleID("Edit")] = ko ? "선택 오브젝트 편집" : "Edit selected object",
        [s.GetOptionLabelLocaleID(nameof(Settings.EditKey))] = ko ? "선택 오브젝트 편집" : "Edit selected object",
        [s.GetOptionDescLocaleID(nameof(Settings.EditKey))] = "Shift + M",
        [s.GetOptionGroupLocaleID("General")] = ko ? "배치와 진단" : "Placement and diagnostics",
        [s.GetBindingKeyLocaleID("Hold")] = ko ? "배치 프리뷰 고정" : "Hold placement preview",
        [s.GetOptionLabelLocaleID(nameof(Settings.HoldKey))] = ko ? "배치 프리뷰 고정" : "Hold placement preview",
        [s.GetOptionDescLocaleID(nameof(Settings.HoldKey))] = ko ? "기본 Shift+왼쪽 클릭. 일반 배치 실행 전 입력을 감지하여 프리뷰를 고정합니다." : "Default Shift + left click. Captured before native placement dispatch.",
        [s.GetBindingKeyLocaleID("Confirm")] = ko ? "생성 후 계속 배치" : "Place and continue",
        [s.GetBindingKeyLocaleID("Cancel")] = ko ? "고정 취소" : "Release held preview",
        [s.GetOptionLabelLocaleID(nameof(Settings.ConfirmKey))] = ko ? "생성 후 계속 배치" : "Place and continue",
        [s.GetOptionDescLocaleID(nameof(Settings.ConfirmKey))] = ko ? "Shift+클릭으로 고정한 프리뷰를 생성합니다. 기즈모는 유지됩니다." : "Place the held preview and keep the gizmo for the next placement.",
        [s.GetOptionLabelLocaleID(nameof(Settings.CancelKey))] = ko ? "고정 취소" : "Release held preview",
        [s.GetOptionDescLocaleID(nameof(Settings.CancelKey))] = ko ? "일반 마우스 배치로 돌아갑니다. 이미 생성한 오브젝트는 유지됩니다." : "Return to normal placement; already placed objects are preserved.",
        [s.GetOptionLabelLocaleID(nameof(Settings.DiagnosticDetail))] = ko ? "상세 테스트 로그" : "Detailed test logging",
        [s.GetOptionDescLocaleID(nameof(Settings.DiagnosticDetail))] = ko ? "축 드래그 위치와 프리뷰 갱신 시간을 기록합니다. 프레임마다 파일을 쓰지 않습니다." : "Log sampled drag positions and preview timing; no per-frame file writes.",
        [s.GetOptionLabelLocaleID(nameof(Settings.EventLog))] = ko ? "현재 이벤트 로그" : "Current event log",
        [s.GetOptionDescLocaleID(nameof(Settings.EventLog))] = ko ? "UTC 시간 · 고정 · 드래그 · 검증 · 확정 · 소리 · 반복 · 오류" : "UTC timestamps · hold · drag · validation · commit · sound · repeat · errors"
    }.Concat(Catalog.Entries(ko));
    public void Unload() { }
}
}



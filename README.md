# Gizmo XYZ

Move, rotate and duplicate buildings, props and trees with a 3D gizmo in Cities: Skylines II.

## Main features

- **Ctrl+T:** edit the selected object. The selection panel closes and Gizmo opens.
- **Ctrl+D:** duplicate the selected object. With Copy It active, duplicate its selection.
- Drag an axis to move. Drag the center for free movement.
- **R:** switch between move and rotate. Hold **Shift** to rotate in 15° steps.
- **T:** switch between local and global axes.
- **G:** align the pivot with terrain height.
- Pick a rotation pivot from the nine-point selector.
- **Enter:** confirm and continue. In duplicate mode, place another copy.
- **Esc:** cancel the pending preview. Confirmed objects remain.
- Optional Anarchy and Transform Lock buttons appear when Anarchy is available.

English and Korean are included. Community translations use I18NEveryWhere-compatible JSON. Copy It, Anarchy and I18NEveryWhere are optional.

Version 0.6.11 targets game 1.6.2f1. Standalone copies regenerate the selected prefab; they do not copy residents, companies or separately edited children/upgrades. Network and surface editing are outside standalone selection support.

## Bugs and feedback

Use [GitHub Issues](https://github.com/zacumyou/GizmoXYZ/issues). Include reproduction steps, game/mod versions, a screenshot and the relevant log from:

`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\GizmoXYZ`

## Build

Install the official Cities: Skylines II Modding Toolchain, .NET 9 SDK and Node.js 18 or newer. The game SDK environment variables must be set. Game assemblies are not included.

```powershell
cd ui
npm ci
npm run build
cd ..
dotnet build src/GizmoXYZ.csproj -c Release
dotnet run --project tests/Tests.csproj -c Release
node tests/localization.cjs
node tests/gizmo-view.cjs
node tests/actions-insertion.cjs
```

Build output: `local/GizmoXYZ`. See [TRANSLATING.md](TRANSLATING.md) for translations and [NOTICE.md](NOTICE.md) for references and third-party notices.

---

# Gizmo XYZ — 한국어

시티즈: 스카이라인 II의 건물·프롭·나무를 기즈모로 이동·회전·복제합니다.

## 주요 기능

- **Ctrl+T:** 선택한 오브젝트를 편집합니다. 선택 창이 닫히고 Gizmo가 열립니다.
- **Ctrl+D:** 선택한 오브젝트를 복제합니다. Copy It 활성화 중에는 선택 그룹을 복제합니다.
- 축을 드래그해 이동합니다. 중심점을 드래그하면 자유롭게 움직입니다.
- **R:** 이동·회전을 전환합니다. 회전 중 **Shift**를 누르면 15°씩 움직입니다.
- **T:** 로컬·글로벌 축을 전환합니다.
- **G:** 기준점을 지면 높이에 맞춥니다.
- 9점 선택기로 회전 기준점을 바꿉니다.
- **Enter:** 확정 후 계속 편집합니다. 복제 모드에서는 다음 복사본을 배치합니다.
- **Esc:** 미확정 프리뷰를 취소합니다. 확정한 오브젝트는 유지됩니다.
- Anarchy가 있으면 Anarchy·Transform Lock 버튼을 사용할 수 있습니다.

한국어·영어를 지원합니다. I18NEveryWhere 호환 JSON으로 사용자 번역을 추가할 수 있습니다. Copy It·Anarchy·I18NEveryWhere는 선택 사항입니다.

0.6.11은 게임 1.6.2f1용입니다. 단일 복제는 프리팹을 새로 배치합니다. 주민·기업·개별 수정된 자식 오브젝트·추가 확장 건물은 복제하지 않습니다. 단독 선택의 도로·표면 편집은 지원하지 않습니다.

## 오류·버그 제보

[GitHub Issues](https://github.com/zacumyou/GizmoXYZ/issues)에 재현 방법, 게임·모드 버전, 스크린샷과 해당 실행의 로그를 첨부해 주세요.

`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\GizmoXYZ`

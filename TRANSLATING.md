# Gizmo XYZ — 사용자 번역 / User translations

Gizmo XYZ는 게임의 표준 `IDictionarySource`와 `cs2/l10n`을 사용합니다. I18NEveryWhere는 이 사전의 조회를 가로채 번역 JSON을 적용합니다. 별도 DLL 의존성은 없으며, I18N 없이도 한국어·영어가 동작합니다.

## 번역하는 방법

1. 설치된 `GizmoXYZ/lang/en-US.json`을 복사합니다.
2. `lang/ja-JP.json`, `lang/de-DE.json`처럼 지원하려는 게임 언어 코드로 저장합니다.
3. 왼쪽 키는 유지하고 오른쪽 문자열만 번역합니다. UTF-8로 저장하고 `{0}`, `{1}` 같은 자리표시자를 보존하세요. 문장 안에서 순서는 바꿔도 됩니다.
4. I18NEveryWhere를 활성화하고 해당 게임 언어를 선택합니다. 번역 파일 수정 후 I18N의 Reload를 사용합니다. 열린 UI가 갱신되지 않으면 언어를 전환하거나 게임을 다시 실행합니다.
5. 기본 한국어·영어처럼 이미 등록된 번역을 바꾸려면 I18N의 **Enable overwrite** 설정을 켜야 합니다. Gizmo XYZ가 이 설정을 임의 변경하지 않습니다.

0.6.11부터 번역 JSON에는 UI·툴팁·상태 문구 72개와 설정 메뉴·단축키 문구 34개, 총 106개 항목이 있습니다. `Options.*`로 시작하는 키도 그대로 번역하세요. I18N 개발자 메뉴에서 GizmoXYZ.Locale 사전을 내보내는 방법도 사용할 수 있습니다. 단축키에 표시되는 실제 키 이름과 에셋 이름은 게임에서 제공하는 값입니다.

번역이 빠진 UI 문구는 영어로 표시됩니다. 기존 번역 키는 유지하며, 상태 메시지도 문장 전체와 인수를 분리해 번역할 수 있습니다.

## 업데이트와 별도 언어팩

기본 en-US.json/ko-KR.json은 업데이트 때 교체됩니다. 직접 수정했다면 백업하세요. 다른 언어 JSON은 로컬 설치 스크립트가 삭제하지 않습니다. 배포하거나 모드 업데이트와 독립적으로 관리하려면 I18N 언어팩을 사용할 수 있습니다.

예: 게임 사용자 데이터 폴더 아래 `ModsData/GizmoXYZLocalization/i18n.json`:

```json
{
  "Name": "Gizmo XYZ translations",
  "IncludedLanguage": "ja-JP",
  "Author": "Your name",
  "Description": "Community translations for Gizmo XYZ"
}
```

번역은 `ModsData/GizmoXYZLocalization/Localization/ja-JP/GizmoXYZ.json`에 둡니다. 이 경로는 I18N의 새 모드 탐색 및 언어팩 로딩 옵션을 필요로 합니다. Gizmo XYZ의 모드 루트에 i18n.json을 넣지 마세요. I18N이 일반 모드를 언어팩으로 잘못 분류할 수 있습니다.

## English

Copy `lang/en-US.json` to `lang/<game-locale-id>.json`. Translate values, retaining keys and placeholders. The 106 entries include all Gizmo UI/status strings plus options and keybinding labels. Enable I18N's overwrite setting to replace existing native translations. Use a separate language pack to survive mod updates. English/Korean work without I18N.

Contributors: edit `localization/catalog.json` for tool UI/status text and `src/Settings.cs` for options. `node tools/generate-localization.cjs` exports both into lang JSON; `tools/settings-localization.cjs` maps verified native ModSetting keys. UI builds regenerate catalogs; rebuild C# after catalog changes. Preserve existing keys.

## 검토한 구현 / Reviewed implementation

Reviewed upstream main on 2026-09-21:
- https://github.com/baka-gourd/I18NEveryWhere/blob/main/I18NEverywhere/Mod.cs — embedded lang JSON, native source export, language-pack discovery.
- https://github.com/baka-gourd/I18NEveryWhere/blob/main/I18NEverywhere/HookLocalizationDictionary.cs — dictionary lookup and overwrite option.
- Installed Game.dll ModSetting and InputManager — actual Options.* and action/binding key formats.

Build and render tests cover translation lookup, overrides, fallback, placeholders and exported options. Actual I18NEveryWhere loading/reload inside the game remains unverified. This update does not install I18N or change subscriptions/settings.

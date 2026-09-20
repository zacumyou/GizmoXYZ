import React, { useEffect } from "react";
import { Tooltip } from "cs2/ui";
import { bindValue, useValue, trigger } from "cs2/api";
import {useText} from "./localization";
import icon from "./gizmo.svg";
import styles from "./gizmo.module.scss";
const editable$ = bindValue<boolean>("GizmoXYZ", "editable", false);
const reason$ = bindValue<string>("GizmoXYZ", "editReason", "선택 상태 확인 중");
export function SelectedAction() {
    const t=useText();
    const enabled = useValue(editable$);
    const reason = t(useValue(reason$));
    useEffect(() => { trigger("GizmoXYZ", "diagnostic", `Selected action mounted · enabled=${enabled} · ${reason}`); }, [enabled, reason]);
    return <Tooltip tooltip={<span className={styles.tooltipText}>Gizmo XYZ · {enabled ? t("선택한 오브젝트 편집 · Ctrl+T / Shift+M") : reason}</span>}><button className={`${styles.selectedAction} ${enabled ? "" : styles.unavailable}`} aria-label={t("Gizmo XYZ 편집")} aria-disabled={!enabled} onClick={() => { if (enabled)
        trigger("GizmoXYZ", "command", "edit"); }}><img src={icon} alt=""/></button></Tooltip>;
}

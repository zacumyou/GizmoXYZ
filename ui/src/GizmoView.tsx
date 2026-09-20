import React, { useState, useEffect } from "react";
import styles from "./gizmo.module.scss";
import globalIcon from "./global.svg";
import localIcon from "./local.svg";
import placeIcon from "./place.svg";
import closeIcon from "./close.svg";
import anarchyIcon from "./anarchy.svg";
import lockIcon from "./lock.svg";
import rotateIcon from "./rotate.svg";
import moveIcon from "./gizmo.svg";
import referenceIcon from "./reference.svg";
import groundIcon from "./ground.svg";
import {format,TextTranslator} from "./text";
export interface GizmoProps {
    translate?:TextTranslator;
    shortcuts?:string;
    referenceAvailable?:boolean;
    pickingReference?:boolean;
    referenceName?:string;
    referenceOutline?:string;
    held: boolean;
    local: boolean;
    canPlace: boolean;
    axis: number;
    placed: number;
    projection: string;
    status: string;
    command: (value: string) => void;
    editing?: boolean;
    anarchyAvailable?: boolean;
    anarchyOn?: boolean;
    lockOn?: boolean;
    pivotIndex?: number;
    rotating?: boolean;
    rings?: string;
    markers?: string;
    ticks?: string;
    renderTooltip?: (text: string, child: React.ReactElement) => React.ReactElement;
}
const colors = ["#f47670", "#8bcd7a", "#69aaf9"], labels = ["X", "Y", "Z"];
function PreviewTooltip({ text, children }: {
    text: string;
    children: React.ReactElement;
}) {
    const [hover, setHover] = useState(false);
    return <div onMouseEnter={() => setHover(true)} onMouseLeave={() => setHover(false)}>{children}{hover && <div role="tooltip" className={styles.previewTooltip}>{text}</div>}</div>;
}
export function GizmoView(p: GizmoProps) {
    const [hoverAxis,setHoverAxis]=useState(-1);
    useEffect(()=>setHoverAxis(-1),[p.held,p.rotating,p.pickingReference]);
    const highlighted=(i:number)=>p.axis>=0?p.axis===i:hoverAxis===i;
    const axisColor=(i:number)=>highlighted(i)?["#ffb1ab","#b9f0a8","#a9d1ff"][i]:colors[i];
    const shortcuts=(p.shortcuts??"T|R|G|Enter|Esc").split("|");
    const keyFor=(command:string)=>shortcuts[["space","rotate","ground","confirm","cancel"].indexOf(command)]??"";
    const t=p.translate??((source:string,...args:unknown[])=>format(source,args));
    if (!p.held)
        return null;
    const coordinates = p.projection.split(",").map(Number), visible = coordinates.length === 8 && coordinates.every(Number.isFinite);
    const [x, y] = coordinates, scale = window.innerHeight / 1080;
    const ringPoints = (p.rings ?? "").split(",").map(Number);
    const ticks=(p.ticks??"").split(",").map(Number);
    const markers = (p.markers ?? "").split(",").map(Number);
    const freeDragging=p.axis===3&&!p.rotating;
    const left = visible ? Math.max(12 * scale, Math.min(window.innerWidth - ((p.anarchyAvailable ? 316 : 240)+(p.local&&p.referenceAvailable?36:0)) * scale, x / 100 * window.innerWidth + 28 * scale)) : 24 * scale;
    const top = visible ? Math.max(56 * scale, Math.min(window.innerHeight - (p.rotating ? 122 : 68) * scale, y / 100 * window.innerHeight + 45 * scale)) : 100 * scale;
    const tooltip = p.renderTooltip ?? ((text, child) => <PreviewTooltip text={text}>{child}</PreviewTooltip>);
    const button = (icon: string, label: string, tip: string, command: string, extra = "") => <div className={styles.slot}>{tooltip(tip, <button className={`${styles.button} ${extra}`} aria-label={label} aria-disabled={command === "confirm" && !p.canPlace} onClick={() => { if (command !== "confirm" || p.canPlace)
            p.command(command); }}><img className={styles.icon} src={icon} alt=""/>{keyFor(command)&&<span className={styles.keycap} aria-hidden="true">{keyFor(command)}</span>}</button>)}</div>;
    return <div className={styles.overlay}>
    {!freeDragging&&(p.referenceOutline??"").split(";").filter(Boolean).map((line,i)=>{const [ax,ay,bx,by]=line.split(",").map(Number);const dx=(bx-ax)*window.innerWidth,dy=(by-ay)*window.innerHeight;return <div key={`reference-${i}`} className={styles.referenceLine} style={{left:`${ax*100}%`,top:`${ay*100}%`,width:`${Math.hypot(dx,dy)}px`,transform:`rotate(${Math.atan2(dy,dx)*180/Math.PI}deg)`}}/>;})}
    {visible && <div className={styles.origin} style={{ left: `${x}%`, top: `${y}%` }}>
      {!freeDragging&&labels.map((label, i) => {
                if (p.rotating)
                    return <React.Fragment key={label}>{ringPoints.length === 294 && Array.from({ length: 48 }, (_, j) => {
                            const k = i * 98 + j * 2, ax = ringPoints[k], ay = ringPoints[k + 1], dx = ringPoints[k + 2] - ax, dy = ringPoints[k + 3] - ay;
                            return <div key={j} className={styles.axis} style={{ left: `${ax / 10.8}vh`, top: `${ay / 10.8}vh`, width: `${Math.hypot(dx, dy) / 10.8}vh`, height: highlighted(i) ? "4rem" : "2.5rem", backgroundColor: axisColor(i), transform: `rotate(${Math.atan2(dy, dx) * 180 / Math.PI}deg)` }}><div role="button" aria-label={t("{0} 축 회전", label)} className={styles.ringHit} onMouseEnter={()=>setHoverAxis(i)} onMouseLeave={()=>setHoverAxis(-1)} onMouseDown={e => { if (e.button === 0) {
                                e.preventDefault();
                                e.stopPropagation();
                                p.command(`drag:${i}`);
                            } }}/></div>;
                        })}</React.Fragment>;
                const dx = coordinates[2 + i * 2], dy = coordinates[3 + i * 2], length = Math.sqrt(dx * dx + dy * dy), angle = Math.atan2(dy, dx) * 180 / Math.PI;
                return <React.Fragment key={label}>
          <div className={styles.axis} style={{ width: `${length / 10.8}vh`, backgroundColor: axisColor(i), height: highlighted(i) ? "4rem" : "2.5rem", transform: `rotate(${angle}deg)` }}>
            <div className={styles.tip} style={{ borderLeftColor: axisColor(i) }}/>
            <div role="button" aria-label={t("{0} 축 이동", label)} tabIndex={-1} className={styles.hit} onMouseEnter={()=>setHoverAxis(i)} onMouseLeave={()=>setHoverAxis(-1)} onMouseDown={e => { if (e.button === 0) {
                    e.preventDefault();
                    e.stopPropagation();
                    p.command(`drag:${i}`);
                } }}/>
          </div>
          <span className={styles.label} style={{ left: `${dx / 10.8}vh`, top: `${dy / 10.8}vh`, color: axisColor(i) }}>{label}</span>
        </React.Fragment>;
            })}
      {p.rotating&&ticks.length===288&&ticks.every(Number.isFinite)&&Array.from({length:72},(_,index)=>{
        const axis=Math.floor(index/24),k=index*4,ax=ticks[k],ay=ticks[k+1],dx=ticks[k+2]-ax,dy=ticks[k+3]-ay;
        return <div key={`tick-${index}`} data-rotation-tick={axis} className={styles.rotationTick} style={{left:`${ax/10.8}vh`,top:`${ay/10.8}vh`,width:`${Math.hypot(dx,dy)/10.8}vh`,height:highlighted(axis)?"2rem":"1.5rem",backgroundColor:axisColor(axis),transform:`rotate(${Math.atan2(dy,dx)*180/Math.PI}deg)`}}/>;
      })}
      {p.rotating && markers.length === 9 && labels.map((label, i) => <div key={`marker-${label}`} className={styles.ringMarker} style={{ left: `${markers[i * 3] / 10.8}vh`, top: `${markers[i * 3 + 1] / 10.8}vh`, borderLeftColor: axisColor(i), transform: `rotate(${markers[i * 3 + 2]}deg)` }}/>)}
      <div className={styles.center}/>
      {!p.rotating&&!p.pickingReference&&!freeDragging&&<div className={styles.centerHit} role="button" aria-label={t("중심점 드래그 · 자유 이동")} onMouseDown={e=>{if(e.button===0){e.preventDefault();e.stopPropagation();p.command("drag:3");}}}/>}
    </div>}
    {p.pickingReference&&<div className={styles.referencePicker} role="button" aria-label={t("그룹 안의 기준 프롭 클릭")} onMouseDown={e=>{e.preventDefault();e.stopPropagation();if(e.button===0)p.command(`reference:${e.clientX/window.innerWidth},${e.clientY/window.innerHeight}`);}}/>}
    {!freeDragging&&<div className={styles.actions} style={{ left: `${left}px`, top: `${top}px` }} onMouseDown={e => e.stopPropagation()} onKeyDown={e => { if (e.key === "Enter")
        e.preventDefault(); }}>
      {button(p.local ? localIcon : globalIcon, t("좌표계 전환"), p.local ? t("Local · 오브젝트 기준 / 클릭 또는 T로 Global") : t("Global · 월드 기준 / 클릭 또는 T로 Local"), "space", p.local ? styles.selected : "")}
      {p.local&&p.referenceAvailable&&button(referenceIcon,t("기준 오브젝트 선택"),p.pickingReference?t("그룹 안의 기준 프롭 클릭"):t("기준 오브젝트 · {0} / 클릭하여 변경",p.referenceName||t("월드 기준")),"reference",`${styles.small} ${p.pickingReference?styles.selected:""}`)}
      <div className={styles.modeSlot}>
        {button(p.rotating ? moveIcon : rotateIcon, p.rotating ? t("위치 조절 전환") : t("회전 전환"), p.rotating ? t("위치 조절 전환 · R / 회전 중 Shift: 15°") : t("회전 전환 · R"), "rotate")}
        {p.rotating && <div className={styles.pivots} role="group" aria-label={t("회전 기준점")}>{[0, 1, 2].map(row => <div className={styles.pivotRow} key={row}>{[0, 1, 2].map(col => { const i = row * 3 + col, label = [t("왼쪽 위"), t("위 중앙"), t("오른쪽 위"), t("왼쪽 중앙"), t("원점"), t("오른쪽 중앙"), t("왼쪽 아래"), t("아래 중앙"), t("오른쪽 아래")][i]; return <div className={styles.pivotSlot} key={i}>{tooltip(t("회전 기준점 · {0}", label), <button className={`${styles.pivotButton} ${(p.pivotIndex ?? 4) === i ? styles.pivotSelected : ""}`} aria-label={t("기준점 {0}", label)} aria-pressed={(p.pivotIndex ?? 4) === i} onClick={() => p.command(`pivot:${i}`)}><span /></button>)}</div>; })}</div>)}</div>}
      </div>
      {button(groundIcon,t("지면에 붙이기"),t("축 중심을 바로 아래 지면 높이에 맞추기 · G"),"ground")}
      {button(placeIcon, p.editing ? t("수정 확정") : t("생성 후 계속 배치"), p.editing ? (p.canPlace ? t("수정 확정 후 계속 편집 · Enter") : t("수정 확정 · Enter / {0}", p.status)) : p.canPlace ? t("생성 후 계속 배치 · Enter") : t("생성 · Enter / {0}", p.status), "confirm", p.canPlace ? "" : styles.unavailable)}
      {button(closeIcon, t("고정 해제"), p.editing ? t("미확정 이동 취소 · Esc") : t("고정 해제 · Esc"), "cancel")}
      {p.anarchyAvailable && button(anarchyIcon, "Anarchy", `Anarchy · ${p.anarchyOn ? t("켜짐") : t("꺼짐")} / ${p.editing ? t("오브젝트 보호") : t("배치 설정")}`, "anarchy", `${styles.small} ${p.anarchyOn ? styles.selected : ""}`)}
      {p.anarchyAvailable && button(lockIcon, "Transform Lock", `Transform Lock · ${p.lockOn ? t("켜짐") : t("꺼짐")}`, "lock", `${styles.small} ${p.lockOn ? styles.selected : ""}`)}
    </div>}
  </div>;
}

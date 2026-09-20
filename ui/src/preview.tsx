// Browser-only visual fixture. Not bundled into the installed game module.
import React,{useState,useEffect,useRef} from "react";
import {createRoot} from "react-dom/client";
import {GizmoView} from "./GizmoView";
import {insertAfterTrash} from "./actionsInsertion";
import gizmoIcon from "./gizmo.svg";
import styles from "./gizmo.module.scss";
function Preview() {
  const [held,setHeld]=useState(false),[local,setLocal]=useState(false),[placed,setPlaced]=useState(0),[axis,setAxis]=useState(-1),[center,setCenter]=useState([44.8,40.2]);
  const [rotating,setRotating]=useState(false),[pivot,setPivot]=useState(4),[settling,setSettling]=useState(false);
  const timer=useRef<ReturnType<typeof setTimeout>>();
  const rings=Array.from({length:3},(_,axis)=>Array.from({length:49},(_,i)=>{const a=i*Math.PI*2/48;return axis===0?[30*Math.cos(a),100*Math.sin(a)]:axis===1?[100*Math.cos(a),30*Math.sin(a)]:[80*Math.cos(a),80*Math.sin(a)];})).flat(2).join(",");
  const [anarchy,setAnarchy]=useState(false),[locked,setLocked]=useState(true);
  const [status,setStatus]=useState("고정됨 · 축 드래그 후 Enter로 생성");
  const drag=useRef<{axis:number;x:number;y:number;origin:number[]}|null>(null);
  const dirs=local?[[86,32],[-74,44],[0,-133]]:[[119,-39],[-77,64],[0,-133]];
  const command=(value:string)=>{
    if(value.startsWith("pivot:")){setPivot(Number(value.slice(6)));return;}
    if(value==="rotate"){setRotating(v=>!v);return;}
    if(value==="anarchy"){setAnarchy(v=>!v);return;}
    if(value==="lock"){setLocked(v=>!v);return;}
    if(value==="space"){setLocal(v=>!v);return;}
    if(value==="confirm"){if(axis>=0||settling)return;setPlaced(v=>v+1);setStatus("생성 완료 · 축을 움직여 다음 위치를 지정하세요");return;}
    if(value==="cancel"){setHeld(false);return;}
    if(value.startsWith("drag:")){clearTimeout(timer.current);setSettling(true);setAxis(Number(value.slice(5)));setStatus("축 이동 중");}
  };
  useEffect(()=>{
    const down=(e:MouseEvent)=>{const el=e.target as HTMLElement;const label=el.getAttribute("aria-label");if(label?.endsWith("축 이동")){const a=["X","Y","Z"].indexOf(label[0]);drag.current={axis:a,x:e.clientX,y:e.clientY,origin:[...center]};}};
    const move=(e:MouseEvent)=>{const d=drag.current;if(!d)return;const v=dirs[d.axis],length=Math.hypot(...v),dx=v[0]/length,dy=v[1]/length;const amount=(e.clientX-d.x)*dx+(e.clientY-d.y)*dy;setCenter([d.origin[0]+amount*dx/window.innerWidth*100,d.origin[1]+amount*dy/window.innerHeight*100]);};
    const up=()=>{if(axis<0)return;drag.current=null;setAxis(-1);clearTimeout(timer.current);timer.current=setTimeout(()=>setSettling(false),100);};
    const key=(e:KeyboardEvent)=>{if(e.repeat)return;if(e.key.toLowerCase()==="r")command("rotate");if(e.shiftKey&&e.key.toLowerCase()==="m")setHeld(true);if(e.key==="Enter")command("confirm");if(e.key==="Escape")command("cancel");};
    window.addEventListener("mousedown",down,true);window.addEventListener("mousemove",move);window.addEventListener("mouseup",up);window.addEventListener("keydown",key);
    return()=>{window.removeEventListener("mousedown",down,true);window.removeEventListener("mousemove",move);window.removeEventListener("mouseup",up);window.removeEventListener("keydown",key);};
  },[center,local,axis,settling]);
  return <><div style={{position:"fixed",left:20,top:70,color:"white",background:"#1d2737",padding:12,fontSize:14}}>UI 검토용 · 게임 동작 시뮬레이션 아님</div>{insertAfterTrash(<div style={{position:"fixed",left:20,top:125,background:"#151b20",borderRadius:10,padding:12,display:"flex"}}><span><img src="Media/Glyphs/Trash.svg" alt="휴지통"/></span><span data-testid="preserved-action">기존 버튼</span></div>,<button key="gizmo" className={styles.selectedAction} aria-label="Gizmo XYZ 편집" onClick={()=>setHeld(true)}><img src={gizmoIcon} alt=""/></button>)}<GizmoView pivotIndex={pivot} rotating={rotating} rings={rings} markers="30,0,0,100,0,0,56.56,56.56,45" editing={false} anarchyAvailable anarchyOn={anarchy} lockOn={locked} held={held} local={local} canPlace={axis<0&&!settling} axis={axis} placed={placed} projection={[...center,...dirs.flat()].join(",")} status={status} command={command}/></>;
}
createRoot(document.getElementById("root")!).render(<Preview/>);

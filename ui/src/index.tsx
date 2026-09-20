import React,{useEffect} from "react";
import {ModRegistrar} from "cs2/modding";
import {bindValue,trigger,useValue} from "cs2/api";
import {useText} from "./localization";
import {GizmoView} from "./GizmoView";
import {Tooltip} from "cs2/ui";
import styles from "./gizmo.module.scss";
import {SelectedAction} from "./SelectedAction";
import {insertAfterTrash,extendActionSections} from "./actionsInsertion";
const editing$=bindValue<boolean>("GizmoXYZ","editing",false),available$=bindValue<boolean>("GizmoXYZ","anarchyAvailable",false),anarchy$=bindValue<boolean>("GizmoXYZ","anarchyOn",false),lock$=bindValue<boolean>("GizmoXYZ","lockOn",false);
const rotating$=bindValue<boolean>("GizmoXYZ","rotating",false),rings$=bindValue<string>("GizmoXYZ","rings","");
const pivotIndex$=bindValue<number>("GizmoXYZ","pivotIndex",4);
const referenceAvailable$=bindValue<boolean>("GizmoXYZ","referenceAvailable",false),pickingReference$=bindValue<boolean>("GizmoXYZ","pickingReference",false),referenceName$=bindValue<string>("GizmoXYZ","referenceName",""),referenceOutline$=bindValue<string>("GizmoXYZ","referenceOutline","");
const ticks$=bindValue<string>("GizmoXYZ","ticks","");
const markers$=bindValue<string>("GizmoXYZ","markers","");
const held$=bindValue<boolean>("GizmoXYZ","held",false);
const globalAnarchy$=bindValue<boolean>("Anarchy","AnarchyEnabled",false),globalLock$=bindValue<boolean>("Anarchy","LockElevation",false);
const local$=bindValue<boolean>("GizmoXYZ","local",false);
const canPlace$=bindValue<boolean>("GizmoXYZ","canPlace",false);
const axis$=bindValue<number>("GizmoXYZ","axis",-1);
const placed$=bindValue<number>("GizmoXYZ","placed",0);
const projection$=bindValue<string>("GizmoXYZ","projection","");
const shortcuts$=bindValue<string>("GizmoXYZ","shortcuts","");
const status$=bindValue<string>("GizmoXYZ","status","");
const command=(value:string)=>trigger("GizmoXYZ","command",value);
function Gizmo() {
  const t=useText();
  const editing=useValue(editing$),globalAnarchy=useValue(globalAnarchy$),globalLock=useValue(globalLock$),objectAnarchy=useValue(anarchy$),objectLock=useValue(lock$);
  const dispatch=(value:string)=>{if(!editing&&(value==="anarchy"||value==="lock")){trigger("Anarchy",value==="anarchy"?"AnarchyToggled":"LockElevationToggled");return;}command(value);};
  const held=useValue(held$),local=useValue(local$),canPlace=useValue(canPlace$),axis=useValue(axis$),placed=useValue(placed$),projection=useValue(projection$),status=useValue(status$);
  useEffect(()=>{trigger("GizmoXYZ","diagnostic","UI mounted");return()=>trigger("GizmoXYZ","diagnostic","UI unmounted");},[]);
  return <GizmoView ticks={useValue(ticks$)} shortcuts={useValue(shortcuts$)} referenceAvailable={useValue(referenceAvailable$)} pickingReference={useValue(pickingReference$)} referenceName={useValue(referenceName$)} referenceOutline={useValue(referenceOutline$)} translate={t} pivotIndex={useValue(pivotIndex$)} markers={useValue(markers$)} rotating={useValue(rotating$)} rings={useValue(rings$)} editing={editing} anarchyAvailable={useValue(available$)} anarchyOn={editing?objectAnarchy:globalAnarchy} lockOn={editing?objectLock:globalLock} held={held} local={local} canPlace={canPlace} axis={axis} placed={placed} projection={projection} status={t(status)} command={dispatch} renderTooltip={(text,child)=><Tooltip tooltip={<span className={styles.tooltipText} style={{fontFamily:'"Noto Sans KR"'}}>{text}</span>}>{child}</Tooltip>}/>;
}
const register:ModRegistrar=registry=>{registry.append("Game",Gizmo);registry.append("Editor",Gizmo);registry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx","selectedInfoSectionComponents",((sections:any)=>{
  return extendActionSections(sections,(Original:any)=>(props:any)=>{const report={inserted:false};const result=insertAfterTrash(Original(props),<SelectedAction key="GizmoXYZ.edit"/>,report);useEffect(()=>{trigger("GizmoXYZ","diagnostic",`ActionsSection map mounted; trash anchor matched=${report.inserted}`);},[report.inserted]);return result;});
}) as any);};
export default register;

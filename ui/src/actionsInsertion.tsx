import React from "react";
export function extendActionSections(sections:Record<string,any>,wrap:(original:any)=>any){
  const key="Game.UI.InGame.ActionsSection";
  if(typeof sections[key]!=="function")throw new Error("Gizmo XYZ: ActionsSection map entry unavailable");
  return {...sections,[key]:wrap(sections[key])};
}
// Extend the existing React result without replacing the native or another mod's actions.
export function insertAfterTrash(node:React.ReactNode,button:React.ReactElement,report?:{inserted:boolean}):React.ReactNode {
  if(!React.isValidElement(node))return node;
  const children=React.Children.toArray(node.props.children);
  const containsTrash=(child:React.ReactNode):boolean=>React.isValidElement(child)&&((typeof child.props.src==="string"&&/(^|\/)Trash\.svg(?:[?#].*)?$/i.test(child.props.src))||React.Children.toArray(child.props.children).some(containsTrash));
  if(Array.isArray(node.props.children)||children.length>1){
    const index=children.findIndex(containsTrash);
    if(index>=0){if(report)report.inserted=true;children.splice(index+1,0,button);return React.cloneElement(node,undefined,...children);}
  }
  return children.length?React.cloneElement(node,undefined,...children.map(child=>insertAfterTrash(child,button,report))):node;
}

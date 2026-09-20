const fs=require('fs'),path=require('path'),Module=require('module');
const ui=path.resolve(__dirname,'../ui'),ts=require(path.join(ui,'node_modules/typescript')),React=require(path.join(ui,'node_modules/react'));
const filename=path.join(ui,'src/actionsInsertion.tsx');
const mod=new Module(filename,module);mod.filename=filename;mod.paths=Module._nodeModulePaths(path.dirname(filename));
mod._compile(ts.transpileModule(fs.readFileSync(filename,'utf8'),{compilerOptions:{jsx:ts.JsxEmit.React,module:ts.ModuleKind.CommonJS,esModuleInterop:true}}).outputText,filename);
const insert=mod.exports.insertAfterTrash,h=React.createElement;
let count=0;function check(v,msg){if(!v)throw Error(msg);count++;}
for(const src of ['Media/Glyphs/Trash.svg','coui://gameui/Media/Glyphs/Trash.svg?version=2']){
 for(const onlyTrash of [false,true]){
  const trash=h('tooltip',{key:'trash'},h('button',{src}));
  const children=[!onlyTrash&&h('button',{key:'focus'}),trash,!onlyTrash&&h('button',{key:'other-mod'})];
  const tree=h('div',{},h('focus-provider',{},h('action-row',{},children)));
  const report={inserted:false},result=insert(tree,h('button',{key:'gizmo'}),report);
  const keys=React.Children.toArray(result.props.children.props.children.props.children).map(e=>e.key);
  check(report.inserted,'trash match');
  check(keys.findIndex(k=>k.includes('gizmo'))===keys.findIndex(k=>k.includes('trash'))+1,'immediately after trash');
  check(React.Children.toArray(tree.props.children.props.children.props.children).every(e=>!e.key.includes('gizmo')),'original tree unmodified');
  check(onlyTrash||keys.some(k=>k.includes('other-mod')),'other mod retained');
 }
}
const native=()=>h('div',{},[h('button',{key:'trash',src:'Media/Glyphs/Trash.svg'})]);
const other=()=>null,sections={'Game.UI.InGame.ActionsSection':native,'OtherMod.Section':other};
const updated=mod.exports.extendActionSections(sections,original=>()=>insert(original(),h('button',{key:'gizmo'})));
check(updated['Game.UI.InGame.ActionsSection']!==native,'replace captured section-map entry, not unused module export');
check(sections['Game.UI.InGame.ActionsSection']===native,'preserve original mapping');
check(updated['OtherMod.Section']===other,'preserve third-party section');
check(React.Children.toArray(updated['Game.UI.InGame.ActionsSection']().props.children).some(e=>e.key.includes('gizmo')),'renderer using map gets gizmo');
console.log(`PASS: ${count} native-shaped action insertion and registry checks`);

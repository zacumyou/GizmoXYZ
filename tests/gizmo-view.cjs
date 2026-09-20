const fs=require('fs'),path=require('path'),Module=require('module');
const ui=path.resolve(__dirname,'../ui'),ts=require(path.join(ui,'node_modules/typescript')),React=require(path.join(ui,'node_modules/react')),render=require(path.join(ui,'node_modules/react-dom/server')).renderToStaticMarkup;
const file=path.join(ui,'src/GizmoView.tsx'),mod=new Module(file,module);mod.filename=file;mod.paths=Module._nodeModulePaths(path.dirname(file));
const textFile=path.join(ui,'src/text.ts'),textModule=new Module(textFile,module);textModule.filename=textFile;textModule.paths=mod.paths;textModule._compile(ts.transpileModule(fs.readFileSync(textFile,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,esModuleInterop:true}}).outputText,textFile);
const baseRequire=mod.require.bind(mod);mod.require=name=>name==='./text'?textModule.exports:name.endsWith('.svg')?name:name.endsWith('.scss')?{}:baseRequire(name);
mod._compile(ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{jsx:ts.JsxEmit.React,module:ts.ModuleKind.CommonJS,esModuleInterop:true}}).outputText,file);
global.window={innerHeight:1080,innerWidth:1920};let count=0;function check(v,label){if(!v)throw Error(label);count++;}
for(const editing of [false,true])for(const available of [false,true]){
 const html=render(React.createElement(mod.exports.GizmoView,{held:true,local:false,canPlace:true,axis:-1,placed:0,projection:'50,50,100,0,0,100,0,-100',status:'',command:()=>{},editing,anarchyAvailable:available,anarchyOn:true,lockOn:true}));
 check(html.includes('aria-label="Anarchy"')===available,'Anarchy presence matches availability in both modes');
 check(html.includes('aria-label="Transform Lock"')===available,'Transform Lock hidden with unavailable Anarchy');
 check(html.includes('aria-label="회전 전환"'),'base controls remain available without optional mod');
}
console.log('PASS: '+count+' rendered optional-integration UI checks');

for(const local of [false,true])for(const referenceAvailable of [false,true]){
 const props={held:true,local,referenceAvailable,pickingReference:local&&referenceAvailable,canPlace:false,axis:-1,placed:0,projection:'50,50,100,0,0,100,0,-100',status:'',command:()=>{},referenceName:'Tree plate'};
 const html=render(React.createElement(mod.exports.GizmoView,props));
 check(html.includes('aria-label="기준 오브젝트 선택"')===(local&&referenceAvailable),'reference picker only in supported group Object mode');
 check(html.includes('aria-label="그룹 안의 기준 프롭 클릭"')===(local&&referenceAvailable),'world picking overlay only while selecting reference');
}
console.log('PASS: reference picker visibility and capture checks');

for(const axis of [-1,0,3])for(const rotating of [false,true]){
 const html=render(React.createElement(mod.exports.GizmoView,{held:true,local:true,canPlace:false,axis,rotating,placed:0,projection:'50,50,100,0,0,100,0,-100',status:'',command:()=>{}}));
 const free=axis===3&&!rotating;
 check(html.includes('aria-label="좌표계 전환"')===!free,'toolbar hidden only during free move');
 check(html.includes('aria-label="중심점 드래그 · 자유 이동"')===(!rotating&&!free),'center drag handle only in idle translation mode');
 check(html.includes('aria-label="X 축 이동"')===(!rotating&&!free),'axes hidden during free move');
}
console.log('PASS: free move visibility and rotation-mode exclusion');

for(const axis of [-1,3]){const html=render(React.createElement(mod.exports.GizmoView,{held:true,local:false,canPlace:true,axis,placed:0,projection:'50,50,100,0,0,100,0,-100',status:'',command:()=>{}}));check(html.includes('aria-label="지면에 붙이기"')===(axis!==3),'ground button hidden during free drag');}

const remapped=render(React.createElement(mod.exports.GizmoView,{held:true,local:true,canPlace:true,axis:-1,placed:0,projection:'50,50,100,0,0,100,0,-100',status:'',command:()=>{},shortcuts:'Ctrl+K||F3|Enter|Esc'}));check(remapped.includes('Ctrl+K')&&remapped.includes('F3'),'keycaps display actual rebound keys');check(!remapped.includes('>R</span>'),'unbound action has no keycap');console.log('PASS: rebound/unbound shortcut badge checks');

for(const rotating of [false,true])for(const shift of [false,true]){const html=render(React.createElement(mod.exports.GizmoView,{held:true,local:true,canPlace:true,axis:-1,placed:0,rotating,ticks:shift?Array(288).fill(1).join(','):'',projection:'50,50,100,0,0,100,0,-100',status:'',command:()=>{}}));check((html.match(/data-rotation-tick=/g)||[]).length===(rotating&&shift?72:0),'15 degree ticks only during rotation and Shift');}console.log('PASS: Shift tick visibility, 24 marks per axis');

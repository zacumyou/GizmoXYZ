using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using GizmoXYZ;
using Unity.Mathematics;
using V=GizmoXYZ.DragMath.V;

int checks=0;
void Check(bool ok,string name) { checks++;if(!ok)throw new Exception(name); }
CleanupTests.Run(Check);
Check(math.all(PivotMath.RotationPoint(new float3(5,10,15),new float3(9,20,25),4)==new float3(0)),"default rotation pivot is object origin even with offset geometry");
Check(math.all(PivotMath.RotationPoint(new float3(5,10,15),new float3(9,20,25),0)==new float3(5,0,25)),"explicit corner rotation pivot preserved");
var grounded=PivotMath.Ground(new float3(10,25,30),new float3(10,28,30),7);
Check(math.distance(grounded,new float3(10,4,30))<.00001f,"ground snap aligns pivot, preserving horizontal position");
Check(math.abs((grounded.y+3)-7)<.00001f,"offset center lands on sampled terrain");
Check(PivotMath.Ground(new float3(10,25,30),new float3(10,28,30),35).y==32,"buried center rises to ground");
// Free dragging: retain altitude, follow sloped terrain, and preserve the initial grab offset.
Check(FreeDragMath.Surface(new float3(0,50,0),new float3(1,-1,0),5,p=>0,out var freeHit)&&math.distance(freeHit,new float3(45,5,0))<.001f,"free drag preserves height above flat ground");
Check(FreeDragMath.Surface(new float3(0,50,0),new float3(1,-1,0),5,p=>p.x*.5f,out freeHit)&&math.distance(freeHit,new float3(30,20,0))<.001f,"free drag follows a slope with elevation offset");
var dragRoot=new float3(7,8,9);var grab=new float3(12,15,20);
Check(math.distance(FreeDragMath.Move(dragRoot,grab,grab),dragRoot)<.00001f,"grabbing center never jumps");
var cursor=new float3(20,19,30);var freelyMoved=FreeDragMath.Move(dragRoot,grab,cursor);
Check(math.distance(freelyMoved-dragRoot,cursor-grab)<.00001f,"group displacement is rigid and preserves grab offset");
int surfaceSamples=0;
Check(!FreeDragMath.Surface(new float3(0,50,0),new float3(1,0,0),0,p=>{surfaceSamples++;return 0;},out _)&&surfaceSamples<200,"horizon misses are bounded");
Check(!FreeDragMath.Surface(new float3(0,50,0),new float3(0,1,0),0,p=>0,out _),"sky ray does not teleport object");
Check(!FreeDragMath.Surface(new float3(0,50,0),new float3(0),0,p=>0,out _),"zero free-drag ray rejected");
Check(!FreeDragMath.Surface(new float3(0,50,0),new float3(0,-1,0),0,p=>float.NaN,out _),"invalid terrain rejected");
// Reference frame composition must never leak into the actual group placement delta.
var reference=quaternion.EulerXYZ(new float3(.2f,.65f,-.13f));
var deltaGroup=quaternion.EulerXYZ(new float3(-.4f,.3f,.6f));
var frame=PivotMath.ObjectFrame(deltaGroup,reference);
foreach(var vector in new[]{new float3(1,0,0),new float3(0,1,0),new float3(0,0,1),new float3(4,2,-3)}){
 Check(math.distance(math.mul(PivotMath.ObjectFrame(quaternion.identity,reference),vector),math.mul(reference,vector))<.00001f,"initial object frame follows reference");
 Check(math.distance(math.mul(frame,vector),math.mul(deltaGroup,math.mul(reference,vector)))<.00001f,"group rotation follows reference in correct multiplication order");
 Check(math.distance(math.mul(math.inverse(frame),math.mul(frame,vector)),vector)<.00001f,"reference bound corner round trip");
}
Check(CopyIt.ReferenceMath.IsFlat(new float3(2,.2f,2)),"square plate prioritized regardless of taller tree bounds");
Check(!CopyIt.ReferenceMath.IsFlat(new float3(2,4,2)),"tall props lower priority");
var boxMin=new float3(-1,-.03f,-1);var boxMax=new float3(1,.03f,1);
Check(CopyIt.ReferenceMath.Hit(new float3(0,5,0),new float3(0,-1,0),boxMin,boxMax,out var hit)&&math.abs(hit-4.97f)<.00001f,"thin plate hit with parallel x/z ray");
Check(!CopyIt.ReferenceMath.Hit(new float3(2,5,0),new float3(0,-1,0),boxMin,boxMax,out _),"outside parallel slab misses");
Check(!CopyIt.ReferenceMath.Hit(new float3(0,5,0),new float3(0,1,0),boxMin,boxMax,out _),"behind camera excluded");
Check(CopyIt.ReferenceMath.Hit(new float3(0),new float3(1,0,0),boxMin,boxMax,out hit)&&hit==0,"camera inside bounds");
Check(!CopyIt.ReferenceMath.Hit(new float3(0),new float3(0),boxMin,boxMax,out _),"zero ray rejected");
var worldOrigin=new float3(19,3,-8);var inv=math.inverse(frame);
Check(CopyIt.ReferenceMath.Hit(math.mul(inv,math.mul(frame,new float3(0,5,0))+worldOrigin-worldOrigin),math.mul(inv,math.mul(frame,new float3(0,-1,0))),boxMin,boxMax,out hit)&&math.abs(hit-4.97f)<.0001f,"rotated and translated preview picking");
Check(RotationMath.Delta(179,-179)==2,"positive rotation crosses angle seam");
Check(RotationMath.Delta(-179,179)==-2,"negative rotation crosses angle seam");
Check(RotationMath.Snap(7.5,true)==15&&RotationMath.Snap(-7.5,true)==-15,"symmetric half-step snapping");
for(int angle=-1080;angle<=1080;angle++){
 Check(RotationMath.Snap(angle,false)==angle,"free rotation preserved");
 Check(RotationMath.Snap(angle,true)%15==0,"15 degree increments over multiple turns");
 Check(Math.Abs(RotationMath.Snap(angle,true)-angle)<=7.5,"nearest snap target");
}
V Unit(V v) => v*(1/Math.Sqrt(V.Dot(v,v)));
var random=new System.Random(27419);
for(int i=0;i<3000;i++) {
    var axis=Unit(new V(random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1));
    var anchor=new V(random.NextDouble()*1000,random.NextDouble()*1000,random.NextDouble()*1000);
    var camera=anchor+new V(70,90,110);
    var view=Unit(anchor-camera);
    if(Math.Abs(V.Dot(view,axis))>0.99)continue;
    var amount=random.NextDouble()*300-150;
    var target=anchor+axis*amount;
    var ray=Unit(target-camera);
    Check(DragMath.AxisParameter(camera,ray,anchor,axis,view,out var result),"valid drag intersection");
    Check(Math.Abs(result-amount)<1e-7,"signed axis displacement without drift");
    var moved=anchor+axis*result;
    var error=moved-target;
    Check(V.Dot(error,error)<1e-12,"global/local unit axis locks both perpendicular coordinates");
}
Check(!DragMath.AxisParameter(new V(0,0,-10),new V(0,0,1),new V(0,0,0),new V(0,0,1),new V(0,0,1),out _),"end-on camera uses fallback");
Check(!DragMath.AxisParameter(new V(0,0,-10),new V(0,0,-1),new V(0,0,0),new V(1,0,0),new V(0,0,1),out _),"behind-camera intersections rejected");
Check(Math.Abs(DragMath.ScreenDelta(140,70,100,70,1,0,4)-10)<1e-10,"screen displacement scales with zoom");
Check(Math.Abs(DragMath.ScreenDelta(100,30,100,70,0,1,4)+10)<1e-10,"negative fallback displacement");
Check(DragMath.ScreenDelta(100,30,100,70,0,1,0)==0,"zero projection avoids division by zero");
var holdGate=new HoldInputGate();
Check(holdGate.TryConsume(1,true,true,true,true,true,false,false,true,false),"Shift-click captured without any native Apply event");
Check(!holdGate.TryConsume(1,true,true,true,true,true,false,false,true,false),"raw and registered binding in same frame consumed once");
Check(!holdGate.TryConsume(2,false,true,true,true,true,false,false,true,false),"ordinary frame continues native cursor preview");
Check(!holdGate.TryConsume(3,true,true,true,true,true,false,false,false,false),"UI click is not captured");
Check(holdGate.TryConsume(3,true,true,true,true,true,false,false,true,false),"rejected UI observation does not consume the world event");
Check(!holdGate.TryConsume(4,true,true,true,true,true,true,false,true,false),"text input focus rejected");
Check(!holdGate.TryConsume(5,true,true,true,true,true,false,true,true,false),"modal overlay rejected");
Check(!holdGate.TryConsume(6,true,true,true,true,false,false,false,true,false),"unfocused game rejected");
Check(!holdGate.TryConsume(7,true,true,false,true,true,false,false,true,false),"different tool is untouched");
Check(!holdGate.TryConsume(8,true,true,true,false,true,false,false,true,false),"brush and line modes are untouched");
Check(!holdGate.TryConsume(9,true,true,true,true,true,false,false,true,true),"held session is not captured again");
Check(holdGate.TryConsume(10,true,true,true,true,true,false,false,true,false),"fresh click can start another session after release");
Check(!holdGate.TryConsume(11,true,false,true,true,true,false,false,true,false),"unloaded mod cannot capture input");
var min=new float3(-3,-2,-7);var max=new float3(9,5,11);
for(int index=0;index<9;index++){
 var point=PivotMath.Point(min,max,index);
 Check(math.distance(point,new float3(-3+6*(index%3),0,11-9*(index/3)))<.00001f,"nine point footprint "+index);
 foreach(var axis in new[]{new float3(1,0,0),new float3(0,1,0),new float3(0,0,1)})for(int degrees=-180;degrees<=180;degrees+=15){
  var before=quaternion.EulerXYZ(.3f,.6f,-.7f);var origin=new float3(50,100,-35);
  var pivot=origin+math.mul(before,point);var delta=quaternion.AxisAngle(axis,math.radians(degrees));
  var afterPosition=PivotMath.RotatePosition(origin,pivot,delta);var afterRotation=math.mul(delta,before);
  Check(math.distance(afterPosition+math.mul(afterRotation,point),pivot)<.0001f,"selected pivot remains invariant on rotated objects");
  Check(math.abs(math.distance(afterPosition,pivot)-math.distance(origin,pivot))<.0001f,"rotation preserves radius");
 }
}
for(int angle=-180;angle<=180;angle+=15){
 var root=new float3(1,2,3);var parent=new float3(8,9,10);var child=new float3(-4,5,8);var target=new float3(50,-20,60);
 var rotation=quaternion.EulerXYZ(.2f,.3f,.7f);var delta=quaternion.AxisAngle(math.normalize(new float3(1,2,3)),math.radians(angle));
 var movedParent=HierarchyMath.Move(parent,root,target,delta);var movedChild=HierarchyMath.Move(child,root,target,delta);
 Check(math.distance(HierarchyMath.Local(child,parent,rotation),HierarchyMath.Local(movedChild,movedParent,math.mul(delta,rotation)))<.0001f,"nested child retains parent-local position");
 Check(math.distance(HierarchyMath.Move(movedChild,target,root,math.inverse(delta)),child)<.0001f,"child rotation and translation reversible");
}
var token=L10n.Message("GizmoXYZ.Text.7818F582A3","X","15.0","");
Check(L10n.ForLog(token)=="Rotate X · 15.0°","native transport/log preserves numbered arguments");
Check(L10n.ForLog(L10n.Message("GizmoXYZ.Text.4B3280C6D2","Pipe | 한글","3.5"))=="Move on Pipe | 한글 · 3.5 m","transport escapes delimiters and Unicode");
Check(L10n.ForLog(L10n.Message("GizmoXYZ.Text.B089E8C11D",token))=="Confirm changes · Enter / Rotate X · 15.0°","nested native status remains readable in diagnostics");
Check(L10n.ForLog("unrelated diagnostic")=="unrelated diagnostic","unrelated log text unchanged");
var gate=new GizmoXYZ.ConfirmGate();
Check(gate.Ready(1,false,true),"initial valid preview is ready");
Check(!gate.Ready(1,true,true),"dragging disables confirmation even when preview validates");
gate.EndDrag(2);
Check(!gate.Ready(2.099,false,true),"confirmation waits full 100 milliseconds");
Check(gate.Ready(2.1,false,true),"confirmation resumes at 100 milliseconds");
Check(!gate.Ready(3,false,false),"invalid preview stays disabled");
gate.EndDrag(3);gate.EndDrag(3.4);
Check(!gate.Ready(3.499,false,true),"new drag restarts delay");
Check(gate.Ready(3.5,false,true),"new drag delay completes");
gate.Reset();Check(gate.Ready(0,false,true),"new session resets delay");
var game=Path.Combine(Environment.GetEnvironmentVariable("CSII_MANAGEDPATH")!,"Game.dll");
Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(game)))=="AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A","current DLL contract hash");
using(var file=File.OpenRead(game))using(var pe=new PEReader(file)) {
    var md=pe.GetMetadataReader();
    TypeDefinition Type(string name)=>md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t=>md.GetString(t.Namespace)+"."+md.GetString(t.Name)==name);
    void Field(string type,string field)=>Check(Type(type).GetFields().Select(md.GetFieldDefinition).Any(f=>md.GetString(f.Name)==field),type+"."+field);
    void Method(string type,string method,int parameters)=>Check(Type(type).GetMethods().Select(md.GetMethodDefinition).Any(m=>md.GetString(m.Name)==method && m.GetParameters().Select(md.GetParameter).Count(p=>p.SequenceNumber>0)==parameters),type+"."+method);
    Method("Game.Tools.ObjectToolSystem","Apply",2);
    Method("Game.Tools.ObjectToolSystem","OnUpdate",1);
    Method("Game.Tools.ObjectToolSystem","UpdateDefinitions",1);
    Method("Game.Tools.ObjectToolSystem","GetAllowApply",0);
    Method("Game.Tools.ToolBaseSystem","DestroyDefinitions",3);
    Method("Game.Tools.ToolBaseSystem","set_applyMode",1);
    Field("Game.Tools.ObjectToolSystem","m_DefinitionQuery");
    Field("Game.Tools.ObjectToolBaseSystem","m_ToolOutputBarrier");
    Field("Game.Tools.ToolBaseSystem","m_ForceUpdate");
    Field("Game.Prefabs.ToolUXSoundSettingsData","m_PlacePropSound");
    Field("Game.Prefabs.ToolUXSoundSettingsData","m_PlaceBuildingSound");
    Method("Game.Tools.ToolBaseSystem","GetAllowApply",0);
    Field("Game.Tools.CreationDefinition","m_Original");
    Field("Game.Tools.ObjectDefinition","m_LocalPosition");
    Field("Game.Tools.ObjectDefinition","m_ParentMesh");
}
var anarchyRoot=Path.Combine(Environment.GetEnvironmentVariable("CSII_PDXMODSPATH")!,"mods_subscribed","74604_38","Anarchy.dll");
using(var file=File.OpenRead(anarchyRoot))using(var pe=new PEReader(file)){
    var md=pe.GetMetadataReader();
    var bridge=md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t=>md.GetString(t.Namespace)+"."+md.GetString(t.Name)=="Anarchy.Bridge.AnarchyBridge");
    foreach(var name in new[]{"TryAddToolSystem","TryAddAnarchyComponent","TryAddTransformLockComponent","RemoveAnarchyComponent","RemoveTransformLockComponent","GetAnarchyComponentType","GetTransformLockComponentType"})
        Check(bridge.GetMethods().Select(md.GetMethodDefinition).Any(m=>md.GetString(m.Name)==name),"installed Anarchy bridge "+name);
    var record=md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t=>md.GetString(t.Namespace)+"."+md.GetString(t.Name)=="Anarchy.Components.TransformRecord");
    foreach(var name in new[]{"m_Position","m_Rotation"})Check(record.GetFields().Select(md.GetFieldDefinition).Any(f=>md.GetString(f.Name)==name),"installed lock record "+name);
}
Console.WriteLine($"PASS: {checks} checks; randomized axis drag geometry and installed game metadata contracts. No in-game runtime claim.");


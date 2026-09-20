using System;
using System.Globalization;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace GizmoXYZ {
public partial class GizmoSession {
    internal bool RotateMode {get;private set;}
    private quaternion rotationStart;
    private Vector3 ringU,ringV;
    private double previousAngle,rotationAngle;
    private double tickStartAngle;
    private bool rotationPlane;
    private bool RotationAngle(Camera camera,Vector2 screen,out double angle){
        var ray=camera.ScreenPointToRay(screen);var plane=new Plane(dragAxis,dragAnchor);
        if(plane.Raycast(ray,out var distance)){
            var v=ray.GetPoint(distance)-dragAnchor;
            if(v.sqrMagnitude>0.000001f){angle=Math.Atan2(Vector3.Dot(v,ringV),Vector3.Dot(v,ringU))*180/Math.PI;return true;}
        }
        angle=0;return false;
    }
    private void BeginRotation(Camera camera){
        rotationStart=Rotation;rotationAngle=0;
        ringU=Vector3.Cross(dragAxis,Math.Abs(Vector3.Dot(dragAxis,Vector3.up))>.9f?Vector3.right:Vector3.up).normalized;
        ringV=Vector3.Cross(dragAxis,ringU).normalized;
        rotationPlane=Math.Abs(Vector3.Dot(camera.transform.forward,dragAxis))>.08f&&RotationAngle(camera,dragScreen,out previousAngle);
        tickStartAngle=rotationPlane?previousAngle:0;
        Diagnostics.Event("rotation.begin",$"axis={Axis} plane={rotationPlane}");
    }
    private void UpdateRotation(Camera camera,Vector2 screen){
        if(rotationPlane){
            if(!RotationAngle(camera,screen,out var angle))return;
            rotationAngle+=RotationMath.Delta(previousAngle,angle);previousAngle=angle;
        }else rotationAngle=(screen.x-dragScreen.x)-(screen.y-dragScreen.y);
        bool snap=Keyboard.current!=null&&Keyboard.current.shiftKey.isPressed;
        var degrees=RotationMath.Snap(rotationAngle,snap);
        var delta=quaternion.AxisAngle((float3)dragAxis,math.radians((float)degrees));
        var next=math.normalize(math.mul(delta,rotationStart));
        if(math.abs(math.dot(next.value,Rotation.value))>.9999999f)return;
        Position=PivotMath.RotatePosition(dragPosition,(float3)dragAnchor,delta);
        Rotation=next;dirty=true;CanPlace=false;confirm=false;
        Status=L10n.Message("GizmoXYZ.Text.7818F582A3", ((Axis==0?"X":Axis==1?"Y":"Z")), $"{degrees:0.0}", ((snap?" · 15°":"")));
        if(Mod.Options.DiagnosticDetail&&UnityEngine.Time.unscaledTime>=nextDragLog){Diagnostics.Event("rotation.sample",$"axis={Axis} degrees={degrees:0.00} snap={snap} rotation={Rotation.value}");nextDragLog=UnityEngine.Time.unscaledTime+.5f;}
    }
    internal string Rings(){
        var camera=Camera.main;if(!Held||!RotateMode||camera==null)return "";
        var center=camera.WorldToScreenPoint(Pivot);if(center.z<=camera.nearClipPlane)return "";
        float rem=Screen.height/1080f;
        float span=camera.orthographic?camera.orthographicSize*2:2*center.z*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2);
        float radius=span*.095f;
        var result=new List<string>();
        for(int axis=0;axis<3;axis++){
            var normal=Direction(axis);var u=Vector3.Cross(normal,Math.Abs(Vector3.Dot(normal,Vector3.up))>.9f?Vector3.right:Vector3.up).normalized;var v=Vector3.Cross(normal,u);
            for(int j=0;j<=48;j++){
                float a=j*Mathf.PI*2/48;var p=camera.WorldToScreenPoint(Pivot+radius*(u*Mathf.Cos(a)+v*Mathf.Sin(a)));
                if(p.z<=camera.nearClipPlane)return "";
                result.Add(((p.x-center.x)/rem).ToString("0.##",CultureInfo.InvariantCulture));result.Add(((center.y-p.y)/rem).ToString("0.##",CultureInfo.InvariantCulture));
            }
        }
        return string.Join(",",result);
    }
    internal string RotationTicks(){
        var camera=Camera.main;var input=Game.Input.InputManager.instance;
        if(!Held||!RotateMode||camera==null||Keyboard.current==null||!Keyboard.current.shiftKey.isPressed||!Application.isFocused||input.hasInputFieldFocus||input.overlayActive)return "";
        var center=camera.WorldToScreenPoint(Pivot);if(center.z<=camera.nearClipPlane)return "";
        float rem=Screen.height/1080f;
        float radius=(camera.orthographic?camera.orthographicSize*2:2*center.z*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2))*.095f;
        var result=new List<string>();
        for(int axis=0;axis<3;axis++){
            var normal=Direction(axis);var u=Vector3.Cross(normal,Math.Abs(Vector3.Dot(normal,Vector3.up))>.9f?Vector3.right:Vector3.up).normalized;var v=Vector3.Cross(normal,u);
            float phase=Axis==axis?(float)tickStartAngle*Mathf.Deg2Rad:0;
            if(Axis==axis){u=ringU;v=ringV;}
            for(int j=0;j<24;j++){
                float angle=phase+j*Mathf.PI/12;var direction=u*Mathf.Cos(angle)+v*Mathf.Sin(angle);
                float half=j%6==0?.045f:.025f;
                foreach(float factor in new[]{1-half,1+half}){
                    var p=camera.WorldToScreenPoint(Pivot+direction*(radius*factor));if(p.z<=camera.nearClipPlane)return "";
                    result.Add(((p.x-center.x)/rem).ToString("0.##",CultureInfo.InvariantCulture));result.Add(((center.y-p.y)/rem).ToString("0.##",CultureInfo.InvariantCulture));
                }
            }
        }
        return string.Join(",",result);
    }
    internal string RingMarkers(){
        var camera=Camera.main;if(!Held||!RotateMode||camera==null)return "";
        var center=camera.WorldToScreenPoint(Pivot);if(center.z<=camera.nearClipPlane)return "";
        float rem=Screen.height/1080f;
        float radius=(camera.orthographic?camera.orthographicSize*2:2*center.z*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2))*.095f;
        var result=new List<string>();
        for(int axis=0;axis<3;axis++){
            var direction=(Vector3)math.mul(ObjectFrame,axis==1?new float3(1,0,0):new float3(0,0,1));
            direction=Vector3.ProjectOnPlane(direction,Direction(axis));
            if(direction.sqrMagnitude<.0001f)direction=Vector3.ProjectOnPlane((Vector3)math.mul(ObjectFrame,new float3(0,1,0)),Direction(axis));
            if(direction.sqrMagnitude<.0001f)direction=Vector3.Cross(Direction(axis),Vector3.right);
            var point=camera.WorldToScreenPoint(Pivot+direction.normalized*radius);var delta=new Vector2(point.x-center.x,center.y-point.y)/rem;
            result.Add(delta.x.ToString("0.##",CultureInfo.InvariantCulture));result.Add(delta.y.ToString("0.##",CultureInfo.InvariantCulture));result.Add((Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg).ToString("0.##",CultureInfo.InvariantCulture));
        }
        return string.Join(",",result);
    }
}
}

using System;
using System.Globalization;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace GizmoXYZ {
public partial class GizmoSession {
    private quaternion referenceRotation=quaternion.identity;
    private float referenceFlashUntil;
    internal bool ReferenceAvailable=>CopyGroup&&CopyGet(copyTool,"GizmoReferenceVersion") is int version&&version==1;
    internal bool PickingReference {get;private set;}
    internal string ReferenceName=>ReferenceAvailable?(string)CopyGet(copyTool,"GizmoReferenceName"):"";
    private quaternion ObjectFrame=>CopyGroup?PivotMath.ObjectFrame(Rotation,referenceRotation):Rotation;
    private void ReadReference(){
        referenceRotation=ReferenceAvailable?(quaternion)CopyGet(copyTool,"GizmoReferenceRotation"):quaternion.identity;
        if(ReferenceAvailable){pivotMin=(float3)CopyGet(copyTool,"GizmoReferenceBoundsMin");pivotMax=(float3)CopyGet(copyTool,"GizmoReferenceBoundsMax");}
    }
    private bool ReferenceCommand(string command){
        if(command=="reference"){
            if(!ReferenceAvailable||!Local)return true;
            EndDrag("reference picker");PickingReference=!PickingReference;confirm=false;
            Diagnostics.Event("reference.picker",PickingReference.ToString());return true;
        }
        if(!command.StartsWith("reference:"))return false;
        if(!PickingReference||!ReferenceAvailable||!Local)return true;
        var xy=command.Substring(10).Split(',');
        if(xy.Length!=2||!float.TryParse(xy[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)||!float.TryParse(xy[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var y)||!math.isfinite(x)||!math.isfinite(y)||x<0||x>1||y<0||y>1||Camera.main==null)return true;
        var ray=Camera.main.ViewportPointToRay(new Vector3(x,1-y,0));
        // The bridge hit-tests only this group's preview snapshots, never unrelated world objects.
        if((bool)CopyCall("PickGizmoReference",(float3)ray.origin,(float3)ray.direction)){
            ReadReference();PickingReference=false;referenceFlashUntil=UnityEngine.Time.unscaledTime+1.5f;
            Diagnostics.Event("reference.selected",$"name={ReferenceName} rotation={referenceRotation.value}");
        }else Diagnostics.Event("reference.miss","no eligible selected prop under cursor");
        return true;
    }
    internal string ReferenceOutline(){
        if(!Held||!Local||!ReferenceAvailable||(!PickingReference&&UnityEngine.Time.unscaledTime>referenceFlashUntil)||Camera.main==null)return "";
        var corners=(float3[])CopyGet(copyTool,"GizmoReferenceCorners");if(corners.Length!=8)return "";
        var points=new Vector3[8];for(int i=0;i<8;i++){points[i]=Camera.main.WorldToViewportPoint(corners[i]);if(points[i].z<=Camera.main.nearClipPlane)return "";}
        var lines=new List<string>();
        for(int i=0;i<8;i++)for(int bit=1;bit<=4;bit*=2)if((i&bit)==0){var a=points[i];var b=points[i|bit];lines.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3}",a.x,1-a.y,b.x,1-b.y));}
        return string.Join(";",lines);
    }
}
}

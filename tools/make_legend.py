"""Printable play card rendered from the same mesh bank geometry as the mod."""
from pathlib import Path
import sys, math
import numpy as np
from PIL import Image,ImageDraw,ImageFont
import make_art as art

def main(path):
    models=art.build(); ss=2
    im=Image.new("RGB",(1080*ss,652*ss),"#F6F0E2");d=ImageDraw.Draw(im)
    def font(n,bold=False,serif=False):
        names=["georgiab.ttf","DejaVuSerif-Bold.ttf"] if serif else (["seguisb.ttf","DejaVuSans-Bold.ttf"] if bold else ["segoeui.ttf","DejaVuSans.ttf"])
        for nme in names:
            for f in (Path("C:/Windows/Fonts")/nme,nme):
                try:return ImageFont.truetype(str(f),n*ss)
                except OSError:pass
        return ImageFont.load_default()
    def txt(x,y,s,n,ink="#233C34",bold=False,serif=False):d.text((x*ss,y*ss),s,font=font(n,bold,serif),fill=ink)
    def box(rect,col,rad=0):
        b=tuple(int(x*ss) for x in rect);d.rounded_rectangle(b,radius=rad*ss,fill=col)
    def place(mesh,x,y,size):
        pts=np.asarray(mesh.v); faces=np.asarray(mesh.f);inds=faces[:,:3]
        # Front of the counter is -z. Keep the four fixed bowl positions legible.
        pitch=math.radians(49); hor=pts[:,0]; vert=math.cos(pitch)*pts[:,1]+math.sin(pitch)*pts[:,2]
        depth=math.sin(pitch)*pts[:,1]-math.cos(pitch)*pts[:,2]
        proj=np.column_stack(((x+size*hor)*ss,(y-size*vert)*ss))
        normal=np.cross(pts[inds[:,1]]-pts[inds[:,0]],pts[inds[:,2]]-pts[inds[:,0]])
        normal/=np.maximum(np.linalg.norm(normal,axis=1)[:,None],1e-12)
        light=normal@np.array([-.28,.85,-.44]);shades=.80+.20*np.maximum(0,light)
        # Back-face culling prevents the unseen underside of a badge/tile from
        # bleeding into a thin top face in this orthographic painter's render.
        front=normal@np.array([0,math.sin(pitch),-math.cos(pitch)])
        for idx in np.argsort(np.mean(depth[inds],axis=1)):
            if front[idx]<=0:continue
            color=bytes.fromhex(art.SWATCH[art.COL[faces[idx,3]]])
            rgb=tuple(min(255,round(k*shades[idx])) for k in color)
            d.polygon([tuple(pt) for pt in proj[inds[idx]]],fill=rgb)
    box((0,0,1080,152),"#24413B");txt(44,23,"S A M O S A   K I T C H E N",14,"#C6DA9D",True)
    txt(41,49,"The masala counter",54,"#FFF4D8",serif=True)
    txt(697,77,"4 bowls   /   15 possible orders",19,"#E1ECD1")
    labels=[("G","Coriander-pea","Leaf"),("R","Red chilli","Pepper"),("Y","Turmeric","Sun"),("B","Roasted cumin","Three seeds")]
    for i,(code,title,shape) in enumerate(labels):
        x=42+i*252;box((x,175,x+237,254),"#E9E3D3",14)
        place(models["badge_"+code.lower()],x+42,225,222)
        txt(x+82,185,title,18,bold=True);txt(x+82,214,code+"  /  "+shape,15,"#5A6758")
    box((42,275,517,596),"#EBE0C8",18);box((538,275,1038,596),"#E4E8DA",18)
    txt(64,290,"01    SELECT A SCOOP",14,"#475E4C",True)
    counter=art.Mesh();counter.paste(models["counter"])
    positions=((-0.215,-.195),(.215,-.195),(.215,.195),(-.215,.195))
    for i,(x,z) in enumerate(positions):counter.paste(models["badge_"+"gryb"[i]],(x,.607,z),(.6,.6,.6))
    counter.paste(models["selector"],(.215,.608,-.195))
    counter.paste(models["badge_r"],(0,.793,.050),(1.14,1.14,1.14))
    place(counter,278,439,170)
    txt(65,506,"Interact cycles. Grab takes one.",24,bold=True)
    txt(65,546,"The frame and raised sign mark the active bowl.",16)
    txt(561,290,"02    READ THE TICKET",14,"#475E4C",True)
    served=art.Mesh();served.paste(models["plated"])
    for j,k in enumerate("grb"):served.paste(models["badge_"+k],((j-1)*.2128,.092,-.235),(.8,.8,.8))
    place(served,779,434,245)
    txt(561,506,"Leaf + pepper + seeds",24,bold=True)
    txt(561,546,"Add green, red and brown to ONE chopped potato.",16)
    txt(44,613,"Mix before heat.  Cook filling  /  add dough  /  fold  /  fry  /  plate.",18,"#344F42",True)
    path.parent.mkdir(exist_ok=True,parents=True);im.resize((1080,652),Image.Resampling.LANCZOS).save(path)

if __name__=="__main__":main(Path(sys.argv[1]) if len(sys.argv)>1 else art.HOME/"dist"/"Masala-Counter-Guide.png")

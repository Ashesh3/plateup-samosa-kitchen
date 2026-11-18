"""Render README illustrations from the meshes embedded in the player DLL.

The committed mesh bank is the exact resource used by the game. This tool
needs only NumPy and Pillow; it does not read a game installation or a save.
"""
import math
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "images"
SS = 2
INK, PAPER, GOLD, DARK = "#22433C", "#FAF2DF", "#DFAC61", "#163A34"


def load_bank():
    with (ROOT / "Assets" / "samosa.smk").open("rb") as f:
        def read(pattern):
            return struct.unpack("<" + pattern, f.read(struct.calcsize("<" + pattern)))
        assert f.read(4) == b"SMK1"
        palette = np.array([read("3B") for _ in range(read("H")[0])])
        models = {}
        for _ in range(read("H")[0]):
            key = f.read(read("B")[0]).decode("ascii")
            nv, nf = read("II")
            vertices = np.frombuffer(f.read(nv*12), dtype="<f4").reshape(nv,3).astype(float)
            faces = np.array([read("3HB") for _ in range(nf)], dtype=int)
            models[key] = vertices, faces
        assert not f.read(1)
        return models, palette


MODELS, PALETTE = load_bank()


def plate(keys):
    """Same ticket ribbon offsets and scale as the playable plated item."""
    verts, faces = MODELS["plated"]
    vs, fs = [verts], [faces]
    offset = len(verts)
    for j,key in enumerate(keys):
        v, f = MODELS["badge_" + key]
        a = math.radians(-14)
        rx = np.array([[1,0,0],[0,math.cos(a),-math.sin(a)],[0,math.sin(a),math.cos(a)]])
        v = (v*.80) @ rx.T + [(j-(len(keys)-1)*.5)*.2128,.092,-.235]
        f=f.copy(); f[:,:3]+=offset; offset+=len(v)
        vs.append(v);fs.append(f)
    return np.vstack(vs),np.vstack(fs)


def render(image,model,x,y,size,yaw=-12,pitch=34):
    """Flat-shaded orthographic triangle rasterizer, with a proper depth buffer."""
    vertices, faces = model
    a,b = np.radians([yaw,pitch])
    right=np.array([math.cos(a),0,math.sin(a)])
    back=np.array([-math.sin(a),0,math.cos(a)])
    up=np.array([0,math.cos(b),0])+back*math.sin(b)
    near=np.array([0,math.sin(b),0])-back*math.cos(b)
    p=np.column_stack(((x+size*(vertices@right))*SS,(y-size*(vertices@up))*SS))
    depth=vertices@near
    x0=max(0,int(np.floor(p[:,0].min()))-1); y0=max(0,int(np.floor(p[:,1].min()))-1)
    x1=min(image.width,int(np.ceil(p[:,0].max()))+2); y1=min(image.height,int(np.ceil(p[:,1].max()))+2)
    buf=np.asarray(image.crop((x0,y0,x1,y1))).copy()
    zb=np.full((y1-y0,x1-x0),-np.inf)
    p-=np.array([x0,y0])
    light=np.array([-.48,.84,-.25]);light/=np.linalg.norm(light)
    for v0,v1,v2,mat in faces:
        n=np.cross(vertices[v1]-vertices[v0],vertices[v2]-vertices[v0])
        ln=np.linalg.norm(n)
        if ln<1e-10 or n@near<=0:continue
        n/=ln
        va,vb,vc=p[[v0,v1,v2]]
        lx=max(0,math.floor(min(va[0],vb[0],vc[0])));hx=min(buf.shape[1],math.ceil(max(va[0],vb[0],vc[0]))+1)
        ly=max(0,math.floor(min(va[1],vb[1],vc[1])));hy=min(buf.shape[0],math.ceil(max(va[1],vb[1],vc[1]))+1)
        if lx>=hx or ly>=hy:continue
        u=vb-va; w=vc-va; den=u[0]*w[1]-u[1]*w[0]
        if abs(den)<1e-7:continue
        yy,xx=np.ogrid[ly:hy,lx:hx]; dx=xx+.5-va[0];dy=yy+.5-va[1]
        l1=(dx*w[1]-dy*w[0])/den; l2=(u[0]*dy-u[1]*dx)/den
        z=depth[v0]+l1*(depth[v1]-depth[v0])+l2*(depth[v2]-depth[v0])
        sub=zb[ly:hy,lx:hx]
        take=(l1>=0)&(l2>=0)&(l1+l2<=1)&(z>sub)
        sub[take]=z[take]
        buf[ly:hy,lx:hx][take]=np.minimum(255,PALETTE[mat]*(.76+.31*max(0.,float(n@light)))).astype("uint8")
    image.paste(Image.fromarray(buf),(x0,y0))


def font(size,family="body"):
    pairs={"body":("segoeui.ttf","DejaVuSans.ttf"),"bold":("seguisb.ttf","DejaVuSans-Bold.ttf"),"title":("georgiab.ttf","DejaVuSerif-Bold.ttf")}
    for filename in pairs[family]:
        for p in (Path("C:/Windows/Fonts")/filename,filename):
            try:return ImageFont.truetype(str(p),size*SS)
            except OSError:pass
    raise RuntimeError("Install the DejaVu Sans and Serif fonts to render the illustrations.")


class Card:
    def __init__(self,w,h,bg):
        self.size=(w,h);self.im=Image.new("RGB",(w*SS,h*SS),bg);self.d=ImageDraw.Draw(self.im)
    def text(self,x,y,s,n,color=INK,family="body"):
        self.d.text((x*SS,y*SS),s,font=font(n,family),fill=color,anchor="lt")
    def shape(self,method,xy,**kw):
        for k in ("width","radius"):
            if k in kw:kw[k]*=SS
        getattr(self.d,method)(tuple(int(c*SS) for c in xy),**kw)
    def save(self,name):
        OUT.mkdir(exist_ok=True,parents=True)
        self.im.resize(self.size,Image.Resampling.LANCZOS).save(OUT/name,optimize=True)


def banner():
    c=Card(1440,480,DARK)
    c.shape("ellipse",(743,-153,1502,603),fill="#224A40")
    for inset in (0,15,28):
        c.shape("ellipse",(821+inset,0+inset,1344-inset,464-inset),outline="#4A6653",width=1)
    c.shape("rounded_rectangle",(47,43,312,80),radius=18,fill="#D9E8BD")
    c.text(68,53,"PLATEUP!   /   CUSTOM MAIN",17,DARK,"bold")
    c.text(58,103,"Samosa",99,PAPER,"title");c.text(58,205,"Kitchen",99,PAPER,"title")
    c.text(64,338,"Four masalas. One golden fold.",31,"#E7D7AD")
    c.shape("line",(64,403,600,403),fill="#6A806A",width=1)
    c.text(65,422,"15 FILLING COMBINATIONS     /     5-STAR RECIPE",19,"#E8E9C0","bold")
    c.shape("ellipse",(871,287,1251,366),fill="#203E35")
    render(c.im,plate("grb"),1060,291,604,-12,32)
    c.shape("rounded_rectangle",(920,401,1207,451),radius=23,fill="#224A40")
    c.text(941,420,"LEAF  +  PEPPER  +  SEEDS",18,"#EEE1AE","bold")
    c.save("samosa-kitchen.png")


def recipe():
    c=Card(1200,410,PAPER)
    c.text(35,25,"AT THE HOB",16,"#577365","bold")
    c.text(33,53,"From filling to first bite",40,INK,"title")
    cards=(("mix","01","Season","Chopped potato + scoops"),
           ("filling","02","Cook","Heat the filling on a hob"),
           ("open","03","Fill","Add it to kneaded dough"),
           ("raw","04","Fold","Knead the parcel shut"),
           ("golden","05","Fry & serve","Fry, then plate one"))
    for i,(key,no,action,detail) in enumerate(cards):
        x=30+i*231
        c.shape("rounded_rectangle",(x,123,x+216,349),radius=14,fill="#E9E1CD" if i%2==0 else "#E1E6D6")
        c.text(x+14,136,no,16,"#48665B","bold")
        c.shape("ellipse",(x+48,234,x+166,249),fill="#D3CEBA")
        mod=plate("g") if i==4 else MODELS[key]
        render(c.im,mod,x+108,224,193 if i==4 else 205,-10,38)
        c.text(x+14,269,action,25,INK,"bold")
        c.text(x+14,308,detail,15,"#465F52")
    c.text(33,373,"Prep tip: add every requested scoop before the filling goes on the hob.",19,INK,"bold")
    c.save("recipe-stages.png")

if __name__=="__main__":
    banner()
    recipe()

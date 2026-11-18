"""Original miniature food models. No external meshes, textures, or editor required.

Writes an indexed little-endian mesh bank for Unity. --obj exports editable models
and a material library for Blender. Coordinates are Unity's (x right, y up, z back).
"""
import argparse
import math
import random
import struct
from pathlib import Path

HOME = Path(__file__).resolve().parents[1]
TAU = math.tau
SWATCH = {
    "pastry": "E6BD80", "dough": "F2DAC0", "crimp": "CF9B62",
    "honey": "E69A36", "amber": "C87525", "toast": "A35424", "sun": "F7BD60",
    "pea": "64933C", "peaLight": "8DAA4D", "potato": "E7C06C", "masala": "BC792D",
    "cumin": "785332", "chilli": "BE5137", "cream": "EEE5CF", "teal": "356D68",
    "rim": "92B5A4", "wood": "AF7652", "darkWood": "805641", "brass": "EAC074",
    "leaf": "72B845", "pepper": "EA5945", "turmeric": "F6C044", "roast": "A16C55",
    "ink": "263333", "ivory": "FFF5CC", "shadow": "344844"
}
COL = list(SWATCH)


def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]
def norm(a):
    s=math.sqrt(dot(a,a))
    return tuple(x/s for x in a)


class Mesh:
    def __init__(self): self.v=[]; self.f=[]
    def vert(self,p):
        self.v.append(tuple(p)); return len(self.v)-1
    def face(self,a,b,c,col,direction):
        n=cross(sub(self.v[b],self.v[a]),sub(self.v[c],self.v[a]))
        if dot(n,n)<1e-18: return
        if dot(n,direction)<0: b,c=c,b
        self.f.append((a,b,c,COL.index(col)))
    def surface(self,rings,palette):
        # rings bottom to top, never coincident at a pole
        mids=[tuple(sum(p[i] for p in ring)/len(ring) for i in range(3)) for ring in rings]
        ix=[[self.vert(p) for p in rr] for rr in rings]; n=len(ix[0])
        for j in range(len(ix)-1):
            for i in range(n):
                k=(i+1)%n; a,b=ix[j][i],ix[j][k]; c,d=ix[j+1][i],ix[j+1][k]
                # reverse the winding in x/z order: lower i -> upper i -> lower i+1
                direct=cross(sub(self.v[c],self.v[a]),sub(self.v[b],self.v[a]))
                mat=palette(j,i); self.face(a,c,b,mat,direct); self.face(c,d,b,mat,direct)
        for j,up in ((0,-1),(-1,1)):
            mid=self.vert(mids[j])
            for i in range(n): self.face(mid,ix[j][i],ix[j][(i+1)%n],palette(max(0,len(ix)-2) if up==1 else 0,i),(0,up,0))
    def disk(self,rings,n,colors,center=(0,0),phase=0):
        x,z=center
        self.surface([[(x+r*math.cos(phase+TAU*i/n),y,z+r*math.sin(phase+TAU*i/n)) for i in range(n)] for r,y in rings],lambda j,i:colors[min(j,len(colors)-1)])
    def ell(self,center,size,col,n=8):
        x,y,z=center;a,b,c=size
        self.surface([[(x+a*math.cos(h)*math.cos(TAU*i/n+.19),y+b*math.sin(h),z+c*math.cos(h)*math.sin(TAU*i/n+.19)) for i in range(n)] for h in (-1.25,-.58,.22,.85,1.31)],lambda j,i:col)
    def box(self,center,size,col,angle=0):
        x,y,z=center; a,b,c=(v/2 for v in size)
        rings=[]
        for dy,scale in ((-b,.82),(-b*.65,1),(b*.56,1),(b,.78)):
            r=[]
            for u,v in ((-a,-c),(a,-c),(a,c),(-a,c)):
                r.append((x+scale*(u*math.cos(angle)-v*math.sin(angle)),y+dy,z+scale*(u*math.sin(angle)+v*math.cos(angle))))
            rings.append(r)
        self.surface(rings, lambda j,i:col)
    def paste(self,other,where=(0,0,0),scale=(1,1,1),turn=0):
        n=len(self.v); x,y,z=where
        for u,v,w in other.v:
            u*=scale[0];w*=scale[2]
            self.v.append((x+u*math.cos(turn)-w*math.sin(turn),y+v*scale[1],z+u*math.sin(turn)+w*math.cos(turn)))
        self.f.extend((a+n,b+n,c+n,k) for a,b,c,k in other.f)


def filling(cooked,peas_only=False):
    m=Mesh(); rng=random.Random(44)
    m.ell((0,.042,0),(.20,.041,.14), "masala" if cooked else "pea",12)
    pts=[(-.11,-.06),(.02,-.10),(.13,-.04),(-.15,.048),(-.025,.04),(.10,.08),(-.065,-.028),(.05,-.005),(.01,.08)]
    for i,(x,z) in enumerate(pts):
        height=.057+(.046 if i>=6 else 0)
        if not peas_only: m.box((x,height,z),(.115,.073,.089),"masala" if cooked and i%3==0 else "potato",rng.uniform(-1,1))
    for i in range(14 if peas_only else 9):
        angle=TAU*i/(14 if peas_only else 9)+.23
        x=.164*math.cos(angle);z=.116*math.sin(angle)
        m.ell((x,.058 if peas_only else .092,z),(.038,.037,.039),"pea" if i%3 else "peaLight",7)
    if peas_only:
        for i in range(5): m.ell(((i%3-1)*.067,.096,(i//3-.5)*.064),(.039,.039,.037),"peaLight" if i%2 else "pea",7)
    for i in range(7):
        m.box((rng.uniform(-.12,.13),.105 if peas_only else .145,rng.uniform(-.075,.07)),(.040,.012,.014),"chilli" if i%3==0 else "cumin",rng.uniform(0,3))
    return m


def samosa(golden):
    m=Mesh()
    # A softly bulging triangular base, pinched to an off-centre peak. The
    # front seam is modelled, not just painted, so it remains clear from above.
    tri=((-0.302,-.174),(.302,-.174),(.010,.260))
    outline=[]
    for i,p in enumerate(tri):
        q=tri[(i+1)%3]
        for k in range(6):
            t=k/6; x=p[0]*(1-t)+q[0]*t; z=p[1]*(1-t)+q[1]*t
            dx=q[0]-p[0];dz=q[1]-p[1]; d=math.hypot(dx,dz)
            bulge=.019*math.sin(math.pi*t)
            outline.append((x+dz/d*bulge,z-dx/d*bulge))
    rings=[]
    for h,s,lean in ((.008,.83,0),(.052,1,0),(.124,.92,.011),(.232,.68,.029),(.332,.37,.045),(.405,.11,.056)):
        rings.append([(x*s+lean,h+.007*math.sin(i*1.7)*s,z*s+.008*(1-s)) for i,(x,z) in enumerate(outline)])
    m.surface(rings,lambda j,i: ("amber" if j==0 else "honey" if j<4 else "sun") if golden else ("crimp" if j==0 else "pastry" if j==1 else "dough"))
    # Raised zig-zag pinches along one sloped edge.
    for i in range(8):
        t=i/8.2
        x=-.257+.313*t; z=-.153+.162*t; y=.068+.356*t
        m.ell((x,y,z),(.039-.014*t,.017,.024),"amber" if golden and i%2 else "sun" if golden else "crimp",6)
    # Tiny raised bubbles on the two viewer-facing sheets (no texture files).
    for i in range(8):
        t=(i%4)/4+.08; h=.12+(i//4)*.085+(i%2)*.018
        r=1-(h-.07)/.46
        x=(-.185+.38*t)*r+.020; z=-.193*r
        m.ell((x,h,z),(.021,.017,.013),"amber" if golden and i%3==0 else "sun" if golden else "pastry",6)
    return m


def badge(index):
    # A two-tone, thick-edged pictogram on a dark tile. Top face normals point
    # up, so signs and order ribbons remain readable from the gameplay camera.
    m=Mesh(); m.box((0,.018,0),(.25,.036,.23),"shadow")
    col=("leaf","pepper","turmeric","roast")[index]
    m.box((0,.041,0),(.225,.015,.201),col)
    if index==0:  # leaf and central vein
        m.ell((-.012,.065,.005),(.065,.016,.044),"ink",10)
        m.box((.012,.077,.001),(.105,.014,.014),"ivory",-.15)
    elif index==1:  # diagonal chilli with a bent stem
        m.box((-.001,.061,.009),(.110,.026,.046),"ink",-.40)
        m.ell((-.055,.062,.032),(.023,.013,.012),"ivory",7)
        m.box((.054,.064,-.027),(.035,.015,.017),"ivory",.34)
    elif index==2:  # angular sun
        for i in range(4):m.box((0,.059,0),(.139,.010,.020),"ink",i*math.pi/4)
        m.disk([(.039,.062),(.036,.077)],8,["ink"])
        m.disk([(.017,.077),(.016,.083)],8,["ivory"])
    else:         # three distinct cumin seeds, not just another coloured circle
        for x,z in ((-.039,-.026),(.035,-.021),(.003,.036)):
            m.box((x,.065,z),(.054,.021,.022),"ivory",-.55)
    return m


def selector():
    m=Mesh()
    # Open rectangular frame; never occludes the selected bowl.
    for y,h,mat,w in ((.016,.031,"ink",.036),(.044,.029,"ivory",.023)):
        for sign in (-1,1):
            m.box((sign*.163,y,0),(w,h,.324),mat)
            m.box((0,y,sign*.154),(.324,h,w),mat)
    m.ell((0,.066,-.166),(.034,.025,.023),"ivory",7)
    return m


def build():
    peas=filling(False,True);mix=filling(False); fill=filling(True)
    opened=Mesh();opened.disk([(.147,.006),(.277,.033),(.265,.055),(.15,.077)],18,["pastry","crimp","dough"])
    opened.paste(fill,(0,.059,0),(.88,.9,.88))
    raw=samosa(False);cooked=samosa(True)
    served=Mesh()
    served.disk([(.265,.007),(.348,.023),(.342,.060),(.325,.067),(.304,.058),(.246,.040)],32,["cream","cream","teal","cream","cream"])
    served.paste(cooked,(-.035,.043,.005),(.88,.88,.88),.12)
    counter=Mesh()
    counter.box((0,.23,0),(.86,.46,.84),"darkWood")
    counter.box((0,.25,0),(.882,.40,.86),"teal")
    counter.box((0,.461,0),(.92,.066,.90),"wood")
    counter.box((0,.313,-.434),(.31,.123,.02),"brass")
    for j,(x,z) in enumerate(((-.215,-.195),(.215,-.195),(.215,.195),(-.215,.195))):
        counter.disk([(.112,.492),(.153,.559),(.150,.583),(.124,.576)],14,["brass","cream","brass"],(x,z))
        counter.ell((x,.575,z),(.109,.033,.107),("leaf","pepper","turmeric","roast")[j],9)
        for i in range(5):
            a=i*TAU/5+.3*j
            counter.ell((x+.067*math.cos(a),.602,z+.061*math.sin(a)),(.021,.015,.022),("peaLight","chilli","sun","cumin")[j],5)
    models=dict(peas=peas,mix=mix,filling=fill,open=opened,raw=raw,golden=cooked,plated=served,counter=counter,selector=selector())
    for i,k in enumerate("gryb"):
        models["badge_"+k]=badge(i)
        if i:
            scoop=Mesh();scoop.ell((0,.045,0),(.165,.042,.120),("leaf","pepper","turmeric","roast")[i],9)
            for h in range(7):
                a=h*TAU/7; scoop.box((.119*math.cos(a),.083,.076*math.sin(a)),(.055,.027,.039),("pea","chilli","sun","cumin")[i],a)
            models["scoop_"+k]=scoop
    return models


def write_bank(models):
    out=bytearray(struct.pack("<4sH",b"SMK1",len(COL)))
    for col in COL: out+=bytes.fromhex(SWATCH[col])
    out+=struct.pack("<H",len(models))
    for key,m in models.items():
        name=key.encode("ascii");out+=struct.pack("<B",len(name))+name+struct.pack("<II",len(m.v),len(m.f))
        for p in m.v: out+=struct.pack("<3f",*p)
        for f in m.f: out+=struct.pack("<3HB",*f)
    folder=HOME/"Assets";folder.mkdir(exist_ok=True);(folder/"samosa.smk").write_bytes(out)


def export_obj(models,folder):
    folder.mkdir(exist_ok=True,parents=True)
    with (folder/"samosa.mtl").open("w") as f:
        for i,(k,h) in enumerate(SWATCH.items()):
            if i:f.write("\n")
            f.write("newmtl %s\nKd %.5f %.5f %.5f\n"%(k,*[x/255 for x in bytes.fromhex(h)]))
    for name,m in models.items():
        with (folder/(name+".obj")).open("w") as f:
            f.write("mtllib samosa.mtl\no "+name+"\n")
            for p in m.v:f.write("v %.6f %.6f %.6f\n"%p)
            prev=-1
            for a,b,c,k in m.f:
                if prev!=k:f.write("usemtl "+COL[k]+"\n");prev=k
                f.write("f %d %d %d\n"%(a+1,b+1,c+1))

if __name__=="__main__":
    args=argparse.ArgumentParser();args.add_argument("--obj",type=Path);opts=args.parse_args()
    models=build();write_bank(models)
    if opts.obj:export_obj(models,opts.obj)
    print("Mesh bank: "+", ".join("%s:%d tris"%(k,len(m.f)) for k,m in models.items()))

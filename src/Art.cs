using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kitchen;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SamosaKitchen
{
    // Templates stay activeSelf=true under an inactive parent. Instantiating a
    // template into a holder therefore yields a visible item, not an inactive clone.
    internal static class Art
    {
        private sealed class Model
        {
            public Mesh Mesh;
            public Material[] Materials;
        }
        static GameObject library;
        static readonly Dictionary<string,Model> geometry = new Dictionary<string,Model>(StringComparer.Ordinal);
        static readonly Dictionary<string,GameObject> templates = new Dictionary<string,GameObject>(StringComparer.Ordinal);
        static Material[] palette;

        internal static Vector3 BowlPosition(int i,float height)
        {
            return new Vector3(i==0||i==3?-.215f:.215f,height,i<2?-.195f:.195f);
        }
        internal static GameObject Template(string key,int mask=0)
        {
            if(library==null){
                library=new GameObject("Samosa Kitchen - templates");
                library.SetActive(false);
                Object.DontDestroyOnLoad(library);
                templates.Clear();
            }
            string cacheKey=key+":"+mask;
            if(templates.TryGetValue(cacheKey,out var found)&&found!=null)return found;
            if(geometry.Count==0) ReadBank();
            var model=geometry[key=="card"?"plated":key];
            var obj=new GameObject("Samosa - "+key);
            obj.transform.SetParent(library.transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=model.Mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterials=model.Materials;
            if(key=="open"||key=="mix"||key=="plated"){
                // The native view calls ToDictionary on this list. Our complete,
                // fixed two-ingredient models have no optional child toggles.
                obj.AddComponent<ItemGroupView>().ComponentGroups=new List<ItemGroupView.ComponentGroup>();
            }
            if(key=="plated"){
                Child(obj,"Side Container",new Vector3(.24f,.062f,.11f));
                for(int m=1;m<=15;m++){
                    var root=Child(obj,"Mix-"+m,Vector3.zero).gameObject;
                    Ribbon(root,m,.80f,.092f,-.235f);
                    root.SetActive(m==1); // legible default for static snapshots; native group view updates clones
                }
            }
            if(mask!=0){
                bool scoop=key=="peas"||key.StartsWith("scoop_",StringComparison.Ordinal);
                Ribbon(obj,mask,scoop?.74f:.69f,(key=="raw"||key=="golden")?.065f:.06f,scoop?-.145f:-.205f);
            }
            if(key=="card")Ribbon(obj,15,.74f,.092f,-.23f);
            if(key=="counter"){
                // A navmesh obstacle is not a player collider. Vanilla ingredient
                // crates use a non-trigger box on Default (layer 0); respect
                // prep-phase NoClip rather than overriding global collision rules.
                var body=Child(obj,"Collider",Vector3.zero).gameObject;
                body.layer=0;
                var solid=body.AddComponent<BoxCollider>();
                solid.center=new Vector3(0,.35f,0);solid.size=new Vector3(.86f,.70f,.84f);
                solid.isTrigger=false;
                obj.AddComponent<HoldPointContainer>().HoldPoint=Child(obj,"HoldPoint",new Vector3(0,.65f,0));
                var view=obj.AddComponent<MasalaSelectionView>();view.SelectedCards=new Transform[4];
                for(int i=0;i<4;i++){
                    string icon="badge_"+"gryb"[i];var at=BowlPosition(i,.607f);
                    Mark(obj,icon,"Bowl-"+i,at,.60f);
                    var card=Mark(obj,icon,"Selected-"+i,new Vector3(0,.793f,.050f),1.14f);
                    card.localRotation=Quaternion.Euler(-22f,0,0);
                    card.gameObject.SetActive(i==0);view.SelectedCards[i]=card;
                }
                view.Cursor=Mark(obj,"selector","Active bowl",BowlPosition(0,.608f),1);
            }
            templates[cacheKey]=obj;
            return obj;
        }
        static Transform Mark(GameObject obj,string mesh,string name,Vector3 p,float size){
            var t=Child(obj,name,p);t.localScale=Vector3.one*size;var model=geometry[mesh];
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=model.Mesh;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=model.Materials;
            return t;
        }
        static void Ribbon(GameObject obj,int mask,float size,float y,float z){
            int count=0;for(int i=0;i<4;i++)if((mask&(1<<i))!=0)count++;
            int pos=0;for(int i=0;i<4;i++)if((mask&(1<<i))!=0){
                float x=(pos++-(count-1)*.5f)*(.266f*size);
                var t=Mark(obj,"badge_"+"gryb"[i],"Flavor-"+Flavors.Codes[i],new Vector3(x,y,z),size);
                t.localRotation=Quaternion.Euler(-14,0,0);
            }
        }
        static Transform Child(GameObject o,string name,Vector3 position)
        {
            var t=new GameObject(name).transform;
            t.SetParent(o.transform,false); t.localPosition=position; return t;
        }
        static void ReadBank()
        {
            var bank=new Dictionary<string,Model>(StringComparer.Ordinal);
            var shader=Shader.Find("Simple Flat");
            if(shader==null||!shader.isSupported)shader=Shader.Find("Standard");
            if(shader==null||!shader.isSupported)throw new InvalidDataException("Samosa Kitchen: neither food shader is available");
            using(var stream=typeof(Art).Assembly.GetManifestResourceStream("SamosaKitchen.Art.smk"))
            using(var b=new BinaryReader(stream??throw new InvalidDataException("Missing embedded mesh bank"))){
                if(Encoding.ASCII.GetString(b.ReadBytes(4))!="SMK1")throw new InvalidDataException("Invalid samosa mesh header");
                int colors=b.ReadUInt16(); if(colors<1||colors>64)throw new InvalidDataException("Bad palette");
                palette=new Material[colors];
                for(int i=0;i<colors;i++){
                    var rgb=new Color32(b.ReadByte(),b.ReadByte(),b.ReadByte(),255);
                    var mat=new Material(shader){name="Samosa swatch "+i};
                    if(mat.HasProperty("_Color0")){
                        Color c=rgb;c.a=0;mat.SetColor("_Color0",c);
                        if(mat.HasProperty("_Shininess"))mat.SetFloat("_Shininess",.06f);
                        if(mat.HasProperty("_OverlayScale"))mat.SetFloat("_OverlayScale",10f);
                    }else mat.color=rgb;
                    palette[i]=mat;
                }
                int count=b.ReadUInt16(); if(count!=16)throw new InvalidDataException("Missing samosa stage models or flavor symbols");
                for(int j=0;j<count;j++){
                    string key=Encoding.ASCII.GetString(b.ReadBytes(b.ReadByte()));
                    int nv=checked((int)b.ReadUInt32()),nt=checked((int)b.ReadUInt32());
                    if(nv<3||nv>65535||nt<1||nt>3500)throw new InvalidDataException("Invalid stage geometry "+key);
                    var v=new Vector3[nv];for(int i=0;i<nv;i++)v[i]=new Vector3(b.ReadSingle(),b.ReadSingle(),b.ReadSingle());
                    // Split vertices per face for flat normals; reuse mesh/materials for every serving.
                    var vs=new Vector3[nt*3];var buckets=new SortedDictionary<int,List<int>>();
                    for(int i=0;i<nt;i++){
                        vs[i*3]=v[b.ReadUInt16()];vs[i*3+1]=v[b.ReadUInt16()];vs[i*3+2]=v[b.ReadUInt16()];
                        int color=b.ReadByte();if(color>=colors)throw new InvalidDataException("Invalid material in "+key);
                        if(!buckets.TryGetValue(color,out var tri))buckets[color]=tri=new List<int>();
                        tri.Add(i*3);tri.Add(i*3+1);tri.Add(i*3+2);
                    }
                    var mesh=new Mesh{name="Samosa Kitchen / "+key,vertices=vs,subMeshCount=buckets.Count};
                    var mats=new Material[buckets.Count];int k=0;
                    foreach(var sub in buckets){mesh.SetTriangles(sub.Value,k,false);mats[k++]=palette[sub.Key];}
                    mesh.RecalculateNormals();mesh.RecalculateBounds();
                    bank.Add(key,new Model{Mesh=mesh,Materials=mats});
                }
                if(stream.Position!=stream.Length)throw new InvalidDataException("Trailing mesh data");
            }
            foreach(var pair in bank)geometry.Add(pair.Key,pair.Value);
        }
    }
}

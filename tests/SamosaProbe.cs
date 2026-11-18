// Test-only assembly, omitted from all player ZIPs. Real retail matchers,
// build tables and client views; no changes to any player restaurant or save.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kitchen;
using KitchenData;
using KitchenLib.Event;
using KitchenLib.References;
using KitchenLib.Systems;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using U=UnityEngine.Object;

namespace LocalSamosaTest
{
    public sealed class Probe:IModInitializer
    {
        public void PreInject(){} public void PostInject(){}
        public void PostActivate(Mod m){Events.BuildGameDataPostViewInitEvent+=Check;}
        const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static int checks;
        static void Assert(bool b,string detail){if(!b)throw new Exception(detail);}
        static T Our<T>(GameData d,string name) where T:GameDataObject=>d.Get<T>().Single(x=>x.name=="local.samosakitchen - "+name);
        static string Name(string old,int mask)=>mask==1?old:old+"_"+mask;
        static void Check(object s,BuildGameDataEventArgs args){
            var state=UnityEngine.Random.state;
            try{Run(args.gamedata);Debug.Log("[MasalaQA] PASS build="+(++checks)+"; 15/15 order mixes; 64 prep paths; 225 exact-service pairs; selector cycle/Grab/remote display; plate provision/dirty return; burn guards.");}
            catch(Exception e){Debug.LogError("[MasalaQA] FAIL: "+e);}
            finally{UnityEngine.Random.state=state;}
        }
        static void Run(GameData d){
            var dish=Our<Dish>(d,"samosa_main");var plate=Our<ItemGroup>(d,"plated_samosa");var counter=Our<Appliance>(d,"masala_counter");
            string[] spiceNames={"masala_peas","masala_red_chilli","masala_yellow_turmeric","masala_brown_cumin"};
            var spice=spiceNames.Select(n=>Our<Item>(d,n)).ToArray();
            int pot=ItemReferences.PotatoChopped,p=ItemReferences.Plate, dough=ItemReferences.Dough,cook=ProcessReferences.Cook,knead=ProcessReferences.Knead;
            Assert(dish.Type==DishType.Base&&dish.RecipeDifficulty()==5&&MainMenuDishDebugSystem.MenuOptions.Contains(dish.ID),"HQ dish must be a five-star starting recipe");
            Assert(dish.UnlocksMenuItems.Count==1&&dish.UnlocksMenuItems[0].Item.ID==plate.ID&&dish.UnlocksMenuItems[0].Phase==MenuPhase.Main,"single plated main");
            Assert(dish.MinimumIngredients.Select(i=>i.ID).OrderBy(n=>n).SequenceEqual(new[]{ItemReferences.Potato,ItemReferences.Flour,p,spice[0].ID}.OrderBy(n=>n)),"lobby plate, pantry or station count");
            Assert(spice.All(i=>i.DedicatedProvider.ID==counter.ID)&&d.Get<Item>(p).DedicatedProvider.Properties.OfType<CItemProvider>().Any(x=>x.DefaultProvidedItem==p),"dedicated providers");
            Assert(plate.DirtiesTo.ID==ItemReferences.PlateDirty&&plate.DisposesTo.ID==p&&plate.Reward==8&&plate.CanContainSide,"plating lifecycle");
            Action<int,int,int> proc=(id,process,expect)=>{int actual;Assert(d.ProcessesView.GetResultOfProcess(id,process,out actual)&&actual==expect,"process "+id+"/"+process+" -> "+actual+" expected "+expect);};
            proc(ItemReferences.Potato,ProcessReferences.Chop,pot);proc(ItemReferences.Flour,knead,dough);
            var mixes=new ItemGroup[16];var gold=new Item[16];var side=new HashSet<int>();var cookedToMask=new Dictionary<int,int>();
            var hob=(NativeHashMap<ApplianceItemPair,ApplianceProcessPair>)typeof(ProcessesView).GetField("ApplianceItemProcessMap",Any).GetValue(d.ProcessesView);
            void Match(List<int> list,int expect){int actual;Assert(d.ItemSetView.IsValidGroup(new ItemList(list),out actual)==Satisfaction.Correct&&actual==expect,"merge "+string.Join(",",list)+" -> "+actual+" expected "+expect);}
            for(int m=1;m<=15;m++){
                mixes[m]=Our<ItemGroup>(d,Name("cold_filling",m));var fill=Our<Item>(d,Name("cooked_filling",m));
                var open=Our<ItemGroup>(d,Name("open_parcel",m));var raw=Our<Item>(d,Name("folded_samosa",m));
                gold[m]=Our<Item>(d,Name("golden_samosa",m));cookedToMask.Add(gold[m].ID,m);
                var atoms=new List<int>{pot};for(int i=0;i<4;i++)if((m&(1<<i))!=0)atoms.Add(spice[i].ID);
                Match(atoms,mixes[m].ID);proc(mixes[m].ID,cook,fill.ID);Match(new List<int>{fill.ID,dough},open.ID);
                proc(open.ID,knead,raw.ID);proc(raw.ID,cook,gold[m].ID);Match(new List<int>{p,gold[m].ID},plate.ID);
                foreach(var f in new[]{(Item)mixes[m],raw}){ApplianceProcessPair path;Assert(hob.TryGetValue(new ApplianceItemPair(f.ID,ApplianceReferences.HobStarting),out path)&&path.Process==cook&&!path.IsBad,"starter hob "+m);}
                foreach(var f in new[]{fill,gold[m]}){proc(f.ID,cook,ItemReferences.BurnedFood);Assert(f.DerivedProcesses.Any(w=>w.IsBad&&w.Duration>=6),"burn grace");}
                foreach(var stage in new Item[]{mixes[m],fill,open,raw,gold[m]})CheckRibbon(stage.Prefab,m);
                foreach(var cold in new Item[]{mixes[m],fill,open,raw}){int wrong;Assert(d.ItemSetView.IsValidGroup(new ItemList(p,cold.ID),out wrong)!=Satisfaction.Correct||wrong!=plate.ID,"raw plating");}
                foreach(var s in atoms.Skip(1)){var dup=new List<int>(atoms){s};int wrong;Assert(d.ItemSetView.IsValidGroup(new ItemList(dup),out wrong)!=Satisfaction.Correct||!d.Get<Item>(wrong).name.StartsWith("local.samosakitchen"),"duplicate scoop accepted");}
            }
            int paths=0;
            void Walk(int mask,List<int> held){
                for(int i=0;i<4;i++)if((mask&(1<<i))==0){var next=mask|(1<<i);var parts=new List<int>(held){spice[i].ID};
                    Match(parts,mixes[next].ID);parts.Reverse();Match(parts,mixes[next].ID);parts.Reverse();
                    paths++;Walk(next,parts);}
            }
            Walk(0,new List<int>{pot});Assert(paths==64,"order-independent prep permutations");
            var sample=new int[16]; UnityEngine.Random.InitState(63014);
            for(int i=0;i<512;i++){var order=d.ItemSetView.GetRandomConfiguration(plate.ID,side);
                Assert(order.Count==2&&order.Contains(p),"order must have one clean plate");int m=0;
                foreach(int item in order)if(item!=p)Assert(cookedToMask.TryGetValue(item,out m),"non-samosa on ticket");
                Assert(m>0,"empty order");sample[m]++;}
            Assert(sample.Skip(1).All(n=>n>0),"unreachable native order mix");
            var clone=U.Instantiate(plate.Prefab);
            try{var v=clone.GetComponent<IItemSpecificView>();Assert(v!=null,"missing ticket view");for(int a=1;a<=15;a++){
                var ticket=new CItem{ID=plate.ID,Items=new ItemList(p,gold[a].ID)};
                foreach(bool isOrder in new[]{true,false}){v.PerformUpdate(plate.ID,ticket.Items,isOrder);
                    for(int m=1;m<=15;m++)Assert(clone.transform.Find("Mix-"+m).gameObject.activeSelf==(a==m),"stale/missing ticket ribbon");
                    CheckRibbon(clone.transform.Find("Mix-"+a).gameObject,a);}
                for(int b=1;b<=15;b++){var offered=new CItem{ID=plate.ID,Items=new ItemList(gold[b].ID,p)};
                    Assert(OrderingHelpers.IsEquivalentRequest(default(EntityContext),ticket,offered)==(a==b),"wrong equivalent request "+a+"/"+b);
                    // This function consults GameData.Main. On the first build it is
                    // assigned by the caller only after the post-build event returns.
                    if(GameData.Main!=null&&GameData.Main.Has(plate.ID))Assert(OrderingHelpers.IsRequestSatisfied(ticket,offered,out _)==(a==b),"wrong service acceptance "+a+"/"+b);}
            }} finally{U.Destroy(clone);}
            CheckCounter(counter,spice);
        }
        static void CheckRibbon(GameObject root,int mask){
            for(int i=0;i<4;i++){var tag=root.transform.Find("Flavor-"+"GRYB"[i]);Assert((tag!=null)==((mask&(1<<i))!=0),"wrong glyphs for "+root.name+" mask "+mask);
                if(tag!=null){var renderer=tag.GetComponent<MeshRenderer>();Assert(tag.gameObject.activeSelf&&renderer!=null&&renderer.sharedMaterials.All(x=>x!=null&&x.shader!=null&&x.shader.isSupported),"undrawable glyph");}}
        }
        static void CheckCounter(Appliance counter,Item[] spice){
            var body=counter.Prefab.GetComponentsInChildren<BoxCollider>(true).Single();
            Assert(body.enabled&&!body.isTrigger&&body.gameObject.layer==0&&body.size.x>=.8f&&body.size.z>=.8f&&body.size.y>=.6f,"counter is not a solid vanilla-layer appliance");
            Assert(counter.IsPurchasable&&counter.SellOnlyAsDuplicate&&!counter.PreventSale,"unexpected shop availability");
            var props=counter.Properties.OfType<CItemProvider>().Single();
            Assert(props.DefaultProvidedItem==spice[0].ID&&!counter.Properties.OfType<CVariableProvider>().Any(),"incorrect station setup");
            var asm=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="SamosaKitchen");
            var sysType=asm.GetType("SamosaKitchen.CycleMasala",true);
            using(var world=new World("Isolated Samosa QA")){
                var em=world.EntityManager;var system=world.GetOrCreateSystem(sysType);
                Entity Make(int id){
                    var e=em.CreateEntity(typeof(CAppliance),typeof(CItemProvider)); em.SetComponentData(e,new CAppliance{ID=id});
                    var provider=props;provider.SetAsItem(props.DefaultProvidedItem);em.SetComponentData(e,provider);return e;
                }
                var one=Make(counter.ID);var other=Make(counter.ID);var foreign=Make(-9912345);
                var clientRoot=U.Instantiate(counter.Prefab);var hostRoot=U.Instantiate(counter.Prefab);
                try{
                    var client=clientRoot.AddComponent<GenericObjectView>();var host=hostRoot.AddComponent<GenericObjectView>();
                    var gate=sysType.GetMethod("IsPossible",Any);var act=sysType.GetMethod("Perform",Any);var press=sysType.GetMethod("ShouldAct",Any);
                    InteractionData Input(Entity target,InteractionType kind,bool held=false)=>new InteractionData{Attempt=new CAttemptingInteraction{Type=kind,Mode=InteractionMode.Items,Target=target,IsHeld=held}};
                    Assert(!(bool)gate.Invoke(system,new object[]{Input(foreign,InteractionType.Act)}),"touched another appliance");
                    Assert(!(bool)press.Invoke(system,new object[]{Input(one,InteractionType.Grab)})&&!(bool)press.Invoke(system,new object[]{Input(one,InteractionType.Act,true)}),"Grab or hold cycles station");
                    for(int step=0;step<9;step++){
                        int i=step%4;var provider=em.GetComponentData<CItemProvider>(one);
                        Assert(provider.ProvidedItem==spice[i].ID&&provider.ProvidedComponents.IsEquivalent(spice[i].ID)&&provider.HasAvailableItems(),"selected scoop mismatch");
                        var data=new ItemSourceView.ViewData{IconPrefab=provider.ProvidedItem};
                        foreach(var view in new[]{host,client}){
                            data.GetRelevantSubview(view).UpdateData(data);
                            var root=view.transform;for(int j=0;j<4;j++)Assert(root.Find("Selected-"+j).gameObject.activeSelf==(i==j),"client sign mismatch");
                            Vector3 pos=root.Find("Active bowl").localPosition,bowl=root.Find("Bowl-"+i).localPosition;
                            Assert(Mathf.Abs(pos.x-bowl.x)<.001f&&Mathf.Abs(pos.z-bowl.z)<.001f,"cursor not on selected bowl");
                        }
                        var d=Input(one,InteractionType.Act);
                        Assert((bool)gate.Invoke(system,new object[]{d})&&(bool)press.Invoke(system,new object[]{d}),"Interact unavailable");
                        act.Invoke(system,new object[]{d});Assert(em.GetComponentData<CItemProvider>(other).ProvidedItem==spice[0].ID,"selection leaked to second station");
                    }
                }finally{U.Destroy(clientRoot);U.Destroy(hostRoot);}
            }
        }
    }
}

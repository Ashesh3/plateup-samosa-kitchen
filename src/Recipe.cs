using System;
using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using UnityEngine;

namespace SamosaKitchen
{
    internal static class R
    {
        internal static Item Stock(int id)=>(Item)GDOUtils.GetExistingGDO(id);
        internal static Process Action(int id)=>(Process)GDOUtils.GetExistingGDO(id);
        internal static Item Food<T>() where T:CustomGameDataObject=>GDOUtils.GetCastedGDO<Item,T>();
        internal static List<Item.ItemProcess> Work(int id,Item result,float seconds,bool bad=false)
            =>new List<Item.ItemProcess>{new Item.ItemProcess{Process=Action(id),Result=result,Duration=seconds,IsBad=bad}};
        internal static ItemGroup.ItemSet Exact(params Item[] items)=>new ItemGroup.ItemSet{
            Items=new List<Item>(items),Min=items.Length,Max=items.Length,IsMandatory=true};
        internal static List<ItemGroupView.ColourBlindLabel> Tag(Item item,string text)
            =>new List<ItemGroupView.ColourBlindLabel>{new ItemGroupView.ColourBlindLabel{Item=item,Text=text}};
    }

    internal static partial class Flavors
    {
        internal const int Count=4, All=15;
        internal const string Codes="GRYB"; // immutable order: front left, front right, back right, back left
        internal static int Bit<T>() where T:struct,IMask => default(T).Bits;
        internal static string Code(int mask){
            var s="";for(int i=0;i<Count;i++)if((mask&(1<<i))!=0)s+=Codes[i];return s;
        }
        internal static string Stable(string legacy,int mask)=>mask==1?legacy:legacy+"_"+mask;
        internal static Item Scoop(int i){
            switch(i){case 0:return R.Food<MasalaPeas>();case 1:return R.Food<ChilliScoop>();
                case 2:return R.Food<TurmericScoop>();case 3:return R.Food<CuminScoop>();
                default:throw new ArgumentOutOfRangeException(nameof(i));}
        }
        // Guard by the actual masala appliance ID at the interaction boundary.
        // These helpers are also safe in menus prior to KitchenLib's GDO conversion.
        internal static int IndexFor(int id){
            if(id==0)return -1;
            for(int i=0;i<Count;i++){var item=Scoop(i);if(item!=null&&item.ID==id)return i;}
            return -1;
        }
        internal static void RegisterMask<T>(BaseMod mod) where T:struct,IMask{
            mod.AddGameDataObject<ColdFilling<T>>();mod.AddGameDataObject<CookedFilling<T>>();
            mod.AddGameDataObject<OpenParcel<T>>();mod.AddGameDataObject<FoldedSamosa<T>>();
            mod.AddGameDataObject<GoldenSamosa<T>>();
        }
    }
    internal abstract class FoodItem:CustomItem
    {
        protected abstract string ArtKey{get;}
        protected abstract int Mask{get;}
        public override GameObject Prefab=>Art.Template(ArtKey,Mask);
        public override GameObject SidePrefab=>Prefab;
        public override ItemStorage ItemStorageFlags=>ItemStorage.Small;
        public override ItemValue ItemValue=>ItemValue.None;
    }
    internal abstract class FoodGroup:CustomItemGroup
    {
        protected abstract string ArtKey{get;}
        protected abstract int Mask{get;}
        public override GameObject Prefab=>Art.Template(ArtKey,Mask);
        public override GameObject SidePrefab=>Prefab;
        public override ItemStorage ItemStorageFlags=>ItemStorage.Small;
        public override ItemValue ItemValue=>ItemValue.None;
    }
    internal abstract class ScoopItem:FoodItem
    {
        public override Appliance DedicatedProvider=>GDOUtils.GetCastedGDO<Appliance,MasalaCounter>();
        public override string ColourBlindTag=>Flavors.Code(Mask);
    }
    internal sealed class MasalaPeas:ScoopItem{public override string UniqueNameID=>"masala_peas";protected override string ArtKey=>"peas";protected override int Mask=>1;}
    internal sealed class ChilliScoop:ScoopItem{public override string UniqueNameID=>"masala_red_chilli";protected override string ArtKey=>"scoop_r";protected override int Mask=>2;}
    internal sealed class TurmericScoop:ScoopItem{public override string UniqueNameID=>"masala_yellow_turmeric";protected override string ArtKey=>"scoop_y";protected override int Mask=>4;}
    internal sealed class CuminScoop:ScoopItem{public override string UniqueNameID=>"masala_brown_cumin";protected override string ArtKey=>"scoop_b";protected override int Mask=>8;}

    // At prep time a group retains its component list (potato + actual scoops).
    // Exact mandatory sets ensure any order works, and a duplicate cannot match.
    // Heating locks the recipe into a stable item ID until service.
    internal sealed class ColdFilling<T>:FoodGroup where T:struct,IMask
    {
        protected override int Mask=>Flavors.Bit<T>();protected override string ArtKey=>"mix";
        public override string UniqueNameID=>Flavors.Stable("cold_filling",Mask);
        public override List<ItemGroup.ItemSet> Sets{get{
            var items=new List<Item>{R.Stock(ItemReferences.PotatoChopped)};
            for(int i=0;i<4;i++)if((Mask&(1<<i))!=0)items.Add(Flavors.Scoop(i));
            return new List<ItemGroup.ItemSet>{R.Exact(items.ToArray())};
        }}
        public override List<ItemGroupView.ColourBlindLabel> Labels=>R.Tag(R.Stock(ItemReferences.PotatoChopped),Flavors.Code(Mask));
        public override List<Item.ItemProcess> Processes=>R.Work(ProcessReferences.Cook,R.Food<CookedFilling<T>>(),2f);
    }
    internal sealed class CookedFilling<T>:FoodItem where T:struct,IMask
    {
        protected override int Mask=>Flavors.Bit<T>();protected override string ArtKey=>"filling";
        public override string UniqueNameID=>Flavors.Stable("cooked_filling",Mask);
        public override string ColourBlindTag=>"Fi "+Flavors.Code(Mask);
        public override List<Item.ItemProcess> Processes=>R.Work(ProcessReferences.Cook,R.Stock(ItemReferences.BurnedFood),9f,true);
    }
    internal sealed class OpenParcel<T>:FoodGroup where T:struct,IMask
    {
        protected override int Mask=>Flavors.Bit<T>();protected override string ArtKey=>"open";
        public override string UniqueNameID=>Flavors.Stable("open_parcel",Mask);
        public override List<ItemGroup.ItemSet> Sets=>new List<ItemGroup.ItemSet>{R.Exact(R.Stock(ItemReferences.Dough),R.Food<CookedFilling<T>>())};
        public override List<ItemGroupView.ColourBlindLabel> Labels=>R.Tag(R.Food<CookedFilling<T>>(),"Open "+Flavors.Code(Mask));
        public override List<Item.ItemProcess> Processes=>R.Work(ProcessReferences.Knead,R.Food<FoldedSamosa<T>>(),1.2f);
    }
    internal sealed class FoldedSamosa<T>:FoodItem where T:struct,IMask
    {
        protected override int Mask=>Flavors.Bit<T>();protected override string ArtKey=>"raw";
        public override string UniqueNameID=>Flavors.Stable("folded_samosa",Mask);
        public override string ColourBlindTag=>"Raw "+Flavors.Code(Mask);
        public override List<Item.ItemProcess> Processes=>R.Work(ProcessReferences.Cook,R.Food<GoldenSamosa<T>>(),3f);
    }
    internal sealed class GoldenSamosa<T>:FoodItem where T:struct,IMask
    {
        protected override int Mask=>Flavors.Bit<T>();protected override string ArtKey=>"golden";
        public override string UniqueNameID=>Flavors.Stable("golden_samosa",Mask);
        public override string ColourBlindTag=>Flavors.Code(Mask);
        public override ItemStorage ItemStorageFlags=>ItemStorage.StackableFood;
        public override List<Item.ItemProcess> Processes=>R.Work(ProcessReferences.Cook,R.Stock(ItemReferences.BurnedFood),8f,true);
    }
    internal sealed class PlatedSamosa:FoodGroup
    {
        public override string UniqueNameID=>"plated_samosa";
        protected override string ArtKey=>"plated";protected override int Mask=>0;
        public override ItemValue ItemValue=>ItemValue.Large;
        public override ItemStorage ItemStorageFlags=>ItemStorage.None;
        public override bool CanContainSide=>true;
        public override Item DisposesTo=>R.Stock(ItemReferences.Plate);
        public override Item DirtiesTo=>R.Stock(ItemReferences.PlateDirty);
        public override List<ItemGroup.ItemSet> Sets{get{
            var fillings=new List<Item>();for(int m=1;m<=Flavors.All;m++)fillings.Add(Flavors.Golden(m));
            return new List<ItemGroup.ItemSet>{R.Exact(R.Stock(ItemReferences.Plate)),
                new ItemGroup.ItemSet{Items=fillings,Min=1,Max=1,IsMandatory=true}};
        }}
        public override List<ItemGroupView.ColourBlindLabel> Labels{get{
            var labels=new List<ItemGroupView.ColourBlindLabel>();for(int m=1;m<=15;m++)labels.Add(new ItemGroupView.ColourBlindLabel{Item=Flavors.Golden(m),Text=Flavors.Code(m)});return labels;
        }}
        public override void OnRegister(ItemGroup gdo)
        {
            base.OnRegister(gdo);
            // Both the native ticket and held plate call IItemSpecificView.PerformUpdate
            // with their own components. No custom network payload or client RNG.
            var groups=new List<ItemGroupView.ComponentGroup>();
            for(int m=1;m<=15;m++)groups.Add(new ItemGroupView.ComponentGroup{
                Item=Flavors.Golden(m),GameObject=gdo.Prefab.transform.Find("Mix-"+m).gameObject});
            gdo.Prefab.GetComponent<ItemGroupView>().ComponentGroups=groups;
        }
    }
    public sealed class MasalaCounter:CustomAppliance
    {
        public override string UniqueNameID=>"masala_counter";
        public override GameObject Prefab=>Art.Template("counter");
        public override bool IsPurchasable=>true;
        public override bool SellOnlyAsDuplicate=>true;
        public override PriceTier PriceTier=>PriceTier.Cheap;
        public override ShoppingTags ShoppingTags=>ShoppingTags.Cooking;
        public override List<IApplianceProperty> Properties=>new List<IApplianceProperty>{CItemProvider.EditorCreateProvider(Flavors.Scoop(0))};
        public override List<(Locale,ApplianceInfo)> InfoList=>new List<(Locale,ApplianceInfo)>{
            (Locale.English,LocalisationBuilder.NewBuilder<ApplianceInfo>().SetName("Masala Counter")
                .SetDescription("Interact cycles green, red, yellow, brown. Grab one scoop from the framed bowl: coriander-pea, chilli, turmeric, or cumin.")
                .SetSections(new List<Appliance.Section>()).SetTags(new List<string>()))};
    }
    public sealed class SamosaDish:CustomDish
    {
        public const string Instructions="Chop a potato. Interact cycles masalas; Grab takes one. Add each ticket symbol once, then cook. Knead flour, add the filling, knead to fold, fry and plate. Mix before heating!";
        public override string UniqueNameID=>"samosa_main";
        public override DishType Type=>DishType.Base;
        public override bool IsAvailableAsLobbyOption=>true;
        public override bool IsUnlockable=>true;
        public override UnlockGroup UnlockGroup=>UnlockGroup.Dish;
        public override CardType CardType=>CardType.Default;
        public override int Difficulty=>5;
        public override DishCustomerChange CustomerMultiplier=>DishCustomerChange.SmallDecrease;
        public override Unlock.RewardLevel ExpReward=>Unlock.RewardLevel.Medium;
        public override GameObject IconPrefab=>Art.Template("card");
        public override GameObject DisplayPrefab=>Art.Template("card");
        public override List<(Locale,UnlockInfo)> InfoList=>new List<(Locale,UnlockInfo)>{
            (Locale.English,LocalisationBuilder.NewBuilder<UnlockInfo>().SetName("Samosa")
                .SetDescription("Fill crisp Indian pastries with potatoes and the customer's exact blend of masalas.")
                .SetFlavourText("Four spice bowls, fifteen ways to fold."))};
        public override Dictionary<Locale,string> Recipe=>new Dictionary<Locale,string>{{Locale.English,Instructions}};
        public override HashSet<Item> MinimumIngredients=>new HashSet<Item>{R.Stock(ItemReferences.Flour),R.Stock(ItemReferences.Potato),Flavors.Scoop(0),R.Stock(ItemReferences.Plate)};
        public override HashSet<Process> RequiredProcesses=>new HashSet<Process>{R.Action(ProcessReferences.Chop),R.Action(ProcessReferences.Knead),R.Action(ProcessReferences.Cook)};
        public override List<Dish.MenuItem> ResultingMenuItems=>new List<Dish.MenuItem>{
            new Dish.MenuItem{Item=R.Food<PlatedSamosa>(),Phase=MenuPhase.Main,DynamicMenuType=DynamicMenuType.Static,Weight=1}};
        public override List<string> StartingNameSet=>new List<string>{"The Samosa Fold","Chai Lane Kitchen","The Golden Corner","Aloo & Behold"};
    }
}

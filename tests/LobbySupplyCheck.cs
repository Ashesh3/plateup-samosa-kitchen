// Standalone regression: exercise the *compiled* dish without running an
// extra game session. Stock Item objects are lightweight stand-ins for Unity
// assets; the dish getter, KitchenLib lookups and item ID equality are real.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;

internal static class LobbySupplyCheck
{
    const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    static int Run(string[] args)
    {
        string game=args[0], modPath=Path.GetFullPath(args[1]);
        var dirs=new List<string>{Path.Combine(game,"PlateUp_Data","Managed"),Path.GetDirectoryName(modPath)};
        foreach(var f in Directory.GetFiles(Path.Combine(game,"Mods"),"*.dll",SearchOption.AllDirectories)){
            var dir=Path.GetDirectoryName(f); if(!dirs.Contains(dir))dirs.Add(dir);
        }
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object src, ResolveEventArgs evt){
            var name=new AssemblyName(evt.Name).Name+".dll";
            foreach(var dir in dirs){var path=Path.Combine(dir,name);if(File.Exists(path))return Assembly.LoadFrom(path);}
            return null;
        };
        var kl=Assembly.LoadFrom(Path.Combine(game,"Mods","2898069883","content","KitchenLib-Workshop.dll"));
        var gd=Assembly.LoadFrom(Path.Combine(dirs[0],"Kitchen.GameData.dll"));
        var gdObj=gd.GetType("KitchenData.GameDataObject",true); var item=gd.GetType("KitchenData.Item",true);
        var id=gdObj.GetField("ID",Fields);
        var mod=Assembly.LoadFrom(modPath);
        var definition=mod.GetType("SamosaKitchen.SamosaDish",true);
        var peaType=mod.GetType("SamosaKitchen.MasalaPeas",true);
        var vanilla=(IDictionary)kl.GetType("KitchenLib.Utils.GDOUtils",true).GetField("GDOs",Fields).GetValue(null);
        var custom=(IDictionary)kl.GetType("KitchenLib.Customs.CustomGDO",true).GetField("GDOsByType",Fields).GetValue(null);
        var refs=kl.GetType("KitchenLib.References.ItemReferences",true);
        var expected=new HashSet<int>();
        foreach(string name in new[]{"Plate","Flour","Potato"}){
            int n=(int)refs.GetProperty(name,Fields).GetValue(null,null);
            var stock=FormatterServices.GetUninitializedObject(item);
            id.SetValue(stock,n);vanilla.Add(n,stock);expected.Add(n);
        }
        var spice=FormatterServices.GetUninitializedObject(item);id.SetValue(spice,1234567);expected.Add(1234567);
        var spiceDef=FormatterServices.GetUninitializedObject(peaType);
        kl.GetType("KitchenLib.Customs.CustomGameDataObject",true).GetField("GameDataObject",Fields).SetValue(spiceDef,spice);
        custom.Add(peaType,spiceDef);
        var dish=FormatterServices.GetUninitializedObject(definition);
        int difficulty=(int)definition.GetProperty("Difficulty").GetValue(dish,null);
        if(difficulty!=5){Console.WriteLine("FAIL: compiled Samosa difficulty is "+difficulty+"; expected 5/5.");return 1;}
        Console.WriteLine("PASS: compiled Samosa difficulty is 5/5.");
        var ingredients=(IEnumerable)definition.GetProperty("MinimumIngredients").GetValue(dish,null);
        var actual=new HashSet<int>();int count=0;
        foreach(object ingredient in ingredients){count++;actual.Add((int)id.GetValue(ingredient));}
        int plate=(int)refs.GetProperty("Plate",Fields).GetValue(null,null);
        if(!actual.Contains(plate)){Console.WriteLine("FAIL: vanilla plate is missing from Samosa.MinimumIngredients (only "+count+" entries).");return 1;}
        if(count!=4||!expected.SetEquals(actual)){Console.WriteLine("FAIL: incomplete or unexpected starting supplies.");return 1;}
        Console.WriteLine("PASS: compiled dish requests the clean plate, flour, potato and masala peas exactly once.");return 0;
    }
    static int Main(string[] args){try{return Run(args);}catch(Exception e){Console.Error.WriteLine(e);return 2;}}
}

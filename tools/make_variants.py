"""Emit explicit closed generic instantiations; never depend on assembly scan order."""
from pathlib import Path

def generate():
    lines=["// Generated with tools/make_variants.py. Bit order: green, red, yellow, brown.",
           "using KitchenLib;", "using KitchenData;", "namespace SamosaKitchen", "{",
           "    internal interface IMask { int Bits {get;} }"]
    lines += [f"    internal struct M{i:02} : IMask {{ public int Bits => {i}; }}" for i in range(1,16)]
    lines += ["    internal static partial class Flavors", "    {",
              "        internal static void Register(BaseMod mod)", "        {"]
    lines += [f"            RegisterMask<M{i:02}>(mod);" for i in range(1,16)]
    lines += ["        }","        internal static Item Golden(int mask)", "        {","            switch(mask)","            {"]
    lines += [f"                case {i}: return R.Food<GoldenSamosa<M{i:02}>>();" for i in range(1,16)]
    lines += ["                default: throw new System.ArgumentOutOfRangeException(nameof(mask));",
              "            }","        }","    }","}"]
    return "\n".join(lines)+"\n"
if __name__=="__main__":
    (Path(__file__).resolve().parents[1]/"src"/"FlavorVariants.g.cs").write_text(generate(),encoding="utf-8")

// Generated with tools/make_variants.py. Bit order: green, red, yellow, brown.
using KitchenLib;
using KitchenData;
namespace SamosaKitchen
{
    internal interface IMask { int Bits {get;} }
    internal struct M01 : IMask { public int Bits => 1; }
    internal struct M02 : IMask { public int Bits => 2; }
    internal struct M03 : IMask { public int Bits => 3; }
    internal struct M04 : IMask { public int Bits => 4; }
    internal struct M05 : IMask { public int Bits => 5; }
    internal struct M06 : IMask { public int Bits => 6; }
    internal struct M07 : IMask { public int Bits => 7; }
    internal struct M08 : IMask { public int Bits => 8; }
    internal struct M09 : IMask { public int Bits => 9; }
    internal struct M10 : IMask { public int Bits => 10; }
    internal struct M11 : IMask { public int Bits => 11; }
    internal struct M12 : IMask { public int Bits => 12; }
    internal struct M13 : IMask { public int Bits => 13; }
    internal struct M14 : IMask { public int Bits => 14; }
    internal struct M15 : IMask { public int Bits => 15; }
    internal static partial class Flavors
    {
        internal static void Register(BaseMod mod)
        {
            RegisterMask<M01>(mod);
            RegisterMask<M02>(mod);
            RegisterMask<M03>(mod);
            RegisterMask<M04>(mod);
            RegisterMask<M05>(mod);
            RegisterMask<M06>(mod);
            RegisterMask<M07>(mod);
            RegisterMask<M08>(mod);
            RegisterMask<M09>(mod);
            RegisterMask<M10>(mod);
            RegisterMask<M11>(mod);
            RegisterMask<M12>(mod);
            RegisterMask<M13>(mod);
            RegisterMask<M14>(mod);
            RegisterMask<M15>(mod);
        }
        internal static Item Golden(int mask)
        {
            switch(mask)
            {
                case 1: return R.Food<GoldenSamosa<M01>>();
                case 2: return R.Food<GoldenSamosa<M02>>();
                case 3: return R.Food<GoldenSamosa<M03>>();
                case 4: return R.Food<GoldenSamosa<M04>>();
                case 5: return R.Food<GoldenSamosa<M05>>();
                case 6: return R.Food<GoldenSamosa<M06>>();
                case 7: return R.Food<GoldenSamosa<M07>>();
                case 8: return R.Food<GoldenSamosa<M08>>();
                case 9: return R.Food<GoldenSamosa<M09>>();
                case 10: return R.Food<GoldenSamosa<M10>>();
                case 11: return R.Food<GoldenSamosa<M11>>();
                case 12: return R.Food<GoldenSamosa<M12>>();
                case 13: return R.Food<GoldenSamosa<M13>>();
                case 14: return R.Food<GoldenSamosa<M14>>();
                case 15: return R.Food<GoldenSamosa<M15>>();
                default: throw new System.ArgumentOutOfRangeException(nameof(mask));
            }
        }
    }
}

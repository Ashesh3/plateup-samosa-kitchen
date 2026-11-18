using System.Reflection;
using KitchenLib;
using KitchenMods;

[assembly: AssemblyTitle("Samosa Kitchen")]
[assembly: AssemblyDescription("Aloo matar samosas as a starting main for PlateUp!")]
[assembly: AssemblyVersion("1.1.2.0")]
[assembly: AssemblyInformationalVersion("1.1.2")]

namespace SamosaKitchen
{
    public sealed class Mod : BaseMod
    {
        public const string Guid = "local.samosakitchen";
        public Mod() : base(Guid, "Samosa Kitchen", "Local Kitchen", "1.1.2", ">=1.5.3", typeof(Mod).Assembly) { }
        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // Same dish, provider, plate and legacy green IDs. No global gameplay patches.
            AddGameDataObject<MasalaCounter>();
            AddGameDataObject<MasalaPeas>();
            AddGameDataObject<ChilliScoop>(); AddGameDataObject<TurmericScoop>(); AddGameDataObject<CuminScoop>();
            Flavors.Register(this);
            AddGameDataObject<PlatedSamosa>();
            AddGameDataObject<SamosaDish>();
        }
    }
}

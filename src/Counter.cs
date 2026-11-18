using Kitchen;
using KitchenLib.Utils;
using KitchenMods;
using Unity.Entities;
using UnityEngine;

namespace SamosaKitchen
{
    // Same Act/press semantics as SwitchVariableProvider (ice cream), but only on
    // this appliance. Vanilla's variable provider has exactly three slots, so do
    // not attach it or let two systems consume the same button press.
    [UpdateBefore(typeof(ItemTransferGroup))]
    public sealed class CycleMasala : ItemInteractionSystem, IModSystem
    {
        protected override bool IsPossible(ref InteractionData d)
        {
            var counter=GDOUtils.GetCustomGameDataObject<MasalaCounter>();
            return counter!=null&&Require(d.Target,out CAppliance a)&&a.ID==counter.ID&&Require(d.Target,out CItemProvider p)
                &&Flavors.IndexFor(p.ProvidedItem)>=0;
        }
        protected override void Perform(ref InteractionData d)
        {
            // Read at commit time, so two cooks on two counters never share state.
            if(!Require(d.Target,out CItemProvider p)||!Advance(ref p))return;
            SetComponent(d.Target,p);
        }
        internal static bool Advance(ref CItemProvider p)
        {
            var i=Flavors.IndexFor(p.ProvidedItem);if(i<0)return false;
            p.SetAsItem(Flavors.Scoop((i+1)%Flavors.Count).ID);return true;
        }
    }

    // Retail ItemSourceView.ViewData already carries the true ProvidedItem ID to
    // host AND remote clients. No keyboard polling, static selected index, or
    // per-frame lookup; each view owns its visual state.
    public sealed class MasalaSelectionView : ItemSourceView
    {
        public Transform Cursor;
        public Transform[] SelectedCards;
        private int displayed=-1;
        protected override void UpdateData(ItemSourceView.ViewData data)
        {
            int i=Flavors.IndexFor(data.IconPrefab);
            if(i<0 || i==displayed || Cursor==null || SelectedCards==null || SelectedCards.Length!=4)return;
            Cursor.localPosition=Art.BowlPosition(i,.608f);
            for(int j=0;j<4;j++)SelectedCards[j].gameObject.SetActive(j==i);
            displayed=i;
        }
    }
}

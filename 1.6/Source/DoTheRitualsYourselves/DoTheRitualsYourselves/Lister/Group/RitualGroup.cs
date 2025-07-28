using DoTheRitualsYourselves.Lister.GroupLister;
using DoTheRitualsYourselves.Lister.Rituals;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Lister.Group
{
    public abstract class RitualGroup
    {
        private RitualGroupLister lister;

        public RitualGroup(RitualGroupLister lister)
        {
            this.lister = lister;
        }

        public abstract int Priority { get; }
        public abstract string Label { get; }
        public abstract Texture2D Icon { get; }
        public abstract Color Color { get; }

        public abstract IEnumerable<Ritual> GetVisibleRituals();
        public abstract IEnumerable<Ritual> GetAllRituals();
        public virtual bool ValidateSpot(TargetInfo t, Precept_Ritual ritual)
        {
            if (!(t.Thing is Building) || t.Thing.Faction != Faction.OfPlayer)
                return false;

            if (!ritual.activeObligations.NullOrEmpty())
                foreach (RitualObligation activeObligation in ritual.activeObligations)
                    if (ritual.CanUseTarget(t.Thing, activeObligation).canUse)
                        return true;
            try
            {
                return ritual.CanUseTarget(t, null).canUse;
            }
            catch
            {
                return false;
            }
        }
        public IEnumerable<ThingDef> GetBuildingDefs() => lister.GetBuildingDefs();
    }
}

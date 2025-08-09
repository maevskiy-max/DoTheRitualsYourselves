using DoTheRitualsYourselves.Lister.GroupLister;
using DoTheRitualsYourselves.Lister.Rituals;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Lister.Group
{
    public class IdeoRitualGroup : RitualGroup
    {
        private int priority;
        private Ideo ideo;

        public IdeoRitualGroup(RitualGroupLister lister, int priority, Ideo ideo) : base(lister)
        {
            this.priority = priority;
            this.ideo = ideo;
        }

        public override int Priority => priority;
        public override string Label => ideo.name;
        public override Texture2D Icon => ideo.Icon;
        public override Color Color => ideo.Color;
        public Ideo Ideo => ideo;

        public override IEnumerable<Ritual> GetVisibleRituals()
        {
            List<Precept_Ritual> rituals = ideo.PreceptsListForReading.OfType<Precept_Ritual>().ToList();
            foreach (Precept_Ritual ritual in rituals)
            {
                if (ritual.def.visible)
                    yield return new Ritual(this, ritual);
                else if (ritual.def.defName == "Conversion")
                    yield return new Ritual(this, ritual, false);
            }
        }

        public override IEnumerable<Ritual> GetAllRituals()
        {
            List<Precept_Ritual> rituals = ideo.PreceptsListForReading.OfType<Precept_Ritual>().ToList();
            foreach (Precept_Ritual ritual in rituals)
            {
                if (ritual.def.visible)
                    yield return new Ritual(this, ritual);
                else if (ritual.def.defName == "Conversion")
                    yield return new Ritual(this, ritual, false);
                else if (ritual.def.defName == "FuneralNoCorpse")
                {
                    Precept_Ritual funeral = rituals.Find(ritual2 => ritual2.def.defName == "Funeral");
                    if (funeral != null)
                        yield return new ManualIdRitual(this, ritual, funeral.Id);
                }
            }
        }

        public override bool ValidateSpot(TargetInfo t, Precept_Ritual ritual)
        {
            if (t.Thing == null)
                return false;
            if (!GetBuildingDefs().Contains(t.Thing.def))
                return false;

            if (ritual.def.defName == "Funeral")
            {
                List<Precept_Ritual> rituals = ideo.PreceptsListForReading.OfType<Precept_Ritual>().ToList();
                Precept_Ritual funeralNoCorpse = rituals.Find(ritual2 => ritual2.def.defName == "FuneralNoCorpse");
                return base.ValidateSpot(t, ritual) || funeralNoCorpse != null && base.ValidateSpot(t, funeralNoCorpse);
            }
            else
                return base.ValidateSpot(t, ritual);
        }
    }
}

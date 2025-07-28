using DoTheRitualsYourselves.Lister.GroupLister;
using DoTheRitualsYourselves.Lister.Rituals;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Lister.Group
{
    public class ColonyRitualGroup : RitualGroup
    {
        public ColonyRitualGroup(RitualGroupLister lister) : base(lister)
        {
        }

        public override int Priority => 0;
        public override string Label => Faction.OfPlayer.Name;
        public override Texture2D Icon => ContentFinder<Texture2D>.Get("World/WorldObjects/Expanding/Town");
        public override Color Color => Color.cyan;

        public override IEnumerable<Ritual> GetVisibleRituals()
        {
            Ideo ideo = Faction.OfPlayer.ideos.PrimaryIdeo;
            List<Precept_Ritual> rituals = ideo.PreceptsListForReading.OfType<Precept_Ritual>().ToList();

            foreach (Precept_Ritual precept in rituals)
            {
                if (ModsConfig.IsActive("ludeon.rimworld.royalty"))
                {
                    //if (precept.def.defName == "ThroneSpeech")
                    //    yield return new ManualIdRitual(this, precept, -1001);
                    if (precept.def.defName == "AnimaTreeLinking")
                        yield return new ManualIdRitual(this, precept, -1002, false);
                }
                if (ModsConfig.IsActive("ludeon.rimworld.ideology"))
                {
                    if (precept.def.defName == "LeaderSpeech")
                        yield return new ManualIdRitual(this, precept, -2001, false);
                }
            }
        }

        public override IEnumerable<Ritual> GetAllRituals()
        {
            return GetVisibleRituals();
        }

        public override bool ValidateSpot(TargetInfo t, Precept_Ritual ritual)
        {
            if (t.Thing == null)
                return false;
            if (!GetBuildingDefs().Contains(t.Thing.def))
                return false;
            return base.ValidateSpot(t, ritual);
        }
    }
}

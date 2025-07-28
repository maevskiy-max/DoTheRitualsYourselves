using DoTheRitualsYourselves.Lister.Group;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace DoTheRitualsYourselves.Lister.GroupLister
{
    public class ColonyRitualGroupLister : RitualGroupLister
    {
        public override bool IsSkipped => false;
        private List<ThingDef> buildingDefs = new List<ThingDef>();

        public ColonyRitualGroupLister()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
                if (IsColonyRitualBuilding(def))
                    buildingDefs.Add(def);
        }

        private bool IsColonyRitualBuilding(ThingDef def)
        {
            if (def?.building?.buildingTags?.Contains("RitualFocus") ?? false)
                return true;
            if (def.HasComp<CompGatherSpot>())
                return true;
            if (def.HasComp<CompPsylinkable>())
                return true;
            if (def.thingClass.IsSubclassOf(typeof(Building_Throne)))
                return true;
            return false;
        }

        public override IEnumerable<RitualGroup> GetGroups()
        {
            yield return new ColonyRitualGroup(this);
        }

        public override IEnumerable<ThingDef> GetBuildingDefs()
        {
            return buildingDefs;
        }
    }
}

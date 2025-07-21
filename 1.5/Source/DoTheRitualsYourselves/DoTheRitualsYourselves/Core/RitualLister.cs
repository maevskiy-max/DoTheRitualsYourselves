using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DoTheRitualsYourselves.Core
{
    public static class RitualLister
    {
        private static List<ThingDef> ritualBuildingDefs;

        public static void MakeRitualCache()
        {
            ritualBuildingDefs = new List<ThingDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
                if (def.IsRitualBuilding())
                    ritualBuildingDefs.Add(def);
        }

        public static bool IsRitualBuilding(this ThingDef def)
        {
            if (def?.building?.buildingTags?.Contains("RitualFocus") ?? false)
                return true;
            if (def.HasComp<CompGatherSpot>())
                return true;
            if (def.HasComp<CompLightball>())
                return true;
            return false;
        }

        public static IEnumerable<Thing> GetRitualBuildings(this Map map)
        {
            var lister = map.listerThings;
            foreach (ThingDef def in ritualBuildingDefs)
                foreach (Thing thing in lister.ThingsOfDef(def))
                    if (thing.Faction?.IsPlayer ?? false)
                        yield return thing;
        }

        public static IEnumerable<Precept_Ritual> GetRituals(this Ideo ideo)
        {
            foreach (Precept_Ritual ritual in ideo.PreceptsListForReading.OfType<Precept_Ritual>())
                if (ritual.def.visible)
                    yield return ritual;
        }
    }
}

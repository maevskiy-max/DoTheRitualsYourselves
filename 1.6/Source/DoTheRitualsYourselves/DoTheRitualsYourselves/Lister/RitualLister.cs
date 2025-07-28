using DoTheRitualsYourselves.Lister.Group;
using DoTheRitualsYourselves.Lister.GroupLister;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace DoTheRitualsYourselves
{
    [StaticConstructorOnStartup]
    public static class RitualLister
    {
        private static List<RitualGroupLister> groupListers = new List<RitualGroupLister>();

        static RitualLister()
        {
            foreach (Type type in typeof(RitualGroupLister).AllSubclasses())
                groupListers.Add((RitualGroupLister)Activator.CreateInstance(type));
        }

        public static List<RitualGroup> GetRitualGroups()
        {
            List<RitualGroup> result = new List<RitualGroup>();
            foreach (RitualGroupLister lister in groupListers)
                if (!lister.IsSkipped)
                    result.AddRange(lister.GetGroups());
            result.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return result;
        }

        public static IEnumerable<Thing> GetRitualBuildings(this Map map, List<ThingDef> defs)
        {
            foreach (ThingDef def in defs)
                foreach (Thing thing in map.listerThings.ThingsOfDef(def))
                    yield return thing;
        }
    }
}

using DoTheRitualsYourselves.Lister.Group;
using System.Collections.Generic;
using Verse;

namespace DoTheRitualsYourselves.Lister.GroupLister
{
    public abstract class RitualGroupLister
    {
        public abstract bool IsSkipped { get; }
        public abstract IEnumerable<RitualGroup> GetGroups();
        public abstract IEnumerable<ThingDef> GetBuildingDefs();
    }
}

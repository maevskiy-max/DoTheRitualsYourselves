using DoTheRitualsYourselves.Lister.Group;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Lister.Rituals
{
    public class Ritual
    {
        private RitualGroup group;
        protected Precept_Ritual ritual;

        public Ritual(RitualGroup group, Precept_Ritual ritual)
        {
            this.group = group;
            this.ritual = ritual;
        }

        public virtual string Label => ritual.LabelCap;
        public virtual string Type => ritual.ShortDescOverrideCap.NullOrEmpty() ? ritual.def.label : ritual.ShortDescOverrideCap;
        public virtual Texture2D Icon => ritual.Icon;
        public virtual int Id => ritual.Id;
        public Precept_Ritual Precept => ritual;

        public IEnumerable<ThingDef> GetBuildingDefs() => group.GetBuildingDefs();
        public RitualGroup Group => group;
    }
}

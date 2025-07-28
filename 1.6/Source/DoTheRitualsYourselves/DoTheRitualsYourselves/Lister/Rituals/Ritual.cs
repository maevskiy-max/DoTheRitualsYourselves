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
        private bool showType;
        protected Precept_Ritual ritual;

        public Ritual(RitualGroup group, Precept_Ritual ritual, bool showType = true)
        {
            this.group = group;
            this.ritual = ritual;
            this.showType = showType;
        }

        public virtual string Label => showType ? $"{ritual.LabelCap} ({(ritual.ShortDescOverrideCap.NullOrEmpty() ? ritual.def.label : ritual.ShortDescOverrideCap)})" : ritual.LabelCap;
        public virtual Texture2D Icon => ritual.Icon;
        public virtual int Id => ritual.Id;
        public Precept_Ritual Precept => ritual;

        public IEnumerable<ThingDef> GetBuildingDefs() => group.GetBuildingDefs();
        public RitualGroup Group => group;
    }
}

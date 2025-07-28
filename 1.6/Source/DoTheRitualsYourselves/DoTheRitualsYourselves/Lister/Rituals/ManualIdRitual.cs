using DoTheRitualsYourselves.Lister.Group;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Lister.Rituals
{
    public class ManualIdRitual : Ritual
    {
        private int id;

        public ManualIdRitual(RitualGroup group, Precept_Ritual ritual, int id, bool showType = true) : base(group, ritual, showType)
        {
            this.id = id;
        }

        public override int Id => id;
    }
}

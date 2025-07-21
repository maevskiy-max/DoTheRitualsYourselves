using Verse;

namespace DoTheRitualsYourselves.RitualPolicies
{
    public class RitualPolicy_Editable : RitualPolicy
    {

        public RitualPolicy_Editable() : base() 
        {
        }

        public RitualPolicy_Editable(string label) : base()
        {
            this.label = label;
        }

        public RitualPolicy_Editable(
            string label,
            int minPawnCount,
            int maxNonRoleCount,
            FloatRange avgMood,
            IntRange time,
            bool invertTime,
            FloatRange pawnHealth,
            FloatRange pawnMood,
            bool exceptResting,
            bool allowColonist,
            bool allowSlave,
            bool allowOtherIdeo)
            : base(minPawnCount, maxNonRoleCount, avgMood, time, invertTime, pawnHealth, pawnMood, exceptResting, allowColonist, allowSlave, allowOtherIdeo)
        {
            this.label = label;
        }

        public override bool IsConst => false;

        public override string Label => label;
    }
}

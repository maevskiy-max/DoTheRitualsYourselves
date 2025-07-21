using Verse;

namespace DoTheRitualsYourselves.RitualPolicies
{
    public class RitualPolicy_Always : RitualPolicy
    {
        public RitualPolicy_Always() : base()
        {
        }

        public override bool IsConst => true;

        public override string Label => "DoTheRitualsYourselves.Policy.Always".Translate();
    }
}

using DoTheRitualsYourselves.RitualPolicies;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DoTheRitualsYourselves.WorldComponents
{
    public class WorldComponent_RitualPolicy : WorldComponent
    {
        private Dictionary<int, RitualPolicy> policies = new Dictionary<int, RitualPolicy>()
        {
            { 0, new RitualPolicy_Always() },
            { 1, new RitualPolicy_Editable(
                label: "DoTheRitualsYourselves.Policy.DaytimeRitual".Translate(),
                minPawnCount: 1,
                maxNonRoleCount: 50,
                avgMood: new FloatRange(0f, 1f),
                time: new IntRange(6, 22),
                invertTime: false,
                pawnHealth: new FloatRange(0f, 1f),
                pawnMood: new FloatRange(0f, 1f),
                exceptResting: false,
                allowColonist: true,
                allowSlave: true,
                allowOtherIdeo: true) },
            { 2, new RitualPolicy_Editable(
                label: "DoTheRitualsYourselves.Policy.NightRitual".Translate(),
                minPawnCount: 1,
                maxNonRoleCount: 50,
                avgMood: new FloatRange(0f, 1f),
                time: new IntRange(6, 22),
                invertTime: true,
                pawnHealth: new FloatRange(0f, 1f),
                pawnMood: new FloatRange(0f, 1f),
                exceptResting: false,
                allowColonist: true,
                allowSlave: true,
                allowOtherIdeo: true) }
        };
        private int nextID = 3;

        public static WorldComponent_RitualPolicy Instance => Find.World.GetComponent<WorldComponent_RitualPolicy>();

        public WorldComponent_RitualPolicy(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref policies, "DoTheRitualsYourselves.RitualPolicies", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref nextID, "DoTheRitualsYourselves.NextID");
        }

        public void InsertPolicy(RitualPolicy policy)
        {
            policies.Add(nextID++, policy);
        }

        public void RemovePolicy(RitualPolicy policy)
        {
            int key = -1;
            foreach (var k in policies.Keys)
            {
                if (policies[k] == policy)
                {
                    key = k;
                    break;
                }    
            }

            if (key != -1)
                policies.Remove(key);
        }

        public IEnumerable<int> AllPolicyIds
        {
            get
            {
                List<int> keys = policies.Keys.ToList();
                keys.Sort();
                foreach (int key in keys)
                    yield return key;
            }
        }

        public RitualPolicy GetPolicy(int policyID)
        {
            if (!policies.ContainsKey(policyID))
                return null;
            return policies[policyID];
        }
    }
}

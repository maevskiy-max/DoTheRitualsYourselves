using DoTheRitualsYourselves.Core;
using DoTheRitualsYourselves.Extra;
using DoTheRitualsYourselves.RitualPolicies;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.WorldComponents
{
    public class WorldComponent_AutoRituals : WorldComponent
    {
        private Dictionary<int, RitualExtraData> extraDatas = new Dictionary<int, RitualExtraData>();
        private int nextRitualTick = 0;

        public static WorldComponent_AutoRituals Instance => Find.World.GetComponent<WorldComponent_AutoRituals>();

        public WorldComponent_AutoRituals(World world) : base(world) { }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref extraDatas, "DoTheRitualsYourselves.ExtraDatas", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref nextRitualTick, "DoTheRitualsYourselves.NextRitualTick", 0);
        }

        public RitualExtraData GetRitualExtraData(int ritualID)
        {
            if (!extraDatas.ContainsKey(ritualID))
                extraDatas.Add(ritualID, new RitualExtraData());
            return extraDatas[ritualID];
        }

        public RitualPolicy GetRitualPolicy(int ritualID)
        {
            if (!extraDatas.ContainsKey(ritualID))
                extraDatas.Add(ritualID, new RitualExtraData());
            int policyID = extraDatas[ritualID].policyID;

            RitualPolicy policy = WorldComponent_RitualPolicy.Instance.GetPolicy(policyID);
            if (policy == null)
            {
                extraDatas[ritualID].policyID = 0;
                return WorldComponent_RitualPolicy.Instance.GetPolicy(0);
            }
            return policy;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 10 != 0)
                return;

            if (nextRitualTick <= 0)
            {
                var rituals = new List<Precept_Ritual>();

                foreach (Ideo ideo in Faction.OfPlayer.ideos.AllIdeos)
                    foreach (Precept_Ritual ritual in ideo.GetRituals())
                        rituals.Add(ritual);
                rituals.Shuffle();

                foreach (Precept_Ritual ritual in rituals)
                {
                    int id = ritual.Id;
                    if (!extraDatas.ContainsKey(id))
                        extraDatas.Add(id, new RitualExtraData());
                    RitualExtraData ex = extraDatas[id];

                    ex.nextCheckTick -= 10;
                    if (ex.nextCheckTick <= 0)
                    {
                        if (ritual.TryStart())
                        {
                            ex.nextCheckTick = Random.Range(60000, 90000);
                            nextRitualTick = Random.Range(30000, 60000);
                        }
                        else
                            ex.nextCheckTick = Random.Range(2500, 7500);
                    }
                }
            }
            else
            {
                nextRitualTick -= 10;
                if (nextRitualTick < 0)
                    nextRitualTick = 0;
            }
        }
    }
}

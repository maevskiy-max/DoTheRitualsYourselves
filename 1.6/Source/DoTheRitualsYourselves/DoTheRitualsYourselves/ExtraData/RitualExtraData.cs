using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Extra
{
    public class RitualExtraData : IExposable
    {
        public bool autoStart;
        public int policyID;
        public int nextCheckTick;
        public Thing ritualSpot = null;
        public Dictionary<string, List<Pawn>> roles = new Dictionary<string, List<Pawn>>();


        private List<string> roleKeys = new List<string>();
        private List<List<Pawn>> roleValues = new List<List<Pawn>>();


        public void ExposeData()
        {
            Scribe_Values.Look(ref autoStart, "DoTheRitualsYourselves.AutoStart", false);
            Scribe_Values.Look(ref policyID, "DoTheRitualsYourselves.PolicyID", 1);
            Scribe_Values.Look(ref nextCheckTick, "DoTheRitualsYourselves.NextCheckTick", Random.Range(2500, 7500));
            Scribe_References.Look(ref ritualSpot, "DoTheRitualsYourselves.RitualSpot");

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                roleKeys.Clear();
                roleValues.Clear();
                foreach (var pair in roles)
                {
                    roleKeys.Add(pair.Key);
                    roleValues.Add(pair.Value);
                }
            }

            Scribe_Collections.Look(ref roleKeys, "DoTheRitualsYourselves.RoleKeys", LookMode.Value);
            Scribe_Collections.Look(ref roleValues, "DoTheRitualsYourselves.RoleValues", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (roleKeys == null)
                    roleKeys = new List<string>();
                if (roleValues == null)
                    roleValues = new List<List<Pawn>>();

                for (int i = 0; i < roleKeys.Count; i++)
                {
                    roleValues[i].RemoveAll(pawn => pawn.DestroyedOrNull());
                    roles.Add(roleKeys[i], roleValues[i]);
                }
            }
        }
    }
}

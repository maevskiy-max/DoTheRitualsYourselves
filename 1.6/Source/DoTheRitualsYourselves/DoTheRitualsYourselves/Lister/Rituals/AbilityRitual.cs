using DoTheRitualsYourselves.Extra;
using DoTheRitualsYourselves.Lister.Group;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DoTheRitualsYourselves.Lister.Rituals
{
    public class AbilityRitual : Ritual
    {
        private int id;
        private AbilityDef ability;
        private string role;

        public AbilityRitual(RitualGroup group, Precept_Ritual ritual, AbilityDef ability, string role, int id, bool showType = true) : base(group, ritual, showType)
        {
            this.id = id;
            this.ability = ability;
            this.role = role;
        }

        public override int Id => id;

        private Pawn GetPawn(Map map, RitualRoleAssignments ritualRoleAssignments = null)
        {
            RitualExtraData extra = GetRitualExtraData();
            List<Pawn> rolePawns = extra.roles.ContainsKey(role) ? extra.roles[role] : new List<Pawn>();
            List<Pawn> noRoles = extra.roles.ContainsKey("none") ? extra.roles["none"] : new List<Pawn>();

            foreach (Pawn pawn in rolePawns)
                if (map.mapPawns.FreeColonistsSpawned.Contains(pawn)
                    && (ritualRoleAssignments == null || ritualRoleAssignments.PawnParticipating(pawn))
                    && pawn.abilities.AllAbilitiesForReading.Any(ability2 => ability2.def == ability && !ability2.OnCooldown))
                    return pawn;

            Pawn pawn2 = map.mapPawns.FreeColonistsSpawned.Find(p =>
                (ritualRoleAssignments == null || ritualRoleAssignments.PawnParticipating(p))
                && !noRoles.Contains(p)
                && p.abilities.AllAbilitiesForReading.Any(ability2 => ability2.def == ability && !ability2.OnCooldown));

            return pawn2;
        }
        
        public override bool HasObligation(Map map, TargetInfo targetInfo, ref RitualObligation ritualObligation, ref string reason)
        {
            return GetPawn(map) != null;
        }

        public override StartRitualCallback StartRitual(TargetInfo targetInfo, Map map, RitualObligation ritualObligation, RitualRoleAssignments ritualRoleAssignments)
        {
            Pawn pawn = GetPawn(map, ritualRoleAssignments);
            if (pawn == null)
                return null;

            RitualRoleAssignments ritualRoleAssignments2 = CreateRitualRoleAssignments(targetInfo, map, null, new Dictionary<string, Pawn>() { { role, pawn } }, null);
            if (Precept.targetFilter != null)
                targetInfo = Precept.targetFilter.BestTarget(pawn, pawn);

            return () => Precept.behavior.TryExecuteOn(targetInfo, pawn, Precept, ritualObligation, ritualRoleAssignments2, true);
        }
    }
}

using DoTheRitualsYourselves.Extra;
using DoTheRitualsYourselves.Lister.Group;
using DoTheRitualsYourselves.RitualPolicies;
using DoTheRitualsYourselves.WorldComponents;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Lister.Rituals
{
    public class Ritual
    {
        public delegate void StartRitualCallback();

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
        public virtual bool UseRitualSpot => true;
        public Precept_Ritual Precept => ritual;
        public IEnumerable<ThingDef> GetBuildingDefs() => group.GetBuildingDefs();
        public RitualGroup Group => group;

        public RitualExtraData GetRitualExtraData()
        {
            return WorldComponent_AutoRituals.Instance.GetRitualExtraData(Id);
        }

        public RitualPolicy GetRitualPolicy()
        {
            return WorldComponent_AutoRituals.Instance.GetRitualPolicy(Id);
        }

        public virtual bool HasObligation(Map map, TargetInfo targetInfo, ref RitualObligation ritualObligation, ref string reason)
        {
            Precept_Ritual precept = Precept;

            if (!precept.activeObligations.NullOrEmpty())
            {
                foreach (RitualObligation activeObligation in precept.activeObligations)
                {
                    RitualTargetUseReport ritualTargetUseReport2 = precept.CanUseTarget(targetInfo, activeObligation);
                    if (ritualTargetUseReport2.canUse)
                    {
                        ritualObligation = activeObligation;
                        return true;
                    }
                }
            }

            if (precept.isAnytime)
            {
                var ritualTargetUseReport = precept.CanUseTarget(targetInfo, null);
                if (!ritualTargetUseReport.canUse)
                    return false;

                reason = ritualTargetUseReport.failReason;
                ritualObligation = null;
                return true;
            }

            RitualObligationTrigger ritualObligationTrigger = precept.obligationTriggers?.FirstOrDefault((RitualObligationTrigger o) => o is RitualObligationTrigger_Date);
            if (ritualObligationTrigger != null)
            {
                RitualObligationTrigger_Date ritualObligationTrigger_Date = (RitualObligationTrigger_Date)ritualObligationTrigger;
                int num = ritualObligationTrigger_Date.OccursOnTick();
                int num2 = ritualObligationTrigger_Date.CurrentTickRelative();
                if (num2 > num)
                    num += 3600000;
                reason = "DateRitualNoObligation".Translate(precept.LabelCap, (num - num2).ToStringTicksToPeriod(), ritualObligationTrigger_Date.DateString).Resolve();
            }

            if (reason == "")
                reason = "DoTheRitualsYourselves.Reason.RitualNoObligation".Translate();
            return false;
        }

        public RitualRoleAssignments CreateRitualRoleAssignments(TargetInfo targetInfo, Map map, List<Pawn> requiredPawns = null, Dictionary<string, Pawn> forcedForRole = null, Pawn selectedPawn = null)
        {
            RitualPolicy policy = GetRitualPolicy();
            RitualExtraData extra = GetRitualExtraData();
            Dialog_BeginRitual.PawnFilter filter = (Pawn pawn, bool voluntary, bool allowOtherIdeos) => policy.IsCanJoin(Precept, targetInfo, pawn, voluntary, allowOtherIdeos);

            RitualRoleAssignments ritualRoleAssignments = Dialog_BeginRitual.CreateRitualRoleAssignments(Precept, targetInfo, map, filter, requiredPawns, forcedForRole, selectedPawn);
            List<Pawn> noRoles = extra.roles.ContainsKey("none") ? extra.roles["none"] : new List<Pawn>();

            foreach (var pair in extra.roles)
            {
                string roleId = pair.Key;
                List<Pawn> pawns = pair.Value;

                RitualRole role = null;
                foreach (RitualRole role2 in Precept.behavior.def.roles)
                {
                    if (role2.id == roleId)
                    {
                        role = role2;
                        break;
                    }
                }

                if (role != null || roleId == "none")
                {
                    foreach (Pawn pawn in pawns)
                    {
                        if (!ritualRoleAssignments.AllCandidatePawns.Contains(pawn))
                            continue;
                        PsychicRitualRoleDef.Reason reason;
                        if (ritualRoleAssignments.TryAssign(pawn, role, out reason))
                            break;
                    }
                }
            }

            FillPawns(ritualRoleAssignments, noRoles, filter, targetInfo);
            return ritualRoleAssignments;
        }

        private void FillPawns(RitualRoleAssignments ritualRoleAssignments, List<Pawn> noRoles, Dialog_BeginRitual.PawnFilter filter, TargetInfo ritualTarget)
        {
            FieldInfo selectedPawnField = typeof(RitualRoleAssignments).GetField("selectedPawn", BindingFlags.NonPublic | BindingFlags.Instance);
            Pawn selectedPawn = (Pawn)selectedPawnField.GetValue(ritualRoleAssignments);

            FieldInfo requiredPawnsField = typeof(RitualRoleAssignments).GetField("requiredPawns", BindingFlags.NonPublic | BindingFlags.Instance);
            List<Pawn> requiredPawns = (List<Pawn>)requiredPawnsField.GetValue(ritualRoleAssignments);

            FieldInfo forcedRolesField = typeof(RitualRoleAssignments).GetField("forcedRoles", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<string, Pawn> forcedRoles = (Dictionary<string, Pawn>)forcedRolesField.GetValue(ritualRoleAssignments);

            if (!requiredPawns.NullOrEmpty())
                foreach (Pawn requiredPawn in requiredPawns)
                    if (ritualRoleAssignments.RoleForPawn(requiredPawn) == null && (forcedRoles == null || !forcedRoles.ContainsValue(requiredPawn)))
                        ritualRoleAssignments.TryAssignSpectate(requiredPawn);

            string reason;
            PsychicRitualRoleDef.Reason reason2;
            if (selectedPawn != null && ritualRoleAssignments.RoleForPawn(selectedPawn) == null)
            {
                foreach (RitualRole item in ritualRoleAssignments.AllRolesForReading)
                {
                    if (!noRoles.Contains(selectedPawn) && item.defaultForSelectedColonist && item.AppliesToPawn(selectedPawn, out reason, ritualTarget, null, ritualRoleAssignments))
                    {
                        ritualRoleAssignments.TryAssign(selectedPawn, item, out reason2);
                        break;
                    }
                }
            }

            foreach (RitualRole role in ritualRoleAssignments.AllRolesForReading)
            {
                List<Pawn> tmpOrderedPawns = new List<Pawn>(32);

                tmpOrderedPawns.Clear();
                tmpOrderedPawns.AddRange(ritualRoleAssignments.AllCandidatePawns.Where((Pawn pawn) => (filter == null || filter(pawn, !(role is RitualRoleForced), role.allowOtherIdeos)) && !noRoles.Contains(pawn)));
                role.OrderByDesirability(tmpOrderedPawns);
                foreach (Pawn tmpOrderedPawn in tmpOrderedPawns)
                {
                    if (ritualRoleAssignments.RoleForPawn(tmpOrderedPawn) == null)
                    {
                        if (role.maxCount > 0 && ritualRoleAssignments.AssignedPawns(role).Count() >= role.maxCount)
                            break;

                        if (role.AppliesToPawn(tmpOrderedPawn, out reason, ritualTarget, null, ritualRoleAssignments, null, skipReason: true))
                            ritualRoleAssignments.TryAssign(tmpOrderedPawn, role, out reason2);
                    }
                }
            }

            foreach (Pawn item2 in ritualRoleAssignments.SpectatorCandidates())
                ritualRoleAssignments.TryAssignSpectate(item2);

            List<Pawn> pawnsToRemove = new List<Pawn>();
            foreach (Pawn allPawn in ritualRoleAssignments.AllCandidatePawns)
            {
                RitualRole ritualRole = ritualRoleAssignments.RoleForPawn(allPawn);
                if (ritualRole != null && ritualRole.required && ritualRole.substitutable && ritualRoleAssignments.PawnNotAssignableReason(allPawn, ritualRole, out var _) != null)
                {
                    ritualRoleAssignments.RemoveParticipant(allPawn);
                    pawnsToRemove.Add(allPawn);
                }
            }

            ritualRoleAssignments.AllCandidatePawns.RemoveAll((Pawn p) => pawnsToRemove.Contains(p));
        }

        public virtual StartRitualCallback StartRitual(TargetInfo targetInfo, Map map, RitualObligation ritualObligation, RitualRoleAssignments ritualRoleAssignments)
        {
            return () => Precept.behavior.TryExecuteOn(targetInfo, null, Precept, ritualObligation, ritualRoleAssignments, true);
        }
    }
}

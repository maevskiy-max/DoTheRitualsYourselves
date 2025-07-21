using DoTheRitualsYourselves.Extra;
using DoTheRitualsYourselves.RitualPolicies;
using DoTheRitualsYourselves.WorldComponents;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Core
{
    public static class RitualStarter
    {
        public delegate void StartRitualCallback();

        public static float PredictedQuality(Precept_Ritual ritual, TargetInfo targetInfo, RitualObligation ritualObligation, RitualRoleAssignments ritualRoleAssignments)
        {
            var outcome = ritual.outcomeEffect.def;
            float num = outcome.startingQuality;
            float num2 = 0f;
            foreach (RitualOutcomeComp comp in outcome.comps)
            {
                QualityFactor qualityFactor = comp.GetQualityFactor(ritual, targetInfo, ritualObligation, ritualRoleAssignments, ritual?.outcomeEffect?.DataForComp(comp));
                if (qualityFactor != null)
                {
                    if (qualityFactor.uncertainOutcome)
                        num2 += qualityFactor.quality;
                    else
                        num += qualityFactor.quality;
                }
            }

            if (ritual != null && ritual.RepeatPenaltyActive)
                num += ritual.RepeatQualityPenalty;

            Tuple<ExpectationDef, float> expectationsOffset = RitualOutcomeEffectWorker_FromQuality.GetExpectationsOffset(targetInfo.Map, ritual?.def);
            if (expectationsOffset != null)
                num += expectationsOffset.Item2;

            num = Mathf.Clamp(num, outcome.minQuality, outcome.maxQuality);
            num2 += num;
            num2 = Mathf.Clamp(num2, outcome.minQuality, outcome.maxQuality);

            return num;
        }

        public static bool CanStartWithObligations(Precept_Ritual ritual, Thing thing, ref RitualObligation ritualObligation, ref string reason)
        {
            if (!ritual.activeObligations.NullOrEmpty())
            {
                foreach (RitualObligation activeObligation in ritual.activeObligations)
                {
                    RitualTargetUseReport ritualTargetUseReport2 = ritual.CanUseTarget(thing, activeObligation);
                    if (ritualTargetUseReport2.canUse)
                    {
                        ritualObligation = activeObligation;
                        return true;
                    }
                }
            }

            if (ritual.isAnytime)
            {
                var ritualTargetUseReport = ritual.CanUseTarget(thing, null);
                if (!ritualTargetUseReport.canUse)
                    return false;
                reason = ritualTargetUseReport.failReason;
                return true;
            }

            RitualObligationTrigger ritualObligationTrigger = ritual.obligationTriggers?.FirstOrDefault((RitualObligationTrigger o) => o is RitualObligationTrigger_Date);
            if (ritualObligationTrigger != null)
            {
                RitualObligationTrigger_Date ritualObligationTrigger_Date = (RitualObligationTrigger_Date)ritualObligationTrigger;
                int num = ritualObligationTrigger_Date.OccursOnTick();
                int num2 = ritualObligationTrigger_Date.CurrentTickRelative();
                if (num2 > num)
                    num += 3600000;
                reason = "DateRitualNoObligation".Translate(ritual.LabelCap, (num - num2).ToStringTicksToPeriod(), ritualObligationTrigger_Date.DateString).Resolve();
            }

            if (reason == "")
                reason = "DoTheRitualsYourselves.Reason.RitualNoObligation".Translate();
            return false;
        }

        public static bool CanStartWithPawns(Precept_Ritual ritual, Thing thing, RitualObligation ritualObligation, ref string reason, ref StartRitualCallback callback, ref float quality)
        {
            TargetInfo targetInfo = new TargetInfo(thing);
            RitualPolicy policy = WorldComponent_AutoRituals.Instance.GetRitualPolicy(ritual.Id);

            Dialog_BeginRitual.PawnFilter filter = delegate (Pawn pawn, bool voluntary, bool allowOtherIdeos)
            {
                return policy.IsCanJoin(ritual, pawn, voluntary, allowOtherIdeos);
            };

            RitualRoleAssignments ritualRoleAssignments = Dialog_BeginRitual.CreateRitualRoleAssignments(ritual, targetInfo, thing.Map, filter, null, null, null);
            ritualRoleAssignments.FillPawns(filter, targetInfo);

            while (ritualRoleAssignments.SpectatorsForReading.Count > policy.maxNonRoleCount)
            {
                Pawn pawn = ritualRoleAssignments.SpectatorsForReading.RandomElement();
                ritualRoleAssignments.SpectatorsForReading.Remove(pawn);
            }

            if (!ritualRoleAssignments.Participants.Any())
            {
                reason = "MessageRitualNeedsAtLeastOnePerson".Translate();
                return false;
            }

            foreach (Pawn participant in ritualRoleAssignments.Participants)
            {
                if (!participant.IsPrisoner && !participant.SafeTemperatureRange().IncludesEpsilon(targetInfo.Cell.GetTemperature(targetInfo.Map)))
                {
                    reason = "CantJoinRitualInExtremeWeather".Translate();
                    return false;
                }
            }

            if (ritual.behavior.SpectatorsRequired() && ritualRoleAssignments.SpectatorsForReading.Count == 0)
            {
                reason = "MessageRitualNeedsAtLeastOneSpectator".Translate();
                return false;
            }

            if (ritual.outcomeEffect != null)
            {
                foreach (string item in ritual.outcomeEffect.BlockingIssues(ritual, targetInfo, ritualRoleAssignments))
                {
                    reason = item;
                    return false;
                }
            }

            if (ritual.obligationTargetFilter != null)
            {
                foreach (string blockingIssue in ritual.obligationTargetFilter.GetBlockingIssues(targetInfo, ritualRoleAssignments))
                {
                    reason = blockingIssue;
                    return false;
                }
            }

            if (!ritual.behavior.def.roles.NullOrEmpty())
            {
                bool stillAddToPawnList;
                foreach (IGrouping<string, RitualRole> item2 in from r in ritual.behavior.def.roles group r by r.mergeId ?? r.id)
                {
                    RitualRole firstRole = item2.First();
                    int requiredPawnCount = item2.Count((RitualRole r) => r.required);
                    if (requiredPawnCount <= 0)
                        continue;

                    IEnumerable<Pawn> selectedPawns = item2.SelectMany((RitualRole r) => ritualRoleAssignments.AssignedPawns(r));
                    foreach (Pawn item3 in selectedPawns)
                    {
                        string text = ritualRoleAssignments.PawnNotAssignableReason(item3, firstRole, out stillAddToPawnList);
                        if (text != null)
                        {
                            reason = text;
                            return false;
                        }
                    }

                    if (requiredPawnCount == 1 && !selectedPawns.Any())
                    {
                        reason = "MessageLordJobNeedsAtLeastOneRolePawn".Translate(firstRole.Label.Resolve());
                        return false;
                    }
                    else if (requiredPawnCount > 1 && selectedPawns.Count() < requiredPawnCount)
                    {
                        reason = "MessageLordJobNeedsAtLeastNumRolePawn".Translate(Find.ActiveLanguageWorker.Pluralize(firstRole.Label), requiredPawnCount);
                        return false;
                    }
                }

                if (!ritualRoleAssignments.ExtraRequiredPawnsForReading.NullOrEmpty())
                {
                    foreach (Pawn item4 in ritualRoleAssignments.ExtraRequiredPawnsForReading)
                    {
                        string text2 = ritualRoleAssignments.PawnNotAssignableReason(item4, ritualRoleAssignments.RoleForPawn(item4), out stillAddToPawnList);
                        if (text2 != null)
                        {
                            reason = text2;
                            return false;
                        }
                    }
                }
            }

            if (ritual.ritualOnlyForIdeoMembers && !ritualRoleAssignments.Participants.Any((Pawn p) => p.Ideo == ritual.ideo))
            {
                reason = "MessageNeedAtLeastOneParticipantOfIdeo".Translate(ritual.ideo.memberName);
                return false;
            }

            quality = PredictedQuality(ritual, targetInfo, ritualObligation, ritualRoleAssignments);
            callback = () => ritual.behavior.TryExecuteOn(targetInfo, null, ritual, ritualObligation, ritualRoleAssignments, true);

            return true;
        }

        public static bool TryStart(this Precept_Ritual ritual, ref string reason, bool forced, bool simulated, Map map = null)
        {
            if (map == null)
                map = Find.CurrentMap;

            if (!ritual.allowOtherInstances)
            {
                foreach (LordJob_Ritual activeRitual in Find.IdeoManager.GetActiveRituals(map))
                {
                    if (activeRitual.Ritual == ritual)
                    {
                        reason = "CantStartRitualAlreadyInProgress".Translate(ritual.LabelCap);
                        return false;
                    }
                }
            }

            if (!forced && map.dangerWatcher.DangerRating == StoryDanger.High)
            {
                reason = "DoTheRitualsYourselves.Reason.Danger".Translate();
                return false;
            }

            RitualPolicy policy = WorldComponent_AutoRituals.Instance.GetRitualPolicy(ritual.Id);
            if (!map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(pawn => policy.IsCanJoin(ritual, pawn)))
            {
                reason = "DoTheRitualsYourselves.Reason.Nobody".Translate();
                return false;
            }

            if (!forced && !policy.IsAccept(ritual, map, ref reason))
                return false;

            string reason2 = "", reason3 = "";
            StartRitualCallback callback = null;
            float quality = 0;

            RitualExtraData extra = WorldComponent_AutoRituals.Instance.GetRitualExtraData(ritual.Id);
            Thing ritualSpot = extra.ritualSpot;
            if (!simulated && ritualSpot != null)
            {
                if (!ritualSpot.Spawned)
                    extra.ritualSpot = null;
                else if (ritualSpot.Map == map)
                {
                    RitualObligation ritualObligation = null;
                    if (CanStartWithObligations(ritual, ritualSpot, ref ritualObligation, ref reason2))
                    {
                        if (CanStartWithPawns(ritual, ritualSpot, ritualObligation, ref reason3, ref callback, ref quality))
                        {
                            callback();
                            return true;
                        }
                    }
                }
            }

            foreach (Thing thing in map.GetRitualBuildings())
            {
                RitualObligation ritualObligation = null;
                if (!CanStartWithObligations(ritual, thing, ref ritualObligation, ref reason2))
                    continue;

                StartRitualCallback callbackTmp = null;
                float qualityTmp = 0;

                if (simulated)
                    return true;

                if (CanStartWithPawns(ritual, thing, ritualObligation, ref reason3, ref callbackTmp, ref qualityTmp))
                {
                    if (callback == null || quality < qualityTmp)
                    {
                        callback = callbackTmp;
                        quality = qualityTmp;
                    }
                }
            }

            if (callback != null)
            {
                callback();
                return true;
            }

            reason = "DoTheRitualsYourselves.Reason.RitualNoSpot".Translate();
            if (reason3 != "")
                reason = reason3;
            else if (reason2 != "")
                reason = reason2;

            return false;
        }

        public static bool TryStart(this Precept_Ritual ritual)
        {
            RitualExtraData extra = WorldComponent_AutoRituals.Instance.GetRitualExtraData(ritual.Id);
            if (!extra.autoStart)
                return false;

            Thing ritualSpot = extra.ritualSpot;
            if (ritualSpot != null && ritualSpot.Spawned)
            {
                string reason = "";
                if (ritual.TryStart(ref reason, false, false, ritualSpot.Map))
                    return true;
            }
            
            foreach (Map map in Find.Maps)
            {
                string reason = "";
                if (ritual.TryStart(ref reason, false, false, map))
                    return true;
            }

            return false;
        }
    }
}

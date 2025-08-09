using DoTheRitualsYourselves.Extra;
using DoTheRitualsYourselves.Lister.Rituals;
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

        public static bool CanStartWithPawns(Ritual ritual, Map map, TargetInfo targetInfo, RitualObligation ritualObligation, ref string reason, ref Ritual.StartRitualCallback callback, ref float quality)
        {
            Precept_Ritual precept = ritual.Precept;
            RitualPolicy policy = ritual.GetRitualPolicy();

            RitualRoleAssignments ritualRoleAssignments = ritual.CreateRitualRoleAssignments(targetInfo, map);
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

            if (precept.behavior.SpectatorsRequired() && ritualRoleAssignments.SpectatorsForReading.Count == 0)
            {
                reason = "MessageRitualNeedsAtLeastOneSpectator".Translate();
                return false;
            }

            if (precept.outcomeEffect != null)
            {
                foreach (string item in precept.outcomeEffect.BlockingIssues(precept, targetInfo, ritualRoleAssignments))
                {
                    reason = item;
                    return false;
                }
            }

            if (precept.obligationTargetFilter != null)
            {
                foreach (string blockingIssue in precept.obligationTargetFilter.GetBlockingIssues(targetInfo, ritualRoleAssignments))
                {
                    reason = blockingIssue;
                    return false;
                }
            }

            if (!precept.behavior.def.roles.NullOrEmpty())
            {
                bool stillAddToPawnList;
                foreach (IGrouping<string, RitualRole> item2 in from r in precept.behavior.def.roles group r by r.mergeId ?? r.id)
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

            if (precept.ritualOnlyForIdeoMembers && !ritualRoleAssignments.Participants.Any((Pawn p) => p.Ideo == precept.ideo))
            {
                reason = "MessageNeedAtLeastOneParticipantOfIdeo".Translate(precept.ideo.memberName);
                return false;
            }

            callback = ritual.StartRitual(targetInfo, map, ritualObligation, ritualRoleAssignments);
            if (callback != null)
            {
                quality = PredictedQuality(precept, targetInfo, ritualObligation, ritualRoleAssignments);
                return true;
            }
            return false;
        }

        public static bool TryStart(this Ritual ritual, ref string reason, bool forced, bool simulated, Map map = null)
        {
            Precept_Ritual precept = ritual.Precept;

            if (map == null)
                map = Find.CurrentMap;

            if (!precept.allowOtherInstances)
            {
                foreach (LordJob_Ritual activeRitual in Find.IdeoManager.GetActiveRituals(map))
                {
                    if (activeRitual.Ritual == precept)
                    {
                        reason = "CantStartRitualAlreadyInProgress".Translate(precept.LabelCap);
                        return false;
                    }
                }
            }

            if (!forced && map.dangerWatcher.DangerRating == StoryDanger.High)
            {
                reason = "DoTheRitualsYourselves.Reason.Danger".Translate();
                return false;
            }

            RitualPolicy policy = ritual.GetRitualPolicy();
            if (!map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(pawn => policy.IsCanJoin(precept, null, pawn)))
            {
                reason = "DoTheRitualsYourselves.Reason.Nobody".Translate();
                return false;
            }

            if (!forced && !policy.IsAccept(precept, map, ref reason))
                return false;

            string reason2 = "", reason3 = "";
            Ritual.StartRitualCallback callback = null;
            float quality = 0;

            RitualExtraData extra = ritual.GetRitualExtraData();
            Thing ritualSpot = extra.ritualSpot;
            if (!simulated && ritualSpot != null && ritual.UseRitualSpot)
            {
                if (!ritualSpot.Spawned)
                    extra.ritualSpot = null;
                else if (ritualSpot.Map == map)
                {
                    RitualObligation ritualObligation = null;
                    TargetInfo targetInfo = new TargetInfo(ritualSpot);
                    if (ritual.HasObligation(map, targetInfo, ref ritualObligation, ref reason2))
                    {
                        if (CanStartWithPawns(ritual, map, targetInfo, ritualObligation, ref reason3, ref callback, ref quality))
                        {
                            callback();
                            return true;
                        }
                    }
                }
            }

            if (ritual.UseRitualSpot)
            {
                foreach (Thing thing in map.GetRitualBuildings(ritual.GetBuildingDefs().ToList()))
                {
                    RitualObligation ritualObligation = null;
                    TargetInfo targetInfo = new TargetInfo(thing);
                    if (!ritual.HasObligation(map, targetInfo, ref ritualObligation, ref reason2))
                        continue;

                    Ritual.StartRitualCallback callbackTmp = null;
                    float qualityTmp = 0;

                    if (simulated)
                        return true;

                    if (CanStartWithPawns(ritual, map, targetInfo, ritualObligation, ref reason3, ref callbackTmp, ref qualityTmp))
                    {
                        if (callback == null || quality < qualityTmp)
                        {
                            callback = callbackTmp;
                            quality = qualityTmp;
                        }
                    }
                }
            }
            else
            {
                RitualObligation ritualObligation = null;
                if (!ritual.HasObligation(map, TargetInfo.Invalid, ref ritualObligation, ref reason2))
                    return false;

                Ritual.StartRitualCallback callbackTmp = null;
                float qualityTmp = 0;

                if (simulated)
                    return true;

                if (CanStartWithPawns(ritual, map, TargetInfo.Invalid, ritualObligation, ref reason3, ref callbackTmp, ref qualityTmp))
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

            reason = "DoTheRitualsYourselves.Reason.RitualNoSpotOrObligation".Translate();
            if (reason3 != "")
                reason = reason3;
            else if (reason2 != "")
                reason = reason2;

            return false;
        }

        public static bool TryStart(this Ritual ritual)
        {
            RitualExtraData extra = ritual.GetRitualExtraData();
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

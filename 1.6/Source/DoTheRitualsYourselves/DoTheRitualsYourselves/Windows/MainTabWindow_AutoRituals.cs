using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using DoTheRitualsYourselves.Core;
using RimWorld.Planet;
using DoTheRitualsYourselves.WorldComponents;
using DoTheRitualsYourselves.Extra;

namespace DoTheRitualsYourselves.Windows
{
    public class MainTabWindow_AutoRituals : MainTabWindow
    {
        private Ideo selectedIdeo;
        private Ideo DefaultIdeo => Faction.OfPlayer.ideos?.PrimaryIdeo ?? Find.IdeoManager.IdeosListForReading.FirstOrDefault();

        private const float spacing = 6f;
        private const float lineHeight = 28f;
        private const float labelBetween = 12f;
        private const float nameWidth = 350f;
        private const float spaceBetween = 5f;
        private const float longSpace = 180f;
        private const float shortSpace = 100f;

        public override Vector2 InitialSize
        {
            get
            {
                int ritualCount = (selectedIdeo ?? DefaultIdeo).GetRituals().Count();
                return new Vector2(
                    spacing * 2 + nameWidth + spaceBetween * 4 + shortSpace * 2 + longSpace * 2 + 45f,
                    (ritualCount + 2) * (lineHeight + spaceBetween) + labelBetween + spacing + 15f);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            if (WorldRendererUtility.WorldRendered && Find.CurrentMap != null)
                Find.World.renderer.wantedMode = WorldRenderMode.None;

            if (selectedIdeo == null)
                selectedIdeo = DefaultIdeo;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float curY = inRect.y + spacing;

            Text.Font = GameFont.Small;
            var rectIdeo = new Rect(inRect.x + spacing, curY, nameWidth, lineHeight);
            TooltipHandler.TipRegion(rectIdeo, "DoTheRitualsYourselves.UI.Ideo.Tip".Translate());
            if (Widgets.ButtonText(rectIdeo, selectedIdeo.name))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (Ideo ideo in Find.IdeoManager.IdeosListForReading)
                    if (Faction.OfPlayer.ideos.AllIdeos.Contains(ideo))
                        options.Add(new FloatMenuOption(ideo.name, () => selectedIdeo = ideo, ideo.Icon, ideo.Color));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            TextAnchor prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;

            var rectLabelStartNow = new Rect(inRect.x + spacing + nameWidth + spaceBetween, curY, shortSpace, lineHeight);
            var rectLabelAutoStart = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 2 + shortSpace, curY, shortSpace, lineHeight);
            var rectLabelPolicy = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 3 + shortSpace * 2, curY, longSpace, lineHeight);
            var rectLabelSpot = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 4 + shortSpace * 2 + longSpace, curY, longSpace, lineHeight);

            Widgets.Label(rectLabelStartNow, "DoTheRitualsYourselves.UI.StartNow".Translate());
            TooltipHandler.TipRegion(rectLabelStartNow, "DoTheRitualsYourselves.UI.StartNow.Tip".Translate());
            Widgets.Label(rectLabelAutoStart, "DoTheRitualsYourselves.UI.AutoStart".Translate());
            TooltipHandler.TipRegion(rectLabelAutoStart, "DoTheRitualsYourselves.UI.AutoStart.Tip".Translate());
            Widgets.Label(rectLabelPolicy, "DoTheRitualsYourselves.UI.RitualPolicy".Translate());
            TooltipHandler.TipRegion(rectLabelPolicy, "DoTheRitualsYourselves.UI.RitualPolicy.Tip".Translate());
            Widgets.Label(rectLabelSpot, "DoTheRitualsYourselves.UI.RitualSpot".Translate());
            TooltipHandler.TipRegion(rectLabelSpot, "DoTheRitualsYourselves.UI.RitualSpot.Tip".Translate());
            Text.Anchor = prevAnchor;

            curY += lineHeight + labelBetween;
            windowRect.y += windowRect.height - InitialSize.y;
            windowRect.height = InitialSize.y;
            foreach (Precept_Ritual ritual in selectedIdeo.GetRituals())
            {
                Widgets.Label(new Rect(inRect.x + 20f, curY, nameWidth, lineHeight), new GUIContent($"{ritual.LabelCap} ({(ritual.ShortDescOverrideCap.NullOrEmpty() ? ritual.def.label : ritual.ShortDescOverrideCap)})", ritual.Icon));
                RitualExtraData extra = WorldComponent_AutoRituals.Instance.GetRitualExtraData(ritual.Id);

                // start now
                string reason = "";
                Rect startNowRect = new Rect(inRect.x + spacing + nameWidth + spaceBetween, curY, shortSpace, lineHeight);
                if (!ritual.TryStart(ref reason, true, true))
                {
                    if (Widgets.ButtonText(startNowRect, "DoTheRitualsYourselves.UI.CannotStart".Translate()))
                        Messages.Message("DoTheRitualsYourselves.Message.CantStart".Translate(), MessageTypeDefOf.RejectInput);
                    TooltipHandler.TipRegion(startNowRect, reason != "" ? reason : "DoTheRitualsYourselves.Reason.CantUnknown".Translate().RawText);
                }
                else
                {
                    if (Widgets.ButtonText(startNowRect, "DoTheRitualsYourselves.UI.Start".Translate()))
                    {
                        string reason2 = "";
                        ritual.TryStart(ref reason2, true, false);

                        if (!reason2.NullOrEmpty())
                            Messages.Message(reason2, MessageTypeDefOf.RejectInput);
                    }

                    if (ritual.RepeatPenaltyActive)
                        TooltipHandler.TipRegion(startNowRect, "DoTheRitualsYourselves.UI.RepeatPenaltyStartTooltip".Translate());
                    else
                        TooltipHandler.TipRegion(startNowRect, "DoTheRitualsYourselves.UI.StartTooltip".Translate());
                }

                // auto start
                bool auto = extra.autoStart;
                Widgets.Checkbox(new Vector2(inRect.x + spacing + nameWidth + spaceBetween * 2 + shortSpace * 1.5f - 12f, curY), ref auto);
                extra.autoStart = auto;

                // policy
                Rect policyRect = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 3 + shortSpace * 2, curY, longSpace, lineHeight);
                var currentPolicy = WorldComponent_AutoRituals.Instance.GetRitualPolicy(ritual.Id);
                if (Widgets.ButtonText(policyRect, currentPolicy.Label))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (var policyId in WorldComponent_RitualPolicy.Instance.AllPolicyIds)
                    {
                        var policy = WorldComponent_RitualPolicy.Instance.GetPolicy(policyId);
                        options.Add(new FloatMenuOption(policy.Label, () =>
                        {
                            extra.policyID = policyId;
                        }));
                    }
                    options.Add(new FloatMenuOption("DoTheRitualsYourselves.UI.Edit".Translate(), () =>
                    {
                        Find.WindowStack.Add(new Dialog_EditRitualPolicy());
                    }));

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                // spot
                Rect spotRect = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 4 + shortSpace * 2 + longSpace, curY, longSpace, lineHeight);
                Thing ritualSpot = extra.ritualSpot;
                if (ritualSpot != null && !ritualSpot.Spawned)
                {
                    ritualSpot = null;
                    extra.ritualSpot = null;
                }

                if (ritualSpot == null)
                {
                    if (Widgets.ButtonText(spotRect, "DoTheRitualsYourselves.UI.RitualSpot.None".Translate()))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.Assign".Translate(), () => AssignRitualSpot(ritual, extra)),
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
                else
                {
                    TooltipHandler.TipRegion(spotRect, ritualSpot.LabelNoParenthesis);
                    if (Widgets.ButtonText(spotRect, ritualSpot.LabelNoParenthesis))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.Reassign".Translate(), () => AssignRitualSpot(ritual, extra)),
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.Clear".Translate(), () => ClearRitualSpot(extra)),
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.JumpTo".Translate(), () => JumpToRitualSpot(extra))
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }

                curY += lineHeight + spaceBetween;
            }
        }

        public void AssignRitualSpot(Precept_Ritual ritual, RitualExtraData extra)
        {
            var targetingParams = new TargetingParameters
            {
                canTargetBuildings = true,
                validator = t => {
                    if (!(t.Thing is Building) || !t.Thing.def.IsRitualBuilding() || t.Thing.Faction != Faction.OfPlayer)
                        return false;

                    if (!ritual.activeObligations.NullOrEmpty())
                        foreach (RitualObligation activeObligation in ritual.activeObligations)
                            if (ritual.CanUseTarget(t.Thing, activeObligation).canUse)
                                return true; 

                    try
                    {
                        return ritual.CanUseTarget(t, null).canUse;
                    }
                    catch
                    {
                        return false;
                    }
                }
            };

            Find.Targeter.BeginTargeting(targetingParams, 
                target => {
                    extra.ritualSpot = target.Thing;
                },
                target => { });
        }

        public void ClearRitualSpot(RitualExtraData extra)
        {
            extra.ritualSpot = null;
        }

        public void JumpToRitualSpot(RitualExtraData extra)
        {
            Thing thing = extra.ritualSpot;
            if (thing != null)
            {
                CameraJumper.TryJumpAndSelect(thing);
                Find.MainTabsRoot.EscapeCurrentTab();
            }
        }
    }
}
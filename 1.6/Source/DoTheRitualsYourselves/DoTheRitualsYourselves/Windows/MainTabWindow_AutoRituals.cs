using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using DoTheRitualsYourselves.Core;
using RimWorld.Planet;
using DoTheRitualsYourselves.WorldComponents;
using DoTheRitualsYourselves.Extra;
using DoTheRitualsYourselves.Lister.Group;
using DoTheRitualsYourselves.Lister.Rituals;
using Verse.Noise;

namespace DoTheRitualsYourselves.Windows
{
    public class MainTabWindow_AutoRituals : MainTabWindow
    {
        private RitualGroup selectedGroup;
        private RitualGroup DefaultGroup => Faction.OfPlayer.ideos?.PrimaryIdeo != null ?
            RitualLister.GetRitualGroups().Find(group => group is IdeoRitualGroup group2 && group2.Ideo == Faction.OfPlayer.ideos?.PrimaryIdeo)
            ?? RitualLister.GetRitualGroups().First() : RitualLister.GetRitualGroups().First();

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
                int ritualCount = (selectedGroup ?? DefaultGroup).GetVisibleRituals().Count();
                return new Vector2(
                    spacing * 2 + nameWidth + spaceBetween * 5 + shortSpace * 2 + longSpace * 3 + 45f,
                    (ritualCount + 2) * (lineHeight + spaceBetween) + labelBetween + spacing + 15f);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            if (WorldRendererUtility.WorldRendered && Find.CurrentMap != null)
                Find.World.renderer.wantedMode = WorldRenderMode.None;

            if (selectedGroup == null)
                selectedGroup = DefaultGroup;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float curY = inRect.y + spacing;

            Text.Font = GameFont.Small;
            var rectIdeo = new Rect(inRect.x + spacing, curY, nameWidth, lineHeight);
            TooltipHandler.TipRegion(rectIdeo, "DoTheRitualsYourselves.UI.Ideo.Tip".Translate());

            if (Widgets.ButtonText(rectIdeo, selectedGroup.Label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (RitualGroup group in RitualLister.GetRitualGroups())
                    options.Add(new FloatMenuOption(group.Label, () => selectedGroup = group, group.Icon, group.Color));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            TextAnchor prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;

            var rectLabelStartNow = new Rect(inRect.x + spacing + nameWidth + spaceBetween, curY, shortSpace, lineHeight);
            var rectLabelAutoStart = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 2 + shortSpace, curY, shortSpace, lineHeight);
            var rectLabelPolicy = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 3 + shortSpace * 2, curY, longSpace, lineHeight);
            var rectLabelSpot = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 4 + shortSpace * 2 + longSpace, curY, longSpace, lineHeight); 
            var rectLabelRole = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 5 + shortSpace * 2 + longSpace * 2, curY, longSpace, lineHeight);

            Widgets.Label(rectLabelStartNow, "DoTheRitualsYourselves.UI.StartNow".Translate());
            TooltipHandler.TipRegion(rectLabelStartNow, "DoTheRitualsYourselves.UI.StartNow.Tip".Translate());
            Widgets.Label(rectLabelAutoStart, "DoTheRitualsYourselves.UI.AutoStart".Translate());
            TooltipHandler.TipRegion(rectLabelAutoStart, "DoTheRitualsYourselves.UI.AutoStart.Tip".Translate());
            Widgets.Label(rectLabelPolicy, "DoTheRitualsYourselves.UI.RitualPolicy".Translate());
            TooltipHandler.TipRegion(rectLabelPolicy, "DoTheRitualsYourselves.UI.RitualPolicy.Tip".Translate());
            Widgets.Label(rectLabelSpot, "DoTheRitualsYourselves.UI.RitualSpot".Translate());
            TooltipHandler.TipRegion(rectLabelSpot, "DoTheRitualsYourselves.UI.RitualSpot.Tip".Translate());
            Widgets.Label(rectLabelRole, "DoTheRitualsYourselves.UI.RitualRole".Translate());
            TooltipHandler.TipRegion(rectLabelRole, "DoTheRitualsYourselves.UI.RitualRole.Tip".Translate());
            Text.Anchor = prevAnchor;

            curY += lineHeight + labelBetween;
            windowRect.y += windowRect.height - InitialSize.y;
            windowRect.height = InitialSize.y;
            foreach (Ritual ritual in selectedGroup.GetVisibleRituals())
            {
                Rect labelRect = new Rect(inRect.x + 20f, curY, nameWidth, lineHeight);
                Widgets.Label(labelRect, new GUIContent(ritual.Label, ritual.Icon));
                TooltipHandler.TipRegion(labelRect, ritual.Precept.DescriptionForTip);
                RitualExtraData extra = ritual.GetRitualExtraData();

                // start now
                string reason = "";
                Rect startNowRect = new Rect(inRect.x + spacing + nameWidth + spaceBetween, curY, shortSpace, lineHeight);
                if (!ritual.TryStart(ref reason, true, true))
                {
                    var lord = Find.IdeoManager.GetActiveRituals(Find.CurrentMap).Find(activeRitual => activeRitual.Ritual == ritual.Precept);
                    if (!ritual.Precept.allowOtherInstances && lord != null)
                    {
                        if (Widgets.ButtonText(startNowRect, "DoTheRitualsYourselves.UI.Cancel".Translate()))
                            lord.Cancel();
                        TooltipHandler.TipRegion(startNowRect, "DoTheRitualsYourselves.UI.Cancel.Tip".Translate());
                    }
                    else
                    {
                        if (Widgets.ButtonText(startNowRect, "DoTheRitualsYourselves.UI.CannotStart".Translate()))
                            Messages.Message("DoTheRitualsYourselves.Message.CantStart".Translate(), MessageTypeDefOf.RejectInput);
                        TooltipHandler.TipRegion(startNowRect, reason != "" ? reason : "DoTheRitualsYourselves.Reason.CantUnknown".Translate().RawText);
                    }
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

                    if (ritual.Precept.RepeatPenaltyActive)
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
                var currentPolicy = ritual.GetRitualPolicy();
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
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.Assign".Translate(), () => AssignRitualSpot(ritual.Precept, extra, ritual.Group.ValidateSpot)),
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
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.Reassign".Translate(), () => AssignRitualSpot(ritual.Precept, extra, ritual.Group.ValidateSpot)),
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.Clear".Translate(), () => ClearRitualSpot(extra)),
                            new FloatMenuOption("DoTheRitualsYourselves.UI.RitualSpot.JumpTo".Translate(), () => JumpToRitualSpot(extra))
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }

                // role
                Rect roleRect = new Rect(inRect.x + spacing + nameWidth + spaceBetween * 5 + shortSpace * 2 + longSpace * 2, curY, longSpace, lineHeight);
                if (Widgets.ButtonText(roleRect, "DoTheRitualsYourselves.UI.Assign".Translate()))
                    Find.WindowStack.Add(new Dialog_AssignRole(ritual));

                curY += lineHeight + spaceBetween;
            }
        }

        public delegate bool Validator(TargetInfo t, Precept_Ritual ritual);

        public void AssignRitualSpot(Precept_Ritual ritual, RitualExtraData extra, Validator validator)
        {
            var targetingParams = new TargetingParameters
            {
                canTargetBuildings = true,
                validator = t => validator(t, ritual)
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
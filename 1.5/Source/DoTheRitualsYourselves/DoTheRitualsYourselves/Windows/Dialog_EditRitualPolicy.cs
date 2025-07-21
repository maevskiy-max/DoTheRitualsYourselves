using DoTheRitualsYourselves.WorldComponents;
using DoTheRitualsYourselves.RitualPolicies;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace DoTheRitualsYourselves.Windows
{
    public class Dialog_EditRitualPolicy : Window
    {
        private RitualPolicy selectedPolicy;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public Dialog_EditRitualPolicy()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float topSpace = 100f;
            float listWidth = 140f;
            float spacing = 30f;

            // X
            Rect closeRect = new Rect(inRect.xMax - 24f, inRect.y, 24f, 24f);
            if (Widgets.ButtonImage(closeRect, TexButton.CloseXSmall))
                Close();

            // +
            Rect addRect = new Rect(inRect.x + listWidth + 10f, inRect.y + inRect.height - 20f, 16f, 16f);
            TooltipHandler.TipRegion(addRect, "DoTheRitualsYourselves.UI.Add".Translate());
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                int number = WorldComponent_RitualPolicy.Instance.AllPolicyIds.Count() + 1;
                Messages.Message("DoTheRitualsYourselves.Message.NewRitualPolicy".Translate(), MessageTypeDefOf.NeutralEvent);
                WorldComponent_RitualPolicy.Instance.InsertPolicy(new RitualPolicy_Editable("DoTheRitualsYourselves.NewPolicy".Translate() + " " + number));
            }

            // title & desc
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "DoTheRitualsYourselves.UI.Policy.Title".Translate());
            Text.Font = GameFont.Small; 
            Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width, 70f), "DoTheRitualsYourselves.UI.Policy.Desc".Translate());

            // list
            Rect listRect = new Rect(inRect.x, inRect.y + topSpace, listWidth, inRect.height - topSpace);
            Widgets.DrawMenuSection(listRect);

            GUI.enabled = true;
            float listInnerHeight = WorldComponent_RitualPolicy.Instance.AllPolicyIds.Count() * 30f + 30f;
            Rect viewRect = new Rect(0f, 0f, listRect.width, listInnerHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect, showScrollbars: false);
            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);
            foreach (var id in WorldComponent_RitualPolicy.Instance.AllPolicyIds)
            {
                var policy = WorldComponent_RitualPolicy.Instance.GetPolicy(id);
                if (list.ButtonText(policy.Label))
                    selectedPolicy = policy;
            }
            list.End();
            Widgets.EndScrollView();

            if (selectedPolicy == null)
                return;

            float curY = inRect.y + 10f + topSpace;
            float labelWidth = 130f;
            float settingX = inRect.x + listWidth + spacing;

            // label
            Text.Font = GameFont.Medium;
            string label = selectedPolicy.Label;
            if (selectedPolicy.IsConst)
                label += $" ({"DoTheRitualsYourselves.UI.NotEditable".Translate()})";
            Widgets.Label(new Rect(settingX - 10f, curY, inRect.width - settingX - 30f, 30f), label);
            Text.Font = GameFont.Small;
            curY += 45f;

            float sliderWidth = inRect.width - settingX - 150f;

            // min pawn
            var rect1 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect1, "DoTheRitualsYourselves.UI.Policy.MinPawnCount.Tip".Translate());
            Widgets.Label(rect1, "DoTheRitualsYourselves.UI.Policy.MinPawnCount".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            float pawnSlider = Widgets.HorizontalSlider(
                new Rect(settingX + labelWidth, curY, sliderWidth, 24f),
                selectedPolicy.minPawnCount, 1, 50, true, selectedPolicy.minPawnCount.ToString(), roundTo: 1f);
            selectedPolicy.minPawnCount = (int)pawnSlider;
            GUI.enabled = true;
            curY += 35f;

            // max non role
            var rectMaxNonRole = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rectMaxNonRole, "DoTheRitualsYourselves.UI.Policy.MaxNonRoleCount.Tip".Translate());
            Widgets.Label(rectMaxNonRole, "DoTheRitualsYourselves.UI.Policy.MaxNonRoleCount".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            float nonRoleSlider = Widgets.HorizontalSlider(
                new Rect(settingX + labelWidth, curY, sliderWidth, 24f),
                selectedPolicy.maxNonRoleCount, 0, 50, true, selectedPolicy.maxNonRoleCount.ToString(), roundTo: 1f);
            selectedPolicy.maxNonRoleCount = (int)nonRoleSlider;
            GUI.enabled = true;
            curY += 35f;

            // mood
            var rect2 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect2, "DoTheRitualsYourselves.UI.Policy.AvgMood.Tip".Translate());
            Widgets.Label(rect2, "DoTheRitualsYourselves.UI.Policy.AvgMood".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.FloatRange(new Rect(settingX + labelWidth, curY, sliderWidth, 24f), 214578971, ref selectedPolicy.avgMood, 0f, 1f, $"{(selectedPolicy.avgMood.min * 100).ToString("F0")}-{(selectedPolicy.avgMood.max * 100).ToString("F0")}%");
            GUI.enabled = true;
            curY += 35f;

            // time
            var rect3 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect3, "DoTheRitualsYourselves.UI.Policy.Time.Tip".Translate());
            Widgets.Label(rect3, "DoTheRitualsYourselves.UI.Policy.Time".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.IntRange(new Rect(settingX + labelWidth, curY, sliderWidth, 24f), 214578972, ref selectedPolicy.time, 0, 24);
            GUI.enabled = true;
            curY += 35f;

            // invert time
            var rect4 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect4, "DoTheRitualsYourselves.UI.Policy.InvertTime.Tip".Translate());
            Widgets.Label(rect4, "DoTheRitualsYourselves.UI.Policy.InvertTime".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.Checkbox(settingX + labelWidth, curY, ref selectedPolicy.invertTime, 16);
            GUI.enabled = true;
            curY += 70f;

            // participants
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(settingX - 10f, curY, inRect.width - settingX - 30f, 30f), "DoTheRitualsYourselves.UI.Policy.Participants".Translate());
            Text.Font = GameFont.Small;
            curY += 45f;

            // min health
            var rect5 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect5, "DoTheRitualsYourselves.UI.Policy.PawnHealth.Tip".Translate());
            Widgets.Label(rect5, "DoTheRitualsYourselves.UI.Policy.PawnHealth".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.FloatRange(new Rect(settingX + labelWidth, curY, sliderWidth, 24f), 214578973, ref selectedPolicy.pawnHealth, 0f, 1f, $"{(selectedPolicy.pawnHealth.min * 100).ToString("F0")}-{(selectedPolicy.pawnHealth.max * 100).ToString("F0")}%");
            GUI.enabled = true;
            curY += 35f;

            // min health
            var rect6 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect6, "DoTheRitualsYourselves.UI.Policy.PawnMood.Tip".Translate());
            Widgets.Label(rect6, "DoTheRitualsYourselves.UI.Policy.PawnMood".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.FloatRange(new Rect(settingX + labelWidth, curY, sliderWidth, 24f), 214578974, ref selectedPolicy.pawnMood, 0f, 1f, $"{(selectedPolicy.pawnMood.min * 100).ToString("F0")}-{(selectedPolicy.pawnMood.max * 100).ToString("F0")}%");
            GUI.enabled = true;
            curY += 35f;

            // except resting
            var rect7 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect7, "DoTheRitualsYourselves.UI.Policy.ExceptRestring.Tip".Translate());
            Widgets.Label(rect7, "DoTheRitualsYourselves.UI.Policy.ExceptRestring".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.Checkbox(settingX + labelWidth, curY, ref selectedPolicy.exceptResting, 16);
            GUI.enabled = true;
            curY += 35f;

            // allow colonist
            var rect8 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect8, "DoTheRitualsYourselves.UI.Policy.AllowColonist.Tip".Translate());
            Widgets.Label(rect8, "DoTheRitualsYourselves.UI.Policy.AllowColonist".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.Checkbox(settingX + labelWidth, curY, ref selectedPolicy.allowColonist, 16);
            GUI.enabled = true;
            curY += 35f;

            // allow slave
            var rect9 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect9, "DoTheRitualsYourselves.UI.Policy.AllowSlave.Tip".Translate());
            Widgets.Label(rect9, "DoTheRitualsYourselves.UI.Policy.AllowSlave".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.Checkbox(settingX + labelWidth, curY, ref selectedPolicy.allowSlave, 16);
            GUI.enabled = true;
            curY += 35f;

            // allow other ideo
            var rect10 = new Rect(settingX, curY, labelWidth, 24f);
            TooltipHandler.TipRegion(rect10, "DoTheRitualsYourselves.UI.Policy.AllowOtherIdeo.Tip".Translate());
            Widgets.Label(rect10, "DoTheRitualsYourselves.UI.Policy.AllowOtherIdeo".Translate());
            GUI.enabled = !selectedPolicy.IsConst;
            Widgets.Checkbox(settingX + labelWidth, curY, ref selectedPolicy.allowOtherIdeo, 16);
            GUI.enabled = true;
            curY += 35f;

            // delete, rename
            if (!selectedPolicy.IsConst)
            {
                float buttonY = inRect.yMax - 40f;
                float buttonSize = 30f;
                float iconSpacing = 4f;
                float rightAlignX = inRect.xMax - buttonSize - iconSpacing;

                TooltipHandler.TipRegion(new Rect(rightAlignX, buttonY, buttonSize, buttonSize), "DoTheRitualsYourselves.UI.Delete".Translate());
                TooltipHandler.TipRegion(new Rect(rightAlignX - buttonSize - iconSpacing, buttonY, buttonSize, buttonSize), "DoTheRitualsYourselves.UI.Rename".Translate());

                if (Widgets.ButtonImage(new Rect(rightAlignX - buttonSize - iconSpacing, buttonY, buttonSize, buttonSize), TexButton.Rename))
                    Find.WindowStack.Add(new Dialog_RenameRitualPolicy(selectedPolicy));

                if (Widgets.ButtonImage(new Rect(rightAlignX, buttonY, buttonSize, buttonSize), TexButton.Delete))
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("DoTheRitualsYourselves.UI.DeleteConfirm".Translate(), () =>
                    {
                        Messages.Message("DoTheRitualsYourselves.Message.DeleteRitualPolicy".Translate(selectedPolicy.Label), MessageTypeDefOf.NeutralEvent);
                        WorldComponent_RitualPolicy.Instance.RemovePolicy(selectedPolicy);
                        selectedPolicy = null;
                    }));
                }
            }
        }
    }
}

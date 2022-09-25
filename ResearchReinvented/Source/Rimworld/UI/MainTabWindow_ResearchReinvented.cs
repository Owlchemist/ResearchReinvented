﻿using PeteTimesSix.ResearchReinvented.Defs;
using PeteTimesSix.ResearchReinvented.Extensions;
using PeteTimesSix.ResearchReinvented.Managers;
using PeteTimesSix.ResearchReinvented.Opportunities;
using PeteTimesSix.ResearchReinvented.OpportunityComps;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.ResearchReinvented.Rimworld.UI
{
    class MainTabWindow_ResearchReinvented : MainTabWindow
    {
        private List<ResearchOpportunityCategoryDef> collapsedCategories = new List<ResearchOpportunityCategoryDef>();
        private bool compactMode = false;

        public Vector2 scrollPos = new Vector2(0f, 0f);
        public float innerRectSizeCache = 0f;

        public const float COMPACTMODE_BUTTON_WIDTH = 100f;
        public const float COLLAPSE_BUTTON_WIDTH = 100f;
        public const float CLOSEBUTTON_BOUNDING_BOX_SIZE = 26f - MARGIN;
        public const float TITLEBAR_HEIGHT = 30f;
        public const float FOOTER_HEIGHT = 30f;
        public const float HEADER_ROW_HEIGHT = 25f;
        public const float ROW_HEIGHT = 40f;
        public const float ROW_GAP = 2f;
        public const float ICON_GAP = 5f;
        public const float ICON_SIZE = ROW_HEIGHT;
        public const float ICON_LARGE_SIZE = 62f;
        public const float MARGIN = 5f;

        public static Color DARKEN_COLOR = new Color(0f, 0f, 0f, 0.25f);

        public static Color UNAVAILABLE_FINISHED = new Color(0.7f, 1f, 0.7f, 1f);
        public static Color UNAVAILABLE_BLOCKED = new Color(1.0f, 0.5f, 0.5f, 1f);

        protected override float Margin => MARGIN;

        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(((ICON_LARGE_SIZE + ROW_GAP) * 6f) + (Margin * 2f) + 20f, 700f);
            }
        }

        public MainTabWindow_ResearchReinvented() 
        {
            this.doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var cachedStyle = Text.Font;
            var cachedAnchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var fullRect = inRect;
            //Rect fullRect = new Rect(0f, TITLEBAR_HEIGHT, this.size.x, this.size.y - TITLEBAR_HEIGHT).Rounded();
            IReadOnlyCollection<ResearchOpportunityCategoryDef> opportunityCategories = ResearchOpportunityManager.instance.AllCurrentOpportunityCategories;
            IReadOnlyCollection<ResearchOpportunity> opportunities = ResearchOpportunityManager.instance.AllCurrentOpportunities;

            var titlebarRect = fullRect.TopPartPixels(TITLEBAR_HEIGHT);
            var footerRect = fullRect.BottomPartPixels(FOOTER_HEIGHT);
            var contentRect = new Rect(fullRect);
            contentRect.y = TITLEBAR_HEIGHT + 1f;
            contentRect.height -= (TITLEBAR_HEIGHT + 2f + FOOTER_HEIGHT);

            var titlebarCentralRect = new Rect(titlebarRect);
            titlebarCentralRect.width -= CLOSEBUTTON_BOUNDING_BOX_SIZE * 2; //account for the close button
            titlebarCentralRect.x += CLOSEBUTTON_BOUNDING_BOX_SIZE;

            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(titlebarCentralRect, Find.ResearchManager.currentProj != null ? Find.ResearchManager.currentProj.LabelCap : "RR_no_project_selected".Translate());

            Text.Anchor = TextAnchor.MiddleLeft;
            if (Widgets.ButtonText(titlebarRect.LeftPartPixels(COMPACTMODE_BUTTON_WIDTH), compactMode ? "RR_disable_compact_mode".Translate() : "RR_enable_compact_mode".Translate()))
            {
                compactMode = !compactMode;
            }

            Widgets.DrawLineHorizontal(titlebarRect.x, titlebarRect.y + titlebarRect.height, titlebarRect.width);
            DrawOpportunitiesList(contentRect, ResearchOpportunityManager.instance.CurrentProject, opportunityCategories, opportunities);

            if (DebugSettings.godMode) 
            {
                if (GUI.Button(footerRect.LeftPartPixels(120f), "DEBUG:Regen"))
                {
                    ResearchOpportunityManager.instance.GenerateOpportunities(Find.ResearchManager.currentProj, true);
                }
            }

            Text.Font = cachedStyle;
            Text.Anchor = cachedAnchor;
        }

        private void DrawOpportunitiesList(Rect listRect, ResearchProjectDef project, IReadOnlyCollection<ResearchOpportunityCategoryDef> opportunityCategories, IReadOnlyCollection<ResearchOpportunity> opportunities)
        {
            Rect internalRect = new Rect(listRect.x, listRect.y, listRect.width, listRect.height).Rounded();

            internalRect.height = innerRectSizeCache;
            if (listRect.height < internalRect.height) 
                internalRect.width -= 20f; //clear space for scrollbar

            Widgets.BeginScrollView(listRect, ref scrollPos, internalRect);

            float heightTotal = 0f;

            foreach (var opportunityCategory in opportunityCategories.OrderByDescending(c => c.priority))
            {
                var matchingOpportunitites = opportunities.Where(o => o.def.GetCategory(o.relation) == opportunityCategory);
                if (matchingOpportunitites.Any())
                {
                    DrawOpportunityCategory(listRect, internalRect, ref heightTotal, project, opportunityCategory, matchingOpportunitites);
                }
            }
            innerRectSizeCache = heightTotal;

            Widgets.EndScrollView();
        }

        private void DrawOpportunityCategory(Rect wrapperRect, Rect internalRect, ref float heightTotal, ResearchProjectDef project, ResearchOpportunityCategoryDef category, IEnumerable<ResearchOpportunity> matchingOpportunitites)
        {
            var totalsStore = ResearchOpportunityManager.instance.GetTotalsStore(project, category);

            Rect headerRect = new Rect(internalRect.x, internalRect.y + heightTotal, internalRect.width, HEADER_ROW_HEIGHT).Rounded();
            //Rect textRectStart = new Rect(internalRect.x + ICON_SIZE + ICON_GAP, internalRect.y + heightTotal, internalRect.width - (ICON_SIZE + ICON_GAP), ROW_HEIGHT).Rounded();
            //Rect iconRectStart = new Rect(internalRect.x, internalRect.y + heightTotal, ICON_SIZE, ICON_SIZE).Rounded();

            //var categoryProgressFraction = ResearchOpportunityManager.instance.GetCategoryProgressFraction(opportunityCategory);
            var progressRect = new Rect(headerRect).ContractedBy(2f).Rounded();
            //progressRect.width *= categoryProgress;
            //Widgets.DrawBoxSolid(progressRect, progressColor);

            bool collapsed = collapsedCategories.Contains(category);

            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(headerRect, category.LabelCap);
            Text.Anchor = TextAnchor.LowerRight;
            if (!category.infiniteOverflow)
            {
                //{Progress} "X"
                Widgets_Extra.LabelFitHeightAware(headerRect, $"{Math.Round(category.GetCurrentTotal(), 0)} / {Math.Round(totalsStore.allResearchPoints, 0)}");
            }
            else
            {
                //{Progress} "X / Y"
                Widgets_Extra.LabelFitHeightAware(headerRect, $"{Math.Round(category.GetCurrentTotal(), 0)}");
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            if(Widgets.ButtonText(headerRect.LeftPartPixels(COLLAPSE_BUTTON_WIDTH), collapsed ? "RR_uncollapse_category".Translate() : "RR_collapse_category".Translate()))
            {
                if(collapsed)
                    collapsedCategories.Remove(category);
                else
                    collapsedCategories.Add(category);
            }

            heightTotal += headerRect.height + ROW_GAP;
            Widgets.DrawLineHorizontal(headerRect.x + 1f, headerRect.y + headerRect.height, headerRect.width - 2f);

            if (!collapsed)
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                bool odd = true;

                Rect startPosition = new Rect(headerRect.x, headerRect.y + headerRect.height + ROW_GAP, internalRect.width, 0).Rounded();
                float heightTotalLocal = 0f;
                float horizontalOffset = 0f;

                foreach (var opportunity in matchingOpportunitites.OrderByDescending(o => o.MaximumProgress))
                {
                    if (compactMode)
                        DrawOpportunityEntryCompact(wrapperRect, startPosition, ref heightTotalLocal, ref horizontalOffset, odd, opportunity);
                    else
                        DrawOpportunityEntry(wrapperRect, startPosition, ref heightTotalLocal, odd, opportunity);
                    odd = !odd;
                }

                //need to newline in compact mode (but only if we didnt JUST newline)
                if (compactMode && horizontalOffset != 0f)
                {
                    heightTotalLocal += ICON_LARGE_SIZE + ROW_GAP;
                }

                heightTotal += heightTotalLocal;
            }
        }

        private void DrawOpportunityEntryCompact(Rect wrapperRect, Rect templateRect, ref float heightTotal, ref float horizontalOffset, bool odd, ResearchOpportunity opportunity)
        {
            Rect fullRect = new Rect(templateRect.x + horizontalOffset, templateRect.y + heightTotal, ICON_LARGE_SIZE, ICON_LARGE_SIZE).Rounded();

            if (fullRect.y + fullRect.height - scrollPos.y >= wrapperRect.y && fullRect.y - scrollPos.y <= wrapperRect.y + wrapperRect.height)
            {
                Color borderColor = odd ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
                Color bgColor = odd ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
                Color progressColor = odd ? new Color(0.2f, 0.2f, 0.8f) : new Color(0.25f, 0.25f, 0.8f);

                Widgets.DrawBoxSolid(fullRect, borderColor);
                Widgets.DrawBoxSolid(fullRect.ContractedBy(2f).Rounded(), bgColor);
                Rect iconBoxInner = fullRect.ContractedBy(5f).Rounded();

                var progressRect = new Rect(fullRect).ContractedBy(2f).Rounded();
                progressRect.width *= opportunity.ProgressFraction;
                Widgets.DrawBoxSolid(progressRect, progressColor);

                DrawIconForOpportunity(opportunity, iconBoxInner);
                Widgets.DrawBoxSolid(fullRect.ContractedBy(2f).Rounded(), DARKEN_COLOR);

                Rect textBoxInternal = fullRect.ContractedBy(2f, 0f).Rounded();

                Text.Anchor = TextAnchor.UpperCenter;
                //{Requirements description (usually ThingDef name)}
                var labelBox = textBoxInternal.TopPart(0.7f).Rounded();
                labelBox.height = (float)(Text.LineHeightOf(GameFont.Tiny) * Math.Ceiling(labelBox.height / Text.LineHeightOf(GameFont.Tiny))) + 1f;
                Widgets_Extra.LabelFitHeightAware(labelBox, $"{opportunity.requirement.ShortDesc.CapitalizeFirst()}");

                if (ResearchReinventedMod.Settings.debugPrintouts)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = Color.green;
                    if (opportunity.IsAlternate)
                    {
                        GUI.DrawTexture(textBoxInternal, TexUI.GrayTextBG);
                        Widgets_Extra.LabelFitHeightAware(textBoxInternal, $"ALT");
                    }
                    GUI.color = Color.white;
                }

                Text.Anchor = TextAnchor.LowerCenter;
                if (!opportunity.def.GetCategory(opportunity.relation).infiniteOverflow)
                {
                    //{Progress} "X.x%"
                    Widgets_Extra.LabelFitHeightAware(textBoxInternal, $"{Math.Round(opportunity.ProgressFraction * 100, 1)}%");
                }
                else
                {
                    //{Progress} "X / Y"
                    Widgets_Extra.LabelFitHeightAware(textBoxInternal, $"{Math.Round(opportunity.Progress, 0)} / {Math.Round(opportunity.MaximumProgress, 0)}");
                }

                if (opportunity.CurrentAvailability != OpportunityAvailability.Available)
                    DoUnavailabilityLabel(opportunity.CurrentAvailability, textBoxInternal.ContractedBy(2f), true);

                Text.Anchor = TextAnchor.MiddleLeft;
            }

            horizontalOffset += ICON_LARGE_SIZE + ROW_GAP;
            if (horizontalOffset + ICON_LARGE_SIZE > templateRect.width)
            {
                heightTotal += ICON_LARGE_SIZE + ROW_GAP;
                horizontalOffset = 0;
            }
        }


        private void DrawOpportunityEntry(Rect wrapperRect, Rect templateRect, ref float heightTotal, bool odd, ResearchOpportunity opportunity)
        {
            Rect fullRect = new Rect(templateRect.x, templateRect.y + heightTotal, templateRect.width, ROW_HEIGHT).Rounded();

            if (fullRect.y + fullRect.height - scrollPos.y >= wrapperRect.y && fullRect.y - scrollPos.y <= wrapperRect.y + wrapperRect.height)
            {
                Color borderColor = odd ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
                Color bgColor = odd ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.25f, 0.25f, 0.25f);
                Color progressColor = odd ? new Color(0.2f, 0.2f, 0.8f) : new Color(0.25f, 0.25f, 0.8f);
                Rect textRect = new Rect(templateRect.x + ICON_SIZE + ICON_GAP, templateRect.y + heightTotal, templateRect.width - (ICON_SIZE + ICON_GAP), ROW_HEIGHT).Rounded();
                Rect iconRect = new Rect(templateRect.x, templateRect.y + heightTotal, ICON_SIZE, ICON_SIZE).Rounded();

                Widgets.DrawBoxSolid(iconRect, borderColor);
                Widgets.DrawBoxSolid(iconRect.ContractedBy(2f).Rounded(), bgColor);
                Rect iconBoxInner = iconRect.ContractedBy(5f).Rounded();

                DrawIconForOpportunity(opportunity, iconBoxInner);

                Widgets.DrawBoxSolid(textRect, borderColor);
                Widgets.DrawBoxSolid(textRect.ContractedBy(2f).Rounded(), bgColor);
                var progressRect = new Rect(textRect).ContractedBy(2f).Rounded();
                progressRect.width *= opportunity.ProgressFraction;
                Widgets.DrawBoxSolid(progressRect, progressColor);
                Rect textBoxInternal = textRect.ContractedBy(2f, 0f).Rounded();


                Text.Anchor = TextAnchor.MiddleLeft;

                //{Opportunity name}
                Widgets_Extra.LabelFitHeightAware(textBoxInternal.TopHalf().Rounded(), $"{opportunity.def.GetHeaderCap(opportunity.relation)}");
                //{Requirements description (usually ThingDef name)}
                Widgets_Extra.LabelFitHeightAware(textBoxInternal.BottomHalf().Rounded(), $"{opportunity.requirement.ShortDesc.CapitalizeFirst()}");

                    if (ResearchReinventedMod.Settings.debugPrintouts)
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        GUI.color = Color.green;
                        if (opportunity.IsAlternate)
                        {
                            GUI.DrawTexture(textBoxInternal.TopHalf().RightHalf(), TexUI.GrayTextBG);
                            Widgets_Extra.LabelFitHeightAware(textBoxInternal.TopHalf().RightHalf(), $"ALT");
                        }
                        GUI.DrawTexture(textBoxInternal.TopHalf().LeftHalf(), TexUI.GrayTextBG);
                        Widgets_Extra.LabelFitHeightAware(textBoxInternal.TopHalf().LeftHalf(), $"{opportunity.relation}");
                        GUI.DrawTexture(textBoxInternal.BottomHalf(), TexUI.GrayTextBG);
                        Widgets_Extra.LabelFitHeightAware(textBoxInternal.BottomHalf(), $"{opportunity.debug_source}");
                        GUI.color = Color.white;
                    }

                Text.Anchor = TextAnchor.MiddleRight;
                //{Progress} "X.x%"
                if (!opportunity.def.GetCategory(opportunity.relation).infiniteOverflow)
                    Widgets_Extra.LabelFitHeightAware(textBoxInternal.BottomHalf().Rounded(), $"{Math.Round(opportunity.ProgressFraction * 100, 1)}%");

                //{Progress} "X / Y"
                Widgets_Extra.LabelFitHeightAware(textBoxInternal.TopHalf().Rounded(), $"{Math.Round(opportunity.Progress, 0)} / {Math.Round(opportunity.MaximumProgress, 0)}");


                if (opportunity.CurrentAvailability != OpportunityAvailability.Available)
                    DoUnavailabilityLabel(opportunity.CurrentAvailability, fullRect.ContractedBy(2f), false);

                Text.Anchor = TextAnchor.MiddleLeft;

            }

            heightTotal += ROW_HEIGHT + ROW_GAP;
        }

        private void DrawIconForOpportunity(ResearchOpportunity opportunity, Rect iconBox)
        {
            Def onlyDef = null;
            if (opportunity.requirement is ROComp_RequiresThing requiresThingComp)
            {
                Widgets.DefIcon(iconBox, requiresThingComp.thingDef);
                onlyDef = requiresThingComp.thingDef;
            }
            else if (opportunity.requirement is ROComp_RequiresIngredients requiresIngredientsComp)
            {
                var ingredients = requiresIngredientsComp.ingredients;
                for (int i = 0; i < ingredients.Count; i++)
                {
                    var ingredient = ingredients[i];
                    if (ingredient.Value.IsFixedIngredient)
                    {
                        Widgets.DefIcon(iconBox, ingredient.Value.FixedIngredient);
                    }
                    else
                    {
                        var ingredientBox = iconBox.ContractedBy(ICON_SIZE / 4f).OffsetBy(
                            (float)(ICON_SIZE / 4 * Math.Sin(i / ingredients.Count * Math.PI)),
                            (float)(ICON_SIZE / 4 * Math.Cos(i / ingredients.Count * Math.PI))).Rounded();
                        Widgets.DefIcon(ingredientBox, ingredient.Value.FixedIngredient);
                    }
                }
            }
            else if (opportunity.requirement is ROComp_RequiresTerrain requiresTerrainComp)
            {
                Widgets.DefIcon(iconBox, requiresTerrainComp.terrainDef);
                onlyDef = requiresTerrainComp.terrainDef;
            }
            else
            {
                Widgets.DrawTextureFitted(iconBox, Textures.scienceIconDark, 1f);
            }

            if (onlyDef != null)
            {
                if (Widgets.ButtonInvisible(iconBox))
                    Find.WindowStack.Add(new Dialog_InfoCard(onlyDef));
            }
        }
        private static void DoUnavailabilityLabel(OpportunityAvailability availability, Rect textBox, bool compact)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            string reason;
            switch (availability)
            {
                case OpportunityAvailability.Finished:
                    GUI.color = UNAVAILABLE_FINISHED;
                    reason = compact ? "RR_OpportunityBlocked_Finished_short" : "RR_OpportunityBlocked_Finished";
                    break;
                case OpportunityAvailability.CategoryFinished:
                    GUI.color = UNAVAILABLE_FINISHED;
                    reason = compact ? "RR_OpportunityBlocked_CategoryFinished_short" : "RR_OpportunityBlocked_CategoryFinished";
                    break;
                case OpportunityAvailability.ResearchTooLow:
                    GUI.color = UNAVAILABLE_BLOCKED;
                    reason = compact ? "RR_OpportunityBlocked_ResearchTooLow_short" : "RR_OpportunityBlocked_ResearchTooLow";
                    break;
                case OpportunityAvailability.ResearchTooHigh:
                    GUI.color = UNAVAILABLE_BLOCKED;
                    reason = compact ? "RR_OpportunityBlocked_ResearchTooHigh_short" : "RR_OpportunityBlocked_ResearchTooHigh";
                    break;
                case OpportunityAvailability.UnavailableReasonUnknown:
                default:
                    GUI.color = UNAVAILABLE_BLOCKED;
                    reason = compact ? "RR_OpportunityBlocked_ReasonUnknown_short" : "RR_OpportunityBlocked_ReasonUnknown";
                    break;
            }
            reason = reason.Translate();
            GUI.DrawTexture(textBox, TexUI.GrayTextBG);
            Widgets_Extra.LabelFitHeightAware(textBox, reason);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleLeft;
        }
    }
}

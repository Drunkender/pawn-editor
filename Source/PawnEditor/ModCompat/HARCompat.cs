using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[ModCompat("erdelf.humanoidalienraces", "erdelf.humanoidalienraces.dev")]
// ReSharper disable once InconsistentNaming
public static class HARCompat
{
    public static bool Active;
    public static bool EnforceRestrictions = true;

    private static Action<Rect> doRaceTabs;
    private static Action<Pawn> constructorPostfix;
    private static Func<IEnumerable<HeadTypeDef>, Pawn, IEnumerable<HeadTypeDef>> headTypeFilter;
    private static Type thingDef_AlienRace;
    private static AccessTools.FieldRef<object, object> alienRace;
    private static AccessTools.FieldRef<object, object> alienPartGenerator;
    private static AccessTools.FieldRef<object, object> generalSettings;

    private static AccessTools.FieldRef<object, List<BodyTypeDef>> bodyTypes;
    private static AccessTools.FieldRef<object, object> styleSettings;
    private static Func<object, StyleItemDef, Pawn, bool, bool> isValidStyle;

    private static Func<ThingDef, ThingDef, bool> canWear;
    private static Func<ThingDef, ThingDef, bool> canEquip;
    private static Func<TraitDef, Pawn, int, bool> canGetTrait;
    private static Func<GeneDef, ThingDef, bool, bool> canHaveGene;
    private static Func<XenotypeDef, ThingDef, bool> canUseXenotype;

    public static string Name = "Humanoid Alien Races";

    [UsedImplicitly]
    public static void Activate()
    {
        try
        {
            var stylingStation = AccessTools.TypeByName("AlienRace.StylingStation");
            if (stylingStation == null) return;
            var doRaceTabsMi = AccessTools.Method(stylingStation, "DoRaceTabs", new[] { typeof(Rect) });
            var constructorPostfixMi = AccessTools.Method(stylingStation, "ConstructorPostfix", new[] { typeof(Pawn) });
            if (doRaceTabsMi == null || constructorPostfixMi == null) return;
            doRaceTabs = doRaceTabsMi.CreateDelegate<Action<Rect>>();
            constructorPostfix = constructorPostfixMi.CreateDelegate<Action<Pawn>>();

            var patches = AccessTools.TypeByName("AlienRace.HarmonyPatches");
            if (patches == null) return;
            var headTypeFilterMi = AccessTools.Method(patches, "HeadTypeFilter");
            if (headTypeFilterMi == null) return;
            headTypeFilter = headTypeFilterMi.CreateDelegate<Func<IEnumerable<HeadTypeDef>, Pawn, IEnumerable<HeadTypeDef>>>();

            thingDef_AlienRace = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");
            if (thingDef_AlienRace == null) return;
            alienRace = AccessTools.FieldRefAccess<object>(thingDef_AlienRace, "alienRace");
            var alienSettings = AccessTools.Inner(thingDef_AlienRace, "AlienSettings");
            if (alienSettings == null) return;
            generalSettings = AccessTools.FieldRefAccess<object>(alienSettings, "generalSettings");

            var generalSettingsType = AccessTools.TypeByName("AlienRace.GeneralSettings");
            var alienPartGeneratorType = AccessTools.TypeByName("AlienRace.AlienPartGenerator");
            if (generalSettingsType == null || alienPartGeneratorType == null) return;
            alienPartGenerator = AccessTools.FieldRefAccess<object>(generalSettingsType, "alienPartGenerator");
            bodyTypes = AccessTools.FieldRefAccess<List<BodyTypeDef>>(alienPartGeneratorType, "bodyTypes");

            styleSettings = AccessTools.FieldRefAccess<object>(alienSettings, "styleSettings");
            var styleSettingsType = AccessTools.TypeByName("AlienRace.StyleSettings");
            var isValidStyleMi = AccessTools.Method(styleSettingsType, "IsValidStyle");
            if (isValidStyleMi == null) return;
            isValidStyle = isValidStyleMi.CreateDelegateCasting<Func<object, StyleItemDef, Pawn, bool, bool>>();

            var raceRestrictionSettings = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
            if (raceRestrictionSettings == null) return;
            var canWearMi = AccessTools.Method(raceRestrictionSettings, "CanWear");
            var canEquipMi = AccessTools.Method(raceRestrictionSettings, "CanEquip");
            var canGetTraitMi = AccessTools.Method(raceRestrictionSettings, "CanGetTrait", new[] { typeof(TraitDef), typeof(Pawn), typeof(int) });
            var canHaveGeneMi = AccessTools.Method(raceRestrictionSettings, "CanHaveGene");
            var canUseXenotypeMi = AccessTools.Method(raceRestrictionSettings, "CanUseXenotype");
            if (canWearMi == null || canEquipMi == null || canGetTraitMi == null || canHaveGeneMi == null || canUseXenotypeMi == null) return;
            canWear = canWearMi.CreateDelegate<Func<ThingDef, ThingDef, bool>>();
            canEquip = canEquipMi.CreateDelegate<Func<ThingDef, ThingDef, bool>>();
            canGetTrait = canGetTraitMi.CreateDelegate<Func<TraitDef, Pawn, int, bool>>();
            canHaveGene = canHaveGeneMi.CreateDelegate<Func<GeneDef, ThingDef, bool, bool>>();
            canUseXenotype = canUseXenotypeMi.CreateDelegate<Func<XenotypeDef, ThingDef, bool>>();

            Active = true;
        }
        catch (Exception e)
        {
            Log.WarningOnce($"[Pawn Editor] HAR compatibility failed to activate: {e}", 1963432410);
            Active = false;
        }
    }

    public static void Notify_AppearanceEditorOpen(Pawn pawn)
    {
        if (!Active || pawn == null || constructorPostfix == null) return;
        try { constructorPostfix(pawn); }
        catch (Exception e) { Log.WarningOnce($"[Pawn Editor] HAR Notify_AppearanceEditorOpen failed: {e}", 1963432411); }
    }

    public static void DoRaceTabs(Rect inRect)
    {
        if (!Active || doRaceTabs == null) return;
        try { doRaceTabs(inRect); }
        catch (Exception e) { Log.WarningOnce($"[Pawn Editor] HAR DoRaceTabs failed: {e}", 1963432412); }
    }

    public static IEnumerable<HeadTypeDef> FilterHeadTypes(IEnumerable<HeadTypeDef> headTypes, Pawn pawn)
    {
        if (!Active || headTypeFilter == null) return headTypes;
        try { return headTypeFilter(headTypes, pawn); }
        catch (Exception e)
        {
            Log.WarningOnce($"[Pawn Editor] HAR FilterHeadTypes failed: {e}", 1963432413);
            return headTypes;
        }
    }

    public static List<BodyTypeDef> AllowedBodyTypes(Pawn pawn)
    {
        if (!Active || pawn == null || pawn.def == null || thingDef_AlienRace == null) return null;
        if (thingDef_AlienRace.IsInstanceOfType(pawn.def))
        {
            if (alienRace == null || generalSettings == null || alienPartGenerator == null || bodyTypes == null) return null;
            var obj = alienRace(pawn.def);
            if (obj == null) return null;
            obj = generalSettings(obj);
            if (obj == null) return null;
            obj = alienPartGenerator(obj);
            if (obj == null) return null;
            return bodyTypes(obj);
        }

        return null;
    }

    public static bool AllowStyleItem(StyleItemDef item, Pawn pawn)
    {
        if (!Active || pawn == null || pawn.def == null || thingDef_AlienRace == null) return true;
        if (thingDef_AlienRace.IsInstanceOfType(pawn.def))
        {
            if (alienRace == null || styleSettings == null || isValidStyle == null) return true;
            var obj = alienRace(pawn.def);
            if (obj == null) return true;
            var settings = styleSettings(obj);
            if (settings is not Dictionary<Type, object> dict || !dict.TryGetValue(item.GetType(), out var typeSettings)) return true;
            return isValidStyle(typeSettings, item, pawn, false);
        }

        return true;
    }

    public static bool CanWear(ThingDef apparel, Pawn pawn) => Active && canWear != null && pawn?.def != null && canWear(apparel, pawn.def);
    public static bool CanEquip(ThingDef weapon, Pawn pawn) => Active && canEquip != null && pawn?.def != null && canEquip(weapon, pawn.def);
    public static bool CanGetTrait(ListingMenu_Trait.TraitInfo trait, Pawn pawn) => Active && canGetTrait != null && trait?.Trait?.def != null && canGetTrait(trait.Trait.def, pawn, trait.Trait.degree);
    public static bool CanHaveGene(GeneDef gene, Pawn pawn) => Active && canHaveGene != null && pawn?.def != null && canHaveGene(gene, pawn.def, false);
    public static bool CanUseXenotype(XenotypeDef xenotype, Pawn pawn) => Active && canUseXenotype != null && pawn?.def != null && canUseXenotype(xenotype, pawn.def);
}
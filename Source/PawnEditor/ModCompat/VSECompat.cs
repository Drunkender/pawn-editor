using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[ModCompat("vanillaexpanded.skills")]
public static class VSECompat
{
    public static bool Active;
    public static string Name = "Vanilla Skills Expanded";

    private static Type passionManager;
    private static Func<Passion, object> passionToDef;
    private static FieldInfo passionDefArray;
    
    private static Type passionUtilities;
    private static Func<Passion, int, Passion> changePassion;
    
    private static Type learnRateFactorCache;
    private static Action<SkillRecord, Passion?> clearCacheFor;

    private static Type passionDefType;
    private static PropertyInfo iconProperty;
    private static FieldInfo labelField;
    private static FieldInfo indexField;
    
    public static void Activate()
    {
        try
        {
            passionManager = AccessTools.TypeByName("VSE.Passions.PassionManager");
            if (passionManager == null) return;
            var passionToDefMi = AccessTools.Method(passionManager, "PassionToDef");
            passionDefArray = AccessTools.Field(passionManager, "Passions");
            if (passionToDefMi == null || passionDefArray == null) return;
            passionToDef = passionToDefMi.CreateDelegate<Func<Passion, object>>();

            passionUtilities = AccessTools.TypeByName("VSE.Passions.PassionUtilities");
            if (passionUtilities == null) return;
            var changePassionMi = AccessTools.Method(passionUtilities, "ChangePassion");
            if (changePassionMi == null) return;
            changePassion = changePassionMi.CreateDelegate<Func<Passion, int, Passion>>();

            learnRateFactorCache = AccessTools.TypeByName("VSE.Passions.LearnRateFactorCache");
            if (learnRateFactorCache == null) return;
            var clearCacheForMi = AccessTools.Method(learnRateFactorCache, "ClearCacheFor");
            if (clearCacheForMi == null) return;
            clearCacheFor = clearCacheForMi.CreateDelegate<Action<SkillRecord, Passion?>>();

            passionDefType = AccessTools.TypeByName("VSE.Passions.PassionDef");
            if (passionDefType == null) return;
            iconProperty = AccessTools.Property(passionDefType, "Icon");
            labelField = AccessTools.Field(passionDefType, "label");
            indexField = AccessTools.Field(passionDefType, "index");
            if (iconProperty == null || labelField == null || indexField == null) return;

            Active = true;
        }
        catch (Exception e)
        {
            Log.WarningOnce($"[Pawn Editor] VSE compatibility failed to activate: {e}", 1963432420);
            Active = false;
        }
    }

    public static Texture2D GetPassionIcon(Passion passion)
    {
        if (!Active || passionToDef == null || iconProperty == null) return null;
        try
        {
            var passionDef = passionToDef(passion);
            return (Texture2D)iconProperty.GetValue(passionDef);
        }
        catch (Exception e)
        {
            Log.WarningOnce($"[Pawn Editor] VSE GetPassionIcon failed: {e}", 1963432421);
            return null;
        }
    }

    public static Passion ChangePassion(Passion passion) => !Active || changePassion == null ? passion : changePassion(passion, 1);
    public static void ClearCacheFor(SkillRecord sr, Passion passion)
    {
        if (!Active || clearCacheFor == null) return;
        clearCacheFor(sr, passion);
    }

    public static void AddPassionPresets(List<FloatMenuOption> floatMenuOptions, Pawn pawn)
    {
        if (!Active || floatMenuOptions == null || passionDefArray == null || labelField == null || indexField == null) return;
        try
        {
            var passionDefs = passionDefArray.GetValue(null) as Array;
            if (passionDefs == null) return;
            foreach (var passionDef in passionDefs)
            {
                var label = (string)labelField.GetValue(passionDef);
                var index = (ushort)indexField.GetValue(passionDef);
                floatMenuOptions.Add(new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), label),
                    TabWorker_Bio_Humanlike.GetSetDelegate(pawn, true, index)));
            }
        }
        catch (Exception e)
        {
            Log.WarningOnce($"[Pawn Editor] VSE AddPassionPresets failed: {e}", 1963432422);
        }
    }
}
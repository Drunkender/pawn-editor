using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[ModCompat("Nals.FacialAnimation")]
public static class FacialAnimCompat
{
    public static bool Active;
    public static string Name = "Facial Animations";
    private static Type faceTypeDef;
    public static List<Def> FaceTypeDefs;


    [UsedImplicitly]
    public static void Activate()
    {
        try
        {
            faceTypeDef = AccessTools.TypeByName("FacialAnimation.FaceTypeDef");
            if (faceTypeDef == null) return;
            FaceTypeDefs = GenDefDatabase.GetAllDefsInDatabaseForDef(faceTypeDef).ToList();
            Active = true;
        }
        catch (Exception e)
        {
            Log.WarningOnce($"[Pawn Editor] Facial Animations compatibility failed to activate: {e}", 1963432430);
            Active = false;
        }
        
        /*foreach (var typeDef in FaceTypeDefs)
        {
            // Log.Message(typeDef.defName);
        }*/
    }


}
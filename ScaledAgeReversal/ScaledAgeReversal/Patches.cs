//Do you want to use RimValiCore's functions for better debug stuff? (Only a dev should enable this.)
//Also adds some other debug stuff.
#define IS_DEBUG_WITH_RVC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterAgeScaler
{
    [StaticConstructorOnStartup]
  public static class PatchClass
    {
        //These functions are in rimvalicore, but i decided to put them here too.
        public static T GetVar<T>(string fieldName, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, object obj = null)
        {


            return (T)obj.GetType().GetField(fieldName, flags).GetValue(obj);
        }

        public static bool SetVar<T>(string fieldName, T val, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, object obj = null)
        {

            obj.GetType().GetField(fieldName, flags).SetValue(obj, val);


            return true;
        }


        
        //Because waiting is boring :v
        [DebugAction("Better Age Scaler", "Increment pawn age by +1 year",actionType = DebugActionType.ToolMapForPawns,allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetPawnAge(Pawn p)
        {
            long ageTicks = p.ageTracker.AgeBiologicalTicks;
            SetVar("ageBiologicalTicksInt", ageTicks + (3600000L), obj: p.ageTracker);

#if IS_DEBUG_WITH_RVC
            long demandAge = GetVar<long>("ageReversalDemandedAtAgeTicks", obj: p.ageTracker);
            Log.Message($"{p.Name}, age demanded at: {demandAge / 3600000L}");
            RimValiCore.RimValiUtility.InvokeMethodTMP("BirthdayBiological", p.ageTracker, new object[0]);
            RimValiCore.RimValiUtility.InvokeMethodTMP("RecalculateLifeStageIndex",p.ageTracker, new object[0]);
            RimValiCore.RimValiUtility.InvokeMethodTMP("CheckAgeReversalDemand", p.ageTracker, new object[0]);
      
#endif
        }
        [DebugAction("Better Age Scaler", "Increment pawn age by +5 years", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetPawnAgeFive(Pawn p)
        {
            long ageTicks = p.ageTracker.AgeBiologicalTicks;
            SetVar("ageBiologicalTicksInt", ageTicks + (3600000L*5), obj: p.ageTracker);
#if IS_DEBUG_WITH_RVC
            long demandAge = GetVar<long>("ageReversalDemandedAtAgeTicks", obj: p.ageTracker);
            Log.Message($"{p.Name}, age demanded at: {demandAge / 3600000L}");
            RimValiCore.RimValiUtility.InvokeMethodTMP("BirthdayBiological", p.ageTracker, new object[0]);
            RimValiCore.RimValiUtility.InvokeMethodTMP("RecalculateLifeStageIndex", p.ageTracker, new object[0]);
            RimValiCore.RimValiUtility.InvokeMethodTMP("CheckAgeReversalDemand", p.ageTracker, new object[0]);
#endif
        }


        static PatchClass()
        {
            Harmony h = new Harmony("Nesi.BetterAgeScaler.Patches");
            h.PatchAll();
            Log.Message("Better Age Scaler patches done successfully!");

        }

        [HarmonyPatch(typeof(Pawn_AgeTracker), "ResetAgeReversalDemand")]
        public static class Patch
        {
            [HarmonyPostfix]
            public static void PatchMethod(Pawn_AgeTracker.AgeReversalReason reason, bool cancelInitialization, Pawn_AgeTracker __instance)
            {
                Pawn p = GetVar<Pawn>("pawn",obj:__instance);
                if (p != null && p.MapHeld!=null)
                {
                  

                    float lifespan = p.RaceProps.lifeExpectancy;
                    
                    //Get's the race's lifespan relative to the human lifespan
                    long relativeLifespan =(long)(lifespan / ThingDefOf.Human.race.lifeExpectancy);
                    int num;

                    //Basically the vanilla operations, just re-done here to make sure we're getting a totally accurate number
                    long ageReversalDemandedAtAgeTicks = GetVar<long>("ageReversalDemandedAtAgeTicks", obj: __instance);
#if IS_DEBUG_WITH_RVC
                    Log.Message($"Before patch: {p.Name}, age demanded at: {ageReversalDemandedAtAgeTicks/ 3600000L}");
#endif
                    if (reason == Pawn_AgeTracker.AgeReversalReason.Recruited)
                    {
                        num = GetVar<IntRange>("RecruitedPawnAgeReversalDemandInDays", obj: __instance).RandomInRange;
                    }
                    else if (reason == Pawn_AgeTracker.AgeReversalReason.ViaTreatment)
                    {
                        num = 60;
                    }
                    else
                    {
                        num = GetVar<IntRange>("NewPawnAgeReversalDemandInDays", obj: __instance).RandomInRange;
                    }


                    long num2 = num * 60000;

                    long demandAge = (Math.Max(p.ageTracker.AgeBiologicalTicks*relativeLifespan, 72000000L)) + num2;
#if IS_DEBUG_WITH_RVC
                    Log.Message($"{p.Name}, age demanded at: {demandAge/ 3600000L}");
#endif
                    //Vanilla
                    if (reason == Pawn_AgeTracker.AgeReversalReason.Recruited && demandAge < ageReversalDemandedAtAgeTicks)
                    {
                        return;
                    }
                    SetVar("ageReversalDemandedAtAgeTicks", demandAge, obj: __instance);
                    SetVar("lastAgeReversalReason", reason, obj: __instance);

                    //More vanilla
                    if (cancelInitialization)
                    {
                        SetVar("initializedAgeReversalDemand", false, obj: __instance);
                    }
                   
                }
            }
        }
    }

}

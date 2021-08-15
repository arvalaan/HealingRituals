using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ARV_HealingRituals
{
	[DefOf]
	public class HediffDefOf
	{
		public static HediffDef ARV_HealingRituals_HealScars;
		public static HediffDef ARV_HealingRituals_HealBodyParts;
		public static HediffDef ARV_HealingRituals_Regrowing;
	}

	public class HediffComp_HealScars : HediffComp
	{

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			Hediff_Injury hediff_Injury = Utilities.FindPermanentInjury(base.Pawn);
			if (hediff_Injury == null)
			{
				severityAdjustment = -1;
			}

			if (base.Pawn.IsHashIntervalTick(600))
				hediff_Injury.Heal((float)0.1f * Utilities.getHealingAmount(base.Pawn));
		}
	}

	public class HediffComp_HealBodyParts : HediffComp
	{

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			Hediff hediff_Injury = Utilities.FindHealablePart(base.Pawn);
			if (hediff_Injury == null)
			{
				severityAdjustment = -1;
				return;
			}

			float healingAmount = (float)0.1f * Utilities.getHealingAmount(base.Pawn);

			if (base.Pawn.IsHashIntervalTick(600))
			{
				if (hediff_Injury.def.defName == "ARV_HealingRituals_Regrowing")
					hediff_Injury.Heal(healingAmount);
				else
				{
					if (Rand.Chance(healingAmount))
					{
						base.Pawn.health.hediffSet.hediffs.Remove(hediff_Injury);
						Hediff hediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("ARV_HealingRituals_Regrowing"), base.Pawn, hediff_Injury.Part);
						hediff.Severity = hediff_Injury.Part.def.GetMaxHealth(base.Pawn) - 1;
						hediff.TryGetComp<HediffComp_GetsPermanent>().IsPermanent = true;
						base.Pawn.health.AddHediff(hediff);
					}
				}
			}
		}
	}
	public class Utilities
	{
		public static Hediff FindHealablePart(Pawn pawn)
		{
			if (!pawn.Dead)
			{
				IEnumerable<Hediff_Injury> hediffs = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
				Func<Hediff_Injury, bool> predicate = (Hediff_Injury injury) => (injury != null && injury.def.defName == "ARV_HealingRituals_Regrowing");
				IEnumerable<Hediff_Injury> injuryList = hediffs.Where(predicate);
				if (injuryList.Count() != 0) return injuryList.ElementAt(Rand.Range(0, injuryList.Count()));

				IEnumerable<Hediff_MissingPart> hediffs2 = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
				Func<Hediff_MissingPart, bool> predicate2 = (Hediff_MissingPart injury) => (injury != null && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(injury.Part));
				IEnumerable<Hediff_MissingPart> injuryList2 = hediffs2.Where(predicate2);
				if (injuryList2.Count() != 0) return injuryList2.ElementAt(Rand.Range(0, injuryList2.Count()));
			}
			return null;
		}
		public static Hediff_Injury FindPermanentInjury(Pawn pawn, IEnumerable<BodyPartRecord> allowedBodyParts = null)
		{
			if (!pawn.Dead)
			{
				IEnumerable<Hediff_Injury> hediffs = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
				Func<Hediff_Injury, bool> predicate = (Hediff_Injury injury) => (injury != null && injury.Visible && injury.def.everCurableByItem && injury.IsPermanent());
				IEnumerable<Hediff_Injury> injuryList = hediffs.Where(predicate);
				if (injuryList.Count() == 0) return null;
				return injuryList.ElementAt(Rand.Range(0, injuryList.Count()));
			}
			return null;
		}

		public static float getHealingAmount(Pawn pawn)
		{
			float num = 8f;
			if (pawn.GetPosture() != 0)
			{
				num += 4f;
				Building_Bed building_Bed = pawn.CurrentBed();
				if (building_Bed != null)
				{
					num += building_Bed.def.building.bed_healPerDay;
				}
			}
			foreach (Hediff hediff3 in pawn.health.hediffSet.hediffs)
			{
				HediffStage curStage = hediff3.CurStage;
				if (curStage != null && curStage.naturalHealingFactor != -1f)
				{
					num *= curStage.naturalHealingFactor;
				}
			}
			return num * pawn.HealthScale * 0.01f;
		}
	}
}

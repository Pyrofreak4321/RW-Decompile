using System;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class WorkGiver_FeedPatient : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest
		{
			get
			{
				return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
			}
		}

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.ClosestTouch;
			}
		}

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = t as Pawn;
			if (pawn2 == null || pawn2 == pawn)
			{
				return false;
			}
			if (this.def.feedHumanlikesOnly && !pawn2.RaceProps.Humanlike)
			{
				return false;
			}
			if (this.def.feedAnimalsOnly && !pawn2.RaceProps.Animal)
			{
				return false;
			}
			if (pawn2.needs.food == null || pawn2.needs.food.CurLevelPercentage > pawn2.needs.food.PercentageThreshHungry + 0.02f)
			{
				return false;
			}
			if (!FeedPatientUtility.ShouldBeFed(pawn2))
			{
				return false;
			}
			LocalTargetInfo target = t;
			if (!pawn.CanReserve(target, 1, -1, null, forced))
			{
				return false;
			}
			Thing thing;
			ThingDef thingDef;
			if (!FoodUtility.TryFindBestFoodSourceFor(pawn, pawn2, pawn2.needs.food.CurCategory == HungerCategory.Starving, out thing, out thingDef, false, true, false, true, false, false, false))
			{
				JobFailReason.Is("NoFood".Translate(), null);
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = (Pawn)t;
			Thing thing;
			ThingDef thingDef;
			if (FoodUtility.TryFindBestFoodSourceFor(pawn, pawn2, pawn2.needs.food.CurCategory == HungerCategory.Starving, out thing, out thingDef, false, true, false, true, false, false, false))
			{
				float nutrition = FoodUtility.GetNutrition(thing, thingDef);
				return new Job(JobDefOf.FeedPatient)
				{
					targetA = thing,
					targetB = pawn2,
					count = FoodUtility.WillIngestStackCountOf(pawn2, thingDef, nutrition)
				};
			}
			return null;
		}
	}
}

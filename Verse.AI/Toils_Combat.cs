using System;
using UnityEngine;

namespace Verse.AI
{
	public static class Toils_Combat
	{
		public static Toil TrySetJobToUseAttackVerb(TargetIndex targetInd)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				bool allowManualCastWeapons = !actor.IsColonist;
				Verb verb = actor.TryGetAttackVerb(curJob.GetTarget(targetInd).Thing, allowManualCastWeapons);
				if (verb == null)
				{
					actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
					return;
				}
				curJob.verbToUse = verb;
			};
			return toil;
		}

		public static Toil GotoCastPosition(TargetIndex targetInd, bool closeIfDowned = false, float maxRangeFactor = 1f)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				Thing thing = curJob.GetTarget(targetInd).Thing;
				Pawn pawn = thing as Pawn;
				IntVec3 intVec;
				if (!CastPositionFinder.TryFindCastPosition(new CastPositionRequest
				{
					caster = toil.actor,
					target = thing,
					verb = curJob.verbToUse,
					maxRangeFromTarget = ((closeIfDowned && pawn != null && pawn.Downed) ? Mathf.Min(curJob.verbToUse.verbProps.range, (float)pawn.RaceProps.executionRange) : Mathf.Max(curJob.verbToUse.verbProps.range * maxRangeFactor, 1.42f)),
					wantCoverFromTarget = false
				}, out intVec))
				{
					toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
					return;
				}
				toil.actor.pather.StartPath(intVec, PathEndMode.OnCell);
				actor.Map.pawnDestinationReservationManager.Reserve(actor, curJob, intVec);
			};
			toil.FailOnDespawnedOrNull(targetInd);
			toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			return toil;
		}

		public static Toil CastVerb(TargetIndex targetInd, bool canHitNonTargetPawns = true)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				Verb arg_45_0 = toil.actor.jobs.curJob.verbToUse;
				LocalTargetInfo target = toil.actor.jobs.curJob.GetTarget(targetInd);
				bool canHitNonTargetPawns2 = canHitNonTargetPawns;
				arg_45_0.TryStartCastOn(target, false, canHitNonTargetPawns2);
			};
			toil.defaultCompleteMode = ToilCompleteMode.FinishedBusy;
			return toil;
		}

		public static Toil FollowAndMeleeAttack(TargetIndex targetInd, Action hitAction)
		{
			Toil followAndAttack = new Toil();
			followAndAttack.tickAction = delegate
			{
				Pawn actor = followAndAttack.actor;
				Job curJob = actor.jobs.curJob;
				JobDriver curDriver = actor.jobs.curDriver;
				Thing thing = curJob.GetTarget(targetInd).Thing;
				Pawn pawn = thing as Pawn;
				if (!thing.Spawned)
				{
					curDriver.ReadyForNextToil();
					return;
				}
				if (thing != actor.pather.Destination.Thing || (!actor.pather.Moving && !actor.CanReachImmediate(thing, PathEndMode.Touch)))
				{
					actor.pather.StartPath(thing, PathEndMode.Touch);
				}
				else if (actor.CanReachImmediate(thing, PathEndMode.Touch))
				{
					if (pawn != null && pawn.Downed && !curJob.killIncappedTarget)
					{
						curDriver.ReadyForNextToil();
						return;
					}
					hitAction();
				}
			};
			followAndAttack.defaultCompleteMode = ToilCompleteMode.Never;
			return followAndAttack;
		}
	}
}

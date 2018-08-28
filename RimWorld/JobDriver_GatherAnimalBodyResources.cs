using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public abstract class JobDriver_GatherAnimalBodyResources : JobDriver
	{
		private float gatherProgress;

		protected const TargetIndex AnimalInd = TargetIndex.A;

		protected abstract float WorkTotal
		{
			get;
		}

		protected abstract CompHasGatherableBodyResource GetComp(Pawn animal);

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref this.gatherProgress, "gatherProgress", 0f, false);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
			Job job = this.job;
			return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOnDowned(TargetIndex.A);
			this.FailOnNotCasualInterruptible(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil wait = new Toil();
			wait.initAction = delegate
			{
				Pawn actor = wait.actor;
				Pawn pawn = (Pawn)this.$this.job.GetTarget(TargetIndex.A).Thing;
				actor.pather.StopDead();
				PawnUtility.ForceWait(pawn, 15000, null, true);
			};
			wait.tickAction = delegate
			{
				Pawn actor = wait.actor;
				actor.skills.Learn(SkillDefOf.Animals, 0.13f, false);
				this.$this.gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed, true);
				if (this.$this.gatherProgress >= this.$this.WorkTotal)
				{
					this.$this.GetComp((Pawn)((Thing)this.$this.job.GetTarget(TargetIndex.A))).Gathered(this.$this.pawn);
					actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
				}
			};
			wait.AddFinishAction(delegate
			{
				Pawn pawn = (Pawn)this.$this.job.GetTarget(TargetIndex.A).Thing;
				if (pawn != null && pawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
				}
			});
			wait.FailOnDespawnedOrNull(TargetIndex.A);
			wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			wait.AddEndCondition(delegate
			{
				if (!this.$this.GetComp((Pawn)((Thing)this.$this.job.GetTarget(TargetIndex.A))).ActiveAndFull)
				{
					return JobCondition.Incompletable;
				}
				return JobCondition.Ongoing;
			});
			wait.defaultCompleteMode = ToilCompleteMode.Never;
			wait.WithProgressBar(TargetIndex.A, () => this.$this.gatherProgress / this.$this.WorkTotal, false, -0.5f);
			wait.activeSkill = (() => SkillDefOf.Animals);
			yield return wait;
		}
	}
}

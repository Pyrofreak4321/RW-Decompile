using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Verse.AI
{
	public class JobDriver_Wait : JobDriver
	{
		private const int TargetSearchInterval = 4;

		public override string GetReport()
		{
			if (this.job.def != JobDefOf.Wait_Combat)
			{
				return base.GetReport();
			}
			if (this.pawn.RaceProps.Humanlike && this.pawn.story.WorkTagIsDisabled(WorkTags.Violent))
			{
				return "ReportStanding".Translate();
			}
			return base.GetReport();
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil wait = new Toil();
			wait.initAction = delegate
			{
				this.$this.Map.pawnDestinationReservationManager.Reserve(this.$this.pawn, this.$this.job, this.$this.pawn.Position);
				this.$this.pawn.pather.StopDead();
				this.$this.CheckForAutoAttack();
			};
			wait.tickAction = delegate
			{
				if (this.$this.job.expiryInterval == -1 && this.$this.job.def == JobDefOf.Wait_Combat && !this.$this.pawn.Drafted)
				{
					Log.Error(this.$this.pawn + " in eternal WaitCombat without being drafted.", false);
					this.$this.ReadyForNextToil();
					return;
				}
				if ((Find.TickManager.TicksGame + this.$this.pawn.thingIDNumber) % 4 == 0)
				{
					this.$this.CheckForAutoAttack();
				}
			};
			this.DecorateWaitToil(wait);
			wait.defaultCompleteMode = ToilCompleteMode.Never;
			yield return wait;
		}

		public virtual void DecorateWaitToil(Toil wait)
		{
		}

		public override void Notify_StanceChanged()
		{
			if (this.pawn.stances.curStance is Stance_Mobile)
			{
				this.CheckForAutoAttack();
			}
		}

		private void CheckForAutoAttack()
		{
			if (this.pawn.Downed)
			{
				return;
			}
			if (this.pawn.stances.FullBodyBusy)
			{
				return;
			}
			this.collideWithPawns = false;
			bool flag = this.pawn.story == null || !this.pawn.story.WorkTagIsDisabled(WorkTags.Violent);
			bool flag2 = this.pawn.RaceProps.ToolUser && this.pawn.Faction == Faction.OfPlayer && !this.pawn.story.WorkTagIsDisabled(WorkTags.Firefighting);
			if (flag || flag2)
			{
				Fire fire = null;
				for (int i = 0; i < 9; i++)
				{
					IntVec3 c = this.pawn.Position + GenAdj.AdjacentCellsAndInside[i];
					if (c.InBounds(this.pawn.Map))
					{
						List<Thing> thingList = c.GetThingList(base.Map);
						for (int j = 0; j < thingList.Count; j++)
						{
							if (flag)
							{
								Pawn pawn = thingList[j] as Pawn;
								if (pawn != null && !pawn.Downed && this.pawn.HostileTo(pawn))
								{
									this.pawn.meleeVerbs.TryMeleeAttack(pawn, null, false);
									this.collideWithPawns = true;
									return;
								}
							}
							if (flag2)
							{
								Fire fire2 = thingList[j] as Fire;
								if (fire2 != null && (fire == null || fire2.fireSize < fire.fireSize || i == 8) && (fire2.parent == null || fire2.parent != this.pawn))
								{
									fire = fire2;
								}
							}
						}
					}
				}
				if (fire != null && (!this.pawn.InMentalState || this.pawn.MentalState.def.allowBeatfire))
				{
					this.pawn.natives.TryBeatFire(fire);
					return;
				}
				if (flag && this.pawn.Faction != null && this.job.def == JobDefOf.Wait_Combat && (this.pawn.drafter == null || this.pawn.drafter.FireAtWill))
				{
					Verb currentEffectiveVerb = this.pawn.CurrentEffectiveVerb;
					if (currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack)
					{
						TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat;
						if (currentEffectiveVerb.IsIncendiary())
						{
							targetScanFlags |= TargetScanFlags.NeedNonBurning;
						}
						Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this.pawn, targetScanFlags, null, 0f, 9999f);
						if (thing != null)
						{
							this.pawn.TryStartAttack(thing);
							this.collideWithPawns = true;
							return;
						}
					}
				}
			}
		}
	}
}

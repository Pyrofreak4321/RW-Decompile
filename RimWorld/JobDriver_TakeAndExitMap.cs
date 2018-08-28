using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class JobDriver_TakeAndExitMap : JobDriver
	{
		private const TargetIndex ItemInd = TargetIndex.A;

		private const TargetIndex ExitCellInd = TargetIndex.B;

		protected Thing Item
		{
			get
			{
				return this.job.GetTarget(TargetIndex.A).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.Item;
			Job job = this.job;
			return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
			Toil gotoCell = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			gotoCell.AddPreTickAction(delegate
			{
				if (this.$this.Map.exitMapGrid.IsExitCell(this.$this.pawn.Position))
				{
					this.$this.pawn.ExitMap(true, CellRect.WholeMap(this.$this.Map).GetClosestEdge(this.$this.pawn.Position));
				}
			});
			yield return gotoCell;
			yield return new Toil
			{
				initAction = delegate
				{
					if (this.$this.pawn.Position.OnEdge(this.$this.pawn.Map) || this.$this.pawn.Map.exitMapGrid.IsExitCell(this.$this.pawn.Position))
					{
						this.$this.pawn.ExitMap(true, CellRect.WholeMap(this.$this.Map).GetClosestEdge(this.$this.pawn.Position));
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}

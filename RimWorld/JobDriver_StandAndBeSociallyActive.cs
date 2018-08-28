using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class JobDriver_StandAndBeSociallyActive : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return new Toil
			{
				tickAction = delegate
				{
					Pawn pawn = this.$this.FindClosePawn();
					if (pawn != null)
					{
						this.$this.pawn.rotationTracker.FaceCell(pawn.Position);
					}
					this.$this.pawn.GainComfortFromCellIfPossible();
				},
				socialMode = RandomSocialMode.SuperActive,
				defaultCompleteMode = ToilCompleteMode.Never,
				handlingFacing = true
			};
		}

		private Pawn FindClosePawn()
		{
			IntVec3 position = this.pawn.Position;
			for (int i = 0; i < 24; i++)
			{
				IntVec3 intVec = position + GenRadial.RadialPattern[i];
				if (intVec.InBounds(base.Map))
				{
					Thing thing = intVec.GetThingList(base.Map).Find((Thing x) => x is Pawn);
					if (thing != null && thing != this.pawn)
					{
						if (GenSight.LineOfSight(position, intVec, base.Map, false, null, 0, 0))
						{
							return (Pawn)thing;
						}
					}
				}
			}
			return null;
		}
	}
}

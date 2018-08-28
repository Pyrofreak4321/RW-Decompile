using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
	public class JobDriver_PrepareCaravan_GatherItems : JobDriver
	{
		private const TargetIndex ToHaulInd = TargetIndex.A;

		private const TargetIndex CarrierInd = TargetIndex.B;

		private const int PlaceInInventoryDuration = 25;

		public Thing ToHaul
		{
			get
			{
				return this.job.GetTarget(TargetIndex.A).Thing;
			}
		}

		public Pawn Carrier
		{
			get
			{
				return (Pawn)this.job.GetTarget(TargetIndex.B).Thing;
			}
		}

		private List<TransferableOneWay> Transferables
		{
			get
			{
				return ((LordJob_FormAndSendCaravan)this.job.lord.LordJob).transferables;
			}
		}

		private TransferableOneWay Transferable
		{
			get
			{
				TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(this.ToHaul, this.Transferables, TransferAsOneMode.PodsOrCaravanPacking);
				if (transferableOneWay != null)
				{
					return transferableOneWay;
				}
				throw new InvalidOperationException("Could not find any matching transferable.");
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.ToHaul;
			Job job = this.job;
			return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => !this.$this.Map.lordManager.lords.Contains(this.$this.job.lord));
			Toil reserve = Toils_Reserve.Reserve(TargetIndex.A, 1, -1, null).FailOnDespawnedOrNull(TargetIndex.A);
			yield return reserve;
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return this.DetermineNumToHaul();
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);
			yield return this.AddCarriedThingToTransferables();
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve, TargetIndex.A, TargetIndex.None, true, (Thing x) => this.$this.Transferable.things.Contains(x));
			Toil findCarrier = this.FindCarrier();
			yield return findCarrier;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).JumpIf(() => !JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(this.$this.Carrier, this.$this.pawn, true), findCarrier);
			yield return Toils_General.Wait(25, TargetIndex.None).JumpIf(() => !JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(this.$this.Carrier, this.$this.pawn, true), findCarrier).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
			yield return this.PlaceTargetInCarrierInventory();
		}

		private Toil DetermineNumToHaul()
		{
			return new Toil
			{
				initAction = delegate
				{
					int num = GatherItemsForCaravanUtility.CountLeftToTransfer(this.pawn, this.Transferable, this.job.lord);
					if (this.pawn.carryTracker.CarriedThing != null)
					{
						num -= this.pawn.carryTracker.CarriedThing.stackCount;
					}
					if (num <= 0)
					{
						this.pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true);
					}
					else
					{
						this.job.count = num;
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
				atomicWithPrevious = true
			};
		}

		private Toil AddCarriedThingToTransferables()
		{
			return new Toil
			{
				initAction = delegate
				{
					TransferableOneWay transferable = this.Transferable;
					if (!transferable.things.Contains(this.pawn.carryTracker.CarriedThing))
					{
						transferable.things.Add(this.pawn.carryTracker.CarriedThing);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
				atomicWithPrevious = true
			};
		}

		private Toil FindCarrier()
		{
			return new Toil
			{
				initAction = delegate
				{
					Pawn pawn = this.FindBestCarrier(true);
					if (pawn == null)
					{
						bool flag = this.pawn.GetLord() == this.job.lord;
						if (flag && !MassUtility.IsOverEncumbered(this.pawn))
						{
							pawn = this.pawn;
						}
						else
						{
							pawn = this.FindBestCarrier(false);
							if (pawn == null)
							{
								if (flag)
								{
									pawn = this.pawn;
								}
								else
								{
									IEnumerable<Pawn> source = from x in this.job.lord.ownedPawns
									where JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(x, this.pawn, true)
									select x;
									if (!source.Any<Pawn>())
									{
										base.EndJobWith(JobCondition.Incompletable);
										return;
									}
									pawn = source.RandomElement<Pawn>();
								}
							}
						}
					}
					this.job.SetTarget(TargetIndex.B, pawn);
				}
			};
		}

		private Toil PlaceTargetInCarrierInventory()
		{
			return new Toil
			{
				initAction = delegate
				{
					Pawn_CarryTracker carryTracker = this.pawn.carryTracker;
					Thing carriedThing = carryTracker.CarriedThing;
					this.Transferable.AdjustTo(Mathf.Max(this.Transferable.CountToTransfer - carriedThing.stackCount, 0));
					carryTracker.innerContainer.TryTransferToContainer(carriedThing, this.Carrier.inventory.innerContainer, carriedThing.stackCount, true);
				}
			};
		}

		public static bool IsUsableCarrier(Pawn p, Pawn forPawn, bool allowColonists)
		{
			return p.IsFormingCaravan() && (p == forPawn || (!p.DestroyedOrNull() && p.Spawned && !p.inventory.UnloadEverything && forPawn.CanReach(p, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn) && ((allowColonists && p.IsColonist) || ((p.RaceProps.packAnimal || p.HostFaction == Faction.OfPlayer) && !p.IsBurning() && !p.Downed && !MassUtility.IsOverEncumbered(p)))));
		}

		private float GetCarrierScore(Pawn p)
		{
			float lengthHorizontal = (p.Position - this.pawn.Position).LengthHorizontal;
			float num = MassUtility.EncumbrancePercent(p);
			float num2 = 1f - num;
			return num2 - lengthHorizontal / 10f * 0.2f;
		}

		private Pawn FindBestCarrier(bool onlyAnimals)
		{
			Lord lord = this.job.lord;
			Pawn pawn = null;
			float num = 0f;
			if (lord != null)
			{
				for (int i = 0; i < lord.ownedPawns.Count; i++)
				{
					Pawn pawn2 = lord.ownedPawns[i];
					if (pawn2 != this.pawn)
					{
						if (!onlyAnimals || pawn2.RaceProps.Animal)
						{
							if (JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(pawn2, this.pawn, false))
							{
								float carrierScore = this.GetCarrierScore(pawn2);
								if (pawn == null || carrierScore > num)
								{
									pawn = pawn2;
									num = carrierScore;
								}
							}
						}
					}
				}
			}
			return pawn;
		}
	}
}

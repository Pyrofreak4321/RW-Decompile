using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class Pawn_PlayerSettings : IExposable
	{
		private Pawn pawn;

		private Area areaAllowedInt;

		public int joinTick = -1;

		public Pawn master;

		public bool animalsReleased;

		public MedicalCareCategory medCare = MedicalCareCategory.Best;

		public HostilityResponseMode hostilityResponse = HostilityResponseMode.Flee;

		public Area AreaRestriction
		{
			get
			{
				if (!this.RespectsAllowedArea)
				{
					return null;
				}
				return this.areaAllowedInt;
			}
			set
			{
				this.areaAllowedInt = value;
			}
		}

		public bool RespectsAllowedArea
		{
			get
			{
				return this.pawn.Faction == Faction.OfPlayer && this.pawn.HostFaction == null;
			}
		}

		public bool UsesConfigurableHostilityResponse
		{
			get
			{
				return this.pawn.IsColonist && this.pawn.HostFaction == null;
			}
		}

		public Pawn_PlayerSettings(Pawn pawn)
		{
			this.pawn = pawn;
			if (Current.ProgramState == ProgramState.MapPlaying)
			{
				this.joinTick = Find.TickManager.TicksGame;
			}
			else
			{
				this.joinTick = 0;
			}
			if (pawn.RaceProps.Humanlike)
			{
				if (pawn.IsPrisoner)
				{
					this.medCare = MedicalCareCategory.NoMeds;
				}
				else
				{
					this.medCare = MedicalCareCategory.Best;
				}
			}
			else
			{
				this.medCare = MedicalCareCategory.NoMeds;
			}
		}

		public void ExposeData()
		{
			Scribe_Values.LookValue<int>(ref this.joinTick, "joinTick", 0, false);
			Scribe_Values.LookValue<bool>(ref this.animalsReleased, "animalsReleased", false, false);
			Scribe_Values.LookValue<MedicalCareCategory>(ref this.medCare, "medCare", MedicalCareCategory.NoCare, false);
			Scribe_References.LookReference<Area>(ref this.areaAllowedInt, "areaAllowed", false);
			Scribe_References.LookReference<Pawn>(ref this.master, "master", false);
			Scribe_Values.LookValue<HostilityResponseMode>(ref this.hostilityResponse, "hostilityResponse", HostilityResponseMode.Flee, false);
		}

		[DebuggerHidden]
		public IEnumerable<Gizmo> GetGizmos()
		{
			if (this.pawn.Drafted)
			{
				if (PawnUtility.SpawnedMasteredPawns(this.pawn).Any((Pawn p) => p.training.IsCompleted(TrainableDefOf.Release)))
				{
					yield return new Command_Toggle
					{
						defaultLabel = "CommandReleaseAnimalsLabel".Translate(),
						defaultDesc = "CommandReleaseAnimalsDesc".Translate(),
						icon = TexCommand.ReleaseAnimals,
						hotKey = KeyBindingDefOf.Misc7,
						isActive = (() => this.<>f__this.animalsReleased),
						toggleAction = delegate
						{
							this.<>f__this.animalsReleased = !this.<>f__this.animalsReleased;
							if (this.<>f__this.animalsReleased)
							{
								foreach (Pawn current in PawnUtility.SpawnedMasteredPawns(this.<>f__this.pawn))
								{
									if (current.caller != null)
									{
										current.caller.Notify_Released();
									}
									current.jobs.EndCurrentJob(JobCondition.InterruptForced);
								}
							}
						}
					};
				}
			}
		}

		public void Notify_AreaRemoved(Area area)
		{
			if (this.areaAllowedInt == area)
			{
				this.areaAllowedInt = null;
			}
		}
	}
}
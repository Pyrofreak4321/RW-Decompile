using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class Building_TurretGun : Building_Turret
	{
		protected int burstCooldownTicksLeft;

		protected int burstWarmupTicksLeft;

		protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

		private bool holdFire;

		public Thing gun;

		protected TurretTop top;

		protected CompPowerTrader powerComp;

		protected CompMannable mannableComp;

		private const int TryStartShootSomethingIntervalTicks = 10;

		public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

		public CompEquippable GunCompEq
		{
			get
			{
				return this.gun.TryGetComp<CompEquippable>();
			}
		}

		public override LocalTargetInfo CurrentTarget
		{
			get
			{
				return this.currentTargetInt;
			}
		}

		private bool WarmingUp
		{
			get
			{
				return this.burstWarmupTicksLeft > 0;
			}
		}

		public override Verb AttackVerb
		{
			get
			{
				return this.GunCompEq.PrimaryVerb;
			}
		}

		private bool PlayerControlled
		{
			get
			{
				return (base.Faction == Faction.OfPlayer || this.MannedByColonist) && !this.MannedByNonColonist;
			}
		}

		private bool CanSetForcedTarget
		{
			get
			{
				return this.mannableComp != null && this.PlayerControlled;
			}
		}

		private bool CanToggleHoldFire
		{
			get
			{
				return this.PlayerControlled;
			}
		}

		private bool IsMortar
		{
			get
			{
				return this.def.building.IsMortar;
			}
		}

		private bool IsMortarOrProjectileFliesOverhead
		{
			get
			{
				return this.AttackVerb.ProjectileFliesOverhead() || this.IsMortar;
			}
		}

		private bool CanExtractShell
		{
			get
			{
				if (!this.PlayerControlled)
				{
					return false;
				}
				CompChangeableProjectile compChangeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
				return compChangeableProjectile != null && compChangeableProjectile.Loaded;
			}
		}

		private bool MannedByColonist
		{
			get
			{
				return this.mannableComp != null && this.mannableComp.ManningPawn != null && this.mannableComp.ManningPawn.Faction == Faction.OfPlayer;
			}
		}

		private bool MannedByNonColonist
		{
			get
			{
				return this.mannableComp != null && this.mannableComp.ManningPawn != null && this.mannableComp.ManningPawn.Faction != Faction.OfPlayer;
			}
		}

		public Building_TurretGun()
		{
			this.top = new TurretTop(this);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			this.powerComp = base.GetComp<CompPowerTrader>();
			this.mannableComp = base.GetComp<CompMannable>();
		}

		public override void PostMake()
		{
			base.PostMake();
			this.MakeGun();
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			base.DeSpawn(mode);
			this.ResetCurrentTarget();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
			Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
			Scribe_TargetInfo.Look(ref this.currentTargetInt, "currentTarget");
			Scribe_Values.Look<bool>(ref this.holdFire, "holdFire", false, false);
			Scribe_Deep.Look<Thing>(ref this.gun, "gun", new object[0]);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				BackCompatibility.TurretPostLoadInit(this);
				this.UpdateGunVerbs();
			}
		}

		public override bool ClaimableBy(Faction by)
		{
			return base.ClaimableBy(by) && (this.mannableComp == null || this.mannableComp.ManningPawn == null) && (this.powerComp == null || !this.powerComp.PowerOn);
		}

		public override void OrderAttack(LocalTargetInfo targ)
		{
			if (!targ.IsValid)
			{
				if (this.forcedTarget.IsValid)
				{
					this.ResetForcedTarget();
				}
				return;
			}
			if ((targ.Cell - base.Position).LengthHorizontal < this.AttackVerb.verbProps.EffectiveMinRange(targ, this))
			{
				Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
				return;
			}
			if ((targ.Cell - base.Position).LengthHorizontal > this.AttackVerb.verbProps.range)
			{
				Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
				return;
			}
			if (this.forcedTarget != targ)
			{
				this.forcedTarget = targ;
				if (this.burstCooldownTicksLeft <= 0)
				{
					this.TryStartShootSomething(false);
				}
			}
			if (this.holdFire)
			{
				Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(new object[]
				{
					this.def.label
				}), this, MessageTypeDefOf.RejectInput, false);
			}
		}

		public override void Tick()
		{
			base.Tick();
			if (this.forcedTarget.IsValid && !this.CanSetForcedTarget)
			{
				this.ResetForcedTarget();
			}
			if (!this.CanToggleHoldFire)
			{
				this.holdFire = false;
			}
			if (this.forcedTarget.ThingDestroyed)
			{
				this.ResetForcedTarget();
			}
			bool flag = (this.powerComp == null || this.powerComp.PowerOn) && (this.mannableComp == null || this.mannableComp.MannedNow);
			if (flag && base.Spawned)
			{
				this.GunCompEq.verbTracker.VerbsTick();
				if (!this.stunner.Stunned && this.AttackVerb.state != VerbState.Bursting)
				{
					if (this.WarmingUp)
					{
						this.burstWarmupTicksLeft--;
						if (this.burstWarmupTicksLeft == 0)
						{
							this.BeginBurst();
						}
					}
					else
					{
						if (this.burstCooldownTicksLeft > 0)
						{
							this.burstCooldownTicksLeft--;
						}
						if (this.burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
						{
							this.TryStartShootSomething(true);
						}
					}
					this.top.TurretTopTick();
				}
			}
			else
			{
				this.ResetCurrentTarget();
			}
		}

		protected void TryStartShootSomething(bool canBeginBurstImmediately)
		{
			if (!base.Spawned || (this.holdFire && this.CanToggleHoldFire) || (this.AttackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) || !this.AttackVerb.Available())
			{
				this.ResetCurrentTarget();
				return;
			}
			bool isValid = this.currentTargetInt.IsValid;
			if (this.forcedTarget.IsValid)
			{
				this.currentTargetInt = this.forcedTarget;
			}
			else
			{
				this.currentTargetInt = this.TryFindNewTarget();
			}
			if (!isValid && this.currentTargetInt.IsValid)
			{
				SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
			}
			if (this.currentTargetInt.IsValid)
			{
				if (this.def.building.turretBurstWarmupTime > 0f)
				{
					this.burstWarmupTicksLeft = this.def.building.turretBurstWarmupTime.SecondsToTicks();
				}
				else if (canBeginBurstImmediately)
				{
					this.BeginBurst();
				}
				else
				{
					this.burstWarmupTicksLeft = 1;
				}
			}
			else
			{
				this.ResetCurrentTarget();
			}
		}

		protected LocalTargetInfo TryFindNewTarget()
		{
			IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
			Faction faction = attackTargetSearcher.Thing.Faction;
			float range = this.AttackVerb.verbProps.range;
			Building t;
			if (Rand.Value < 0.5f && this.AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate(Building x)
			{
				float num = this.AttackVerb.verbProps.EffectiveMinRange(x, this);
				float num2 = (float)x.Position.DistanceToSquared(this.Position);
				return num2 > num * num && num2 < range * range;
			}).TryRandomElement(out t))
			{
				return t;
			}
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
			if (!this.AttackVerb.ProjectileFliesOverhead())
			{
				targetScanFlags |= TargetScanFlags.NeedLOSToAll;
				targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
			}
			if (this.AttackVerb.IsIncendiary())
			{
				targetScanFlags |= TargetScanFlags.NeedNonBurning;
			}
			return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, new Predicate<Thing>(this.IsValidTarget), 0f, 9999f);
		}

		private IAttackTargetSearcher TargSearcher()
		{
			if (this.mannableComp != null && this.mannableComp.MannedNow)
			{
				return this.mannableComp.ManningPawn;
			}
			return this;
		}

		private bool IsValidTarget(Thing t)
		{
			Pawn pawn = t as Pawn;
			if (pawn != null)
			{
				if (this.AttackVerb.ProjectileFliesOverhead())
				{
					RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
					if (roofDef != null && roofDef.isThickRoof)
					{
						return false;
					}
				}
				if (this.mannableComp == null)
				{
					return !GenAI.MachinesLike(base.Faction, pawn);
				}
				if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
				{
					return false;
				}
			}
			return true;
		}

		protected void BeginBurst()
		{
			this.AttackVerb.TryStartCastOn(this.CurrentTarget, false, true);
			base.OnAttackedTarget(this.CurrentTarget);
		}

		protected void BurstComplete()
		{
			this.burstCooldownTicksLeft = this.BurstCooldownTime().SecondsToTicks();
		}

		protected float BurstCooldownTime()
		{
			if (this.def.building.turretBurstCooldownTime >= 0f)
			{
				return this.def.building.turretBurstCooldownTime;
			}
			return this.AttackVerb.verbProps.defaultCooldownTime;
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string inspectString = base.GetInspectString();
			if (!inspectString.NullOrEmpty())
			{
				stringBuilder.AppendLine(inspectString);
			}
			if (this.AttackVerb.verbProps.minRange > 0f)
			{
				stringBuilder.AppendLine("MinimumRange".Translate() + ": " + this.AttackVerb.verbProps.minRange.ToString("F0"));
			}
			if (base.Spawned && this.IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
			{
				stringBuilder.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
			}
			else if (base.Spawned && this.burstCooldownTicksLeft > 0 && this.BurstCooldownTime() > 5f)
			{
				stringBuilder.AppendLine("CanFireIn".Translate() + ": " + this.burstCooldownTicksLeft.ToStringSecondsFromTicks());
			}
			CompChangeableProjectile compChangeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
			if (compChangeableProjectile != null)
			{
				if (compChangeableProjectile.Loaded)
				{
					stringBuilder.AppendLine("ShellLoaded".Translate(new object[]
					{
						compChangeableProjectile.LoadedShell.LabelCap
					}));
				}
				else
				{
					stringBuilder.AppendLine("ShellNotLoaded".Translate());
				}
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}

		public override void Draw()
		{
			this.top.DrawTurret();
			base.Draw();
		}

		public override void DrawExtraSelectionOverlays()
		{
			float range = this.AttackVerb.verbProps.range;
			if (range < 90f)
			{
				GenDraw.DrawRadiusRing(base.Position, range);
			}
			float num = this.AttackVerb.verbProps.EffectiveMinRange(false);
			if (num < 90f && num > 0.1f)
			{
				GenDraw.DrawRadiusRing(base.Position, num);
			}
			if (this.WarmingUp)
			{
				int degreesWide = (int)((float)this.burstWarmupTicksLeft * 0.5f);
				GenDraw.DrawAimPie(this, this.CurrentTarget, degreesWide, (float)this.def.size.x * 0.5f);
			}
			if (this.forcedTarget.IsValid && (!this.forcedTarget.HasThing || this.forcedTarget.Thing.Spawned))
			{
				Vector3 b;
				if (this.forcedTarget.HasThing)
				{
					b = this.forcedTarget.Thing.TrueCenter();
				}
				else
				{
					b = this.forcedTarget.Cell.ToVector3Shifted();
				}
				Vector3 a = this.TrueCenter();
				b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
				a.y = b.y;
				GenDraw.DrawLineBetween(a, b, Building_TurretGun.ForcedTargetLineMat);
			}
		}

		[DebuggerHidden]
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo c in base.GetGizmos())
			{
				yield return c;
			}
			if (this.CanExtractShell)
			{
				CompChangeableProjectile changeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
				yield return new Command_Action
				{
					defaultLabel = "CommandExtractShell".Translate(),
					defaultDesc = "CommandExtractShellDesc".Translate(),
					icon = changeableProjectile.LoadedShell.uiIcon,
					iconAngle = changeableProjectile.LoadedShell.uiIconAngle,
					iconOffset = changeableProjectile.LoadedShell.uiIconOffset,
					iconDrawScale = GenUI.IconDrawScale(changeableProjectile.LoadedShell),
					alsoClickIfOtherInGroupClicked = false,
					action = delegate
					{
						GenPlace.TryPlaceThing(changeableProjectile.RemoveShell(), this.$this.Position, this.$this.Map, ThingPlaceMode.Near, null, null);
					}
				};
			}
			if (this.CanSetForcedTarget)
			{
				Command_VerbTarget attack = new Command_VerbTarget();
				attack.defaultLabel = "CommandSetForceAttackTarget".Translate();
				attack.defaultDesc = "CommandSetForceAttackTargetDesc".Translate();
				attack.icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true);
				attack.verb = this.AttackVerb;
				attack.hotKey = KeyBindingDefOf.Misc4;
				if (base.Spawned && this.IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
				{
					attack.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
				}
				yield return attack;
			}
			if (this.forcedTarget.IsValid)
			{
				Command_Action stop = new Command_Action();
				stop.defaultLabel = "CommandStopForceAttack".Translate();
				stop.defaultDesc = "CommandStopForceAttackDesc".Translate();
				stop.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
				stop.action = delegate
				{
					this.$this.ResetForcedTarget();
					SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				};
				if (!this.forcedTarget.IsValid)
				{
					stop.Disable("CommandStopAttackFailNotForceAttacking".Translate());
				}
				stop.hotKey = KeyBindingDefOf.Misc5;
				yield return stop;
			}
			if (this.CanToggleHoldFire)
			{
				yield return new Command_Toggle
				{
					defaultLabel = "CommandHoldFire".Translate(),
					defaultDesc = "CommandHoldFireDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", true),
					hotKey = KeyBindingDefOf.Misc6,
					toggleAction = delegate
					{
						this.$this.holdFire = !this.$this.holdFire;
						if (this.$this.holdFire)
						{
							this.$this.ResetForcedTarget();
						}
					},
					isActive = (() => this.$this.holdFire)
				};
			}
		}

		private void ResetForcedTarget()
		{
			this.forcedTarget = LocalTargetInfo.Invalid;
			this.burstWarmupTicksLeft = 0;
			if (this.burstCooldownTicksLeft <= 0)
			{
				this.TryStartShootSomething(false);
			}
		}

		private void ResetCurrentTarget()
		{
			this.currentTargetInt = LocalTargetInfo.Invalid;
			this.burstWarmupTicksLeft = 0;
		}

		public void MakeGun()
		{
			this.gun = ThingMaker.MakeThing(this.def.building.turretGunDef, null);
			this.UpdateGunVerbs();
		}

		private void UpdateGunVerbs()
		{
			List<Verb> allVerbs = this.gun.TryGetComp<CompEquippable>().AllVerbs;
			for (int i = 0; i < allVerbs.Count; i++)
			{
				Verb verb = allVerbs[i];
				verb.caster = this;
				verb.castCompleteCallback = new Action(this.BurstComplete);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class GameCondition_Flashstorm : GameCondition
	{
		private static readonly IntRange AreaRadiusRange = new IntRange(45, 60);

		private static readonly IntRange TicksBetweenStrikes = new IntRange(320, 800);

		private const int RainDisableTicksAfterConditionEnds = 30000;

		public IntVec2 centerLocation;

		private int areaRadius;

		private int nextLightningTicks;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<IntVec2>(ref this.centerLocation, "centerLocation", default(IntVec2), false);
			Scribe_Values.Look<int>(ref this.areaRadius, "areaRadius", 0, false);
			Scribe_Values.Look<int>(ref this.nextLightningTicks, "nextLightningTicks", 0, false);
		}

		public override void Init()
		{
			base.Init();
			this.areaRadius = GameCondition_Flashstorm.AreaRadiusRange.RandomInRange;
			this.FindGoodCenterLocation();
		}

		public override void GameConditionTick()
		{
			if (Find.TickManager.TicksGame > this.nextLightningTicks)
			{
				Vector2 vector = Rand.UnitVector2 * Rand.Range(0f, (float)this.areaRadius);
				IntVec3 intVec = new IntVec3((int)Math.Round((double)vector.x) + this.centerLocation.x, 0, (int)Math.Round((double)vector.y) + this.centerLocation.z);
				if (this.IsGoodLocationForStrike(intVec))
				{
					base.SingleMap.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(base.SingleMap, intVec));
					this.nextLightningTicks = Find.TickManager.TicksGame + GameCondition_Flashstorm.TicksBetweenStrikes.RandomInRange;
				}
			}
		}

		public override void End()
		{
			base.SingleMap.weatherDecider.DisableRainFor(30000);
			base.End();
		}

		private void FindGoodCenterLocation()
		{
			if (base.SingleMap.Size.x <= 16 || base.SingleMap.Size.z <= 16)
			{
				throw new Exception("Map too small for flashstorm.");
			}
			for (int i = 0; i < 10; i++)
			{
				this.centerLocation = new IntVec2(Rand.Range(8, base.SingleMap.Size.x - 8), Rand.Range(8, base.SingleMap.Size.z - 8));
				if (this.IsGoodCenterLocation(this.centerLocation))
				{
					break;
				}
			}
		}

		private bool IsGoodLocationForStrike(IntVec3 loc)
		{
			return loc.InBounds(base.SingleMap) && !loc.Roofed(base.SingleMap) && loc.Standable(base.SingleMap);
		}

		private bool IsGoodCenterLocation(IntVec2 loc)
		{
			int num = 0;
			int num2 = (int)(3.14159274f * (float)this.areaRadius * (float)this.areaRadius / 2f);
			foreach (IntVec3 current in this.GetPotentiallyAffectedCells(loc))
			{
				if (this.IsGoodLocationForStrike(current))
				{
					num++;
				}
				if (num >= num2)
				{
					break;
				}
			}
			return num >= num2;
		}

		[DebuggerHidden]
		private IEnumerable<IntVec3> GetPotentiallyAffectedCells(IntVec2 center)
		{
			for (int x = center.x - this.areaRadius; x <= center.x + this.areaRadius; x++)
			{
				for (int z = center.z - this.areaRadius; z <= center.z + this.areaRadius; z++)
				{
					if ((center.x - x) * (center.x - x) + (center.z - z) * (center.z - z) <= this.areaRadius * this.areaRadius)
					{
						yield return new IntVec3(x, 0, z);
					}
				}
			}
		}
	}
}

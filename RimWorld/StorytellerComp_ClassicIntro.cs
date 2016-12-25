using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace RimWorld
{
	public class StorytellerComp_ClassicIntro : StorytellerComp
	{
		protected int IntervalsPassed
		{
			get
			{
				return Find.TickManager.TicksGame / 1000;
			}
		}

		[DebuggerHidden]
		public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
		{
			if (target == Find.Maps.Find((Map x) => x.IsPlayerHome))
			{
				if (this.IntervalsPassed == 150)
				{
					IncidentDef inc = IncidentDefOf.VisitorGroup;
					if (inc.TargetAllowed(target))
					{
						yield return new FiringIncident(inc, this, null)
						{
							parms = 
							{
								target = target,
								points = (float)Rand.Range(40, 100)
							}
						};
					}
				}
				IncidentDef incDef;
				if (this.IntervalsPassed == 204 && (from def in DefDatabase<IncidentDef>.AllDefs
				where def.TargetAllowed(this.target) && def.category == IncidentCategory.ThreatSmall
				select def).TryRandomElementByWeight(new Func<IncidentDef, float>(this.IncidentChanceAdjustedForPopulation), out incDef))
				{
					yield return new FiringIncident(incDef, this, null)
					{
						parms = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, incDef.category, target)
					};
				}
				if (this.IntervalsPassed == 264)
				{
					IncidentDef inc2 = IncidentDefOf.WandererJoin;
					if (inc2.TargetAllowed(target))
					{
						FiringIncident qi = new FiringIncident(inc2, this, this.GenerateParms(inc2.category, target));
						yield return qi;
					}
				}
				if (this.IntervalsPassed == 324)
				{
					IncidentDef inc3 = IncidentDefOf.RaidEnemy;
					if (inc3.TargetAllowed(target))
					{
						yield return new FiringIncident(inc3, this, null)
						{
							parms = this.GenerateParms(inc3.category, target)
						};
					}
				}
			}
		}
	}
}

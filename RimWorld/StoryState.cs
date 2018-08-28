using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
	public class StoryState : IExposable
	{
		private IIncidentTarget target;

		private int lastThreatBigTick = -1;

		public Dictionary<IncidentDef, int> lastFireTicks = new Dictionary<IncidentDef, int>();

		public int LastThreatBigTick
		{
			get
			{
				if (this.lastThreatBigTick > Find.TickManager.TicksGame + 1000)
				{
					Log.Error(string.Concat(new object[]
					{
						"Latest big threat queue time was ",
						this.lastThreatBigTick,
						" at tick ",
						Find.TickManager.TicksGame,
						". This is too far in the future. Resetting."
					}), false);
					this.lastThreatBigTick = Find.TickManager.TicksGame - 1;
				}
				return this.lastThreatBigTick;
			}
		}

		public StoryState(IIncidentTarget target)
		{
			this.target = target;
		}

		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref this.lastThreatBigTick, "lastThreatBigTick", 0, true);
			Scribe_Collections.Look<IncidentDef, int>(ref this.lastFireTicks, "lastFireTicks", LookMode.Def, LookMode.Value);
		}

		public void Notify_IncidentFired(FiringIncident fi)
		{
			if (fi.parms.forced || fi.parms.target != this.target)
			{
				return;
			}
			int ticksGame = Find.TickManager.TicksGame;
			if (fi.def.category == IncidentCategoryDefOf.ThreatBig || fi.def.category == IncidentCategoryDefOf.RaidBeacon)
			{
				this.lastThreatBigTick = ticksGame;
				Find.StoryWatcher.statsRecord.numThreatBigs++;
			}
			if (this.lastFireTicks.ContainsKey(fi.def))
			{
				this.lastFireTicks[fi.def] = ticksGame;
			}
			else
			{
				this.lastFireTicks.Add(fi.def, ticksGame);
			}
		}

		public void CopyTo(StoryState other)
		{
			other.lastThreatBigTick = this.lastThreatBigTick;
			other.lastFireTicks.Clear();
			foreach (KeyValuePair<IncidentDef, int> current in this.lastFireTicks)
			{
				other.lastFireTicks.Add(current.Key, current.Value);
			}
		}
	}
}

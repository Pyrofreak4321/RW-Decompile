using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class Alert_StarvationAnimals : Alert
	{
		private IEnumerable<Pawn> StarvingAnimals
		{
			get
			{
				return from p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep
				where p.HostFaction == null && !p.RaceProps.Humanlike
				where p.needs.food != null && (p.needs.food.TicksStarving > 30000 || (p.health.hediffSet.HasHediff(HediffDefOf.Pregnant, true) && p.needs.food.TicksStarving > 5000))
				select p;
			}
		}

		public Alert_StarvationAnimals()
		{
			this.defaultLabel = "StarvationAnimals".Translate();
		}

		public override string GetExplanation()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Pawn current in from a in this.StarvingAnimals
			orderby a.def.label
			select a)
			{
				stringBuilder.Append("    " + current.LabelShort.CapitalizeFirst());
				if (current.Name.IsValid && !current.Name.Numerical)
				{
					stringBuilder.Append(" (" + current.def.label + ")");
				}
				stringBuilder.AppendLine();
			}
			return string.Format("StarvationAnimalsDesc".Translate(), stringBuilder.ToString());
		}

		public override AlertReport GetReport()
		{
			return AlertReport.CulpritsAre(this.StarvingAnimals);
		}
	}
}

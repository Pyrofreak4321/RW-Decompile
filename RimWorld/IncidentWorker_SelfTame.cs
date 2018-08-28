using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld
{
	public class IncidentWorker_SelfTame : IncidentWorker
	{
		private IEnumerable<Pawn> Candidates(Map map)
		{
			return from x in map.mapPawns.AllPawnsSpawned
			where x.RaceProps.Animal && x.Faction == null && !x.Position.Fogged(x.Map) && !x.InMentalState && !x.Downed && x.RaceProps.wildness > 0f
			select x;
		}

		protected override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			return this.Candidates(map).Any<Pawn>();
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			Pawn pawn = null;
			if (!this.Candidates(map).TryRandomElement(out pawn))
			{
				return false;
			}
			if (pawn.guest != null)
			{
				pawn.guest.SetGuestStatus(null, false);
			}
			string text = pawn.LabelIndefinite();
			bool flag = pawn.Name != null;
			pawn.SetFaction(Faction.OfPlayer, null);
			string text2;
			if (!flag && pawn.Name != null)
			{
				if (pawn.Name.Numerical)
				{
					text2 = "LetterAnimalSelfTameAndNameNumerical".Translate(new object[]
					{
						text,
						pawn.Name.ToStringFull
					}).CapitalizeFirst();
				}
				else
				{
					text2 = "LetterAnimalSelfTameAndName".Translate(new object[]
					{
						text,
						pawn.Name.ToStringFull
					}).CapitalizeFirst();
				}
			}
			else
			{
				text2 = "LetterAnimalSelfTame".Translate(new object[]
				{
					pawn.LabelIndefinite()
				}).CapitalizeFirst();
			}
			Find.LetterStack.ReceiveLetter("LetterLabelAnimalSelfTame".Translate(new object[]
			{
				pawn.KindLabel
			}).CapitalizeFirst(), text2, LetterDefOf.PositiveEvent, pawn, null, null);
			return true;
		}
	}
}

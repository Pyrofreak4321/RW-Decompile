using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace RimWorld
{
	public class TaleData_Pawn : TaleData
	{
		public Pawn pawn;

		public PawnKindDef kind;

		public Faction faction;

		public Gender gender;

		public Name name;

		public string title;

		public ThingDef primaryEquipment;

		public ThingDef notableApparel;

		public override void ExposeData()
		{
			Scribe_References.Look<Pawn>(ref this.pawn, "pawn", true);
			Scribe_Defs.Look<PawnKindDef>(ref this.kind, "kind");
			Scribe_References.Look<Faction>(ref this.faction, "faction", false);
			Scribe_Values.Look<Gender>(ref this.gender, "gender", Gender.None, false);
			Scribe_Deep.Look<Name>(ref this.name, "name", new object[0]);
			Scribe_Values.Look<Gender>(ref this.gender, "title", Gender.None, false);
			Scribe_Defs.Look<ThingDef>(ref this.primaryEquipment, "peq");
			Scribe_Defs.Look<ThingDef>(ref this.notableApparel, "app");
		}

		public override IEnumerable<Rule> GetRules(string prefix)
		{
			return GrammarUtility.RulesForPawn(prefix, this.name, this.title, this.kind, this.gender, this.faction, null);
		}

		public static TaleData_Pawn GenerateFrom(Pawn pawn)
		{
			TaleData_Pawn taleData_Pawn = new TaleData_Pawn();
			taleData_Pawn.pawn = pawn;
			taleData_Pawn.kind = pawn.kindDef;
			taleData_Pawn.faction = pawn.Faction;
			taleData_Pawn.gender = ((!pawn.RaceProps.hasGenders) ? Gender.None : pawn.gender);
			if (pawn.story != null)
			{
				taleData_Pawn.title = pawn.story.title;
			}
			if (pawn.RaceProps.Humanlike)
			{
				taleData_Pawn.name = pawn.Name;
				if (pawn.equipment.Primary != null)
				{
					taleData_Pawn.primaryEquipment = pawn.equipment.Primary.def;
				}
				Apparel apparel;
				if (pawn.apparel.WornApparel.TryRandomElement(out apparel))
				{
					taleData_Pawn.notableApparel = apparel.def;
				}
			}
			return taleData_Pawn;
		}

		public static TaleData_Pawn GenerateRandom()
		{
			PawnKindDef random = DefDatabase<PawnKindDef>.GetRandom();
			Faction faction = FactionUtility.DefaultFactionFrom(random.defaultFactionType);
			Pawn pawn = PawnGenerator.GeneratePawn(random, faction);
			return TaleData_Pawn.GenerateFrom(pawn);
		}
	}
}

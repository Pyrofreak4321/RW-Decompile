using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.Grammar;

namespace RimWorld
{
	public class TaleData_Def : TaleData
	{
		public Def def;

		private string tmpDefName;

		private Type tmpDefType;

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				this.tmpDefName = ((this.def == null) ? null : this.def.defName);
				this.tmpDefType = ((this.def == null) ? null : this.def.GetType());
			}
			Scribe_Values.Look<string>(ref this.tmpDefName, "defName", null, false);
			Scribe_Values.Look<Type>(ref this.tmpDefType, "defType", null, false);
			if (Scribe.mode == LoadSaveMode.LoadingVars && this.tmpDefName != null)
			{
				this.def = GenDefDatabase.GetDef(this.tmpDefType, BackCompatibility.BackCompatibleDefName(this.tmpDefType, this.tmpDefName, false), true);
			}
		}

		[DebuggerHidden]
		public override IEnumerable<Rule> GetRules(string prefix)
		{
			if (this.def != null)
			{
				yield return new Rule_String(prefix + "_label", this.def.label);
				yield return new Rule_String(prefix + "_definite", Find.ActiveLanguageWorker.WithDefiniteArticle(this.def.label, false, false));
				yield return new Rule_String(prefix + "_indefinite", Find.ActiveLanguageWorker.WithIndefiniteArticle(this.def.label, false, false));
			}
		}

		public static TaleData_Def GenerateFrom(Def def)
		{
			return new TaleData_Def
			{
				def = def
			};
		}
	}
}

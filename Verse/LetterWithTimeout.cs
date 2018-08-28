using System;
using UnityEngine;

namespace Verse
{
	public abstract class LetterWithTimeout : Letter
	{
		public int disappearAtTick = -1;

		public bool TimeoutActive
		{
			get
			{
				return this.disappearAtTick >= 0;
			}
		}

		public bool TimeoutPassed
		{
			get
			{
				return this.TimeoutActive && Find.TickManager.TicksGame >= this.disappearAtTick;
			}
		}

		public override bool CanShowInLetterStack
		{
			get
			{
				return base.CanShowInLetterStack && !this.TimeoutPassed;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.disappearAtTick, "disappearAtTick", -1, false);
		}

		public void StartTimeout(int duration)
		{
			this.disappearAtTick = Find.TickManager.TicksGame + duration;
		}

		protected override string PostProcessedLabel()
		{
			string text = base.PostProcessedLabel();
			if (this.TimeoutActive)
			{
				int num = Mathf.RoundToInt((float)(this.disappearAtTick - Find.TickManager.TicksGame) / 2500f);
				string text2 = text;
				text = string.Concat(new object[]
				{
					text2,
					" (",
					num,
					"LetterHour".Translate(),
					")"
				});
			}
			return text;
		}
	}
}

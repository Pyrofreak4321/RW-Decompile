using System;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public static class GenDate
	{
		public const int TicksPerDay = 60000;

		public const int HoursPerDay = 24;

		public const int DaysPerTwelfth = 5;

		public const int TwelfthsPerYear = 12;

		public const int GameStartHourOfDay = 6;

		public const int TicksPerTwelfth = 300000;

		public const int TicksPerSeason = 900000;

		public const int TicksPerQuadrum = 900000;

		public const int TicksPerYear = 3600000;

		public const int DaysPerYear = 60;

		public const int DaysPerSeason = 15;

		public const int DaysPerQuadrum = 15;

		public const int TicksPerHour = 2500;

		public const float TimeZoneWidth = 15f;

		public const int DefaultStartingYear = 5500;

		private static int TicksGame
		{
			get
			{
				return Find.TickManager.TicksGame;
			}
		}

		public static int DaysPassed
		{
			get
			{
				return GenDate.DaysPassedAt(GenDate.TicksGame);
			}
		}

		public static float DaysPassedFloat
		{
			get
			{
				return (float)GenDate.TicksGame / 60000f;
			}
		}

		public static int TwelfthsPassed
		{
			get
			{
				return GenDate.TwelfthsPassedAt(GenDate.TicksGame);
			}
		}

		public static float TwelfthsPassedFloat
		{
			get
			{
				return (float)GenDate.TicksGame / 300000f;
			}
		}

		public static int YearsPassed
		{
			get
			{
				return GenDate.YearsPassedAt(GenDate.TicksGame);
			}
		}

		public static float YearsPassedFloat
		{
			get
			{
				return (float)GenDate.TicksGame / 3600000f;
			}
		}

		public static int TickAbsToGame(int absTick)
		{
			return absTick - Find.TickManager.gameStartAbsTick;
		}

		public static int TickGameToAbs(int gameTick)
		{
			return gameTick + Find.TickManager.gameStartAbsTick;
		}

		public static int DaysPassedAt(int gameTicks)
		{
			return Mathf.FloorToInt((float)gameTicks / 60000f);
		}

		public static int TwelfthsPassedAt(int gameTicks)
		{
			return Mathf.FloorToInt((float)gameTicks / 300000f);
		}

		public static int YearsPassedAt(int gameTicks)
		{
			return Mathf.FloorToInt((float)gameTicks / 3600000f);
		}

		private static long LocalTicksOffsetFromLongitude(float longitude)
		{
			return (long)GenDate.TimeZoneAt(longitude) * 2500L;
		}

		public static int HourOfDay(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return GenMath.PositiveModRemap(x, 2500, 24);
		}

		public static int DayOfTwelfth(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return GenMath.PositiveModRemap(x, 60000, 5);
		}

		public static int DayOfYear(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return GenMath.PositiveModRemap(x, 60000, 60);
		}

		public static Twelfth Twelfth(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return (Twelfth)GenMath.PositiveModRemap(x, 300000, 12);
		}

		public static Season Season(long absTicks, Vector2 longLat)
		{
			return GenDate.Season(absTicks, longLat.y, longLat.x);
		}

		public static Season Season(long absTicks, float latitude, float longitude)
		{
			float yearPct = GenDate.YearPercent(absTicks, longitude);
			return SeasonUtility.GetReportedSeason(yearPct, latitude);
		}

		public static Quadrum Quadrum(long absTicks, float longitude)
		{
			Twelfth twelfth = GenDate.Twelfth(absTicks, longitude);
			return twelfth.GetQuadrum();
		}

		public static int Year(long absTicks, float longitude)
		{
			long num = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return 5500 + Mathf.FloorToInt((float)num / 3600000f);
		}

		public static int DayOfSeason(long absTicks, float longitude)
		{
			int num = GenDate.DayOfYear(absTicks, longitude);
			return (num - (int)(SeasonUtility.FirstSeason.GetFirstTwelfth(0f) * RimWorld.Twelfth.Sixth)) % 15;
		}

		public static int DayOfQuadrum(long absTicks, float longitude)
		{
			int num = GenDate.DayOfYear(absTicks, longitude);
			return (num - (int)(QuadrumUtility.FirstQuadrum.GetFirstTwelfth() * RimWorld.Twelfth.Sixth)) % 15;
		}

		public static int DayTick(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return (int)GenMath.PositiveMod(x, 60000L);
		}

		public static float DayPercent(long absTicks, float longitude)
		{
			int num = GenDate.DayTick(absTicks, longitude);
			if (num == 0)
			{
				num = 1;
			}
			return (float)num / 60000f;
		}

		public static float YearPercent(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			int num = (int)GenMath.PositiveMod(x, 3600000L);
			return (float)num / 3600000f;
		}

		public static int HourInteger(long absTicks, float longitude)
		{
			long x = absTicks + GenDate.LocalTicksOffsetFromLongitude(longitude);
			return GenMath.PositiveModRemap(x, 2500, 24);
		}

		public static float HourFloat(long absTicks, float longitude)
		{
			return GenDate.DayPercent(absTicks, longitude) * 24f;
		}

		public static string DateFullStringAt(long absTicks, Vector2 location)
		{
			int num = GenDate.DayOfSeason(absTicks, location.x) + 1;
			string text = Find.ActiveLanguageWorker.OrdinalNumber(num);
			return "FullDate".Translate(new object[]
			{
				text,
				GenDate.Quadrum(absTicks, location.x).Label(),
				GenDate.Year(absTicks, location.x),
				num
			});
		}

		public static string DateReadoutStringAt(long absTicks, Vector2 location)
		{
			int num = GenDate.DayOfSeason(absTicks, location.x) + 1;
			string text = Find.ActiveLanguageWorker.OrdinalNumber(num);
			return "DateReadout".Translate(new object[]
			{
				text,
				GenDate.Quadrum(absTicks, location.x).Label(),
				GenDate.Year(absTicks, location.x),
				num
			});
		}

		public static string SeasonDateStringAt(long absTicks, Vector2 longLat)
		{
			int num = GenDate.DayOfSeason(absTicks, longLat.x) + 1;
			string text = Find.ActiveLanguageWorker.OrdinalNumber(num);
			return "SeasonFullDate".Translate(new object[]
			{
				text,
				GenDate.Season(absTicks, longLat).Label(),
				num
			});
		}

		public static string SeasonDateStringAt(Twelfth twelfth, Vector2 longLat)
		{
			return GenDate.SeasonDateStringAt((long)((int)twelfth * 300000 + 1), longLat);
		}

		public static string QuadrumDateStringAt(long absTicks, float longitude)
		{
			int num = GenDate.DayOfQuadrum(absTicks, longitude) + 1;
			string text = Find.ActiveLanguageWorker.OrdinalNumber(num);
			return "SeasonFullDate".Translate(new object[]
			{
				text,
				GenDate.Quadrum(absTicks, longitude).Label(),
				num
			});
		}

		public static string QuadrumDateStringAt(Quadrum quadrum)
		{
			return GenDate.QuadrumDateStringAt((long)((int)quadrum * 900000 + 1), 0f);
		}

		public static string QuadrumDateStringAt(Twelfth twelfth)
		{
			return GenDate.QuadrumDateStringAt((long)((int)twelfth * 300000 + 1), 0f);
		}

		public static float TicksToDays(this int numTicks)
		{
			return (float)numTicks / 60000f;
		}

		public static string ToStringTicksToDays(this int numTicks, string format = "F1")
		{
			string text = numTicks.TicksToDays().ToString(format);
			if (text == "1")
			{
				return "Period1Day".Translate();
			}
			return text + " " + "DaysLower".Translate();
		}

		public static string ToStringTicksToPeriod(this int numTicks)
		{
			if (numTicks < 2500 && (numTicks < 600 || Math.Round((double)((float)numTicks / 2500f), 1) == 0.0))
			{
				int num = Mathf.RoundToInt((float)numTicks / 60f);
				if (num == 1)
				{
					return "Period1Second".Translate();
				}
				return "PeriodSeconds".Translate(new object[]
				{
					num
				});
			}
			else if (numTicks < 60000)
			{
				if (numTicks < 2500)
				{
					string text = ((float)numTicks / 2500f).ToString("0.#");
					if (text == "1")
					{
						return "Period1Hour".Translate();
					}
					return "PeriodHours".Translate(new object[]
					{
						text
					});
				}
				else
				{
					int num2 = Mathf.RoundToInt((float)numTicks / 2500f);
					if (num2 == 1)
					{
						return "Period1Hour".Translate();
					}
					return "PeriodHours".Translate(new object[]
					{
						num2
					});
				}
			}
			else if (numTicks < 3600000)
			{
				string text2 = ((float)numTicks / 60000f).ToStringDecimalIfSmall();
				if (text2 == "1")
				{
					return "Period1Day".Translate();
				}
				return "PeriodDays".Translate(new object[]
				{
					text2
				});
			}
			else
			{
				string text3 = ((float)numTicks / 3600000f).ToStringDecimalIfSmall();
				if (text3 == "1")
				{
					return "Period1Year".Translate();
				}
				return "PeriodYears".Translate(new object[]
				{
					text3
				});
			}
		}

		public static string ToStringTicksToPeriodVerbose(this int numTicks, bool allowHours = true, bool allowQuadrums = true)
		{
			if (numTicks < 0)
			{
				return "0";
			}
			int num;
			int num2;
			int num3;
			float num4;
			numTicks.TicksToPeriod(out num, out num2, out num3, out num4);
			if (!allowQuadrums)
			{
				num3 += 15 * num2;
				num2 = 0;
			}
			if (num > 0)
			{
				string text;
				if (num == 1)
				{
					text = "Period1Year".Translate();
				}
				else
				{
					text = "PeriodYears".Translate(new object[]
					{
						num
					});
				}
				if (num2 > 0)
				{
					text += ", ";
					if (num2 == 1)
					{
						text += "Period1Quadrum".Translate();
					}
					else
					{
						text += "PeriodQuadrums".Translate(new object[]
						{
							num2
						});
					}
				}
				return text;
			}
			if (num2 > 0)
			{
				string text2;
				if (num2 == 1)
				{
					text2 = "Period1Quadrum".Translate();
				}
				else
				{
					text2 = "PeriodQuadrums".Translate(new object[]
					{
						num2
					});
				}
				if (num3 > 0)
				{
					text2 += ", ";
					if (num3 == 1)
					{
						text2 += "Period1Day".Translate();
					}
					else
					{
						text2 += "PeriodDays".Translate(new object[]
						{
							num3
						});
					}
				}
				return text2;
			}
			if (num3 > 0)
			{
				string text3;
				if (num3 == 1)
				{
					text3 = "Period1Day".Translate();
				}
				else
				{
					text3 = "PeriodDays".Translate(new object[]
					{
						num3
					});
				}
				int num5 = (int)num4;
				if (allowHours && num5 > 0)
				{
					text3 += ", ";
					if (num5 == 1)
					{
						text3 += "Period1Hour".Translate();
					}
					else
					{
						text3 += "PeriodHours".Translate(new object[]
						{
							num5
						});
					}
				}
				return text3;
			}
			if (!allowHours)
			{
				return "PeriodDays".Translate(new object[]
				{
					0
				});
			}
			if (num4 > 1f)
			{
				int num6 = Mathf.RoundToInt(num4);
				if (num6 == 1)
				{
					return "Period1Hour".Translate();
				}
				return "PeriodHours".Translate(new object[]
				{
					num6
				});
			}
			else
			{
				if (Math.Round((double)num4, 1) == 1.0)
				{
					return "Period1Hour".Translate();
				}
				return "PeriodHours".Translate(new object[]
				{
					num4.ToString("0.#")
				});
			}
		}

		public static string ToStringTicksToPeriodVague(this int numTicks, bool vagueMin = true, bool vagueMax = true)
		{
			if (vagueMax && numTicks > 36000000)
			{
				return "OverADecade".Translate();
			}
			if (vagueMin && numTicks < 60000)
			{
				return "LessThanADay".Translate();
			}
			return numTicks.ToStringTicksToPeriod();
		}

		public static void TicksToPeriod(this int numTicks, out int years, out int quadrums, out int days, out float hoursFloat)
		{
			((long)numTicks).TicksToPeriod(out years, out quadrums, out days, out hoursFloat);
		}

		public static void TicksToPeriod(this long numTicks, out int years, out int quadrums, out int days, out float hoursFloat)
		{
			if (numTicks < 0L)
			{
				Log.ErrorOnce("Tried to calculate period for negative ticks", 12841103, false);
			}
			years = (int)(numTicks / 3600000L);
			long num = numTicks - (long)years * 3600000L;
			quadrums = (int)(num / 900000L);
			num -= (long)quadrums * 900000L;
			days = (int)(num / 60000L);
			num -= (long)days * 60000L;
			hoursFloat = (float)num / 2500f;
		}

		public static string ToStringApproxAge(this float yearsFloat)
		{
			if (yearsFloat >= 1f)
			{
				return ((int)yearsFloat).ToStringCached();
			}
			int num = (int)(yearsFloat * 3600000f);
			num = Mathf.Min(num, 3599999);
			int num2;
			int num3;
			int num4;
			float num5;
			num.TicksToPeriod(out num2, out num3, out num4, out num5);
			if (num2 > 0)
			{
				if (num2 == 1)
				{
					return "Period1Year".Translate();
				}
				return "PeriodYears".Translate(new object[]
				{
					num2
				});
			}
			else if (num3 > 0)
			{
				if (num3 == 1)
				{
					return "Period1Quadrum".Translate();
				}
				return "PeriodQuadrums".Translate(new object[]
				{
					num3
				});
			}
			else if (num4 > 0)
			{
				if (num4 == 1)
				{
					return "Period1Day".Translate();
				}
				return "PeriodDays".Translate(new object[]
				{
					num4
				});
			}
			else
			{
				int num6 = (int)num5;
				if (num6 == 1)
				{
					return "Period1Hour".Translate();
				}
				return "PeriodHours".Translate(new object[]
				{
					num6
				});
			}
		}

		public static int TimeZoneAt(float longitude)
		{
			return Mathf.RoundToInt(GenDate.TimeZoneFloatAt(longitude));
		}

		public static float TimeZoneFloatAt(float longitude)
		{
			return longitude / 15f;
		}
	}
}

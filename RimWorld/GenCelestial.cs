using System;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public static class GenCelestial
	{
		public struct LightInfo
		{
			public Vector2 vector;

			public float intensity;
		}

		public enum LightType
		{
			Shadow,
			LightingSun,
			LightingMoon
		}

		public const float ShadowMaxLengthDay = 15f;

		public const float ShadowMaxLengthNight = 15f;

		private const float ShadowGlowLerpSpan = 0.15f;

		private const float ShadowDayNightThreshold = 0.6f;

		private static SimpleCurve SunPeekAroundDegreesFactorCurve = new SimpleCurve
		{
			{
				new CurvePoint(70f, 1f),
				true
			},
			{
				new CurvePoint(75f, 0.05f),
				true
			}
		};

		private static SimpleCurve SunOffsetFractionFromLatitudeCurve = new SimpleCurve
		{
			{
				new CurvePoint(70f, 0.2f),
				true
			},
			{
				new CurvePoint(75f, 1.5f),
				true
			}
		};

		private static int TicksAbsForSunPosInWorldSpace
		{
			get
			{
				if (Current.ProgramState != ProgramState.Entry)
				{
					return GenTicks.TicksAbs;
				}
				int startingTile = Find.GameInitData.startingTile;
				float longitude = (startingTile < 0) ? 0f : Find.WorldGrid.LongLatOf(startingTile).x;
				return Mathf.RoundToInt(2500f * (12f - GenDate.TimeZoneFloatAt(longitude)));
			}
		}

		public static float CurCelestialSunGlow(Map map)
		{
			return GenCelestial.CelestialSunGlow(map, Find.TickManager.TicksAbs);
		}

		public static float CelestialSunGlow(Map map, int ticksAbs)
		{
			Vector2 vector = Find.WorldGrid.LongLatOf(map.Tile);
			return GenCelestial.CelestialSunGlowPercent(vector.y, GenDate.DayOfYear((long)ticksAbs, vector.x), GenDate.DayPercent((long)ticksAbs, vector.x));
		}

		public static float CurShadowStrength(Map map)
		{
			return Mathf.Clamp01(Mathf.Abs(GenCelestial.CurCelestialSunGlow(map) - 0.6f) / 0.15f);
		}

		public static GenCelestial.LightInfo GetLightSourceInfo(Map map, GenCelestial.LightType type)
		{
			float num = GenLocalDate.DayPercent(map);
			bool flag;
			float intensity;
			if (type == GenCelestial.LightType.Shadow)
			{
				flag = GenCelestial.IsDaytime(GenCelestial.CurCelestialSunGlow(map));
				intensity = GenCelestial.CurShadowStrength(map);
			}
			else if (type == GenCelestial.LightType.LightingSun)
			{
				flag = true;
				intensity = Mathf.Clamp01((GenCelestial.CurCelestialSunGlow(map) - 0.6f + 0.2f) / 0.15f);
			}
			else if (type == GenCelestial.LightType.LightingMoon)
			{
				flag = false;
				intensity = Mathf.Clamp01(-(GenCelestial.CurCelestialSunGlow(map) - 0.6f - 0.2f) / 0.15f);
			}
			else
			{
				Log.ErrorOnce("Invalid light type requested", 64275614, false);
				flag = true;
				intensity = 0f;
			}
			float t;
			float num2;
			float num3;
			if (flag)
			{
				t = num;
				num2 = -1.5f;
				num3 = 15f;
			}
			else
			{
				if (num > 0.5f)
				{
					t = Mathf.InverseLerp(0.5f, 1f, num) * 0.5f;
				}
				else
				{
					t = 0.5f + Mathf.InverseLerp(0f, 0.5f, num) * 0.5f;
				}
				num2 = -0.9f;
				num3 = 15f;
			}
			float num4 = Mathf.LerpUnclamped(-num3, num3, t);
			float y = num2 - 2.5f * (num4 * num4 / 100f);
			return new GenCelestial.LightInfo
			{
				vector = new Vector2(num4, y),
				intensity = intensity
			};
		}

		public static Vector3 CurSunPositionInWorldSpace()
		{
			int ticksAbsForSunPosInWorldSpace = GenCelestial.TicksAbsForSunPosInWorldSpace;
			return GenCelestial.SunPositionUnmodified((float)GenDate.DayOfYear((long)ticksAbsForSunPosInWorldSpace, 0f), GenDate.DayPercent((long)ticksAbsForSunPosInWorldSpace, 0f), new Vector3(0f, 0f, -1f), 0f);
		}

		public static bool IsDaytime(float glow)
		{
			return glow > 0.6f;
		}

		private static Vector3 SunPosition(float latitude, int dayOfYear, float dayPercent)
		{
			Vector3 target = GenCelestial.SurfaceNormal(latitude);
			Vector3 current = GenCelestial.SunPositionUnmodified((float)dayOfYear, dayPercent, new Vector3(1f, 0f, 0f), latitude);
			float num = GenCelestial.SunPeekAroundDegreesFactorCurve.Evaluate(latitude);
			current = Vector3.RotateTowards(current, target, 0.331612557f * num, 9999999f);
			float num2 = Mathf.InverseLerp(60f, 0f, Mathf.Abs(latitude));
			if (num2 > 0f)
			{
				current = Vector3.RotateTowards(current, target, 6.28318548f * (17f * num2 / 360f), 9999999f);
			}
			return current.normalized;
		}

		private static Vector3 SunPositionUnmodified(float dayOfYear, float dayPercent, Vector3 initialSunPos, float latitude = 0f)
		{
			Vector3 point = initialSunPos * 100f;
			float num = dayOfYear / 60f;
			float f = num * 3.14159274f * 2f;
			float num2 = -Mathf.Cos(f);
			point.y += num2 * 100f * GenCelestial.SunOffsetFractionFromLatitudeCurve.Evaluate(latitude);
			float angle = (dayPercent - 0.5f) * 360f;
			point = Quaternion.AngleAxis(angle, Vector3.up) * point;
			return point.normalized;
		}

		private static float CelestialSunGlowPercent(float latitude, int dayOfYear, float dayPercent)
		{
			Vector3 vector = GenCelestial.SurfaceNormal(latitude);
			Vector3 rhs = GenCelestial.SunPosition(latitude, dayOfYear, dayPercent);
			float value = Vector3.Dot(vector.normalized, rhs);
			float value2 = Mathf.InverseLerp(0f, 0.7f, value);
			return Mathf.Clamp01(value2);
		}

		public static float AverageGlow(float latitude, int dayOfYear)
		{
			float num = 0f;
			for (int i = 0; i < 24; i++)
			{
				num += GenCelestial.CelestialSunGlowPercent(latitude, dayOfYear, (float)i / 24f);
			}
			return num / 24f;
		}

		private static Vector3 SurfaceNormal(float latitude)
		{
			Vector3 vector = new Vector3(1f, 0f, 0f);
			vector = Quaternion.AngleAxis(latitude, new Vector3(0f, 0f, 1f)) * vector;
			return vector;
		}

		public static void LogSunGlowForYear()
		{
			for (int i = -90; i <= 90; i += 10)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("Sun visibility percents for latitude " + i + ", for each hour of each day of the year");
				stringBuilder.AppendLine("---------------------------------------");
				stringBuilder.Append("Day/hr".PadRight(6));
				for (int j = 0; j < 24; j += 2)
				{
					stringBuilder.Append((j.ToString() + "h").PadRight(6));
				}
				stringBuilder.AppendLine();
				for (int k = 0; k < 60; k += 5)
				{
					stringBuilder.Append(k.ToString().PadRight(6));
					for (int l = 0; l < 24; l += 2)
					{
						stringBuilder.Append(GenCelestial.CelestialSunGlowPercent((float)i, k, (float)l / 24f).ToString("F2").PadRight(6));
					}
					stringBuilder.AppendLine();
				}
				Log.Message(stringBuilder.ToString(), false);
			}
		}

		public static void LogSunAngleForYear()
		{
			for (int i = -90; i <= 90; i += 10)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("Sun angles for latitude " + i + ", for each hour of each day of the year");
				stringBuilder.AppendLine("---------------------------------------");
				stringBuilder.Append("Day/hr".PadRight(6));
				for (int j = 0; j < 24; j += 2)
				{
					stringBuilder.Append((j.ToString() + "h").PadRight(6));
				}
				stringBuilder.AppendLine();
				for (int k = 0; k < 60; k += 5)
				{
					stringBuilder.Append(k.ToString().PadRight(6));
					for (int l = 0; l < 24; l += 2)
					{
						float num = Vector3.Angle(GenCelestial.SurfaceNormal((float)i), GenCelestial.SunPositionUnmodified((float)k, (float)l / 24f, new Vector3(1f, 0f, 0f), 0f));
						stringBuilder.Append((90f - num).ToString("F0").PadRight(6));
					}
					stringBuilder.AppendLine();
				}
				Log.Message(stringBuilder.ToString(), false);
			}
		}
	}
}

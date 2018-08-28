using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class ColonistBarColonistDrawer
	{
		private Dictionary<string, string> pawnLabelsCache = new Dictionary<string, string>();

		private static readonly Texture2D MoodBGTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.4f, 0.47f, 0.53f, 0.44f));

		private static readonly Texture2D DeadColonistTex = ContentFinder<Texture2D>.Get("UI/Misc/DeadColonist", true);

		private static readonly Texture2D Icon_FormingCaravan = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/FormingCaravan", true);

		private static readonly Texture2D Icon_MentalStateNonAggro = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/MentalStateNonAggro", true);

		private static readonly Texture2D Icon_MentalStateAggro = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/MentalStateAggro", true);

		private static readonly Texture2D Icon_MedicalRest = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/MedicalRest", true);

		private static readonly Texture2D Icon_Sleeping = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Sleeping", true);

		private static readonly Texture2D Icon_Fleeing = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Fleeing", true);

		private static readonly Texture2D Icon_Attacking = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Attacking", true);

		private static readonly Texture2D Icon_Idle = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Idle", true);

		private static readonly Texture2D Icon_Burning = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Burning", true);

		private static readonly Texture2D Icon_Inspired = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Inspired", true);

		public static readonly Vector2 PawnTextureSize = new Vector2(ColonistBar.BaseSize.x - 2f, 75f);

		public static readonly Vector3 PawnTextureCameraOffset = new Vector3(0f, 0f, 0.3f);

		public const float PawnTextureCameraZoom = 1.28205f;

		private const float PawnTextureHorizontalPadding = 1f;

		private const float BaseIconSize = 20f;

		private const float BaseGroupFrameMargin = 12f;

		public const float DoubleClickTime = 0.5f;

		private static Vector2[] bracketLocs = new Vector2[4];

		private ColonistBar ColonistBar
		{
			get
			{
				return Find.ColonistBar;
			}
		}

		public void DrawColonist(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
		{
			float num = this.ColonistBar.GetEntryRectAlpha(rect);
			this.ApplyEntryInAnotherMapAlphaFactor(pawnMap, ref num);
			if (reordering)
			{
				num *= 0.5f;
			}
			Color color = new Color(1f, 1f, 1f, num);
			GUI.color = color;
			GUI.DrawTexture(rect, ColonistBar.BGTex);
			if (colonist.needs != null && colonist.needs.mood != null)
			{
				Rect position = rect.ContractedBy(2f);
				float num2 = position.height * colonist.needs.mood.CurLevelPercentage;
				position.yMin = position.yMax - num2;
				position.height = num2;
				GUI.DrawTexture(position, ColonistBarColonistDrawer.MoodBGTex);
			}
			if (highlight)
			{
				int thickness = (rect.width > 22f) ? 3 : 2;
				GUI.color = Color.white;
				Widgets.DrawBox(rect, thickness);
				GUI.color = color;
			}
			Rect rect2 = rect.ContractedBy(-2f * this.ColonistBar.Scale);
			bool flag = (!colonist.Dead) ? Find.Selector.SelectedObjects.Contains(colonist) : Find.Selector.SelectedObjects.Contains(colonist.Corpse);
			if (flag && !WorldRendererUtility.WorldRenderedNow)
			{
				this.DrawSelectionOverlayOnGUI(colonist, rect2);
			}
			else if (WorldRendererUtility.WorldRenderedNow && colonist.IsCaravanMember() && Find.WorldSelector.IsSelected(colonist.GetCaravan()))
			{
				this.DrawCaravanSelectionOverlayOnGUI(colonist.GetCaravan(), rect2);
			}
			GUI.DrawTexture(this.GetPawnTextureRect(rect.position), PortraitsCache.Get(colonist, ColonistBarColonistDrawer.PawnTextureSize, ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));
			GUI.color = new Color(1f, 1f, 1f, num * 0.8f);
			this.DrawIcons(rect, colonist);
			GUI.color = color;
			if (colonist.Dead)
			{
				GUI.DrawTexture(rect, ColonistBarColonistDrawer.DeadColonistTex);
			}
			float num3 = 4f * this.ColonistBar.Scale;
			Vector2 pos = new Vector2(rect.center.x, rect.yMax - num3);
			GenMapUI.DrawPawnLabel(colonist, pos, num, rect.width + this.ColonistBar.SpaceBetweenColonistsHorizontal - 2f, this.pawnLabelsCache, GameFont.Tiny, true, true);
			Text.Font = GameFont.Small;
			GUI.color = Color.white;
		}

		private Rect GroupFrameRect(int group)
		{
			float num = 99999f;
			float num2 = 0f;
			float num3 = 0f;
			List<ColonistBar.Entry> entries = this.ColonistBar.Entries;
			List<Vector2> drawLocs = this.ColonistBar.DrawLocs;
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].group == group)
				{
					num = Mathf.Min(num, drawLocs[i].x);
					num2 = Mathf.Max(num2, drawLocs[i].x + this.ColonistBar.Size.x);
					num3 = Mathf.Max(num3, drawLocs[i].y + this.ColonistBar.Size.y);
				}
			}
			return new Rect(num, 0f, num2 - num, num3).ContractedBy(-12f * this.ColonistBar.Scale);
		}

		public void DrawGroupFrame(int group)
		{
			Rect position = this.GroupFrameRect(group);
			List<ColonistBar.Entry> entries = this.ColonistBar.Entries;
			Map map = entries.Find((ColonistBar.Entry x) => x.group == group).map;
			float num;
			if (map == null)
			{
				if (WorldRendererUtility.WorldRenderedNow)
				{
					num = 1f;
				}
				else
				{
					num = 0.75f;
				}
			}
			else if (map != Find.CurrentMap || WorldRendererUtility.WorldRenderedNow)
			{
				num = 0.75f;
			}
			else
			{
				num = 1f;
			}
			Widgets.DrawRectFast(position, new Color(0.5f, 0.5f, 0.5f, 0.4f * num), null);
		}

		private void ApplyEntryInAnotherMapAlphaFactor(Map map, ref float alpha)
		{
			if (map == null)
			{
				if (!WorldRendererUtility.WorldRenderedNow)
				{
					alpha = Mathf.Min(alpha, 0.4f);
				}
			}
			else if (map != Find.CurrentMap || WorldRendererUtility.WorldRenderedNow)
			{
				alpha = Mathf.Min(alpha, 0.4f);
			}
		}

		public void HandleClicks(Rect rect, Pawn colonist, int reorderableGroup, out bool reordering)
		{
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2 && Mouse.IsOver(rect))
			{
				Event.current.Use();
				CameraJumper.TryJump(colonist);
			}
			reordering = ReorderableWidget.Reorderable(reorderableGroup, rect, true);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(rect))
			{
				Event.current.Use();
			}
		}

		public void HandleGroupFrameClicks(int group)
		{
			Rect rect = this.GroupFrameRect(group);
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && Mouse.IsOver(rect) && !this.ColonistBar.AnyColonistOrCorpseAt(UI.MousePositionOnUIInverted))
			{
				bool worldRenderedNow = WorldRendererUtility.WorldRenderedNow;
				if ((!worldRenderedNow && !Find.Selector.dragBox.IsValidAndActive) || (worldRenderedNow && !Find.WorldSelector.dragBox.IsValidAndActive))
				{
					Find.Selector.dragBox.active = false;
					Find.WorldSelector.dragBox.active = false;
					ColonistBar.Entry entry = this.ColonistBar.Entries.Find((ColonistBar.Entry x) => x.group == group);
					Map map = entry.map;
					if (map == null)
					{
						if (WorldRendererUtility.WorldRenderedNow)
						{
							CameraJumper.TrySelect(entry.pawn);
						}
						else
						{
							CameraJumper.TryJumpAndSelect(entry.pawn);
						}
					}
					else
					{
						if (!CameraJumper.TryHideWorld() && Find.CurrentMap != map)
						{
							SoundDefOf.MapSelected.PlayOneShotOnCamera(null);
						}
						Current.Game.CurrentMap = map;
					}
				}
			}
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(rect))
			{
				Event.current.Use();
			}
		}

		public void Notify_RecachedEntries()
		{
			this.pawnLabelsCache.Clear();
		}

		public Rect GetPawnTextureRect(Vector2 pos)
		{
			float x = pos.x;
			float y = pos.y;
			Vector2 vector = ColonistBarColonistDrawer.PawnTextureSize * this.ColonistBar.Scale;
			Rect rect = new Rect(x + 1f, y - (vector.y - this.ColonistBar.Size.y) - 1f, vector.x, vector.y);
			rect = rect.ContractedBy(1f);
			return rect;
		}

		private void DrawIcons(Rect rect, Pawn colonist)
		{
			if (colonist.Dead)
			{
				return;
			}
			float num = 20f * this.ColonistBar.Scale;
			Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);
			bool flag = false;
			if (colonist.CurJob != null)
			{
				JobDef def = colonist.CurJob.def;
				if (def == JobDefOf.AttackMelee || def == JobDefOf.AttackStatic)
				{
					flag = true;
				}
				else if (def == JobDefOf.Wait_Combat)
				{
					Stance_Busy stance_Busy = colonist.stances.curStance as Stance_Busy;
					if (stance_Busy != null && stance_Busy.focusTarg.IsValid)
					{
						flag = true;
					}
				}
			}
			if (colonist.IsFormingCaravan())
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_FormingCaravan, ref vector, "ActivityIconFormingCaravan".Translate());
			}
			if (colonist.InAggroMentalState)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_MentalStateAggro, ref vector, colonist.MentalStateDef.LabelCap);
			}
			else if (colonist.InMentalState)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_MentalStateNonAggro, ref vector, colonist.MentalStateDef.LabelCap);
			}
			else if (colonist.InBed() && colonist.CurrentBed().Medical)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_MedicalRest, ref vector, "ActivityIconMedicalRest".Translate());
			}
			else if (colonist.CurJob != null && colonist.jobs.curDriver.asleep)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_Sleeping, ref vector, "ActivityIconSleeping".Translate());
			}
			else if (colonist.CurJob != null && colonist.CurJob.def == JobDefOf.FleeAndCower)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_Fleeing, ref vector, "ActivityIconFleeing".Translate());
			}
			else if (flag)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_Attacking, ref vector, "ActivityIconAttacking".Translate());
			}
			else if (colonist.mindState.IsIdle && GenDate.DaysPassed >= 1)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_Idle, ref vector, "ActivityIconIdle".Translate());
			}
			if (colonist.IsBurning() && vector.x + num <= rect.xMax)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_Burning, ref vector, "ActivityIconBurning".Translate());
			}
			if (colonist.Inspired && vector.x + num <= rect.xMax)
			{
				this.DrawIcon(ColonistBarColonistDrawer.Icon_Inspired, ref vector, colonist.InspirationDef.LabelCap);
			}
		}

		private void DrawIcon(Texture2D icon, ref Vector2 pos, string tooltip)
		{
			float num = 20f * this.ColonistBar.Scale;
			Rect rect = new Rect(pos.x, pos.y, num, num);
			GUI.DrawTexture(rect, icon);
			TooltipHandler.TipRegion(rect, tooltip);
			pos.x += num;
		}

		private void DrawSelectionOverlayOnGUI(Pawn colonist, Rect rect)
		{
			Thing obj = colonist;
			if (colonist.Dead)
			{
				obj = colonist.Corpse;
			}
			float num = 0.4f * this.ColonistBar.Scale;
			Vector2 textureSize = new Vector2((float)SelectionDrawerUtility.SelectedTexGUI.width * num, (float)SelectionDrawerUtility.SelectedTexGUI.height * num);
			SelectionDrawerUtility.CalculateSelectionBracketPositionsUI<object>(ColonistBarColonistDrawer.bracketLocs, obj, rect, SelectionDrawer.SelectTimes, textureSize, 20f * this.ColonistBar.Scale);
			this.DrawSelectionOverlayOnGUI(ColonistBarColonistDrawer.bracketLocs, num);
		}

		private void DrawCaravanSelectionOverlayOnGUI(Caravan caravan, Rect rect)
		{
			float num = 0.4f * this.ColonistBar.Scale;
			Vector2 textureSize = new Vector2((float)SelectionDrawerUtility.SelectedTexGUI.width * num, (float)SelectionDrawerUtility.SelectedTexGUI.height * num);
			SelectionDrawerUtility.CalculateSelectionBracketPositionsUI<WorldObject>(ColonistBarColonistDrawer.bracketLocs, caravan, rect, WorldSelectionDrawer.SelectTimes, textureSize, 20f * this.ColonistBar.Scale);
			this.DrawSelectionOverlayOnGUI(ColonistBarColonistDrawer.bracketLocs, num);
		}

		private void DrawSelectionOverlayOnGUI(Vector2[] bracketLocs, float selectedTexScale)
		{
			int num = 90;
			for (int i = 0; i < 4; i++)
			{
				Widgets.DrawTextureRotated(bracketLocs[i], SelectionDrawerUtility.SelectedTexGUI, (float)num, selectedTexScale);
				num += 90;
			}
		}
	}
}

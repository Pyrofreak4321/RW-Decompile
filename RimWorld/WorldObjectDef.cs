using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class WorldObjectDef : Def
	{
		public Type worldObjectClass = typeof(WorldObject);

		public bool canHaveFaction = true;

		public bool saved = true;

		public bool canBePlayerHome;

		public List<WorldObjectCompProperties> comps = new List<WorldObjectCompProperties>();

		public bool allowCaravanIncidentsWhichGenerateMap;

		public bool isTempIncidentMapOwner;

		public List<IncidentTargetTagDef> IncidentTargetTags;

		public bool selectable = true;

		public bool neverMultiSelect;

		public MapGeneratorDef mapGenerator;

		public List<Type> inspectorTabs;

		[Unsaved]
		public List<InspectTabBase> inspectorTabsResolved;

		public bool useDynamicDrawer;

		public bool expandingIcon;

		[NoTranslate]
		public string expandingIconTexture;

		public float expandingIconPriority;

		[NoTranslate]
		public string texture;

		[Unsaved]
		private Material material;

		[Unsaved]
		private Texture2D expandingIconTextureInt;

		public bool expandMore;

		public bool blockExitGridUntilBattleIsWon;

		public Material Material
		{
			get
			{
				if (this.texture.NullOrEmpty())
				{
					return null;
				}
				if (this.material == null)
				{
					this.material = MaterialPool.MatFrom(this.texture, ShaderDatabase.WorldOverlayTransparentLit, WorldMaterials.WorldObjectRenderQueue);
				}
				return this.material;
			}
		}

		public Texture2D ExpandingIconTexture
		{
			get
			{
				if (this.expandingIconTextureInt == null)
				{
					if (this.expandingIconTexture.NullOrEmpty())
					{
						return null;
					}
					this.expandingIconTextureInt = ContentFinder<Texture2D>.Get(this.expandingIconTexture, true);
				}
				return this.expandingIconTextureInt;
			}
		}

		public override void PostLoad()
		{
			base.PostLoad();
			if (this.inspectorTabs != null)
			{
				for (int i = 0; i < this.inspectorTabs.Count; i++)
				{
					if (this.inspectorTabsResolved == null)
					{
						this.inspectorTabsResolved = new List<InspectTabBase>();
					}
					try
					{
						this.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(this.inspectorTabs[i]));
					}
					catch (Exception ex)
					{
						Log.Error(string.Concat(new object[]
						{
							"Could not instantiate inspector tab of type ",
							this.inspectorTabs[i],
							": ",
							ex
						}), false);
					}
				}
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			for (int i = 0; i < this.comps.Count; i++)
			{
				this.comps[i].ResolveReferences(this);
			}
		}

		[DebuggerHidden]
		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string e in base.ConfigErrors())
			{
				yield return e;
			}
			for (int i = 0; i < this.comps.Count; i++)
			{
				foreach (string e2 in this.comps[i].ConfigErrors(this))
				{
					yield return e2;
				}
			}
			if (this.expandMore && !this.expandingIcon)
			{
				yield return "has expandMore but doesn't have any expanding icon";
			}
		}
	}
}

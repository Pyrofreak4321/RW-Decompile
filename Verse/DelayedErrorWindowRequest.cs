using System;
using System.Collections.Generic;

namespace Verse
{
	public static class DelayedErrorWindowRequest
	{
		private struct Request
		{
			public string text;

			public string title;
		}

		private static List<DelayedErrorWindowRequest.Request> requests = new List<DelayedErrorWindowRequest.Request>();

		public static void DelayedErrorWindowRequestOnGUI()
		{
			try
			{
				for (int i = 0; i < DelayedErrorWindowRequest.requests.Count; i++)
				{
					WindowStack arg_50_0 = Find.WindowStack;
					string text = DelayedErrorWindowRequest.requests[i].text;
					string buttonAText = "OK".Translate();
					string title = DelayedErrorWindowRequest.requests[i].title;
					arg_50_0.Add(new Dialog_MessageBox(text, buttonAText, null, null, null, title, false, null, null));
				}
			}
			finally
			{
				DelayedErrorWindowRequest.requests.Clear();
			}
		}

		public static void Add(string text, string title = null)
		{
			DelayedErrorWindowRequest.Request item = default(DelayedErrorWindowRequest.Request);
			item.text = text;
			item.title = title;
			DelayedErrorWindowRequest.requests.Add(item);
		}
	}
}

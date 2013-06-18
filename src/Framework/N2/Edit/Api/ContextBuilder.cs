﻿using N2.Edit;
using N2.Edit.Versioning;
using N2.Engine;
using N2.Engine.Globalization;
using N2.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace N2.Management.Api
{
	public class ContextData
	{
		public ContextLanguage Language { get; set; }

		public TreeNode CurrentItem { get; set; }

		public ExtendedContentInfo ExtendedInfo { get; set; }

		public List<string> Flags { get; set; }
	}

	public class ContextLanguage
	{
		public string FlagUrl { get; set; }

		public string LanguageTitle { get; set; }

		public string LanguageCode { get; set; }
	}

	public class ExtendedContentInfo
	{
		public string Created { get; set; }
		public string Expires { get; set; }
		public string FuturePublishDate { get; set; }
		public bool IsPage { get; set; }
		public string Published { get; set; }
		public string SavedBy { get; set; }
		public string Updated { get; set; }
		public bool Visible { get; set; }
		public string ZoneName { get; set; }
		public int VersionIndex { get; set; }
		public string Url { get; set; }
		public bool ReadProtected { get; set; }
		public string TypeName { get; set; }
		public ExtendedContentInfo VersionOf { get; set; }
		public ExtendedContentInfo Draft { get; set; }
	}

	public class ContextBuiltEventArgs : EventArgs
	{
		public ContextData Data { get; internal set; }
	}

	[Service]
	public class ContextBuilder
	{
		private Engine.IEngine engine;

		public ContextBuilder(Engine.IEngine engine)
		{
			this.engine = engine;
		}

		public event EventHandler<ContextBuiltEventArgs> ContextBuilt;

		public virtual ContextData GetInterfaceContextData(ContentItem item, string selectedUrl)
		{
			var data = new ContextData();

			if (item != null)
			{
				var adapter = engine.GetContentAdapter<NodeAdapter>(item);
				data.CurrentItem = adapter.GetTreeNode(item);
				data.ExtendedInfo = CreateExtendedContextData(item, resolveVersions: true);
				var l = adapter.GetLanguage(item);
				if (l != null)
					data.Language = new ContextLanguage { FlagUrl = Url.ToAbsolute(l.FlagUrl), LanguageCode = l.LanguageCode, LanguageTitle = l.LanguageTitle };
				data.Flags = adapter.GetNodeFlags(item).ToList();
			}
			else
				data.Flags = new List<string>();

			var mangementUrl = "{ManagementUrl}".ResolveUrlTokens();
			if (selectedUrl != null && selectedUrl.StartsWith(mangementUrl, StringComparison.InvariantCultureIgnoreCase))
			{
				data.Flags.Add("Management");
				data.Flags.Add(selectedUrl.Substring(mangementUrl.Length).ToUrl().PathWithoutExtension.Replace("/", ""));
			}

			if (ContextBuilt != null)
				ContextBuilt(this, new ContextBuiltEventArgs { Data = data });

			return data;
		}

		private ExtendedContentInfo CreateExtendedContextData(ContentItem item, bool resolveVersions = false)
		{
			if (item == null)
				return null;

			var data = new ExtendedContentInfo
			{
				Created = item.Created.ToString("o"),
				Expires = item.Expires.HasValue ? item.Expires.Value.ToString("o") : null,
				IsPage = item.IsPage,
				Published = item.Published.HasValue ? item.Published.Value.ToString("o") : null,
				SavedBy = item.SavedBy,
				Updated = item.Updated.ToString("o"),
				Visible = item.Visible,
				ZoneName = item.ZoneName,
				VersionIndex = item.VersionIndex,
				Url = item.Url,
				ReadProtected = !engine.SecurityManager.IsAuthorized(item, new GenericPrincipal(new GenericIdentity(""), null)),
				TypeName = item.GetContentType().Name
			};
			if (resolveVersions)
			{
				var draftInfo = engine.Resolve<DraftRepository>().GetDraftInfo(item);
				data.Draft = CreateExtendedContextData(draftInfo != null ? engine.Resolve<IVersionManager>().GetVersion(item, draftInfo.VersionIndex) : null);
				if (data.Draft != null)
				{
					data.Draft.SavedBy = draftInfo.SavedBy;
					data.Draft.Updated = draftInfo.Saved.ToString("o");
				}
				data.VersionOf = CreateExtendedContextData(item.VersionOf);
			};
			if (item.State == ContentState.Waiting)
			{
				DateTime? futurePublishDate = (DateTime?)item["FuturePublishDate"];
				if (futurePublishDate.HasValue)
					data.FuturePublishDate = futurePublishDate.Value.ToString("o");
			}
			return data;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using cYo.Common;
using cYo.Projects.ComicRack.Engine;

namespace cYo.Projects.ComicRack.Viewer.Views
{
	internal static class ComicTagUtility
	{
		public sealed class TagEntry
		{
			public string Group { get; set; }
			public string Value { get; set; }
			public int Count { get; set; }

			public string Key => MakeKey(Group, Value);
		}

		public static IEnumerable<KeyValuePair<string, List<string>>> GetTagGroups(ComicBook comic)
		{
			Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			if (comic == null)
			{
				return groups;
			}
			AddTags(groups, "tag", comic.Tags);
			AddTags(groups, "genre", comic.Genre);
			AddTags(groups, "character", comic.Characters);
			AddTags(groups, "group", comic.Teams);
			AddTags(groups, "location", comic.Locations);
			foreach (StringPair pair in comic.GetCustomValues())
			{
				if (pair.Key.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					AddTags(groups, NormalizeGroup(pair.Key), pair.Value);
				}
			}
			return groups.Where(pair => pair.Value.Count != 0)
				.OrderBy(pair => GetGroupSort(pair.Key))
				.ThenBy(pair => GetDisplayGroup(pair.Key));
		}

		public static IEnumerable<TagEntry> GetTagEntries(IEnumerable<ComicBook> comics)
		{
			Dictionary<string, TagEntry> entries = new Dictionary<string, TagEntry>(StringComparer.OrdinalIgnoreCase);
			foreach (ComicBook comic in comics ?? Enumerable.Empty<ComicBook>())
			{
				foreach (KeyValuePair<string, List<string>> group in GetTagGroups(comic))
				{
					foreach (string value in group.Value)
					{
						string key = MakeKey(group.Key, value);
						if (!entries.TryGetValue(key, out TagEntry entry))
						{
							entries[key] = entry = new TagEntry
							{
								Group = NormalizeGroup(group.Key),
								Value = value,
								Count = 0
							};
						}
						entry.Count++;
					}
				}
			}
			return entries.Values
				.OrderBy(entry => GetGroupSort(entry.Group))
				.ThenBy(entry => GetDisplayGroup(entry.Group))
				.ThenBy(entry => entry.Value);
		}

		public static bool MatchesAllTags(ComicBook comic, IEnumerable<string> tagKeys)
		{
			string[] keys = (tagKeys ?? Enumerable.Empty<string>()).Where(key => !string.IsNullOrEmpty(key)).ToArray();
			if (keys.Length == 0)
			{
				return true;
			}
			HashSet<string> comicTags = new HashSet<string>(GetTagGroups(comic).SelectMany(group => group.Value.Select(tag => MakeKey(group.Key, tag))), StringComparer.OrdinalIgnoreCase);
			return keys.All(comicTags.Contains);
		}

		public static void AddTags(Dictionary<string, List<string>> groups, string defaultGroup, string tags)
		{
			if (string.IsNullOrWhiteSpace(tags))
			{
				return;
			}
			foreach (string rawTag in tags.Split(new char[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string text = rawTag.Trim();
				if (text.Length == 0)
				{
					continue;
				}
				string group = defaultGroup;
				string value = text;
				int separator = text.IndexOf(':');
				if (separator <= 0 || separator >= text.Length - 1)
				{
					separator = GetHyphenSeparator(text);
				}
				if (separator > 0 && separator < text.Length - 1)
				{
					group = text.Substring(0, separator).Trim();
					value = text.Substring(separator + 1).Trim();
				}
				group = NormalizeGroup(group);
				if (!groups.TryGetValue(group, out List<string> values))
				{
					groups[group] = values = new List<string>();
				}
				if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
				{
					values.Add(value);
				}
			}
		}

		public static string NormalizeGroup(string group)
		{
			switch ((group ?? string.Empty).Trim().ToLowerInvariant())
			{
			case "artists":
				return "artist";
			case "groups":
			case "teams":
				return "group";
			case "parodies":
				return "parody";
			case "characters":
				return "character";
			case "genres":
				return "genre";
			case "languages":
				return "language";
			case "tag":
			case "misc":
				return "tag";
			case "locations":
				return "location";
			default:
				return (group ?? string.Empty).Trim().ToLowerInvariant();
			}
		}

		public static string GetDisplayGroup(string group)
		{
			return NormalizeGroup(group);
		}

		public static int GetGroupSort(string group)
		{
			switch (NormalizeGroup(group))
			{
			case "artist":
				return 0;
			case "group":
				return 1;
			case "parody":
				return 2;
			case "character":
				return 3;
			case "female":
				return 4;
			case "male":
				return 5;
			case "genre":
				return 6;
			case "language":
				return 7;
			case "tag":
				return 8;
			default:
				return 20;
			}
		}

		public static Color GetColor(string group)
		{
			switch ((group ?? string.Empty).ToLowerInvariant())
			{
			case "artist":
				return Color.FromArgb(255, 246, 199);
			case "group":
			case "male":
				return Color.FromArgb(222, 239, 255);
			case "parody":
			case "language":
			case "tags":
				return Color.FromArgb(232, 255, 236);
			case "character":
				return Color.FromArgb(248, 216, 255);
			case "female":
				return Color.FromArgb(255, 226, 237);
			case "genre":
				return Color.FromArgb(239, 247, 252);
			default:
				return Color.FromArgb(238, 238, 238);
			}
		}

		public static string MakeKey(string group, string value)
		{
			return (group ?? string.Empty).Trim().ToLowerInvariant() + ":" + (value ?? string.Empty).Trim();
		}

		private static int GetHyphenSeparator(string text)
		{
			int separator = text.IndexOf('-');
			if (separator <= 0 || separator >= text.Length - 1)
			{
				return -1;
			}
			string group = NormalizeGroup(text.Substring(0, separator));
			return IsGroup(group) ? separator : -1;
		}

		private static bool IsGroup(string group)
		{
			switch ((group ?? string.Empty).ToLowerInvariant())
			{
			case "artist":
			case "group":
			case "parody":
			case "character":
			case "female":
			case "male":
			case "genre":
			case "language":
			case "tag":
			case "misc":
			case "location":
				return true;
			default:
				return false;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using cYo.Common.Collections;
using cYo.Common.Drawing;
using cYo.Common.Windows.Forms;
using cYo.Projects.ComicRack.Engine;
using cYo.Projects.ComicRack.Engine.Database;

namespace cYo.Projects.ComicRack.Viewer.Views
{
	public class ComicTagsView : SubView
	{
		private readonly FlowLayoutPanel tagsPanel;
		private readonly ComicBrowserView comicBrowserView;
		private readonly Label titleLabel;
		private readonly HashSet<string> selectedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private ComicBookContainer currentList;
		private bool refreshing;

		public ComicTagsView()
		{
			BackColor = SystemColors.Window;

			Panel header = new Panel
			{
				Dock = DockStyle.Top,
				Height = FormUtility.ScaleDpiY(27),
				Padding = new Padding(FormUtility.ScaleDpiX(8), FormUtility.ScaleDpiY(4), FormUtility.ScaleDpiX(8), 0),
				BackColor = SystemColors.Window
			};
			titleLabel = new Label
			{
				AutoEllipsis = true,
				Dock = DockStyle.Fill,
				Font = FC.Get("Arial", 8f, FontStyle.Bold),
				ForeColor = SystemColors.WindowText,
				Text = "Tags",
				TextAlign = ContentAlignment.MiddleLeft
			};
			header.Controls.Add(titleLabel);

			tagsPanel = new FlowLayoutPanel
			{
				AutoScroll = true,
				BackColor = SystemColors.Window,
				Dock = DockStyle.Top,
				Height = FormUtility.ScaleDpiY(150),
				Padding = new Padding(FormUtility.ScaleDpiX(7), 0, FormUtility.ScaleDpiX(7), FormUtility.ScaleDpiY(5)),
				WrapContents = true
			};

			comicBrowserView = new ComicBrowserView
			{
				Dock = DockStyle.Fill
			};

			Controls.Add(comicBrowserView);
			Controls.Add(tagsPanel);
			Controls.Add(header);
			VisibleChanged += ComicTagsView_VisibleChanged;
		}

		protected override void OnMainFormChanged()
		{
			base.OnMainFormChanged();
			comicBrowserView.Main = Main;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				currentList?.Dispose();
			}
			base.Dispose(disposing);
		}

		private void ComicTagsView_VisibleChanged(object sender, EventArgs e)
		{
			if (Visible)
			{
				RefreshTagsAndList();
			}
		}

		public void RefreshTagsAndList()
		{
			if (refreshing || Program.Database == null)
			{
				return;
			}
			refreshing = true;
			try
			{
				ComicBook[] books = Program.Database.Books.ToArray();
				HashSet<string> availableTags = new HashSet<string>(ComicTagUtility.GetTagEntries(books).Select(entry => entry.Key), StringComparer.OrdinalIgnoreCase);
				selectedTags.RemoveWhere(key => !availableTags.Contains(key));
				UpdateTags(books);
				UpdateBookList(books);
			}
			finally
			{
				refreshing = false;
			}
		}

		private void UpdateTags(ComicBook[] books)
		{
			tagsPanel.SuspendLayout();
			tagsPanel.Controls.Clear();
			try
			{
				ComicTagUtility.TagEntry[] entries = ComicTagUtility.GetTagEntries(books).ToArray();
				if (entries.Length == 0)
				{
					AddMessageLabel("No tags found in the library");
					UpdateTitle(books.Length);
					return;
				}

				AddAllTagsLabel();
				string currentGroup = null;
				foreach (ComicTagUtility.TagEntry entry in entries)
				{
					if (!string.Equals(currentGroup, entry.Group, StringComparison.OrdinalIgnoreCase))
					{
						currentGroup = entry.Group;
						AddGroupLabel(ComicTagUtility.GetDisplayGroup(currentGroup) + ":");
					}
					AddTagLabel(entry);
				}
				UpdateTitle(books.Length);
			}
			finally
			{
				tagsPanel.ResumeLayout();
			}
		}

		private void UpdateBookList(ComicBook[] books)
		{
			ComicBook[] matches = books.Where(book => ComicTagUtility.MatchesAllTags(book, selectedTags)).OrderBy(book => book.Caption).ToArray();
			ComicBookContainer nextList = new ComicBookContainer(GetListName(matches.Length))
			{
				EditMode = Program.Database.EditMode & ~ComicsEditModes.EditComicList
			};
			nextList.Books.AddRange(matches);
			ComicBookContainer oldList = currentList;
			currentList = nextList;
			comicBrowserView.BookList = currentList;
			oldList?.Dispose();
		}

		private string GetListName(int count)
		{
			if (selectedTags.Count == 0)
			{
				return string.Format("All Tags ({0})", count);
			}
			return string.Format("Tags ({0})", count);
		}

		private void UpdateTitle(int totalBooks)
		{
			if (selectedTags.Count == 0)
			{
				titleLabel.Text = string.Format("Tags - all library comics ({0})", totalBooks);
				return;
			}
			titleLabel.Text = string.Format("Tags - {0} selected", selectedTags.Count);
		}

		private void AddAllTagsLabel()
		{
			Label label = CreateChip("All tags", SystemColors.Control, selectedTags.Count == 0);
			label.Tag = string.Empty;
			label.Click += delegate
			{
				selectedTags.Clear();
				RefreshTagsAndList();
			};
			tagsPanel.Controls.Add(label);
		}

		private void AddGroupLabel(string text)
		{
			Label label = new Label
			{
				AutoSize = true,
				BackColor = SystemColors.Window,
				Font = FC.Get("Arial", 8f, FontStyle.Bold),
				ForeColor = SystemColors.WindowText,
				Margin = new Padding(FormUtility.ScaleDpiX(2), FormUtility.ScaleDpiY(5), FormUtility.ScaleDpiX(4), FormUtility.ScaleDpiY(4)),
				Padding = new Padding(0, 2, 0, 2),
				Text = text
			};
			tagsPanel.Controls.Add(label);
		}

		private void AddTagLabel(ComicTagUtility.TagEntry entry)
		{
			bool selected = selectedTags.Contains(entry.Key);
			Label label = CreateChip(string.Format("{0} ({1})", entry.Value, entry.Count), ComicTagUtility.GetColor(entry.Group), selected);
			label.Tag = entry.Key;
			label.Click += TagLabel_Click;
			tagsPanel.Controls.Add(label);
		}

		private Label CreateChip(string text, Color backColor, bool selected)
		{
			return new Label
			{
				AutoSize = true,
				BackColor = backColor,
				BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None,
				Cursor = Cursors.Hand,
				Font = FC.Get("Arial", 8f, FontStyle.Regular),
				ForeColor = SystemColors.WindowText,
				Margin = new Padding(FormUtility.ScaleDpiX(3), FormUtility.ScaleDpiY(3), FormUtility.ScaleDpiX(8), FormUtility.ScaleDpiY(5)),
				Padding = new Padding(FormUtility.ScaleDpiX(6), FormUtility.ScaleDpiY(2), FormUtility.ScaleDpiX(6), FormUtility.ScaleDpiY(2)),
				Text = text
			};
		}

		private void AddMessageLabel(string text)
		{
			Label label = new Label
			{
				AutoSize = true,
				BackColor = SystemColors.Window,
				Font = FC.Get("Arial", 8f, FontStyle.Regular),
				ForeColor = SystemColors.GrayText,
				Margin = new Padding(FormUtility.ScaleDpiX(3), FormUtility.ScaleDpiY(4), 0, 0),
				Text = text
			};
			tagsPanel.Controls.Add(label);
		}

		private void TagLabel_Click(object sender, EventArgs e)
		{
			string key = (sender as Control)?.Tag as string;
			if (string.IsNullOrEmpty(key))
			{
				return;
			}
			if (!selectedTags.Remove(key))
			{
				selectedTags.Add(key);
			}
			RefreshTagsAndList();
		}
	}
}



﻿using OpenLocoTool;
using OpenLocoTool.Types;

namespace OpenLocoToolGui
{
	public partial class StringTableUserControl : UserControl
	{
		public StringTableUserControl() => InitializeComponent();

		StringTable _data { get; set; }

		public void SetDataBinding(StringTable data)
		{
			_data = data;
			lbStringSelector.DataSource = null;

			if (data == null)
			{
				return;
			}

			// Set up data binding for the outer dictionary DataGridView.
			lbStringSelector.DataSource = data.Table.Keys.ToList();

			// Subscribe to the SelectionChanged event to populate the inner DataGridView.
			lbStringSelector.SelectedValueChanged += (sender, e) => UpdateDGVSource();

			// update the DGV now as well
			UpdateDGVSource();

		}

		void UpdateDGVSource()
		{
			flpLanguageStrings.SuspendLayout();
			flpLanguageStrings.Controls.Clear();

			var sel = lbStringSelector.SelectedValue as string;
			if (sel != null && _data.Table.ContainsKey(sel))
			{
				foreach (var language in _data.Table[sel])
				{
					var lblLanguage = new Label
					{
						BorderStyle = BorderStyle.FixedSingle,
						Text = language.Key.ToString(),
						Dock = DockStyle.Left,
						Height = 24,
						Width = 128,
						TextAlign = ContentAlignment.MiddleLeft,
					};

					var pn = new Panel
					{
						//AutoSize = true,
						//Dock = DockStyle.Top,
						Height = 24,
						Width = flpLanguageStrings.Width,
						Margin = new Padding(0, 0, 0, 0),
						Padding = new Padding(0, 0, 0, 0),
					};

					var tbText = new TextBox
					{
						BorderStyle = BorderStyle.FixedSingle,
						Text = language.Value,
						Dock = DockStyle.Left,
						Height = 24,
						Width = pn.Width - lblLanguage.Width - 4,
						TextAlign = HorizontalAlignment.Left,
					};
					tbText.TextChanged += (a, b) => _data.Table[sel][Enum.Parse<LanguageId>(lblLanguage.Text)] = tbText.Text;

					pn.Controls.Add(tbText);
					pn.Controls.Add(lblLanguage);

					flpLanguageStrings.Controls.Add(pn);
				}
			}

			flpLanguageStrings.ResumeLayout(true);
		}
	}
}

using Avalonia.Controls;
using OpenLoco.Gui.ViewModels;

namespace OpenLoco.Gui.Views
{
	public partial class EditSettingsWindow : Window
	{
		public EditSettingsWindow()
			=> InitializeComponent();

		void Window_Closing(object? sender, WindowClosingEventArgs e)
			=> (DataContext as EditorSettingsWindowViewModel)?.Commit();
	}
}

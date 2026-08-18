using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Layout;

namespace SourceGit.Views
{
    public partial class EditRepositoryNode : UserControl
    {
        public EditRepositoryNode()
        {
            InitializeComponent();
        }

        // [fork:colored-tabs] Trailing swatch on the bookmark picker opens the color picker
        private async void OnPickBookmarkCustomColor(object sender, System.EventArgs e)
        {
            if (DataContext is not ViewModels.EditRepositoryNode vm)
                return;

            var result = await ShowCustomColorPicker(vm.BookmarkCustom == 0 ? 0xFF0078D7u : vm.BookmarkCustom);
            if (result.HasValue)
                vm.BookmarkCustom = result.Value;
            else if (vm.BookmarkCustom == 0)
                vm.Bookmark = 0;
        }

        // [fork:colored-tabs] Same flow for the tab color picker
        private async void OnPickTabCustomColor(object sender, System.EventArgs e)
        {
            if (DataContext is not ViewModels.EditRepositoryNode vm)
                return;

            var result = await ShowCustomColorPicker(vm.TabColorCustom == 0 ? 0xFF0078D7u : vm.TabColorCustom);
            if (result.HasValue)
                vm.TabColorCustom = result.Value;
            else if (vm.TabColorCustom == 0)
                vm.TabColor = 0;
        }

        // [fork:colored-tabs] Modal color picker, mirrors LauncherTabBar.ShowCustomColorPicker
        private async Task<uint?> ShowCustomColorPicker(uint initialColor)
        {
            var picker = new ColorPicker();
            picker.Value = initialColor;
            picker.HorizontalAlignment = HorizontalAlignment.Center;

            var applyButton = new Button();
            applyButton.Content = App.Text("Sure");
            applyButton.HorizontalAlignment = HorizontalAlignment.Center;
            applyButton.Margin = new Avalonia.Thickness(0, 8, 0, 0);
            applyButton.Padding = new Avalonia.Thickness(16, 4);

            var panel = new StackPanel();
            panel.Margin = new Avalonia.Thickness(8);
            panel.Children.Add(picker);
            panel.Children.Add(applyButton);

            uint? result = null;

            var window = new ChromelessWindow();
            window.Title = App.Text("PageTabBar.Tab.CustomColor");
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.CanResize = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Content = panel;

            applyButton.Click += (_, _) =>
            {
                result = picker.Value;
                window.Close();
            };

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner != null)
                await window.ShowDialog(owner);

            return result;
        }
    }
}

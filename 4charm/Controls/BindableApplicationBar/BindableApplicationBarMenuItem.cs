using Microsoft.Phone.Shell;

namespace PhotosApp.Controls
{
    public class BindableApplicationBarMenuItem : BindableApplicationBarItemBase
    {
        public ApplicationBarMenuItem ApplicationBarMenuItem { get; private set; }

        public BindableApplicationBarMenuItem() : base()
        {
            ApplicationBarMenuItem = new ApplicationBarMenuItem(Text);
            ApplicationBarMenuItem.Click += OnClick;
        }

        protected override void ApplyCommandCanExecute()
        {
            ApplicationBarMenuItem.IsEnabled = Command.CanExecute(null);
        }

        protected override void UpdateText(string text)
        {
            ApplicationBarMenuItem.Text = text;
        }
    }
}

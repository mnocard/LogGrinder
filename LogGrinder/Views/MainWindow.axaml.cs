using Avalonia;
using Avalonia.Controls;

namespace LogGrinder
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
                this.AttachDevTools();
#endif
        }
    }
}

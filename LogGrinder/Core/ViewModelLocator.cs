using LogGrinder.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace LogGrinder.Core
{
    internal class ViewModelLocator
    {
        public MainWindowViewModel MWModel => App.Services.GetRequiredService<MainWindowViewModel>();
        public SearchWindowViewModel SearchWindowModel => App.Services.GetRequiredService<SearchWindowViewModel>();
        public InfoWindowViewModel InfoWindowModel => App.Services.GetRequiredService<InfoWindowViewModel>();
    }
}

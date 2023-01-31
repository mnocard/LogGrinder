using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using LogGrinder.Interfaces;
using LogGrinder.Services;
using LogGrinder.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;

using Serilog;

namespace LogGrinder
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow();

            base.OnFrameworkInitializationCompleted();
        }

        private static IHost? _Hosting;
        public static IHost Hosting
        {
            get
            {
                if (_Hosting != null) return _Hosting;
                var host_builder = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
                    .ConfigureServices(ConfigureServices);

                host_builder.UseSerilog((host, log) => log.
                    MinimumLevel.Information().
                    Enrich.FromLogContext().
                    WriteTo.File(path: "log.txt",
                                rollingInterval: RollingInterval.Day,
                                outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level}] {Message}{NewLine}{Exception}")
                );

                return _Hosting = host_builder.Build();
            }
        }

        public static IServiceProvider Services => Hosting.Services;

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<SearchWindowViewModel>();
            services.AddSingleton<InfoWindowViewModel>();
            services.AddSingleton<IFileManager, FileManager>();

            services.AddTransient<IFileHandler, FileHandler>();
            services.AddTransient<ILineHandler, LineHandler>();
            services.AddTransient<ISearcher, Searcher>();
            services.AddTransient<ISearchLineHandler, SearchLineHandler>();
        }
    }
}

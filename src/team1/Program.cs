using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using team1.Controls.Modern;

using team1.Core;
using team1.Core.Repositories;
using team1.Core.Stores;
using team1.Core.Usecases;
using team1.Core.ViewModels;
using team1.Repositories;

namespace team1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var options = AppOptions.Load(args, Path.Combine(baseDirectory, "kiosk.json"));

            using var services = ConfigureServices(options, baseDirectory);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // 大きな画面では画面全体を拡大する (文字・ボタン・地図のラベル)。Form を作る前に決める
            Theme.UiScale = (float)options.ResolveUiScale(LogicalScreenHeight());

            CrashGuard.Install(options);
            Application.Run(services.GetRequiredService<Form1>());
        }

        /// <summary>Windows の拡大 (125% など) を引いた後の、主画面の高さ</summary>
        private static double LogicalScreenHeight()
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            double scale = g.DpiY / 96.0;
            return Screen.PrimaryScreen is { } screen && scale > 0 ? screen.Bounds.Height / scale : 0;
        }

        private static ServiceProvider ConfigureServices(AppOptions options, string baseDirectory)
        {
            var dataDirectory = Path.Combine(baseDirectory, "Data");
            var svgPath = Path.Combine(dataDirectory, "map.svg");

            var services = new ServiceCollection();

            services.AddSingleton(options);
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            // Repository
            services.AddSingleton<IPlaceRepository>(new JsonPlaceRepository(dataDirectory));
            services.AddSingleton<IMapGraphRepository>(new SvgMapGraphRepository(svgPath, Path.Combine(dataDirectory, "outdoor_edges.txt")));
            services.AddSingleton<IMapResourceRepository>(new MapResourceRepository(svgPath));
            services.AddSingleton<IFavoriteRepository>(options.Kiosk
                ? new DisabledFavoriteRepository()
                // 実行ディレクトリ (Program Files 配下など) は書き込めないことがあるため、
                // ユーザーごとの AppData に保存する。旧版は exe と同じ場所に書いていたので引き継ぐ
                : new JsonFavoriteRepository(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KosenCampusNavi", "favorite.json"),
                    Path.Combine(baseDirectory, "favorite.json")));

            // Usecase
            services.AddSingleton<IPlaceSearchUsecase, PlaceSearchUsecase>();
            services.AddSingleton<IRouteUsecase, RouteUsecase>();
            services.AddSingleton<IFavoriteUsecase, FavoriteUsecase>();

            // Store
            services.AddSingleton<IRouteStateStore, RouteStateStore>();
            services.AddSingleton<IMapStateStore, MapStateStore>();

            // ViewModel / View
            services.AddSingleton<MainViewModel>();
            services.AddTransient<Form1>();

            // 登録漏れがあれば起動時にすぐ分かるよう、構築時に検証する
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }
    }
}

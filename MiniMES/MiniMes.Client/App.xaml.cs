using Microsoft.Extensions.DependencyInjection;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMes.Client.ViewModels;
using System;
using System.Windows;
using MiniMES.Infastructure.Services;

namespace MiniMes.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 서비스들을 담아두는 바구니(컨테이너)입니다.
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. 서비스 등록 (IWorkOrderService를 요청하면 WorkOrderService를 준다)
            // AddSingleton: 프로그램이 켜져 있는 동안 딱 하나만 만들어서 공유합니다.
            services.AddSingleton<IWorkOrderService, WorkOrderService>();
            services.AddSingleton<IWorkResultService, WorkResultServicePro>();

            // PLC 통신 서비스도 등록
            //services.AddSingleton<PlcCommunicationService>();

            // 2. ViewModel 등록
            // AddTransient: 필요할 때마다(창을 열 때마다) 새로 만듭니다.
            services.AddTransient<WorkOrderListViewModel>();
            services.AddTransient<WorkOrderEditViewModel>();
            services.AddTransient<WorkResultListViewModel>();
            services.AddTransient<WorkResultRegisterViewModel>();
        }
    }

}


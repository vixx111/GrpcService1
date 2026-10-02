using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using Microsoft.Extensions.Hosting;

namespace GrpcService1.Services
{
    public class HealthStatusManager : BackgroundService
    {
        private readonly HealthServiceImpl _healthService;
        private readonly ErrorTracker _errorTracker;
        public const string ServiceName = "work.Worker";

        public HealthStatusManager(HealthServiceImpl healthService, ErrorTracker errorTracker)
        {
            _healthService = healthService;
            _errorTracker = errorTracker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.Unknown);
            _healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Unknown);
            Console.WriteLine("[Health] Сервер стартовал. Состояние: UNKNOWN");

            await Task.Delay(2000, stoppingToken);

            _healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.Serving);
            _healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
            Console.WriteLine("[Health] Инициализация завершена. Состояние: SERVING");

            while (!stoppingToken.IsCancellationRequested)
            {
                int errors = _errorTracker.GetRecentErrorCount();

                if (errors > 10)
                {
                    _healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.NotServing);
                    _healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.NotServing);
                    Console.WriteLine($"[Health] ВНИМАНИЕ: Ошибок за минуту: {errors} (>10). Переход в NOT_SERVING!");
                }
                else
                {
                    _healthService.SetStatus(ServiceName, HealthCheckResponse.Types.ServingStatus.Serving);
                    _healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using GrpcClientApp;
using GrpcService1;

namespace GrpcClientApp
{
    internal class Program
    {
        private static HealthCheckResponse.Types.ServingStatus _currentStatus =
            HealthCheckResponse.Types.ServingStatus.Unknown;

        static async Task Main(string[] args)
        {
            string serverAddress = "https://localhost:7091";

            using var channel = GrpcChannel.ForAddress(serverAddress);

            var healthClient = new Health.HealthClient(channel);
            var workerClient = new Worker.WorkerClient(channel);

            using var cts = new CancellationTokenSource();

            _ = Task.Run(() => WatchHealthAsync(healthClient, cts.Token));

            Console.WriteLine("==================================================");
            Console.WriteLine("Управление:");
            Console.WriteLine("  [1] - Отправить нормальный запрос (DoWork)");
            Console.WriteLine("  [2] - Отправить 11 сбойных запросов (триггер NOT_SERVING)");
            Console.WriteLine("  [Q] - Выход");
            Console.WriteLine("==================================================\n");

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Q) break;

                if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1)
                {
                    await ExecuteTaskAsync(workerClient, shouldFail: false);
                }
                else if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2)
                {
                    Console.WriteLine("\n--> Отправка пачки сбойных запросов для превышения лимита...");
                    for (int i = 1; i <= 11; i++)
                    {
                        await ExecuteTaskAsync(workerClient, shouldFail: true);
                        await Task.Delay(40);
                    }
                }
            }

            cts.Cancel();
        }

        private static async Task WatchHealthAsync(Health.HealthClient healthClient, CancellationToken token)
        {
            var request = new HealthCheckRequest { Service = "work.Worker" };

            try
            {
                using var call = healthClient.Watch(request, cancellationToken: token);

                await foreach (var response in call.ResponseStream.ReadAllAsync(token))
                {
                    _currentStatus = response.Status;

                    Console.ForegroundColor = response.Status switch
                    {
                        HealthCheckResponse.Types.ServingStatus.Serving => ConsoleColor.Green,
                        HealthCheckResponse.Types.ServingStatus.NotServing => ConsoleColor.Red,
                        _ => ConsoleColor.Yellow
                    };

                    Console.WriteLine($"\n[СТАТУС ИЗМЕНЁН]: {_currentStatus}");
                    Console.ResetColor();
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Ошибка мониторинга здоровья]: {ex.Message}");
            }
        }

        private static async Task ExecuteTaskAsync(Worker.WorkerClient client, bool shouldFail)
        {
            if (_currentStatus != HealthCheckResponse.Types.ServingStatus.Serving)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[ЗАПРОС ЗАБЛОКИРОВАН КЛИЕНТОМ] Сервер не готов принимать данные. Текущий статус: {_currentStatus}");
                Console.ResetColor();
                return;
            }

            try
            {
                var response = await client.DoWorkAsync(new WorkRequest
                {
                    TaskName = $"Task_{Guid.NewGuid().ToString()[..4]}",
                    ShouldFail = shouldFail
                });

                Console.WriteLine($"[ОТВЕТ СЕРВЕРА]: {response.Result}");
            }
            catch (RpcException rpcEx)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[ОШИБКА gRPC]: {rpcEx.Status.Detail}");
                Console.ResetColor();
            }
        }
    }
}
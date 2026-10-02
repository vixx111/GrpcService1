using System;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcService1.Services;

namespace GrpcService1.Services
{
    public class WorkerService : Worker.WorkerBase
    {
        private readonly ErrorTracker _errorTracker;

        public WorkerService(ErrorTracker errorTracker)
        {
            _errorTracker = errorTracker;
        }

        public override Task<WorkReply> DoWork(WorkRequest request, ServerCallContext context)
        {
            if (request.ShouldFail)
            {
                _errorTracker.RecordError();
                throw new RpcException(new Status(StatusCode.Internal, $"Имитация сбоя задачи '{request.TaskName}'."));
            }

            return Task.FromResult(new WorkReply
            {
                Result = $"Задача '{request.TaskName}' успешно обработана в {DateTime.Now:HH:mm:ss}."
            });
        }
    }
}
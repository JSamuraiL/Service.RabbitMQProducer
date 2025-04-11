using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Service.RabbitMQProducer.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Service.RabbitMQProducer.Domain.Services;
public class StudentsProducerService : IAsyncDisposable, IStudentsProducerService
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private string _replyQueueName = string.Empty;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingMessages = new();
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);

    public StudentsProducerService(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_channel != null) return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_channel != null) return;

            _connection = await _connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            var replyQueue = await _channel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true);

            _replyQueueName = replyQueue.QueueName;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                if (_pendingMessages.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                {
                    tcs.TrySetResult(ea.Body.ToArray());
                }
                await Task.Yield();
            };

            await _channel.BasicConsumeAsync(
                consumer: consumer,
                queue: _replyQueueName,
                autoAck: true);
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<byte[]> PublishStudentAsync(
        string queueName,
        object message,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<byte[]>();
        _pendingMessages.TryAdd(correlationId, tcs);

        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            body: body,
            cancellationToken: cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        return await tcs.Task.WaitAsync(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
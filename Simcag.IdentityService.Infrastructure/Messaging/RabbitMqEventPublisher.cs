using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Simcag.IdentityService.Infrastructure.Messaging
{
    public class RabbitMqEventPublisher
    {
        private readonly IConnection _connection;

        public RabbitMqEventPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public void Publish<T>(string queue, T message)
        {
            using var channel = _connection.CreateModel();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            channel.BasicPublish(
                exchange: "",
                routingKey: queue,
                basicProperties: null,
                body: body
            );
        }
    }
}

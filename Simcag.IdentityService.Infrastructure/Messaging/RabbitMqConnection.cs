using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Simcag.IdentityService.Infrastructure.Messaging
{
    public class RabbitMqConnection
    {
        public void Consume(string message)
        {
            var data = JsonSerializer.Deserialize<DataProcessedEvent>(message);

            // processar evento
        }
    }
}

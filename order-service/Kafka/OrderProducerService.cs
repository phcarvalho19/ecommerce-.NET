using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace order_service.Kafka
{
   public class OrderProducerService
{
    private readonly IProducer<Null, string> _producer;
    private const string Topic = "order-created";

    public OrderProducerService(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task PublishOrderEvent(OrderCreatedEvent evt)
    {
        var message = JsonSerializer.Serialize(evt);

        await _producer.ProduceAsync(Topic, new Message<Null, string>
        {
            Value = message
        });
    }
}

}
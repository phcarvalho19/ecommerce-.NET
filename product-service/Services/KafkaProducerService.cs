using System.Text.Json;
using Confluent.Kafka;
using product_service.Events;

namespace product_service.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;
        private const string TopicProductCreated = "product-created";
        private const string TopicOrderRejected = "order-rejected";

        public KafkaProducerService(IConfiguration config)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"]
            };

            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        public async Task PublishProductCreated(ProductEvent productEvent)
        {
            var message = JsonSerializer.Serialize(productEvent);

            await _producer.ProduceAsync(TopicProductCreated, new Message<Null, string>
            {
                Value = message
            });
        }

        public async Task PublishOrderRejected(OrderRejectedEvent rejectedEvent)
        {
            var message = JsonSerializer.Serialize(rejectedEvent);

            await _producer.ProduceAsync(TopicOrderRejected, new Message<Null, string>
            {
                Value = message
            });
        }
    }
}
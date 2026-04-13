using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using order_service.Data;
using order_service.Model;

namespace order_service.Kafka
{
    public class ProductConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;

        public ProductConsumerService(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _config = config;
        }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    Console.WriteLine("🔥 Iniciando Kafka Consumer...");

    await Task.Delay(8000, stoppingToken); // 🔥 espera Kafka subir

    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = _config["Kafka:BootstrapServers"],
        GroupId = "order-service-group",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true
    };

    using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

    consumer.Subscribe("product-created");

    Console.WriteLine("✅ Kafka Consumer conectado e inscrito no tópico");

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var result = consumer.Consume(stoppingToken);

            if (result?.Message == null)
                continue;

            Console.WriteLine($"📥 Mensagem recebida: {result.Message.Value}");

            var product = JsonSerializer.Deserialize<AvailableProduct>(result.Message.Value);

            if (product == null)
            {
                Console.WriteLine("⚠️ Produto nulo recebido");
                continue;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var exists = await db.AvailableProducts
                .AnyAsync(p => p.Id == product.Id, stoppingToken);

            if (!exists)
            {
                db.AvailableProducts.Add(product);
                await db.SaveChangesAsync(stoppingToken);

                Console.WriteLine($"✅ Produto salvo: {product.Name}");
            }
            else
            {
                Console.WriteLine($"⚠️ Produto já existe: {product.Name}");
            }
        }
        catch (ConsumeException ex)
        {
            Console.WriteLine($"❌ Kafka consume error: {ex.Error.Reason}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro geral: {ex.Message}");
            await Task.Delay(3000, stoppingToken);
        }
    }

    consumer.Close();
}
    }
}
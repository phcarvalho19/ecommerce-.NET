using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using product_service.Data;
using product_service.Services;

namespace product_service.Events
{
    public class OrderConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;

        public OrderConsumerService(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Permite que o host finalize o startup antes do loop síncrono de consumo.
            await Task.Yield();

            var config = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "product-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("order-created");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(result.Message.Value);

                    if (evt == null || evt.Quantity <= 0) continue;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var kafkaProducer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();

                    var product = await db.Products.FindAsync(evt.ProductId);

                    if (product == null)
                    {
                        Console.WriteLine($"Kafka warning: produto {evt.ProductId} não encontrado");
                        continue;
                    }

                    if (string.Equals(evt.Action, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        // Restaurar estoque quando o pedido é cancelado
                        product.Stock += evt.Quantity;
                        await db.SaveChangesAsync();
                        Console.WriteLine($"Estoque restaurado para produto {evt.ProductId}. Novo estoque: {product.Stock}");

                        // Sincroniza o estoque no order-service (fonte local dele: AvailableProducts).
                        await kafkaProducer.PublishProductCreated(new ProductEvent
                        {
                            Id = product.Id,
                            Name = product.Name,
                            Price = product.Price,
                            Stock = product.Stock
                        });
                    }
                    else if (string.Equals(evt.Action, "Created", StringComparison.OrdinalIgnoreCase))
                    {
                        // Validar se há estoque suficiente
                        if (product.Stock < evt.Quantity)
                        {
                            Console.WriteLine($"Erro: Estoque insuficiente para produto {evt.ProductId}. Solicitado: {evt.Quantity}, Disponível: {product.Stock}");

                            // Publicar evento de rejeição
                            await kafkaProducer.PublishOrderRejected(new OrderRejectedEvent
                            {
                                ProductId = evt.ProductId,
                                QuantityRequested = evt.Quantity,
                                StockAvailable = product.Stock,
                                Reason = $"Estoque insuficiente. Disponível: {product.Stock}, Solicitado: {evt.Quantity}"
                            });
                            continue;
                        }

                        // Debitar estoque
                        product.Stock -= evt.Quantity;

                        // Garantir que nunca fique negativo (segurança extra)
                        if (product.Stock < 0)
                        {
                            product.Stock = 0;
                            Console.WriteLine($"Aviso: Estoque foi ajustado para 0 para produto {evt.ProductId}");
                        }

                        await db.SaveChangesAsync();
                        Console.WriteLine($"Estoque debitado para produto {evt.ProductId}. Novo estoque: {product.Stock}");

                        // Sincroniza o estoque no order-service (fonte local dele: AvailableProducts).
                        await kafkaProducer.PublishProductCreated(new ProductEvent
                        {
                            Id = product.Id,
                            Name = product.Name,
                            Price = product.Price,
                            Stock = product.Stock
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kafka error: {ex.Message}");
                }
            }
        }
    }
}
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Autofill.Tests;
using Xunit;
using Assert = Xunit.Assert;

namespace Kafka.Autofill.IntegrationTests;

public class KafkaIntegrationTests : IDisposable
{
    private const string BootstrapServers = "localhost:9092";
    private const string SchemaRegistryUrl = "http://localhost:8081";
    private const string TopicName = "People";
    private readonly IProducer<string, Person> _producer;
    private readonly IConsumer<string, Person> _consumer;
    private readonly CachedSchemaRegistryClient _schemaRegistry;

    public KafkaIntegrationTests()
    {
        _schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            ClientId = "kafka-autofill-producer-test",
            Acks = Acks.All,
            MessageTimeoutMs = 10000
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = $"kafka-autofill-test-{Guid.NewGuid()}",
            ClientId = "kafka-autofill-consumer-test",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var serializerConfig = new AvroSerializerConfig()
        {
            AutoRegisterSchemas = true
        };

        var serializer = new AvroSerializer<Person>(_schemaRegistry, serializerConfig);
        var deserializer = new AvroDeserializer<Person>(_schemaRegistry);

        _producer = new ProducerBuilder<string, Person>(producerConfig)
            .SetValueSerializer(serializer).Build();
        
        _consumer = new ConsumerBuilder<string, Person>(consumerConfig)
            .SetValueDeserializer(deserializer.AsSyncOverAsync()).Build();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [TestCategory("Integration")]
    public async Task Should_Publish_Person_Message_To_Kafka()
    {
        Person person = TestData.Person;
        var key = person.Id.ToString();
        
        var deliveryResult = await _producer.ProduceAsync(TopicName, new Message<string, Person>
        {
            Key = key,
            Value = person
        });
        
        Assert.NotNull(deliveryResult);
        Assert.Equal(PersistenceStatus.Persisted, deliveryResult.Status);
        Assert.Equal(TopicName, deliveryResult.Topic);
        Assert.True(deliveryResult.Offset >= 0);
        Assert.Equal(key, deliveryResult.Message.Key);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [TestCategory("Integration")]
    public void Should_Consume_Person_Message_From_Kafka()
    {
        var originalPerson = TestData.Person;
        var key = originalPerson.Id.ToString();
        
        _consumer.Subscribe(TopicName);

        var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(10));
        
        Assert.NotNull(consumeResult);
        Assert.Equal(TopicName, consumeResult.Topic);
        Assert.Equal(key, consumeResult.Message.Key);
        Assert.NotNull(consumeResult.Message.Value);
        
        var receivedPerson = consumeResult.Message.Value;
        var properties = originalPerson.GetType().GetProperties().Where(p => p.Name != "Schema");
        
        foreach (var prop in properties)
        {
            var expected = prop.GetValue(originalPerson);
            var actual = prop.GetValue(receivedPerson);
            
            Assert.Equal(expected, actual);
        }
        
        _consumer.Commit(consumeResult);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _consumer?.Close();
        _consumer?.Dispose();
        _producer?.Dispose();
        _schemaRegistry?.Dispose();
    }
}


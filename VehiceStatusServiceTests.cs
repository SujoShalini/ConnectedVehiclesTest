using Azure.Messaging.ServiceBus;
using ConnectedVehicles.Models;
using Moq;
using System.Text;

public class VehiceStatusServiceTests
{
    [Fact]
    public async Task ReceiveMessagesAsync_ShouldReturnMessagesList()
    {
        // Arrange
        var connectionString = "Endpoint=sb://scaniaconnectedvehicles.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxx\"";
        var queueName = "pushstatus";
        var mockClient = new Mock<ServiceBusClient>();
        var mockReceiver = new Mock<ServiceBusReceiver>();
        var testMessages = new List<ServiceBusReceivedMessage>
        {
            ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromBytes( Encoding.UTF8.GetBytes("Connected"))),
            ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromBytes( Encoding.UTF8.GetBytes("DisConnected")))
        };

        mockClient.Setup(c => c.CreateReceiver(queueName, It.IsAny<ServiceBusReceiverOptions>()))
            .Returns(mockReceiver.Object);
        mockReceiver.Setup(r => r.ReceiveMessagesAsync(10, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(testMessages);

        var service = new VehiceStatusService(connectionString, queueName);

        // Act
        var result = await service.ReceiveMessagesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Connected", result);
        Assert.Contains("DisConnected", result);

    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendCorrectMessage()
    {
        var connectionString = "Endpoint=sb://scaniaconnectedvehicles.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxx";
        var queueName = "pushstatus";
        var mockClient = new Mock<ServiceBusClient>();
        var mockSender = new Mock<ServiceBusSender>();
        var vehicleData = new VehicleStatus
        {
           VehicleId= "YS2R4X20005399401",
           StatusMessage="Connected"
        };


        mockClient.Setup(c => c.CreateSender(queueName))
            .Returns(mockSender.Object);
        mockSender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, token) =>
            {
                var sentData = System.Text.Json.JsonSerializer.Deserialize<VehicleStatus>(message.Body.ToString());
                Assert.Equal(vehicleData, sentData);
            })
        .Returns(Task.CompletedTask);

        var service = new VehiceStatusService(connectionString, queueName);

        await service.SendMessageAsync(vehicleData);

        mockSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

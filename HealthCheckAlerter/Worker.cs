using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace HealthCheckAlerter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IConnection _connection;
        private IModel _channel;
        private const string _queueName = "InvestmentManagerHealthChecks";
        private const string _host = "localhost";
        private dynamic _healthReport;
        private DateTime _datetimeOfSuccessfullHR = DateTime.Now;
        private bool _receivedAtleastoneHealthCheck = false;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker started at: {DateTime.Now}");

            var factory = new ConnectionFactory() { HostName = _host };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += MessageReceived;
            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

            return base.StartAsync(cancellationToken);
        }

        private void MessageReceived(object? model, BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var message = Encoding.UTF8.GetString(body);

            try
            {
                _healthReport = JsonConvert.DeserializeObject(message);
                _datetimeOfSuccessfullHR = DateTime.Now;
                _receivedAtleastoneHealthCheck = true;
            } catch (Exception e)
            {
                MessageToConsole("Health report error...", ConsoleColor.Red);
                MessageToConsole(e.Message, ConsoleColor.DarkRed);
                _healthReport = null;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            { 
                if (_receivedAtleastoneHealthCheck)
                {
                    UpdateConsoleWithHealthReport();
                    await Task.Delay(2000, stoppingToken);
                    Console.Clear();
                }
            }
        }

        private void UpdateConsoleWithHealthReport()
        {
            bool issue = false;

            if ((DateTime.Now - _datetimeOfSuccessfullHR).TotalSeconds>20 || _healthReport is null)
            {
                MessageToConsole("No health report received!", ConsoleColor.Red);
                issue = true;
            }

            if (!issue && _healthReport != null && _healthReport.Status != 2)
            {
                MessageToConsole("Unhealthy or degraded health report", ConsoleColor.Red);
                issue = true;
            }

            if (issue)
            {
                MessageToConsole("Health check issues sending email alert...", ConsoleColor.Yellow);
            }
            else { 
                MessageToConsole("Health check received, and health is good!", ConsoleColor.Green);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");

            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation($"Worker disposed at: {DateTime.Now}");
            _channel.Dispose();
            _connection.Dispose();
            base.Dispose();
        }

        private void MessageToConsole(string line, ConsoleColor col = default)
        {
            Console.ForegroundColor = col;
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }
}

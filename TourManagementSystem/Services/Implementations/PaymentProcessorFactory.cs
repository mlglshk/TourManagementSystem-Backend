using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class PaymentProcessorFactory : IPaymentProcessorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public PaymentProcessorFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public IPaymentProcessor GetPaymentProcessor()
        {
            var mode = _configuration["Payment:Mode"] ?? "Test";

            return mode switch
            {
                "YooKassa" => _serviceProvider.GetRequiredService<YooKassaPaymentService>(),
                "SbpQR" => _serviceProvider.GetRequiredService<SbpQrPaymentService>(),
                _ => _serviceProvider.GetRequiredService<BasicPaymentService>()
            };
        }
    }
}
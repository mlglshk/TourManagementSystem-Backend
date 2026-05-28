namespace TourManagementSystem.Services.Interfaces
{
    public interface IPaymentProcessorFactory
    {
        IPaymentProcessor GetPaymentProcessor();
    }
}
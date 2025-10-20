using System.Threading.Tasks;

namespace XeroDotnetSampleApp.Clients
{
    public interface IAkahuClient
    {
        /// <summary>
        /// POST /v1/one-off-payments
        /// Create a new payment for authorisation by your user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest request);


        /// <summary>
        /// GET /v1/one-off-payments/{id}
        /// Get payment details including status.
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        Task<PollPaymentResponse> PollPaymentStatus(string paymentId);
    }
}
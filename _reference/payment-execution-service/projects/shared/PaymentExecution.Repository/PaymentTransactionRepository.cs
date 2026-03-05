using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Common.Models;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Repository;

public interface IPaymentTransactionRepository
{
    Task<bool> HealthCheck();

    Task<Result<Guid>> InsertPaymentTransactionIfNotExist(InsertPaymentTransactionDto insertPaymentTransactionDto);

    Task<Result<PaymentTransactionDto?>> GetPaymentTransactionsByPaymentRequestId(Guid paymentRequestId);

    Task<Result> UpdatePaymentTransactionStatusOnly(UpdateStatusPaymentTransactionDto paymentTransactionDto);

    Task<Result> UpdateSuccessPaymentTransactionData(UpdateSuccessPaymentTransactionDto paymentTransactionDto);

    Task<Result> UpdateFailurePaymentTransactionData(UpdateFailurePaymentTransactionDto paymentTransactionDto);

    Task<Result> UpdateCancelledPaymentTransactionData(UpdateCancelledPaymentTransactionDto paymentTransactionDto);
    Task<Result> SetPaymentTransactionFailed(Guid paymentRequestId, string failureDetails, string failureStatus);

    Task<int> DeleteAllDataByOrganisationId(Guid organisationId);
    Task<int> CountPaymentTransactionsByOrganisationId(Guid organisationId);
    Task<Result> UpdatePaymentTransactionWithProviderDetails(UpdateForSubmitFlowDto updateForSubmitFlowDto);


}

public class PaymentTransactionRepository(IConnectionFactory connectionFactory, ILogger<PaymentTransactionRepository> logger, IDapperWrapper dapperWrapper, TimeProvider timeProvider) : SqlRepository(connectionFactory, DatabaseName, PaymentTransactionTable), IPaymentTransactionRepository
{
    public static string InsertPaymentTransactionErrorMessage =>
        "An error has occurred during payment transaction record insertion.";
    public static string UpdatePaymentTransactionErrorMessage =>
        "An error has occurred during payment transaction record update.";

    public static string SetPaymentTransactionFailedErrorMessage =>
        "An error has occurred during payment transaction record update when setting it to failed.";

    private const string PaymentTransactionTable = "PaymentTransaction";
    private const string DatabaseName = "PaymentExecutionDB";
    private const string DeleteAllRecordsByOrganisationId = @"
        with prs_deleted as (
          DELETE FROM payment_execution.paymenttransaction
          WHERE OrganisationId = @organisationId
          RETURNING *
        )
        SELECT count(1) FROM prs_deleted;";

    private const string SearchPaymentTransactionsByOrganisationIdSql = @"
            SELECT count(1) FROM payment_execution.paymenttransaction
            WHERE OrganisationId = @organisationId;
        ";
    public async Task<bool> HealthCheck()
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            await dapperWrapper.QueryAsync<int>(conn, "SELECT 1");
            conn.Close();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occured during database healthcheck: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<Result<Guid>> InsertPaymentTransactionIfNotExist(InsertPaymentTransactionDto insertPaymentTransactionDto)
    {
        logger.LogInformation("Inserting payment transaction for Payment Request ID: {PaymentRequestId}", insertPaymentTransactionDto.PaymentRequestId);

        const string PaymentTransactionInsertSql = @"
                WITH ins AS (
                    INSERT INTO payment_execution.PaymentTransaction (paymentRequestId, status, providerType, createdUTC, updatedUTC, organisationId) 
                    VALUES (@PaymentRequestId, @Status, @ProviderType, @CreatedUtc, @UpdatedUtc, @OrganisationId)
                    ON CONFLICT (PaymentRequestId) DO NOTHING
                    RETURNING paymentTransactionId
                )
                SELECT paymentTransactionId
                FROM ins
                UNION ALL
                SELECT paymentTransactionId
                FROM payment_execution.PaymentTransaction 
                WHERE PaymentRequestId = @PaymentRequestId
                LIMIT 1;
                ";
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var paymentTransactionId = await dapperWrapper.ExecuteScalarAsync<Guid>(conn, PaymentTransactionInsertSql, new
            {
                PaymentRequestId = insertPaymentTransactionDto.PaymentRequestId,
                Status = insertPaymentTransactionDto.Status,
                ProviderType = insertPaymentTransactionDto.ProviderType,
                CreatedUtc = timeProvider.GetUtcNow().DateTime,
                UpdatedUtc = timeProvider.GetUtcNow().DateTime,
                OrganisationId = insertPaymentTransactionDto.OrganisationId
            }, transaction);

            transaction.Commit();
            conn.Close();
            return paymentTransactionId;
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occurred during payment transaction record insertion: {Message} for Payment Request ID: {PaymentRequestId}",
                redactedException.Message, insertPaymentTransactionDto.PaymentRequestId);
            return Result.Fail(new PaymentExecutionError(InsertPaymentTransactionErrorMessage));
        }

    }

    public async Task<Result<PaymentTransactionDto?>> GetPaymentTransactionsByPaymentRequestId(Guid paymentRequestId)
    {
        logger.LogInformation("Getting payment transaction data for Payment Request ID: {PaymentRequestId}",
            paymentRequestId);
        try
        {
            var getPaymentTransactionsSql =
                @"SELECT 
                   PaymentTransactionId, 
                   PaymentRequestId, 
                   ProviderServiceId, 
                   Status, 
                   Fee, 
                   FeeCurrency, 
                   PaymentProviderPaymentReferenceId, 
                   PaymentProviderPaymentTransactionId, 
                   FailureDetails, 
                   EventCreatedDateTimeUtc, 
                   ProviderType, 
                   CreatedUtc, 
                   UpdatedUtc,
                   CancellationReason FROM payment_execution.PaymentTransaction 
                  WHERE paymentRequestId = @PaymentRequestId";

            using var conn = GetConnection();
            conn.Open();

            var result = await dapperWrapper.QueryFirstOrDefaultAsync<PaymentTransactionDto>(conn,
                getPaymentTransactionsSql, new { PaymentRequestId = paymentRequestId });

            conn.Close();
            return result;
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occured during payment transaction record retrieval: {Message} for Payment Request ID: {PaymentRequestId}",
                redactedException.Message, paymentRequestId);
            return Result.Fail("Failed to get payment transaction from DB");
        }
    }

    public async Task<Result> UpdatePaymentTransactionWithProviderDetails(UpdateForSubmitFlowDto updateForSubmitFlowDto)
    {
        try
        {
            var updatePaymentTransactionStatusSql = @"UPDATE payment_execution.PaymentTransaction SET paymentProviderPaymentTransactionId = @PaymentProviderPaymentTransactionId, providerServiceId = @ProviderServiceId, updatedUTC = @UpdatedUtc WHERE paymentTransactionId = @PaymentTransactionId";

            using var conn = GetConnection();
            conn.Open();

            await dapperWrapper.ExecuteAsync(conn, updatePaymentTransactionStatusSql,
                new
                {
                    PaymentProviderPaymentTransactionId =
                        updateForSubmitFlowDto.PaymentProviderPaymentTransactionId,
                    ProviderServiceId = updateForSubmitFlowDto.ProviderServiceId,
                    PaymentTransactionId = updateForSubmitFlowDto.PaymentTransactionId,
                    UpdatedUtc = timeProvider.GetUtcNow().DateTime
                });

            conn.Close();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occurred during payment transaction record update. Payment TransactionId ID: {PaymentTransactionId}",
                updateForSubmitFlowDto.PaymentTransactionId);
            return Result.Fail(
                new PaymentExecutionError(UpdatePaymentTransactionErrorMessage));
        }

    }

    public async Task<Result> UpdatePaymentTransactionStatusOnly(UpdateStatusPaymentTransactionDto paymentTransactionDto)
    {
        logger.LogInformation("Updating status for payment request ID: {PaymentRequestId}",
            paymentTransactionDto.PaymentRequestId);
        try
        {
            var updatePaymentTransactionStatusSql =
                @"UPDATE payment_execution.PaymentTransaction 
                    SET status = @Status, 
                        updatedUTC = @UpdatedUtc 
                    WHERE paymentRequestId = @PaymentRequestId AND providerServiceId = @ProviderServiceId";

            using var conn = GetConnection();
            conn.Open();

            await dapperWrapper.ExecuteAsync(conn, updatePaymentTransactionStatusSql,
                new
                {
                    PaymentRequestId = paymentTransactionDto.PaymentRequestId,
                    ProviderServiceId = paymentTransactionDto.ProviderServiceId,
                    Status = paymentTransactionDto.Status,
                    UpdatedUtc = timeProvider.GetUtcNow().DateTime
                });

            conn.Close();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occured during payment transaction record update: {Message} for Payment Request ID: {PaymentRequestId}",
                redactedException.Message, paymentTransactionDto.PaymentRequestId);
            return Result.Fail("Failed to update payment transaction status in DB");
        }
    }

    public async Task<Result> UpdateSuccessPaymentTransactionData(UpdateSuccessPaymentTransactionDto paymentTransactionDto)
    {
        logger.LogInformation(
            "Updating payment transaction data as suuccessful for Payment Request ID: {PaymentRequestId}",
            paymentTransactionDto.PaymentRequestId);
        try
        {
            var updatePaymentTransactionDataSql =
                @"UPDATE payment_execution.PaymentTransaction 
                    SET status = @Status, 
                        fee = @Fee, 
                        feeCurrency = @FeeCurrency, 
                        paymentProviderPaymentReferenceId = @PaymentProviderPaymentReferenceId, 
                        eventcreatedDateTimeUtc = @EventCreatedDateTimeUtc,
                        updatedUTC = @UpdatedUtc 
                    WHERE paymentRequestId = @PaymentRequestId AND providerServiceId = @ProviderServiceId";

            using var conn = GetConnection();
            conn.Open();

            await dapperWrapper.ExecuteAsync(conn, updatePaymentTransactionDataSql,
                new
                {
                    PaymentRequestId = paymentTransactionDto.PaymentRequestId,
                    ProviderServiceId = paymentTransactionDto.ProviderServiceId,
                    Status = paymentTransactionDto.Status,
                    Fee = paymentTransactionDto.Fee,
                    FeeCurrency = paymentTransactionDto.FeeCurrency,
                    PaymentProviderPaymentReferenceId = paymentTransactionDto.PaymentProviderPaymentReferenceId,
                    EventCreatedDateTimeUtc = paymentTransactionDto.EventCreatedDateTimeUtc,
                    UpdatedUtc = timeProvider.GetUtcNow().DateTime
                });

            conn.Close();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occurred while attempting to update payment transaction as successful for Payment Request ID: {PaymentRequestId}",
                paymentTransactionDto.PaymentRequestId);
            return Result.Fail("Failed to update payment transaction data in DB");
        }
    }

    public async Task<Result> UpdateFailurePaymentTransactionData(UpdateFailurePaymentTransactionDto paymentTransactionDto)
    {
        logger.LogInformation("Updating payment transaction data as failure for Payment Request ID: {PaymentRequestId}",
            paymentTransactionDto.PaymentRequestId);
        try
        {
            var updatePaymentTransactionDataSql =
                @"UPDATE payment_execution.PaymentTransaction 
                    SET status = @Status, 
                        fee = @Fee, 
                        feeCurrency = @FeeCurrency, 
                        paymentProviderPaymentReferenceId = @PaymentProviderPaymentReferenceId, 
                        eventcreatedDateTimeUtc = @EventCreatedDateTimeUtc, 
                        failureDetails = @FailureDetails,
                        updatedUTC = @UpdatedUtc 
                    WHERE paymentRequestId = @PaymentRequestId AND providerServiceId = @ProviderServiceId";

            using var conn = GetConnection();
            conn.Open();

            await dapperWrapper.ExecuteAsync(conn, updatePaymentTransactionDataSql,
                new
                {
                    PaymentRequestId = paymentTransactionDto.PaymentRequestId,
                    ProviderServiceId = paymentTransactionDto.ProviderServiceId,
                    Status = paymentTransactionDto.Status,
                    Fee = paymentTransactionDto.Fee,
                    FeeCurrency = paymentTransactionDto.FeeCurrency,
                    PaymentProviderPaymentReferenceId = paymentTransactionDto.PaymentProviderPaymentReferenceId,
                    FailureDetails = paymentTransactionDto.FailureDetails,
                    EventCreatedDateTimeUtc = paymentTransactionDto.EventCreatedDateTimeUtc,
                    UpdatedUtc = timeProvider.GetUtcNow().DateTime
                });

            conn.Close();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occurred while attempting to update payment transaction as failed for Payment Request ID: {PaymentRequestId}",
                paymentTransactionDto.PaymentRequestId);
            return Result.Fail("Failed to update payment transaction data as failed in DB");
        }
    }

    public async Task<Result> UpdateCancelledPaymentTransactionData(UpdateCancelledPaymentTransactionDto paymentTransactionDto)
    {
        logger.LogInformation(
            "Updating payment transaction data as cancelled for Payment Request ID: {PaymentRequestId}",
            paymentTransactionDto.PaymentRequestId);
        try
        {
            string? truncatedCancellationReason = null;
            if (paymentTransactionDto.CancellationReason is not null)
            {
                truncatedCancellationReason = paymentTransactionDto.CancellationReason.Length <= 125 ? paymentTransactionDto.CancellationReason : paymentTransactionDto.CancellationReason.Substring(0, 124);
            }
            var updatePaymentTransactionDataSql =
                @"UPDATE payment_execution.PaymentTransaction 
                    SET status = @Status, 
                        eventcreatedDateTimeUtc = @EventCreatedDateTimeUtc,
                        updatedUTC = @UpdatedUtc,
                        cancellationReason = @cancellationReason
                    WHERE paymentRequestId = @PaymentRequestId AND providerServiceId = @ProviderServiceId";

            using var conn = GetConnection();
            conn.Open();

            await dapperWrapper.ExecuteAsync(conn, updatePaymentTransactionDataSql,
                new
                {
                    PaymentRequestId = paymentTransactionDto.PaymentRequestId,
                    ProviderServiceId = paymentTransactionDto.ProviderServiceId,
                    Status = paymentTransactionDto.Status,
                    EventCreatedDateTimeUtc = paymentTransactionDto.EventCreatedDateTimeUtc,
                    UpdatedUtc = timeProvider.GetUtcNow().DateTime,
                    CancellationReason = truncatedCancellationReason
                });

            conn.Close();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                "An error has occurred while attempting to update payment transaction as cancelled for Payment Request ID: {PaymentRequestId}",
                paymentTransactionDto.PaymentRequestId);
            return Result.Fail("Failed to update payment transaction data in DB");
        }
    }

    public async Task<Result> SetPaymentTransactionFailed(Guid paymentRequestId,
        string failureDetails, string failureStatus)
    {
        logger.LogInformation("Set payment transaction failed for Payment Request ID: {PaymentRequestId}",
            paymentRequestId);
        var truncatedFailureDetails = failureDetails.Length <= 125 ? failureDetails
            : failureDetails.Substring(0, 125);

        try
        {
            var updatePaymentTransactionDataSql = @"UPDATE payment_execution.PaymentTransaction SET status = @Status, failureDetails = @FailureDetails, updatedUTC = @UpdatedUtc WHERE paymentRequestId = @PaymentRequestId";

            using var conn = GetConnection();
            conn.Open();

            await dapperWrapper.ExecuteAsync(conn, updatePaymentTransactionDataSql,
                new
                {
                    FailureDetails = truncatedFailureDetails,
                    PaymentRequestId = paymentRequestId,
                    UpdatedUtc = timeProvider.GetUtcNow().DateTime,
                    Status = failureStatus
                });

            conn.Close();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.DatabaseException);
            logger.LogError(redactedException,
                SetPaymentTransactionFailedErrorMessage + " Payment Request ID: {PaymentRequestId}",
                paymentRequestId);
            return Result.Fail(new PaymentExecutionError(SetPaymentTransactionFailedErrorMessage));
        }
    }

    public async Task<int> CountPaymentTransactionsByOrganisationId(Guid organisationId)
    {
        using var conn = GetConnection();
        conn.Open();
        var numberOfTransactions = (await dapperWrapper.QueryAsync<int>(conn, SearchPaymentTransactionsByOrganisationIdSql, new
        {
            organisationId
        })).ToList();
        conn.Close();

        return numberOfTransactions.Count != 0 ? numberOfTransactions[0] : 0;
    }

    /// <summary>
    /// Deletes Payment Transaction record and all related rows in other tables
    /// </summary>
    /// <param name="organisationId" />
    /// <returns>integer representing number of payment transactions deleted</returns>
    public async Task<int> DeleteAllDataByOrganisationId(Guid organisationId)
    {
        using var conn = GetConnection();
        conn.Open();
        var prsDeleted = await dapperWrapper.QueryAsync<int>(conn, DeleteAllRecordsByOrganisationId, new
        {
            organisationId
        });
        conn.Close();
        return prsDeleted.FirstOrDefault(0);
    }
}

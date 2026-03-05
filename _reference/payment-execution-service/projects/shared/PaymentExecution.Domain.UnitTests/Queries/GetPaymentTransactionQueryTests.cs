using AutoMapper;
using FluentAssertions;
using FluentResults;
using Moq;
using PaymentExecution.Domain.Queries;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
namespace PaymentExecution.Domain.UnitTests.Queries;

public class GetPaymentTransactionQueryTests
{
    private readonly GetPaymentTransactionQueryHandler _handler;
    private readonly Mock<IPaymentTransactionRepository> _paymentTransactinRepo;
    private readonly Mock<IMapper> _mapper;

    public GetPaymentTransactionQueryTests()
    {
        _paymentTransactinRepo = new Mock<IPaymentTransactionRepository>();
        _mapper = new Mock<IMapper>();

        _handler = new GetPaymentTransactionQueryHandler(_paymentTransactinRepo.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task GivenPaymentRequestId_WhenGetPaymentTransactionQueryHandler_ThenReturnsOkResponseWithBody()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var query = new GetPaymentTransactionQuery
        {
            PaymentRequestId = paymentRequestId
        };

        var expectedPaymentTransactionDto = new PaymentTransactionDto
        {
            PaymentRequestId = paymentRequestId,
            PaymentTransactionId = Guid.NewGuid(),
            ProviderServiceId = Guid.NewGuid(),
            PaymentProviderPaymentReferenceId = "ch_1234",
            FeeCurrency = "USD",
            Fee = 10.0m,
            ProviderType = "Stripe",
            Status = "submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            CancellationReason = "test string"
        };

        var expectedResponse = new GetPaymentTransactionQueryResponse
        {
            PaymentRequestId = paymentRequestId,
            PaymentTransactionId = expectedPaymentTransactionDto.PaymentTransactionId,
            ProviderServiceId = expectedPaymentTransactionDto.ProviderServiceId,
            PaymentProviderPaymentReferenceId = expectedPaymentTransactionDto.PaymentProviderPaymentReferenceId,
            FeeCurrency = expectedPaymentTransactionDto.FeeCurrency,
            Fee = expectedPaymentTransactionDto.Fee,
            ProviderType = expectedPaymentTransactionDto.ProviderType,
            Status = expectedPaymentTransactionDto.Status,
            CancellationReason = expectedPaymentTransactionDto.CancellationReason
        };

        _paymentTransactinRepo.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(expectedPaymentTransactionDto);

        _mapper.Setup(mock => mock.Map<GetPaymentTransactionQueryResponse>(It.IsAny<PaymentTransactionDto>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        result.Value.PaymentRequestId.Should().Be(paymentRequestId);
    }

    [Fact]
    public async Task GivenRepositoryDoesNotFindRecord_WhenGetPaymentTransactionQueryHandler_ThenReturnsResultOkWithNull()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var query = new GetPaymentTransactionQuery
        {
            PaymentRequestId = paymentRequestId
        };
        _paymentTransactinRepo.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync((PaymentTransactionDto)null!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GivenRepositoryReturnsFailedResult_WhenGetPaymentTransactionQueryHandler_ThenReturnsFailedResponse()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var query = new GetPaymentTransactionQuery
        {
            PaymentRequestId = paymentRequestId
        };
        _paymentTransactinRepo.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Fail("Database error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenExceptionRaisedByDB_WhenGetPaymentTransactionQueryHandler_ThenReturnsFailedResponseWithMessage()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var query = new GetPaymentTransactionQuery
        {
            PaymentRequestId = paymentRequestId
        };

        _paymentTransactinRepo.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ThrowsAsync(new Exception("DB Error"));

        // Act & Assert
        await _handler.Invoking(async subject => await _handler.Handle(query, default))
            .Should().ThrowAsync<Exception>();
    }
}

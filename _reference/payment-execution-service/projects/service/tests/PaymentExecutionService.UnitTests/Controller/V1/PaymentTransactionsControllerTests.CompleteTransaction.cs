using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Controllers.V1;
using PaymentExecutionService.Mapping;
using PaymentExecutionService.Models;
using Xunit;
using static Xero.Accelerators.Api.Core.Constants;
namespace PaymentExecutionService.UnitTests.Controller.V1;

public class PaymentTransactionsControllerTests_CompleteTransaction
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly PaymentsController _sut;
    private readonly IMapper _mapper;
    public PaymentTransactionsControllerTests_CompleteTransaction()
    {
        _mockMediator = new Mock<IMediator>();
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ControllerToDomainMappingProfile>())
            .CreateMapper();
        _sut = new PaymentsController(_mockMediator.Object, _mapper);

        // Set up HTTP context with required headers
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaders.XeroTenantId] = Guid.NewGuid().ToString();
        httpContext.Request.Headers[HttpHeaders.XeroCorrelationId] = Guid.NewGuid().ToString();
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GivenValidInputAndNoSqsError_WhenCompleteTransaction_ReturnsAcceptedResult()
    {
        CompletePaymentTransactionRequest validRequest = CreateValidCompleteRequest();
        _mockMediator.Setup(m => m.Send(It.IsAny<CompletePaymentTransactionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);

        var result = await _sut.CompletePaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), validRequest);

        result.Should().BeOfType<AcceptedResult>();
    }

    [Fact]
    public async Task GivenSomeExceptionOccurs_WhenCompleteTransaction_ErrorIsThrown()
    {
        var validRequest = CreateValidCompleteRequest();

        //Throwing exception at mediator level represents an exception occuring at some stage
        _mockMediator.Setup(m => m.Send(It.IsAny<CompletePaymentTransactionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("An exception has occured somewhere"));

        await Assert.ThrowsAsync<Exception>(async () => await _sut.CompletePaymentTransaction(Guid.NewGuid(), Guid.NewGuid(), validRequest));
    }

    private static CompletePaymentTransactionRequest CreateValidCompleteRequest()
    {
        return new CompletePaymentTransactionRequest
        {
            Fee = 100,
            FeeCurrency = "USD",
            ProviderType = ProviderType.Stripe,
            PaymentProviderPaymentTransactionId = "123456",
            Status = TerminalStatus.Succeeded,
            EventCreatedDateTime = DateTime.UtcNow
        };
    }
}

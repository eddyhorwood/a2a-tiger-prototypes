using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.SQS;
using Amazon.SQS.Model;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.SqsIntegrationClient.Options;
using PaymentExecution.TestUtilities;

namespace PaymentExecutionWorker.ComponentTests;

public class ComponentTestFixture : WebApplicationFactory<Program>
{
    private readonly Action<IWebHostBuilder> _configuration;
    private readonly Mapper _mapper;
    private readonly HttpClient _wireMockClient = new HttpClient();
    private readonly JsonSerializerOptions _defaultJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    public ComponentTestFixture(Action<IWebHostBuilder> configuration)
    {
        _configuration = configuration;

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PaymentTransactionDto, UpdateStatusPaymentTransactionDto>();
            cfg.CreateMap<PaymentTransactionDto, UpdateSuccessPaymentTransactionDto>();
            cfg.CreateMap<PaymentTransactionDto, UpdateFailurePaymentTransactionDto>();
            cfg.CreateMap<PaymentTransactionDto, UpdateCancelledPaymentTransactionDto>();
        });
        _mapper = new Mapper(mapperConfiguration);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _configuration(builder.Configure(_ => { }));
        builder.ConfigureServices(services =>
        {
            var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
            services.AddSingleton<TimeProvider>(fakeTimeProvider);

            // Configure no delay for tests, and only retry once. Note - configuring this to have a non-zero delay will
            // break polly, as it's dependent on TimeProvider which is being faked above and frozen in time
            services.Configure<PaymentRequestRetryOptions>(o =>
            {
                o.MaxRetryAttempts = 1;
                o.DelaySeconds = 0;
            });

            services.AddScoped<IPaymentTransactionComponentTestRepository, PaymentTransactionComponentTestRepository>();
            services.AddSingleton<Worker.Worker>();
        });
    }

    private IAmazonSQS SqsClient => Services.GetRequiredService<IAmazonSQS>();
    private ExecutionQueueOptions ExecutionQueueOptions => Services.GetRequiredService<IOptions<ExecutionQueueOptions>>().Value;
    private PaymentRequestServiceOptions PaymentRequestOptions =>
        Services.GetRequiredService<IOptions<PaymentRequestServiceOptions>>().Value;

    public IPaymentTransactionComponentTestRepository GetTestRepositoryInterface()
    {
        var scope = Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionComponentTestRepository>();
        return repository;
    }

    public async Task<PaymentTransactionDto> GetPaymentTransactionByPaymentRequestId(Guid paymentRequestId)
    {
        var scope = Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();

        var paymentTransactionResult = await repository.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);
        return paymentTransactionResult.Value!;
    }

    public async Task InsertPaymentTransactionsWithProviderData(List<PaymentTransactionDto> dtos)
    {
        var scope = Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();

        foreach (var dto in dtos)
        {
            var insertResult = await repository.InsertPaymentTransactionIfNotExist(new InsertPaymentTransactionDto()
            {
                PaymentRequestId = dto.PaymentRequestId,
                ProviderType = dto.ProviderType,
                Status = dto.Status,
                OrganisationId = Guid.NewGuid()
            });
            var paymentTransactionId = insertResult.Value;
            await repository.UpdatePaymentTransactionWithProviderDetails(new UpdateForSubmitFlowDto()
            {
                PaymentTransactionId = paymentTransactionId,
                ProviderServiceId = dto.ProviderServiceId ?? Guid.Empty,
                PaymentProviderPaymentTransactionId = dto.PaymentProviderPaymentTransactionId ?? "pi_1234"
            });
        }
    }

    public async Task InsertPaymentTransactions(List<InsertPaymentTransactionDto> dtos)
    {
        var scope = Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();

        foreach (var dto in dtos)
        {
            await repository.InsertPaymentTransactionIfNotExist(dto);
        }
    }

    public async Task PopulateSqsWithMessages(IList<Message> messages)
    {
        await SqsClient.SetQueueAttributesAsync(ExecutionQueueOptions.QueueUrl, new Dictionary<string, string>
        {
            { "VisibilityTimeout", "0" },
        });

        var messageEntries = new List<SendMessageBatchRequestEntry>();
        for (int i = 0; i < messages.Count; i++)
        {
            messageEntries.Add(new SendMessageBatchRequestEntry()
            {
                Id = $"{i + 1}",
                MessageBody = messages[i].Body,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    {
                        ExecutionConstants.XeroCorrelationId, new MessageAttributeValue()
                        {
                            DataType = "String",
                            StringValue = Guid.NewGuid().ToString(),
                        }
                    },
                    {
                        ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                        {
                            DataType = "String",
                            StringValue = Guid.NewGuid().ToString(),
                        }
                    }
                }
            });
        }
        var messageRequest = new SendMessageBatchRequest()
        {
            Entries = messageEntries,
            QueueUrl = ExecutionQueueOptions.QueueUrl
        };

        await SqsClient.SendMessageBatchAsync(messageRequest);
    }

    public async Task SetupSqsAndDbWithCount(int paymentRequestIdCount)
    {
        var paymentRequestIds = new List<Guid>(paymentRequestIdCount);
        for (var i = 0; i < paymentRequestIdCount; i++)
        {
            paymentRequestIds.Add(Guid.NewGuid());
        }

        var executionMessageList = paymentRequestIds.Select(guid => new ExecutionQueueMessage()
        {
            PaymentRequestId = guid,
            ProviderServiceId = Guid.NewGuid(),
            ProviderType = ProviderType.Stripe.ToString(),
            Fee = 5,
            FeeCurrency = "aud",
            PaymentProviderPaymentTransactionId = "pi_123456",
            PaymentProviderPaymentReferenceId = "ch_123456",
            Status = TerminalStatus.Succeeded.ToString()
        }).ToList();

        var paymentTransactionDtos = executionMessageList.Select(m => new PaymentTransactionDto()
        {
            CreatedUtc = DateTime.Now,
            Fee = m.Fee,
            FeeCurrency = m.FeeCurrency,
            PaymentRequestId = m.PaymentRequestId,
            ProviderServiceId = m.ProviderServiceId,
            PaymentProviderPaymentReferenceId = m.PaymentProviderPaymentReferenceId,
            PaymentProviderPaymentTransactionId = m.PaymentProviderPaymentTransactionId,
            ProviderType = m.ProviderType,
            Status = m.Status
        }).ToList();

        var queueMessages = executionMessageList.Select(exeMessage => new Message()
        {
            Body = JsonSerializer.Serialize(exeMessage)
        }).ToList();

        await InsertPaymentTransactionsWithProviderData(paymentTransactionDtos);
        await PopulateSqsWithMessages(queueMessages);
    }

    public async Task SetupSqsAndDbWithTwoInvalidMessages(int paymentRequestIdCount)
    {
        var paymentRequestIds = new List<Guid>(paymentRequestIdCount);
        for (var i = 0; i < paymentRequestIdCount - 2; i++)
        {
            paymentRequestIds.Add(Guid.NewGuid());
        }

        var executionMessageList = paymentRequestIds.Select(guid => new ExecutionQueueMessage()
        {
            PaymentRequestId = guid,
            ProviderServiceId = Guid.NewGuid(),
            ProviderType = ProviderType.Stripe.ToString(),
            Fee = 5,
            FeeCurrency = "aud",
            PaymentProviderPaymentTransactionId = "pi_123456",
            PaymentProviderPaymentReferenceId = "ch_123456",
            Status = TerminalStatus.Succeeded.ToString()
        }).ToList();

        var paymentTransactionDtos = executionMessageList.Select(m => new PaymentTransactionDto()
        {
            CreatedUtc = DateTime.Now,
            Fee = m.Fee,
            FeeCurrency = m.FeeCurrency,
            PaymentRequestId = m.PaymentRequestId,
            ProviderServiceId = m.ProviderServiceId,
            PaymentProviderPaymentReferenceId = m.PaymentProviderPaymentReferenceId,
            PaymentProviderPaymentTransactionId = m.PaymentProviderPaymentTransactionId,
            ProviderType = m.ProviderType,
            Status = m.Status
        }).ToList();

        var queueMessages = executionMessageList.Select(exeMessage => new Message()
        {
            Body = JsonSerializer.Serialize(exeMessage)
        }).ToList();

        //Add 2 invalid messages 
        //Note that no corresponding DB record is created as these messages fail before db step
        queueMessages.Add(new Message()
        {
            Body = "this-body-be-fake!"
        });
        queueMessages.Add(new Message()
        {
            Body = $@"
                        {{
                            ""Fee"":""5"",
                            ""FeeCurrency"":""aud"",
                            ""ProviderType"": ""Stripe"",
                            ""ProviderTransactionReference"": ""MyTransActionRef"",
                            ""PaymentRequestId"":""{Guid.NewGuid()}"",
                            ""ProviderServiceId"":""{Guid.NewGuid()}"",
                            ""Status"": ""maybe-the-fakest-status-weve-ever-seen""
                          }}",
        });

        await InsertPaymentTransactionsWithProviderData(paymentTransactionDtos);
        await PopulateSqsWithMessages(queueMessages);
    }
    public async Task PurgeQueue()
    {
        await SqsClient.PurgeQueueAsync(ExecutionQueueOptions.QueueUrl);
    }

    public async Task<ReceiveMessageResponse> GetMessagesFromQueue()
    {
        ReceiveMessageRequest receiveMessageRequest = new()
        {
            QueueUrl = ExecutionQueueOptions.QueueUrl,
            MaxNumberOfMessages = ExecutionQueueOptions.MaxNumberOfMessages,
            WaitTimeSeconds = ExecutionQueueOptions.LongPollingTimeoutSeconds
        };

        return await SqsClient.ReceiveMessageAsync(receiveMessageRequest);
    }

    public async Task<ReceiveMessageResponse> GetMessagesFromDlq()
    {
        ReceiveMessageRequest receiveMessageRequest = new()
        {
            QueueUrl = "http://sqs.ap-southeast-2.localhost.localstack.cloud:4566/000000000000/collectingpayments-execution-payment-execution-dlq-test",
            MaxNumberOfMessages = ExecutionQueueOptions.MaxNumberOfMessages,
            WaitTimeSeconds = ExecutionQueueOptions.LongPollingTimeoutSeconds
        };

        return await SqsClient.ReceiveMessageAsync(receiveMessageRequest);
    }

    public async Task MockPaymentRequestExecutionSucceedToReturnStatusCode(string stubMappingId, int statusCode)
    {
        var httpClient = new HttpClient() { BaseAddress = new Uri(PaymentRequestOptions.BaseUrl) };

        var bodyContent = new JsonObject()
        {
            ["id"] = stubMappingId,
            ["priority"] = 1,
            ["request"] = new JsonObject()
            {
                ["method"] = "POST",
                ["urlPathPattern"] = "/v1/payment-requests/.*/execution-succeed",
            },
            ["response"] = new JsonObject()
            {
                ["status"] = statusCode,
                ["body"] = "Mocked response from wiremock Execution Succeed"
            }
        };

        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            Content = new StringContent(bodyContent.ToJsonString()),
            RequestUri = new Uri("http://localhost:12111/__admin/mappings")
        };

        await httpClient.SendAsync(request);
    }

    public async Task MockPaymentRequestGetPaymentRequestToReturnStatusCode(string stubMappingId, int statusCode)
    {
        var httpClient = new HttpClient() { BaseAddress = new Uri(PaymentRequestOptions.BaseUrl) };

        var bodyContent = new JsonObject()
        {
            ["id"] = stubMappingId,
            ["priority"] = 1,
            ["request"] = new JsonObject()
            {
                ["method"] = "GET",
                ["urlPathPattern"] = "/v1/payment-requests/([0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12})",
            },
            ["response"] = new JsonObject()
            {
                ["status"] = statusCode,
                ["body"] = "Mocked response from wiremock - Get Payment Request"
            }
        };

        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            Content = new StringContent(bodyContent.ToJsonString()),
            RequestUri = new Uri("http://localhost:12111/__admin/mappings")
        };

        await httpClient.SendAsync(request);
    }

    public static async Task RemoveMappingOverrideOnPaymentRequestService(string stubMappingId)
    {
        var httpClient = new HttpClient();
        var deleteRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"http://localhost:12111/__admin/mappings/{stubMappingId}")
        };
        await httpClient.SendAsync(deleteRequest);
    }

    public static Task RemoveMappingOverrideOnPaymentRequestFromStubIdList(List<string> stubMappingIds)
    {
        var removeMappingTasks = stubMappingIds.Select(RemoveMappingOverrideOnPaymentRequestService);
        return Task.WhenAll(removeMappingTasks);
    }

    public async Task<WiremockUtility.WiremockRequest[]?> GetListOfRequestUrlsSentToPaymentRequestWiremock()
    {
        var wiremockHttpResponse = await _wireMockClient.GetAsync("http://localhost:12111/__admin/requests");
        var wiremockRequestsJsonResponse = JsonDocument.Parse(await wiremockHttpResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("requests").ToString();
        var wiremockRequestsMadeWithUrl = JsonSerializer.Deserialize<WiremockUtility.WiremockRequest[]>(wiremockRequestsJsonResponse, _defaultJsonSerializerOptions);

        return wiremockRequestsMadeWithUrl;
    }
}

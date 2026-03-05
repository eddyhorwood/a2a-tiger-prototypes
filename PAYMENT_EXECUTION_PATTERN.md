# Payment Execution Service - SQS Status Update Pattern

## Overview
The Payment Execution Service uses a **provider-agnostic SQS-based pattern** for processing payment completion webhooks. This pattern separates webhook receipt from webhook processing, making it easy to add new payment providers (like Akahu for NZ A2A) without changing core logic.

## Architecture Components

### 1. Payment Execution Service (API)
**Location**: `_reference/payment-execution-service/projects/service`

**Key Endpoint**: `POST /v1/payments/payment-requests/{paymentRequestId}/provider-executions/{providerServiceId}/complete`

**What it does**:
- Receives webhook notifications from provider-specific services (e.g., Stripe Execution Service)
- Validates the request with conditional validation attributes
- **Immediately enqueues** the message to SQS
- Returns `202 Accepted` (async processing)

**Request Model**: [`CompletePaymentTransactionRequest`](../../_reference/payment-execution-service/projects/service/src/PaymentExecutionService.Api/Models/CompletePaymentTransactionRequest.cs)
```csharp
{
    Fee: decimal?,
    FeeCurrency: string?,
    PaymentProviderPaymentTransactionId: string?, // e.g., Stripe PaymentIntent ID
    PaymentProviderPaymentReferenceId: string?,   // e.g., Stripe Charge ID
    ProviderType: enum?,                          // Stripe, Akahu, etc.
    Status: enum?,                                // Succeeded, Failed, Cancelled
    FailureDetails: string?,
    EventCreatedDateTime: DateTime,
    PaymentProviderLastUpdatedAt: DateTime?,
    CancellationReason: string?
}
```

### 2. SQS Completion Queue
**What it does**:
- **Decouples** webhook receipt from processing
- Provides durability (webhooks won't be lost if Worker is down)
- Enables batching and parallel processing
- Allows retry on failure

**Message Format**: [`CompleteMessageBody`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Models/CompleteMessageBody.cs)
```csharp
{
    PaymentRequestId: Guid,
    ProviderServiceId: Guid,
    Status: string,
    Fee: decimal?,
    FeeCurrency: string?,
    PaymentProviderPaymentTransactionId: string?,
    PaymentProviderPaymentReferenceId: string?,
    FailureDetails: string?,
    EventCreatedDateTime: DateTime?,
    PaymentProviderLastUpdatedAt: DateTime?,
    CancellationReason: string?
}
```

**Message Attributes** (headers):
- `Xero-Correlation-Id`: For distributed tracing
- `Xero-Tenant-Id`: Organization context
- **New Relic distributed trace headers**

### 3. Payment Execution Worker
**Location**: `_reference/payment-execution-service/projects/worker`

**Polling Behavior**:
- **Long-polls** SQS every **20ms** ([`Worker.cs` line 36](../../_reference/payment-execution-service/projects/worker/src/PaymentExecutionWorker.Worker/Worker.cs#L36))
- Receives up to 10 messages per poll (configurable)
- Processes messages **in parallel** (batched)
- **Batch-deletes** successfully processed messages

**Processing Flow**: [`ProcessCompleteMessagesCommand`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Commands/ProcessCompleteMessagesCommand.cs)

1. **Receive batch** from SQS
2. **Process in parallel** (each message):
   - Transform to domain model (`CompleteMessage`)
   - Load payment transaction from DB
   - **Stale event detection**: Check if event is older than DB state → ignore
   - Update DB with new status
   - Call **payment-request-service** to notify upstream
3. **Batch-delete** all successfully processed messages
4. Log failures (failed messages stay in queue for retry)

### 4. Stale Event Detection
**Location**: [`ProcessCompleteMessageDomainService.ShouldEventBeIgnored()`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Service/ProcessCompleteMessageDomainService.cs#L50)

**Why it matters**: Payment providers can send duplicate/out-of-order webhooks

**Logic**:
```
IF (webhook.EventCreatedDateTime <= db.LastUpdatedAt) THEN
    Log: "Stale event - ignoring"
    Mark message as successfully processed (delete from queue)
    Don't update DB or call payment-request-service
END
```

## How Stripe Uses This Pattern

### Stripe Execution Service
**Not found in payment-execution-service repo** (likely in `collecting-payments-execution-stripe-execution-service`)

**What it does**:
1. **Receives Stripe webhooks** directly from Stripe
2. Validates webhook signature
3. **Maps Stripe event** to `CompletePaymentTransactionRequest`
4. **POSTs to** Payment Execution Service `/complete` endpoint
5. Payment Execution Service enqueues to SQS
6. Returns success to Stripe

## How Akahu Would Fit In

### Option 1: Thin Lambda (Recommended in TDR)
```
┌────────────┐     ┌──────────────┐     ┌─────────────────┐     ┌─────────┐
│   Akahu    │────►│ Lambda/API   │────►│ Payment Exec    │────►│   SQS   │
│  Webhook   │     │  (Thin)      │     │    Service      │     │  Queue  │
└────────────┘     └──────────────┘     └─────────────────┘     └─────────┘
                          │
                    Map Akahu → CompletePaymentTransactionRequest
```

**Implementation**:
1. Create AWS Lambda or API endpoint
2. Receive Akahu webhook POST
3. Validate Akahu signature (if available)
4. **Map Akahu status** to `CompletePaymentTransactionRequest`:
   ```csharp
   {
       PaymentRequestId: /* from payment metadata */,
       ProviderServiceId: /* Akahu provider ID */,
       ProviderType: ProviderType.Akahu, // New enum value
       Status: MapAkahuStatus(akahuPayment.Status),
       PaymentProviderPaymentTransactionId: akahuPayment.Id,
       PaymentProviderPaymentReferenceId: akahuPayment.Reference,
       EventCreatedDateTime: akahuPayment.UpdatedAt,
       PaymentProviderLastUpdatedAt: akahuPayment.UpdatedAt,
       Fee: null, // Akahu doesn't charge per-txn fees
       FeeCurrency: null
   }
   ```
5. **POST to Payment Execution Service** `/complete` endpoint
6. Done! Worker handles the rest identically

**Status Mapping**:
```csharp
Akahu Status → Payment Execution Status
─────────────────────────────────────────
"SENT"        → TerminalStatus.Succeeded
"FAILED"      → TerminalStatus.Failed
"CANCELLED"   → TerminalStatus.Cancelled
"PENDING"     → (Ignore - not terminal)
"AUTHORISED"  → (Ignore - not terminal)
```

### Option 2: Direct SQS Enqueue (Alternative)
Skip Payment Execution Service API, put message directly on SQS:

```
┌────────────┐     ┌──────────────┐     ┌─────────┐
│   Akahu    │────►│ Lambda       │────►│   SQS   │
│  Webhook   │     │ (Thin)       │     │  Queue  │
└────────────┘     └──────────────┘     └─────────┘
```

**Pros**: 
- Fewer hops
- Lower latency

**Cons**: 
- Bypasses API validation
- No API-level auth/circuit breakers
- Must construct `CompleteMessageBody` + message attributes directly

## Key Insights for A2A Implementation

### 1. Zero Changes Needed to Worker
The Worker is **completely provider-agnostic**. It processes `CompleteMessageBody` messages regardless of source:
- No Stripe-specific logic in Worker
- No if/else branching on `ProviderType`
- Stale event detection works identically

### 2. Provider-Specific Logic Stays at Edges
- **Webhook receipt**: Provider-specific (Stripe Lambda, Akahu Lambda)
- **Status mapping**: Provider-specific
- **Core processing**: Shared (Worker, DB updates, payment-request-service calls)

### 3. Pattern Advantages
- **Async by default**: Webhooks return immediately
- **Resilient**: SQS provides durability & retry
- **Observable**: Distributed tracing via message attributes
- **Scalable**: Worker can process batches in parallel
- **Testable**: Can directly enqueue test messages to SQS

### 4. What You Need to Build

#### Minimal (Lambda + Mapping)
1. **Akahu Webhook Lambda** (new)
2. **Status mapper** (Akahu → `CompletePaymentTransactionRequest`)
3. **HTTP client** to POST to Payment Execution Service

#### Required Config
- Payment Execution Service URL
- SQS queue name (if Option 2)
- Auth credentials for Payment Execution Service
- Akahu webhook signature validation key

#### Database Changes
- Add `ProviderType.Akahu` enum value
- Potentially: Add Akahu-specific fields to payment transaction table

#### Testing
- Can test Worker by directly enqueueing mock Akahu messages to SQS
- No need to invoke real Akahu webhooks for integration tests

## Code References

### Key Files to Study
1. **Worker loop**: [`Worker.cs`](../../_reference/payment-execution-service/projects/worker/src/PaymentExecutionWorker.Worker/Worker.cs)
2. **Message processor**: [`ProcessCompleteMessagesCommand.cs`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Commands/ProcessCompleteMessagesCommand.cs)
3. **SQS client**: [`ExecutionQueueService.cs`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.SqsIntegrationClient/Service/ExecutionQueueService.cs)
4. **Stale event detection**: [`ProcessCompleteMessageDomainService.cs`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Service/ProcessCompleteMessageDomainService.cs)
5. **API endpoint**: [`PaymentsController.cs`](../../_reference/payment-execution-service/projects/service/src/PaymentExecutionService.Api/Controllers/V1/PaymentsController.cs)
6. **Request model**: [`CompletePaymentTransactionRequest.cs`](../../_reference/payment-execution-service/projects/service/src/PaymentExecutionService.Api/Models/CompletePaymentTransactionRequest.cs)
7. **Message types**: 
   - [`CompleteMessageBody.cs`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Models/CompleteMessageBody.cs)
   - [`CompleteMessage.cs`](../../_reference/payment-execution-service/projects/shared/PaymentExecution.Domain/Models/CompleteMessage.cs)

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         WEBHOOK SOURCES                              │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────┐         ┌─────────────────┐                   │
│  │ Stripe Webhook  │         │ Akahu Webhook   │                   │
│  │   (existing)    │         │    (new)        │                   │
│  └────────┬────────┘         └────────┬────────┘                   │
│           │                           │                             │
│           ▼                           ▼                             │
│  ┌─────────────────┐         ┌─────────────────┐                   │
│  │ Stripe Lambda/  │         │ Akahu Lambda/   │                   │
│  │    Service      │         │    Service      │                   │
│  │                 │         │                 │                   │
│  │ • Validate sig  │         │ • Validate sig  │                   │
│  │ • Map to std    │         │ • Map to std    │                   │
│  └────────┬────────┘         └────────┬────────┘                   │
│           │                           │                             │
└───────────┼───────────────────────────┼─────────────────────────────┘
            │                           │
            └──────────┬────────────────┘
                       │
                       ▼
         ┌─────────────────────────────┐
         │ Payment Execution Service   │
         │         (API)               │
         │                             │
         │  POST /v1/payments/.../     │
         │        complete             │
         │                             │
         │  • Validate request         │
         │  • Enqueue to SQS           │
         │  • Return 202 Accepted      │
         └──────────────┬──────────────┘
                        │
                        ▼
              ┌──────────────────┐
              │   SQS Queue      │
              │  (Completion)    │
              │                  │
              │  • Durability    │
              │  • Retry logic   │
              │  • Batching      │
              └────────┬─────────┘
                       │
                       │ Long-poll every 20ms
                       │
                       ▼
         ┌─────────────────────────────┐
         │ Payment Execution Worker    │
         │     (Background Service)    │
         │                             │
         │  FOR EACH message:          │
         │    1. Transform to domain   │
         │    2. Load from DB          │
         │    3. Check for stale       │
         │    4. Update DB             │
         │    5. Notify request-svc    │
         │                             │
         │  Batch-delete processed     │
         └──────────┬─────────┬────────┘
                    │         │
                    │         └─────────────────┐
                    │                           │
                    ▼                           ▼
         ┌──────────────────┐      ┌───────────────────────┐
         │  PostgreSQL DB   │      │ Payment Request Svc   │
         │                  │      │                       │
         │  • Update status │      │ • Mark invoice paid   │
         │  • Store metadata│      │ • Trigger webhook     │
         └──────────────────┘      └───────────────────────┘
```

## Conclusion

The SQS-based pattern is **production-ready and extensible**. Adding Akahu support requires:
1. **One new Lambda/service** to receive Akahu webhooks
2. **One status mapper** (10-20 lines)
3. **One HTTP POST** to existing Payment Execution Service

The Worker, DB logic, and payment-request-service integration remain **completely unchanged**.

---

**Related Files**:
- [A2A Payments App (current prototype)](../A2APaymentsApp/)
- [Payment Execution Service (reference)](../_reference/payment-execution-service/)

# Payment Webhook and Credit Flow

This document describes the flow and design choices behind the payment completion and webhook handling in the FlexFit Payment microservice.

## Webhook Handling Strategy

The system receives PayOS webhooks at `POST /api/payment/payos-webhook`. The flow processes the payment through the following stages:

1. **Signature Verification**: Validates the payload signature using the PayOS SDK.
2. **Transaction Lookup**: Maps the `orderCode` provided by PayOS to a `ProviderTransactionCode` in our `Payments` table.
3. **Idempotency Check (Redis & SQL)**: 
   - A best-effort Redis distributed lock is used to prevent parallel processing for the same payment or user wallet.
   - If Redis is unavailable, the webhook proceeds gracefully.
   - **Strict SQL Idempotency**: Inside a transaction, we use `ExecuteUpdateAsync` to atomically transition the `Status` from `Pending` to `Success`. If the transition affects 0 rows, we know another process already completed it.

## The Atomic Completion

Once the atomic status update succeeds, we safely:
1. Increment the user's `UserCredits`.
2. Insert a `CreditTransaction` reflecting the deposit.
3. Append a `PaymentCompleted` or `PaymentFailed` event into the `OutboxMessages` table.
4. Issue a `COMMIT` to save all above changes simultaneously.

## Redis Independence

To satisfy the system constraint of running independently from Redis:
- Cache invalidation and lock acquisition wrap the `RedisConnectionException` in `try/catch` and gracefully ignore them. 
- All data integrity guarantees rely entirely on SQL Server transactions and row locks.
- If Redis is down, Outbox events are still successfully recorded in SQL. The Worker continues checking the Outbox SQL table independently.

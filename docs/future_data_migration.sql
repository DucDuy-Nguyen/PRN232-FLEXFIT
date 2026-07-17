-- OPTIONAL FUTURE DATA MIGRATION SCRIPT
-- This script outlines how to migrate payment-related data from the monolith database (FlexFitDb)
-- to the separate Payment microservice database (FlexFitPaymentDb).
-- DO NOT EXECUTE THIS SCRIPT YET.

/*
BEGIN TRANSACTION;

-- 1. Migrate CreditPackages
-- If Identity Insert is needed, enable it first.
-- SET IDENTITY_INSERT FlexFitPaymentDb.dbo.CreditPackages ON;
INSERT INTO FlexFitPaymentDb.dbo.CreditPackages (
    PackageId, PackageName, CreditAmount, BonusCredit, Price, Description, IsPopular, IsActive, CreatedAt
)
SELECT 
    PackageId, PackageName, CreditAmount, BonusCredit, Price, Description, IsPopular, IsActive, CreatedAt
FROM FlexFitDb.dbo.CreditPackages;
-- SET IDENTITY_INSERT FlexFitPaymentDb.dbo.CreditPackages OFF;

-- 2. Migrate UserCredits
INSERT INTO FlexFitPaymentDb.dbo.UserCredits (
    UserCreditId, UserId, Balance, TotalEarned, TotalSpent, UpdatedAt
)
SELECT 
    UserCreditId, UserId, Balance, TotalEarned, TotalSpent, UpdatedAt
FROM FlexFitDb.dbo.UserCredits;

-- 3. Migrate CreditTransactions
INSERT INTO FlexFitPaymentDb.dbo.CreditTransactions (
    TransactionId, UserId, Amount, BalanceBefore, BalanceAfter, Type, ReferenceId, ReferenceType, Description, CreatedAt
)
SELECT 
    TransactionId, UserId, Amount, BalanceBefore, BalanceAfter, Type, ReferenceId, ReferenceType, Description, CreatedAt
FROM FlexFitDb.dbo.CreditTransactions;

-- 4. Migrate Payments
-- Disable constraint check if needed, migrate, then re-enable
INSERT INTO FlexFitPaymentDb.dbo.Payments (
    PaymentId, UserId, PackageId, Amount, PaymentMethod, ProviderTransactionCode, Status, PaidAt, CreatedAt
)
SELECT 
    PaymentId, UserId, PackageId, Amount, PaymentMethod, ProviderTransactionCode, Status, PaidAt, CreatedAt
FROM FlexFitDb.dbo.Payments;

COMMIT TRANSACTION;
*/

-- Rollback: rollback_loans.sql
-- Drops the loans table and loan_type enum

DROP TABLE IF EXISTS loan_repayments;
DROP TABLE IF EXISTS loans;
DROP TYPE  IF EXISTS loan_type;

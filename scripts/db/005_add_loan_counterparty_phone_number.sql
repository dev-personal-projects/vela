-- Migration: 005_add_loan_counterparty_phone_number.sql
-- Adds an optional counterparty phone number for loan contact details

ALTER TABLE loans
ADD COLUMN IF NOT EXISTS counterparty_phone_number TEXT;

ALTER TABLE loans
DROP CONSTRAINT IF EXISTS chk_counterparty_phone_number_not_blank;

ALTER TABLE loans
ADD CONSTRAINT chk_counterparty_phone_number_not_blank
CHECK (
    counterparty_phone_number IS NULL
    OR char_length(btrim(counterparty_phone_number)) > 0
);

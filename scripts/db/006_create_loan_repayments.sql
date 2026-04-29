-- Migration: 006_create_loan_repayments.sql
-- Creates repayment history records for loans

CREATE TABLE IF NOT EXISTS loan_repayments (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id     UUID            NOT NULL REFERENCES loans(id) ON DELETE CASCADE,
    amount      NUMERIC(18, 2)  NOT NULL CHECK (amount > 0),
    date        TIMESTAMPTZ     NOT NULL,
    note        TEXT            NOT NULL DEFAULT '',
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_loan_repayments_loan_id ON loan_repayments(loan_id);
CREATE INDEX IF NOT EXISTS idx_loan_repayments_date    ON loan_repayments(date DESC);

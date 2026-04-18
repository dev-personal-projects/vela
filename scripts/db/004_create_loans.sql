-- Migration: 004_create_loans.sql
-- Creates the loans table scoped to Supabase auth users
-- loan_type: 'Owed' = money I borrowed (liability)
--            'Given' = money I lent (asset)

DO $$ BEGIN
    CREATE TYPE loan_type AS ENUM ('Owed', 'Given');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS loans (
    id                 UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            UUID        NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    counterparty_name  TEXT        NOT NULL CHECK (char_length(counterparty_name) > 0),
    amount             NUMERIC(18, 2) NOT NULL CHECK (amount > 0),
    amount_repaid      NUMERIC(18, 2) NOT NULL DEFAULT 0 CHECK (amount_repaid >= 0),
    type               loan_type   NOT NULL,
    description        TEXT        NOT NULL DEFAULT '',
    date               TIMESTAMPTZ NOT NULL,
    due_date           TIMESTAMPTZ,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT chk_repaid_lte_amount CHECK (amount_repaid <= amount),
    CONSTRAINT chk_due_date_after_date CHECK (due_date IS NULL OR due_date > date)
);

CREATE INDEX IF NOT EXISTS idx_loans_user_id ON loans(user_id);
CREATE INDEX IF NOT EXISTS idx_loans_type    ON loans(type);

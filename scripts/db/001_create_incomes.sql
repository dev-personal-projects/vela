-- Migration: 001_create_incomes.sql
-- Creates the incomes table scoped to Supabase auth users

CREATE TABLE IF NOT EXISTS incomes (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    amount      NUMERIC(18, 2) NOT NULL CHECK (amount > 0),
    source      TEXT        NOT NULL CHECK (char_length(source) > 0),
    description TEXT        NOT NULL DEFAULT '',
    date        TIMESTAMPTZ NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_incomes_user_id ON incomes(user_id);
CREATE INDEX IF NOT EXISTS idx_incomes_date    ON incomes(date);

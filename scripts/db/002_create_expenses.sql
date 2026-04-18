-- Migration: 002_create_expenses.sql
-- Creates the expenses table scoped to Supabase auth users

CREATE TABLE IF NOT EXISTS expenses (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    amount      NUMERIC(18, 2) NOT NULL CHECK (amount > 0),
    category    TEXT        NOT NULL CHECK (char_length(category) > 0),
    description TEXT        NOT NULL DEFAULT '',
    date        TIMESTAMPTZ NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_expenses_user_id  ON expenses(user_id);
CREATE INDEX IF NOT EXISTS idx_expenses_date     ON expenses(date);
CREATE INDEX IF NOT EXISTS idx_expenses_category ON expenses(category);

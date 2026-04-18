-- Migration: 003_create_savings_goals.sql
-- Creates the savings_goals table scoped to Supabase auth users

CREATE TABLE IF NOT EXISTS savings_goals (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID        NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    name           TEXT        NOT NULL CHECK (char_length(name) > 0),
    target_amount  NUMERIC(18, 2) NOT NULL CHECK (target_amount > 0),
    current_amount NUMERIC(18, 2) NOT NULL DEFAULT 0 CHECK (current_amount >= 0),
    deadline       TIMESTAMPTZ,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT chk_current_lte_target CHECK (current_amount <= target_amount)
);

CREATE INDEX IF NOT EXISTS idx_savings_goals_user_id ON savings_goals(user_id);

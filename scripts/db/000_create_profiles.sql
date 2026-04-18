-- Migration: 000_create_profiles.sql
-- Public profiles table linked to Supabase auth.users.
-- Supabase manages auth.users (email + password) — we never create or modify it.
-- This table is a lightweight mirror: id matches auth.users.id, email is copied on signup.

CREATE TABLE IF NOT EXISTS profiles (
    id         UUID        PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email      TEXT        NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
    
);

CREATE INDEX IF NOT EXISTS idx_profiles_email ON profiles(email);

-- Auto-create a profile row when a new Supabase auth user signs up
CREATE OR REPLACE FUNCTION handle_new_user()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER SET search_path = public
AS $$
BEGIN
    INSERT INTO public.profiles (id, email)
    VALUES (NEW.id, NEW.email)
    ON CONFLICT (id) DO NOTHING;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW
    EXECUTE FUNCTION handle_new_user();

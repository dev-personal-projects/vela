-- Rollback: rollback_profiles.sql

DROP TRIGGER  IF EXISTS on_auth_user_created ON auth.users;
DROP FUNCTION IF EXISTS handle_new_user();
DROP TABLE    IF EXISTS profiles;

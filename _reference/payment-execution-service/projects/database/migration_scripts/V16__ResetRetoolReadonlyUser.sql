-- This script resets the password for the retool_readonly user
-- The temp password will be replaced with the actual password during deployment

ALTER USER retool_readonly WITH PASSWORD 'temp_p@ssw0rd';


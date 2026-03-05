-- This script creates the role required for DRE to load this database in DataConsole. 
-- See https://xero.atlassian.net/wiki/spaces/DRE/pages/270735966353/How+to+connect+DataConsole+to+a+Postgres+database

CREATE USER dataconsolereader WITH
    LOGIN
    PASSWORD 'Free+hEd@Ta'
    NOSUPERUSER
    INHERIT
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION;
CREATE ROLE dataconsolereaderrole WITH
    NOLOGIN
    NOSUPERUSER
    INHERIT
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION;

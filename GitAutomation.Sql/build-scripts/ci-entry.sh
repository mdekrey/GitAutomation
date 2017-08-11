#!/bin/bash

/opt/mssql/bin/sqlservr &

./wait-for-sql.sh

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -Q "CREATE DATABASE [gitautomation]"

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -d gitautomation \
    -i TargetBranch.sql \
    -i BaseBranch.sql

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -d gitautomation \
    -Q "BACKUP DATABASE [gitautomation] TO DISK = N'/out/obj/Docker/publish/master.bak' WITH NOFORMAT, NOINIT, NAME = 'gitautomation-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"


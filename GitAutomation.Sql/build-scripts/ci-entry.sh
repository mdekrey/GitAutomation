#!/bin/bash

/opt/mssql/bin/sqlservr &

./wait-for-sql.sh

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -d master \
    -i TargetBranch.sql \
    -i BaseBranch.sql

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -d master \
    -Q "BACKUP DATABASE [master] TO DISK = N'/out/obj/Docker/publish/master.bak' WITH NOFORMAT, NOINIT, NAME = 'gitautomation-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"


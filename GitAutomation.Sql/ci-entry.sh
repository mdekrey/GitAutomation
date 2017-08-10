#!/bin/bash

/opt/mssql/bin/sqlservr &

retries=100
while ((retries > 0)); do
    /opt/mssql-tools/bin/sqlcmd -S localhost \
        -U sa -P $SA_PASSWORD \
        -d master \
        -q "SELECT 1" \
        > /dev/null \
    && break

    sleep 1
    ((retries --))
done
if ((retries == 0 )); then
    exit 1
fi

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -d master \
    -i TargetBranch.sql \
    -i BaseBranch.sql

/opt/mssql-tools/bin/sqlcmd -S localhost \
    -U sa -P $SA_PASSWORD \
    -d master \
    -Q "BACKUP DATABASE [master] TO DISK = N'/src/obj/Release/master.bak' WITH NOFORMAT, NOINIT, NAME = 'gitautomation-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"


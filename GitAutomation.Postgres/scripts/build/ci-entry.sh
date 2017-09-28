#!/bin/bash

head -n -1 /docker-entrypoint.sh > ./postgres-setup.sh
chmod 755 ./postgres-setup.sh
./postgres-setup.sh postgres
su postgres -c "/usr/lib/postgresql/$PG_MAJOR/bin/postgres" &

# sleep 1000

./wait-for-sql.sh

mkdir -p /out/obj/Docker/publish/
pg_dump postgres -U postgres > /out/obj/Docker/publish/init.sql

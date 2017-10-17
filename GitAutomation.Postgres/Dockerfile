FROM postgres:9.6

COPY obj/Docker/publish/init-branching.sql /docker-entrypoint-initdb.d/
COPY obj/Docker/publish/init-security.sql /docker-entrypoint-initdb.d/

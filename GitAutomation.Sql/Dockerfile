FROM microsoft/mssql-server-linux:latest

WORKDIR /src

ARG source
COPY shared-scripts/*.sh ./
COPY runtime-scripts/*.sh ./
COPY obj/Docker/publish/master.bak ./

# Needs environment variables set up;
# https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker
CMD /bin/bash -c "./entrypoint.sh"

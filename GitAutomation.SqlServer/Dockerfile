FROM microsoft/mssql-server-linux:2017-GA as builder

WORKDIR /working
COPY shared-scripts/*.sh ./
COPY build-scripts/*.sh ./
COPY obj/Docker/publish/*.sql ./

ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=weakPASSw0rd

RUN /bin/bash -c "./ci-entry.sh"

FROM microsoft/mssql-server-linux:2017-GA

WORKDIR /src

ARG source
COPY shared-scripts/*.sh ./
COPY runtime-scripts/*.sh ./
COPY --from=builder /out/obj/Docker/publish/master.bak ./

EXPOSE 1433

# Needs environment variables set up;
# https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker
CMD /bin/bash -c "./entrypoint.sh"

FROM microsoft/mssql-server-linux:latest

WORKDIR /app
COPY *.sh ./
COPY *.sql ./

# Needs environment variables set up;
# https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker
CMD ./entrypoint.sh

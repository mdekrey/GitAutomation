FROM microsoft/aspnetcore:1.1
RUN apt-get update && apt-get install -y \
  git
ARG source

WORKDIR /app/scripts
RUN echo "cat /run/secrets/gitcredentials" > /app/scripts/get-git-password.sh && chmod 700 /app/scripts/get-git-password.sh
ENV GIT_ASKPASS=/app/scripts/get-git-password.sh

WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "GitAutomation.dll"]

#!/bin/bash

docker build -t testing-git-server -f ./git-server/Dockerfile ./git-server
docker build -t gitautomation/psql-host -f ./GitAutomation.Postgres/Dockerfile ./GitAutomation.Postgres

docker build -t gitautomation/web -f ./GitAutomation/Dockerfile ./GitAutomation

docker build -t gitautomation/psql-addon -f ./Dockerfile-addon --build-arg subfolder=GitAutomation.Postgres .
docker build -t gitautomation/sql-addon -f ./Dockerfile-addon --build-arg subfolder=GitAutomation.SqlServer .
docker build -t gitautomation/github-addon -f ./Dockerfile-addon --build-arg subfolder=GitAutomation.GitHub .
docker build -t gitautomation/microsoft-team-services-addon -f ./Dockerfile-addon --build-arg subfolder=GitAutomation.MicrosoftTeamServices .

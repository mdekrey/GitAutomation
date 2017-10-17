#!/bin/bash

# Intended to be run with cwd being the root of the project inside docker

cd GitAutomation
yarn
yarn build --env.NODE_ENV=production
cd ..

dotnet restore ./GitAutomation.sln && dotnet publish ./GitAutomation.sln -c ci-docker -o ./obj/Docker/publish

cd GitAutomation.Postgres
# Need to build the debug version to generate scripts
dotnet build
dotnet ef migrations script --no-build --context GitAutomation.EFCore.SecurityModel.SecurityContext -o obj/Docker/publish/init-security.sql
dotnet ef migrations script --no-build --context GitAutomation.EFCore.BranchingModel.BranchingContext -o obj/Docker/publish/init-branching.sql
cd ..

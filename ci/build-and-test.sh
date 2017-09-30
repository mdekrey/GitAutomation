cd GitAutomation
yarn build
cd ..

dotnet restore ./GitAutomation.sln

# TODO - figure out how to run the docker db's for testing
# dotnet test ./GitAutomation.sln -c ci-docker

dotnet publish ./GitAutomation.sln -c ci-docker -o ./obj/Docker/publish

# Automation for Scaled Git Flow!

[![Build Status](https://travis-ci.org/mdekrey/GitAutomation.svg?branch=latest)](https://travis-ci.org/mdekrey/GitAutomation)
[![Docker Stars](https://img.shields.io/docker/pulls/gitautomation/web.svg)](https://hub.docker.com/u/gitautomation/)

This is an automation project for [Scaled Git Flow](https://medium.com/@matt.dekrey/a-better-git-branching-model-b3bc8b73e472).
This currently a work in progress, as is noted by the Issues list, but will work for a minimal set of use-cases.

## Prerequisites

You need docker for your operating system. That's it!

# Running GitAutomation
Currently, we're still in a pre-release mode; I think it could be sufficiently considered in an "alpha" state, though it is quite stable!

* See the [production-sample](./production-sample) folder for an example docker-compose file and instructions on how to set up.

# Development

If you want to hack on GitAutomation...

## Before you get started
Local files that are not included in the repository include:

 * /configuration.json - the various configuration settings to use, including the git repo and persistence database. See the `configuration.sample.json` for format. This has several values that need to be replaced by you. For example, most of this file is set up to use GitHub; see [GitHub Setup](./GitAutomation.GitHub/github-setup.md).
 * /git-credentials.txt - the password to use for the git repository. Not persisted in the docker image for security purposes.
 * /psql-credentials.txt - the environment variable definition for the postgres database. Not persisted in the docker image for security purposes.

When you add these files, they should be without line endings and without UTF headers or you'll get difficult-to-track errors.

*Note*: Changing these files does not automatically change the values inside the docker containers. Since the default configuration does not preserve the database between runs, be careful when adjusting the secrets.

## Testing

As part of the debug compose file, there is a docker image that sets up a basic repository; you can set up and tear down the test suite's docker-compose to try out various features and techniques.

Repositories:
 - http://tester@git-server/git/gittesting1.git
 - http://tester@git-server/git/gittesting2.git
 - http://tester@git-server/git/gittesting3.git

Password:
 - TEST_PASSWORD

They all start out as identical. If you want to clone them locally, they should be accessible on port 8082.

# Building via Visual Studio 2017

As of VS2017 update 15.3, the [Visual Studio Tools for Docker no longer handle non-dotnet Dockerfiles as part of compose.](https://developercommunity.visualstudio.com/content/problem/96130/solution-build-fails-with-docker-compose-error-in.html)
This is a bug, but the repositories have been updated to reflect it.

This uses [.NET Core 2.0.0 SDK](https://github.com/dotnet/core/blob/master/release-notes/download-archives/2.0.0-download.md) to run. ([Permalink to that version.](https://github.com/dotnet/core/blob/5f845efbe93063325bf317dadd81ddce42fd3b63/release-notes/download-archives/2.0.0-download.md))

1. Make sure the configuration files mentioned above are in place.
2. `docker-compose -f docker-compose.ci.build.yml up --build`

    *Note:* If you have Visual Studio running during this step, it may hang while "Building ci-build".

3. `docker-compose -f docker-compose.yml -f docker-compose.build.yml build`
4. Launch the sln file and build.

And then to run it...

5. Run the docker-compose project.

To ensure updates to the secrets are seen within the containers, rebuild the docker-compose project.

# Building via Docker

1. Make sure the configuration files mentioned above are in place.
2. `docker-compose -f docker-compose.ci.build.yml up --build`
3. `docker-compose -f docker-compose.yml -f docker-compose.build.yml build`

And then to run it...

4. `docker-compose up`

    If you want to use the `git-server` repository, run it with files specified: `docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build`

5. `docker ps` to get the port mapping of the `gitautomation` image.

# To run the tests

1. Make sure the files mentioned above are in place.
2. Also make the following files:

    * /GitAutomation.Core.Tests/configuration.json - See the corresponding `configuration.sample.json` for an example.
	* /GitAutomation.Core.Tests/sql-credentials.txt - This should correspond to the configuration.json.

3. Start the docker-compose file from the working directory of the tests:

        docker-compose up --build

4. Run the tests via Visual Studio or run `dotnet test`. (Dockerfile for the tests to come.)

# Developing outside of docker and Visual Studio

1. Make sure the files mentioned above are in place, with one exception: Move the `configuration.json` into the GitAutomation project folder and rename it to `appsettings.local.json`. Also, merge in the following json:

        {
			"OS": {
				"additional-configuration": null,
				"addon-path": "..\\GitAutomation.AllAddons\\bin\\Debug\\netcoreapp2.0"
			}
		}

2. Install the dotnet command line tools. Check for the major/minor version used in the GitAutomation/GitAutomation.csproj file; currently we use 2.0.

        <PackageReference Include="Microsoft.AspNetCore" Version="Major.Minor.0" />

3. Install `yarn`.
4. From your GitAutomation project folder, run `yarn && yarn start` to start watching the typescript files for UI changes.
5. From your GitAutomation project folder, run `dotnet run -c Debug` to start the development server. (You could instead run the GitAutomation project in Visual Studio for this step.)

# GraphQL

There is a GraphiQL page set up at `/graphiql.html`. It uses CDN versions of the graphql-toolbox, so you will need public internet access to use it. (It's for debugging purposes only.)

# SQL Server

We don't use SQL Server by default due to the extra requirements. As a result, the SQL Server project may end up out of date intermittently before the release of version 1.0.

The Docker SQL Server images require at least 3.25GB of RAM. See https://github.com/Microsoft/mssql-docker/issues/114 for how to set the memory requirements.

To run the SQL docker container, you must accept the EULA. It is also linked to from [the dockerhub page](https://hub.docker.com/r/microsoft/mssql-server-linux/). This is a development-only license by default; you will need to get a full license from Microsoft or work outside the docker container if you want to use SQL Server.

## Local files not included

 * /sql-credentials.txt - the password to set up for the SA role for the dockerized SQL container. Not persisted in the docker image for security purposes.
 * /sql-eula.txt - whether you agree to Microsoft's EULA for the SQL docker container. Should contain a single 'Y' if you do.

## Steps to use

These steps won't add it to your debugging container via Visual Studio due to memory constraints.

1. Start the SQL Server docker-compose with the rest:

        docker-compose -f docker-compose.yml -f docker-compose.build.yml -f -f docker-compose.sqlserver.yml up --build

# Hosting outside of Docker

We highly recommend hosting inside of docker due to convenience of getting all the libraries set up. (Git, dotnet, etc.) However, all the paths for inside the linux container can be changed in the config files. The configuration.json can also be merged with the appsettings.json for an easier management experience.

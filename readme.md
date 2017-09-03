
# Notices
The Docker SQL images require at least 3.5GB of RAM. See https://github.com/Microsoft/mssql-docker/issues/114 for how to set the memory requirements.

To run the SQL docker container, you must accept the EULA. It is also linked to from [the dockerhub page](https://hub.docker.com/r/microsoft/mssql-server-linux/). The express edition will be available at GA, they say. 

# Before you get started
To clone, you should make sure you have the `autocrlf` in git set to `input`. For example:

    git clone https://github.com/mdekrey/GitAutomation.git --config core.autocrlf=input

Local files that are not included in the repository include:

 * /configuration.json - the various configuration settings to use, including the git repo and persistence database. See the `configuration.sample.json` for format.
 * /git-credentials.txt - the password to use for the git repository. Not persisted in the docker image for security purposes.
 * /sql-credentials.txt - the password to set up for the SA role for the dockerized SQL container. Not persisted in the docker image for security purposes.
 * /sql-eula.txt - whether you agree to Microsoft's EULA for the SQL docker container. Should contain a single 'Y' if you do.

# Building via Visual Studio 2017

1. Make sure the files mentioned above are in place.
2. Launch the sln file and build.

And then to run it...

3. Run the docker-compose project.

# Building via Docker

1. Make sure the files mentioned above are in place.
2. `docker-compose -f docker-compose.ci.build.yml up --build` 
4. `docker-compose build`

And then to run it...

5. `docker-compose up`
6. `docker ps` to get the port mapping of the `gitautomation` image.




The Docker SQL images require at least 3.5GB of RAM. See https://github.com/Microsoft/mssql-docker/issues/114 for how to set the memory requirements.

To run the SQL docker container, you must accept the EULA. It is also linked to from [the dockerhub page](https://hub.docker.com/r/microsoft/mssql-server-linux/). The express edition will be available at GA, they say. 

Local files that are not included in the repository include:

 * /GitAutomation/git.json - the git repository to use. See
 * /git-credentials.txt - the password to use. Not persisted in the docker image for security purposes.
  

# Resets a github repository to the same repeatable state each run
# Run the following command, substituting your own username:
#
#     docker build --build-arg username=mdekrey -t reset-github -f Dockerfile.reset .
#
# From then on, you may reset your repository with only:
#
#     docker run -ti --rm reset-github

FROM buildpack-deps:jessie-scm

RUN apt-get update && apt-get install -y \
  git

COPY demo-repo.sh /demo-repo.sh
RUN chmod +x /demo-repo.sh
WORKDIR /demo-repo
COPY travis-ci.txt /demo-repo/.travis.yml
RUN /demo-repo.sh


RUN git config credential.helper 'cache --timeout 900'

ARG username
RUN git remote add origin https://$username@github.com/$username/GitAutomation-Testing.git
CMD git fetch \
    && (eval "$(echo git push origin --delete `git branch -r | sed 's/  origin\///' | sed ':a;N;$!ba;s/\n/ /g'`)" || echo 1) \
    && git push origin -f original \
    && git push origin -f --all

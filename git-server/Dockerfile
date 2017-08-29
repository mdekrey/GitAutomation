FROM nmarus/docker-gitbox

COPY start.sh /start.sh
RUN chmod +x /start.sh

COPY init.sh /init.sh
RUN chmod +x /init.sh

COPY demo-repo.sh /demo-repo.sh
RUN chmod +x /demo-repo.sh
RUN ./demo-repo.sh

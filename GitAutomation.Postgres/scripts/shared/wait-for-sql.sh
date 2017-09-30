#!/bin/bash

retries=100
while ((retries > 0)); do
    psql \
        -U postgres \
        -c "SELECT 1" \
        > /dev/null \
    && break

    sleep 1
    ((retries --))
done
if ((retries == 0 )); then
    exit 1
fi

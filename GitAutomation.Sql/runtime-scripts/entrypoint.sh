#!/bin/bash

/opt/mssql/bin/sqlservr &

./wait-for-sql.sh


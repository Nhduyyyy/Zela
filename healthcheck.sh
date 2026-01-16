#!/bin/bash
/opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'Str0ng_Pa$$w0rd!' -Q 'SELECT 1' > /dev/null 2>&1


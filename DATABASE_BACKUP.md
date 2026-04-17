**Бэкапы хранятся в `/var/backups/db`**

Создать бэкап без сжатия
```bash
docker exec -t postgres pg_dumpall -c -U bg-admin-postgres > dump_$(date +%Y-%m-%d_%H_%M_%S).sql
```
Применить бэкап без сжатия к БД
```bash
cat /var/backups/db/dump_2025-02-18_06_14_31.sql | docker exec -i postgres psql -U bg-admin-postgres
```

Создать бэкап с жатием gzip
```bash
docker exec -t postgres pg_dumpall -c -U bg-admin-postgres | gzip > dump_$(date +%Y-%m-%d_%H_%M_%S).gz
```

Применить сжатый бэкап к БД
```bash
gunzip < dump_2025-02-18_06_14_31.gz | docker exec -i postgres psql -U bg-admin-postgres
```


#!/bin/bash

# Имя выходного SQL файла
OUTPUT_FILE="insert_shifts.sql"

# Список ID сотрудников
EMPLOYEE_IDS=(33 135 70 34 136 130 78 66 80)

# Диапазон дат (YYYY-MM-DD)
START_DATE="2025-06-16"
END_DATE="2025-06-19"

# Чистим файл перед началом
> "$OUTPUT_FILE"

# Преобразуем даты в timestamp для цикла
CURR_DATE=$(date -d "$START_DATE" +%s)
END_DATE_TS=$(date -d "$END_DATE" +%s)

# Цикл по датам
while [ $CURR_DATE -le $END_DATE_TS ]; do
    CURRENT_DAY=$(date -d "@$CURR_DATE" +"%Y-%m-%d")

    # Цикл по сотрудникам
    for EMP_ID in "${EMPLOYEE_IDS[@]}"; do
        STARTED="'${CURRENT_DAY} 09:00:00.695657+05'"
        ENDED="'${CURRENT_DAY} 18:00:00.695657+05'"
        LAST_PAUSE_START="'${CURRENT_DAY} 18:00:00.695657+05'"
        CREATED="'${CURRENT_DAY} 09:00:00.695657+05'"
        UPDATED="'${CURRENT_DAY} 18:00:00.695657+05'"

        LINE="INSERT INTO \"Shifts\" (\"EmployeeId\", \"Started\", \"Ended\", \"LastPauseStart\", \"PauseTime\", \"LegalStartTime\", \"LegalEndTime\", \"Created\", \"Updated\") VALUES ($EMP_ID, $STARTED, $ENDED, $LAST_PAUSE_START, '00:00:00.00000', '0001-01-01 08:59:33+04:02:33', '0001-01-01 17:59:33+04:02:33', $CREATED, $UPDATED);"

        echo "$LINE" >> "$OUTPUT_FILE"
    done

    # Переход на следующий день
    CURR_DATE=$((CURR_DATE + 86400))  # +1 день в секундах
done

echo "SQL файл создан: $OUTPUT_FILE"

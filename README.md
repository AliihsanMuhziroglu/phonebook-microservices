# Phonebook Microservices

## Servisler
- **ContactService**: Kişi CRUD ve iletişim bilgileri
- **ReportService**: Asenkron raporlama (Kafka üzerinden)

## Çalıştırma
```bash
docker compose up -d postgres zookeeper kafka kafka-ui
docker compose build contactservice reportservice
docker compose up -d contactservice reportservice

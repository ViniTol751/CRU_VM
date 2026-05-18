-- Script para corrigir UpdatedAt no PostgreSQL
-- Execute este script no seu banco PostgreSQL para forçar sincronização completa

-- Verifica quantos registros têm UpdatedAt antigo ou NULL
SELECT 
    'Projects' as tabela, 
    COUNT(*) as total,
    COUNT(*) FILTER (WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01') as precisa_atualizar
FROM "Projects"
UNION ALL
SELECT 'Users', COUNT(*), COUNT(*) FILTER (WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01') FROM "Users"
UNION ALL
SELECT 'Employees', COUNT(*), COUNT(*) FILTER (WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01') FROM "Employees"
UNION ALL
SELECT 'Equipments', COUNT(*), COUNT(*) FILTER (WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01') FROM "Equipments"
UNION ALL
SELECT 'Companions', COUNT(*), COUNT(*) FILTER (WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01') FROM "Companions"
UNION ALL
SELECT 'Reports', COUNT(*), COUNT(*) FILTER (WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01') FROM "Reports";

-- Atualiza todos os registros para NOW()
UPDATE "Projects" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Users" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Employees" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Equipments" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Companions" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Reports" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "WeatherDetails" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Activities" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Occurrences" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Materials" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Photos" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "Signatures" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "ProjectMembers" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "ReportEquipments" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';
UPDATE "ReportCompanions" SET "UpdatedAt" = NOW() WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < '2020-01-01';

-- Verifica novamente
SELECT 
    'Projects' as tabela, 
    COUNT(*) as total,
    MIN("UpdatedAt") as mais_antigo,
    MAX("UpdatedAt") as mais_recente
FROM "Projects"
UNION ALL
SELECT 'Users', COUNT(*), MIN("UpdatedAt"), MAX("UpdatedAt") FROM "Users"
UNION ALL
SELECT 'Employees', COUNT(*), MIN("UpdatedAt"), MAX("UpdatedAt") FROM "Employees"
UNION ALL
SELECT 'Equipments', COUNT(*), MIN("UpdatedAt"), MAX("UpdatedAt") FROM "Equipments"
UNION ALL
SELECT 'Companions', COUNT(*), MIN("UpdatedAt"), MAX("UpdatedAt") FROM "Companions"
UNION ALL
SELECT 'Reports', COUNT(*), MIN("UpdatedAt"), MAX("UpdatedAt") FROM "Reports";

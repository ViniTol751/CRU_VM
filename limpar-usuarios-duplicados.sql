-- ============================================================
-- Script: limpar-usuarios-duplicados.sql
-- Objetivo: Remover usuários no padrão antigo (NomeSobrenome /
--           ViníciusToledo) mantendo apenas os no novo padrão
--           (nome.sobrenome / vinicius.toledo)
-- ============================================================

-- Visualizar todos os usuários antes de deletar (opcional)
-- SELECT Id, Nome, Email, Ativo FROM Usuarios ORDER BY Email;

-- Remove usuários cujo login NÃO contém ponto E não é "admin"
-- (padrão antigo era CamelCase sem ponto)
DELETE FROM Usuarios
WHERE Email != 'admin'
  AND Email NOT LIKE '%.%';

-- Verificar resultado
SELECT Id, Nome, Email, Ativo FROM Usuarios ORDER BY Email;

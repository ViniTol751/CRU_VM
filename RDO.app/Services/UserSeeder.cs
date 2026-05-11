using Microsoft.EntityFrameworkCore;
using RDO.Data.Data;
using RDO.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace RDO.App.Services;

/// <summary>
/// Cria os logins dos colaboradores caso ainda não existam.
/// Login = PrimeiroNome + PrimeiroSobrenome (ignora preposições).
/// Senha padrão: F0cus@2026!
/// </summary>
public static class UserSeeder
{
    private static readonly string SenhaPadrao = PasswordHasher.Hash("F0cus@2026!");

    // (Nome completo, login gerado)
    private static readonly (string Nome, string Login)[] Colaboradores =
    {
        ("Aroldo Daiola Borges",                        "AroldoDaiola"),
        ("Bernalize do Rosário Vila Nova Marcolino",    "BernalizeRosário"),
        ("Bruno Fêlix Alcântara Souza",                 "BrunoFêlix"),
        ("Bruno Pires Ribeiro",                         "BrunoPires"),
        ("Edson Martins Garcia",                        "EdsonMartins"),
        ("Felipe Aparecido do Prado",                   "FelipeAparecido"),
        ("Felipe Franco de Paula",                      "FelipeFranco"),
        ("Felipe Gonçalves Duarte",                     "FelipeGonçalves"),
        ("Gabriel D'Alessandro Bravo",                  "GabrielDAlessandro"),
        ("Gabriel Favareli Furtado",                    "GabrielFavareli"),
        ("Gabriel Margato",                             "GabrielMargato"),
        ("Gustavo Henrique Aristeu de Queiroz",         "GustavoHenrique"),
        ("José Henrique David Alves de Oliveira",       "JoséHenrique"),
        ("Juliana Bertoni Justino",                     "JulianaBertoni"),
        ("Luis Felipe Xavier",                          "LuisFelipe"),
        ("Maicon Salomão Caetano",                      "MaiconSalomão"),
        ("Marcus Vinícius Ataíde",                      "MarcusVinícius"),
        ("Murilo Leandro Franco",                       "MuriloLeandro"),
        ("Murillo Vitto Reis Pereira",                  "MurilloVitto"),
        ("Natan Lemes Saura",                           "NatanLemes"),
        ("Rafael Feitosa da Silva",                     "RafaelFeitosa"),
        ("Roberto Tilhaqui Junior",                     "RobertoTilhaqui"),
        ("Simone Schuindt Martins",                     "SimoneSchuindt"),
        ("Thales Garcia Neubern",                       "ThalesGarcia"),
        ("Vinícius Toledo de Carvalho",                 "ViníciusToledo"),
        ("Victor Almeida Arantes Vilela",               "VictorAlmeida"),
        ("Wellington Henrique de Bessa Bortolozo",      "WellingtonHenrique"),
        ("Wesley Danilo de Araújo",                     "WesleyDanilo"),
        ("Wesley Gregório dos Santos",                  "WesleyGregório"),
    };

    public static void Seed(RdoDbContext db)
    {
        // Atualiza senha do admin legado (plain-text "admin") para hash
        var admin = db.Usuarios.FirstOrDefault(u => u.Email == "admin@focusengenharia.com.br");
        if (admin != null && admin.SenhaHash == "admin")
        {
            admin.SenhaHash = PasswordHasher.Hash("admin");
            db.SaveChanges();
        }

        bool alterado = false;
        foreach (var (nome, login) in Colaboradores)
        {
            var existente = db.Usuarios.FirstOrDefault(u => u.Email == login);
            if (existente != null)
            {
                // Atualiza nome caso esteja desatualizado (ex: criado manualmente com nome curto)
                if (existente.Nome != nome) { existente.Nome = nome; alterado = true; }
                continue;
            }

            db.Usuarios.Add(new Usuario
            {
                Nome      = nome,
                Email     = login,
                SenhaHash = SenhaPadrao,
                Perfil    = "Technician",
                Ativo     = true,
            });
            alterado = true;
        }

        // Corrige login alternativo "vinicius.toledo" → vincula ao nome completo correto
        var vinToledo = db.Usuarios.FirstOrDefault(u => u.Email == "vinicius.toledo");
        if (vinToledo != null && vinToledo.Nome != "Vinícius Toledo de Carvalho")
        {
            vinToledo.Nome = "Vinícius Toledo de Carvalho";
            alterado = true;
        }

        if (alterado) db.SaveChanges();
    }
}

namespace Simcag.IdentityService.Domain.Aggregates;
using System.Collections.Generic;

public enum ConformityTypeEnum
{
    Prefeitura, Licenca, AuditoriaContabil, SeguroPredial, CartaoSeguranca
}

public record ConformanceType
{
    public static class Defaults
    {
        public const string Prefeitura = "PREFEITURA";
        public const string Licenca = "LICENCA";
        public const string AuditoriaContabil = "AUDITORIA_CONTABIL";
        public const string SeguroPredial = "SEGURO_PREDIAL";
        public const string CartaoSeguranca = "CERTIFICADO_SEGURANCA";
    }

    public static (string Title, string Description, int DaysBeforeDue, string Code)[] Items =
    {
        ("Licença da Prefeitura", "Atualização de dados cadastrais e verificação de regularidade na prefeitura.", 0, Defaults.Prefeitura),
        ("Renovação de Licença de Funcionamento", "Verificação de expiração da licença de funcionamento municipal. Ação preventiva: renovar 30 dias antes do vencimento.", 30, Defaults.Licenca),
        ("Auditoria Contabil Anual", "Relatório anual auditado por empresa credenciada (CPC 001/05 e normas do CFC).", 45, Defaults.AuditoriaContabil),
        ("Atualização de Seguro Predial", "Verificação e atualização da apólice com valores atuais. Ação preventiva: renovar 60 dias antes do vencimento.", 60, Defaults.SeguroPredial),
        ("Certificado de Segurança Eletrônico", "Verificação de validade do certificado digital emissor/consignatário (ICP-Brasil e A1).", 4, Defaults.CartaoSeguranca)
    };
}

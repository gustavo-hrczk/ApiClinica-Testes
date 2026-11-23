using ApiClinica.Data;
using ApiClinica.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApiClinica.Tests.Helpers;

/// <summary>
/// Banco SQLite em memoria, isolado por teste. A conexao fica aberta enquanto o
/// objeto existir; ao descartar, o banco some. Nenhum teste toca o clinica.db.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conexao;

    public AppDbContext Context { get; }

    public TestDb()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public Paciente AddPaciente(string cpf = "529.982.247-25", string nome = "Ana Souza")
    {
        var paciente = new Paciente
        {
            Nome = nome,
            Email = "ana.souza@example.com",
            Telefone = "(47) 98888-8888",
            DataNasc = new DateOnly(1995, 3, 24),
            Cpf = cpf,
        };
        Context.Pacientes.Add(paciente);
        Context.SaveChanges();
        return paciente;
    }

    public Medico AddMedico(string nome = "Dr. Carlos Silva")
    {
        var medico = new Medico
        {
            Nome = nome,
            Email = "carlos.silva@example.com",
            Telefone = "(47) 97777-7777",
            CRM = "CRM-SC 12345",
        };
        Context.Medicos.Add(medico);
        Context.SaveChanges();
        return medico;
    }

    public Consulta AddConsulta(int pacienteId, int medicoId, DateTime dataHora)
    {
        var consulta = new Consulta
        {
            PacienteId = pacienteId,
            MedicoId = medicoId,
            DataHora = dataHora,
        };
        Context.Consultas.Add(consulta);
        Context.SaveChanges();
        return consulta;
    }

    public void Dispose()
    {
        Context.Dispose();
        _conexao.Dispose();
    }
}

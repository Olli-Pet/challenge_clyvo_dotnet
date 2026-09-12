using Microsoft.EntityFrameworkCore;
using Olli.Api.Domain.Entities;

namespace Olli.Api.Infrastructure.Persistence;

public class OlliDb : DbContext
{
    public OlliDb(DbContextOptions<OlliDb> options) : base(options)
    { }

    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<EventoSaude> EventosSaude => Set<EventoSaude>();
    public DbSet<AlertaPreventivo> AlertasPreventivos => Set<AlertaPreventivo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tutor>()
            .HasMany(tutor => tutor.Pets)
            .WithOne(pet => pet.Tutor)
            .HasForeignKey(pet => pet.IdTutor)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pet>()
            .Property(pet => pet.PesoKg)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Pet>()
            .HasMany(pet => pet.HistoricoSaude)
            .WithOne(eventoSaude => eventoSaude.Pet)
            .HasForeignKey(eventoSaude => eventoSaude.IdPet)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventoSaude>()
            .Property(eventoSaude => eventoSaude.PrecisaRetorno)
            .HasConversion<int>()
            .HasColumnType("NUMBER(1)");

        modelBuilder.Entity<Pet>()
            .HasMany(pet => pet.Alertas)
            .WithOne(alerta => alerta.Pet)
            .HasForeignKey(alerta => alerta.IdPet)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

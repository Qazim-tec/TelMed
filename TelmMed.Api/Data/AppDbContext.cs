// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using TelmMed.Api.Models;

namespace TelmMed.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<NextOfKin> NextOfKin { get; set; } = null!;
        public DbSet<DoctorLanguage> DoctorLanguages { get; set; } = null!;
        public DbSet<AlternativeInterviewSlot> AlternativeInterviewSlots { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Patient
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.PhoneNumber)
                .IsUnique();

            // Doctor
            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.Email)
                .IsUnique();

            // Emergency Contact
            modelBuilder.Entity<EmergencyContact>()
                .HasOne(ec => ec.Patient)
                .WithMany(p => p.EmergencyContacts)
                .HasForeignKey(ec => ec.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctor Relations
            modelBuilder.Entity<DoctorLanguage>()
                .HasOne(dl => dl.Doctor)
                .WithMany(d => d.Languages)
                .HasForeignKey(dl => dl.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NextOfKin>()
                .HasOne(nok => nok.Doctor)
                .WithOne(d => d.NextOfKin)
                .HasForeignKey<NextOfKin>(nok => nok.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AlternativeInterviewSlot>()
                .HasOne(ais => ais.Doctor)
                .WithMany(d => d.AlternativeSlots)
                .HasForeignKey(ais => ais.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
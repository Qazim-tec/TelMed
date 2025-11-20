using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TelmMed.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: false),
                    Sex = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    IsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    AlternatePhone = table.Column<string>(type: "text", nullable: true),
                    WorkEmail = table.Column<string>(type: "text", nullable: true),
                    ResidentialAddress = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Lga = table.Column<string>(type: "text", nullable: false),
                    PinHash = table.Column<string>(type: "text", nullable: false),
                    BiometricEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Specialty = table.Column<string>(type: "text", nullable: false),
                    CurrentWorkplace = table.Column<string>(type: "text", nullable: false),
                    ShortBio = table.Column<string>(type: "text", nullable: false),
                    MedicalLicensePath = table.Column<string>(type: "text", nullable: true),
                    MdcnCertificatePath = table.Column<string>(type: "text", nullable: true),
                    CvPath = table.Column<string>(type: "text", nullable: true),
                    NinSlipPath = table.Column<string>(type: "text", nullable: true),
                    PassportPath = table.Column<string>(type: "text", nullable: true),
                    AdditionalCertificatePath = table.Column<string>(type: "text", nullable: true),
                    AcceptTerms = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptPrivacy = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptDataUse = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptTelemedicine = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredInterview = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InterviewNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    IsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    SelectedCategories = table.Column<List<string>>(type: "text[]", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: false),
                    AlternativeLanguage = table.Column<string>(type: "text", nullable: true),
                    CommunicationTone = table.Column<string>(type: "text", nullable: false),
                    CommunicationChannels = table.Column<List<string>>(type: "text[]", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SexAtBirth = table.Column<string>(type: "text", nullable: false),
                    MedicalConditions = table.Column<List<string>>(type: "text[]", nullable: false),
                    BloodGroup = table.Column<string>(type: "text", nullable: true),
                    Genotype = table.Column<string>(type: "text", nullable: true),
                    Allergies = table.Column<string>(type: "text", nullable: true),
                    CurrentMedications = table.Column<string>(type: "text", nullable: true),
                    AgreesToTerms = table.Column<bool>(type: "boolean", nullable: false),
                    PinHash = table.Column<string>(type: "text", nullable: false),
                    BiometricEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlternativeInterviewSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlternativeInterviewSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlternativeInterviewSlots_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Proficiency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorLanguages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorLanguages_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NextOfKin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Relationship = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NextOfKin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NextOfKin_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Relationship = table.Column<string>(type: "text", nullable: false),
                    AllowLocationTracking = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlternativeInterviewSlots_DoctorId",
                table: "AlternativeInterviewSlots",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorLanguages_DoctorId",
                table: "DoctorLanguages",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_PatientId",
                table: "EmergencyContacts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_NextOfKin_DoctorId",
                table: "NextOfKin",
                column: "DoctorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhoneNumber",
                table: "Patients",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlternativeInterviewSlots");

            migrationBuilder.DropTable(
                name: "DoctorLanguages");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "NextOfKin");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Doctors");
        }
    }
}

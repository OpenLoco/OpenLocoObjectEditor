﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenLoco.Definitions.Database;

#nullable disable

namespace Definitions.Migrations
{
    [DbContext(typeof(LocoDb))]
    partial class LocoDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblAuthor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Authors");
                });

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblLicence", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Licences");
                });

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblLocoObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AuthorId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Availability")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsVanilla")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LastEditDate")
                        .HasColumnType("TEXT");

                    b.Property<int?>("LicenceId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte>("ObjectType")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("OriginalChecksum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OriginalName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PathOnDisk")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("UploadDate")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("TEXT")
                        .HasDefaultValueSql("datetime(datetime('now', 'localtime'), 'utc')");

                    b.Property<byte?>("VehicleType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("LicenceId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("PathOnDisk")
                        .IsUnique();

                    b.HasIndex("OriginalName", "OriginalChecksum")
                        .IsUnique()
                        .IsDescending(true, false);

                    b.ToTable("Objects");
                });

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblModpack", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AuthorId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Modpacks");
                });

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("TblLocoObjectTblModpack", b =>
                {
                    b.Property<int>("ModpacksId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ObjectsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ModpacksId", "ObjectsId");

                    b.HasIndex("ObjectsId");

                    b.ToTable("TblLocoObjectTblModpack");
                });

            modelBuilder.Entity("TblLocoObjectTblTag", b =>
                {
                    b.Property<int>("ObjectsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ObjectsId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("TblLocoObjectTblTag");
                });

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblLocoObject", b =>
                {
                    b.HasOne("OpenLoco.Definitions.Database.TblAuthor", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId");

                    b.HasOne("OpenLoco.Definitions.Database.TblLicence", "Licence")
                        .WithMany()
                        .HasForeignKey("LicenceId");

                    b.Navigation("Author");

                    b.Navigation("Licence");
                });

            modelBuilder.Entity("OpenLoco.Definitions.Database.TblModpack", b =>
                {
                    b.HasOne("OpenLoco.Definitions.Database.TblAuthor", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId");

                    b.Navigation("Author");
                });

            modelBuilder.Entity("TblLocoObjectTblModpack", b =>
                {
                    b.HasOne("OpenLoco.Definitions.Database.TblModpack", null)
                        .WithMany()
                        .HasForeignKey("ModpacksId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenLoco.Definitions.Database.TblLocoObject", null)
                        .WithMany()
                        .HasForeignKey("ObjectsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TblLocoObjectTblTag", b =>
                {
                    b.HasOne("OpenLoco.Definitions.Database.TblLocoObject", null)
                        .WithMany()
                        .HasForeignKey("ObjectsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenLoco.Definitions.Database.TblTag", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

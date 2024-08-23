﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenLoco.Db.Schema;

#nullable disable

namespace DatabaseSchema.Migrations
{
    [DbContext(typeof(LocoDb))]
    partial class LocoDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("OpenLoco.Db.Schema.TblAuthor", b =>
                {
                    b.Property<int>("TblAuthorId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("TblAuthorId");

                    b.ToTable("Authors");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblLicence", b =>
                {
                    b.Property<int>("TblLicenceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("TblLicenceId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Licences");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblLocoObject", b =>
                {
                    b.Property<int>("TblLocoObjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AuthorTblAuthorId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Availability")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastEditDate")
                        .HasColumnType("TEXT");

                    b.Property<int?>("LicenceTblLicenceId")
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

                    b.Property<byte>("SourceGame")
                        .HasColumnType("INTEGER");

                    b.Property<byte?>("VehicleType")
                        .HasColumnType("INTEGER");

                    b.HasKey("TblLocoObjectId");

                    b.HasIndex("AuthorTblAuthorId");

                    b.HasIndex("LicenceTblLicenceId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("PathOnDisk")
                        .IsUnique();

                    b.HasIndex("OriginalName", "OriginalChecksum")
                        .IsUnique()
                        .IsDescending(true, false);

                    b.ToTable("Objects");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblModpack", b =>
                {
                    b.Property<int>("TblModpackId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("TblModpackId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Modpacks");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblTag", b =>
                {
                    b.Property<int>("TblTagId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("TblTagId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("TblLocoObjectTblModpack", b =>
                {
                    b.Property<int>("ModpacksTblModpackId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ObjectsTblLocoObjectId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ModpacksTblModpackId", "ObjectsTblLocoObjectId");

                    b.HasIndex("ObjectsTblLocoObjectId");

                    b.ToTable("TblLocoObjectTblModpack");
                });

            modelBuilder.Entity("TblLocoObjectTblTag", b =>
                {
                    b.Property<int>("ObjectsTblLocoObjectId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagsTblTagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ObjectsTblLocoObjectId", "TagsTblTagId");

                    b.HasIndex("TagsTblTagId");

                    b.ToTable("TblLocoObjectTblTag");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblLocoObject", b =>
                {
                    b.HasOne("OpenLoco.Db.Schema.TblAuthor", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorTblAuthorId");

                    b.HasOne("OpenLoco.Db.Schema.TblLicence", "Licence")
                        .WithMany()
                        .HasForeignKey("LicenceTblLicenceId");

                    b.Navigation("Author");

                    b.Navigation("Licence");
                });

            modelBuilder.Entity("TblLocoObjectTblModpack", b =>
                {
                    b.HasOne("OpenLoco.Db.Schema.TblModpack", null)
                        .WithMany()
                        .HasForeignKey("ModpacksTblModpackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenLoco.Db.Schema.TblLocoObject", null)
                        .WithMany()
                        .HasForeignKey("ObjectsTblLocoObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TblLocoObjectTblTag", b =>
                {
                    b.HasOne("OpenLoco.Db.Schema.TblLocoObject", null)
                        .WithMany()
                        .HasForeignKey("ObjectsTblLocoObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenLoco.Db.Schema.TblTag", null)
                        .WithMany()
                        .HasForeignKey("TagsTblTagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

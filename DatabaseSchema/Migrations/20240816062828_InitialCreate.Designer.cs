﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenLoco.Db.Schema;

#nullable disable

namespace DatabaseSchema.Migrations
{
    [DbContext(typeof(LocoDb))]
    [Migration("20240816062828_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

            modelBuilder.Entity("OpenLoco.Db.Schema.TblLocoObject", b =>
                {
                    b.Property<int>("TblLocoObjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AuthorTblAuthorId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastEditDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte>("ObjectType")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("OriginalBytes")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<uint>("OriginalChecksum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OriginalName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte>("SourceGame")
                        .HasColumnType("INTEGER");

                    b.Property<byte?>("VehicleType")
                        .HasColumnType("INTEGER");

                    b.HasKey("TblLocoObjectId");

                    b.HasIndex("AuthorTblAuthorId");

                    b.ToTable("Objects");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblObjectTagLink", b =>
                {
                    b.Property<int>("TblLocoObjectId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TblTagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("TblLocoObjectId", "TblTagId");

                    b.HasIndex("TblTagId");

                    b.ToTable("ObjectTagLinks");
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

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblLocoObject", b =>
                {
                    b.HasOne("OpenLoco.Db.Schema.TblAuthor", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorTblAuthorId");

                    b.Navigation("Author");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblObjectTagLink", b =>
                {
                    b.HasOne("OpenLoco.Db.Schema.TblLocoObject", "Object")
                        .WithMany("TagLinks")
                        .HasForeignKey("TblLocoObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenLoco.Db.Schema.TblTag", "Tag")
                        .WithMany("TagLinks")
                        .HasForeignKey("TblTagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Object");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblLocoObject", b =>
                {
                    b.Navigation("TagLinks");
                });

            modelBuilder.Entity("OpenLoco.Db.Schema.TblTag", b =>
                {
                    b.Navigation("TagLinks");
                });
#pragma warning restore 612, 618
        }
    }
}

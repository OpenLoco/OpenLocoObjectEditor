﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Database.Migrations
{
    [DbContext(typeof(LocoDb))]
    partial class LocoDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("Schema.TblAuthor", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.ToTable("Authors");
                });

            modelBuilder.Entity("Schema.TblLocoObject", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorName")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastEditDate")
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

                    b.HasKey("Name");

                    b.HasIndex("AuthorName");

                    b.ToTable("Objects");
                });

            modelBuilder.Entity("Schema.TblTag", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Schema.TblLocoObject", b =>
                {
                    b.HasOne("Schema.TblAuthor", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorName");

                    b.Navigation("Author");
                });
#pragma warning restore 612, 618
        }
    }
}

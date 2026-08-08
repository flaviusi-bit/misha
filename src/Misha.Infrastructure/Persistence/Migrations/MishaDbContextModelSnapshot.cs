            b.ToTable("passport_documents");
        });

        modelBuilder.Entity("Misha.Domain.Watchlists.WatchlistCheck", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<string>("Provider").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<int>("Decision").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("MatchReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("ErrorMessage").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CheckedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.ToTable("watchlist_checks");
        });

        modelBuilder.Entity("Misha.Domain.Payments.Payment", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid>("ApplicationId").HasColumnType("uuid");
            b.Property<long>("AmountMinor").HasColumnType("bigint");
            b.Property<string>("Currency").IsRequired().HasMaxLength(3).HasColumnType("character varying(3)");
            b.Property<int>("Status").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Provider").HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("ProviderReference").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("ActionUrl").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<string>("FailureReason").HasMaxLength(1000).HasColumnType("character varying(1000)");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTimeOffset?>("CompletedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("ApplicationId", "CreatedAtUtc");
            b.HasIndex("ProviderReference");
            b.ToTable("payments");
        });
#pragma warning restore 612, 618
    }
}
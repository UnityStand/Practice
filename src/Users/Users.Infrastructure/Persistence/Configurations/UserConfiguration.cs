using Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Users.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Login).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Login).IsUnique();

        builder.Property(u => u.HashedPassword).IsRequired();
        builder.Property(u => u.Role).IsRequired().HasConversion<string>();
    }
}
using Library.Core;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(author => author.Id);
            entity.Property(author => author.Name).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).IsRequired();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(book => book.Id);
            entity.Property(book => book.Title).IsRequired();
            entity.HasOne(book => book.Category)
                .WithMany()
                .HasForeignKey("CategoryId")
                .IsRequired();
            entity.HasMany(book => book.Authors)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "BookAuthors",
                    right => right.HasOne<Author>().WithMany().HasForeignKey("AuthorId"),
                    left => left.HasOne<Book>().WithMany().HasForeignKey("BookId"),
                    join => join.HasKey("BookId", "AuthorId"));
            entity.Navigation(book => book.Authors).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.HasKey(copy => copy.Id);
            entity.Property(copy => copy.InventoryNumber).IsRequired().UseCollation("BINARY");
            entity.Property(copy => copy.Status).IsRequired();
            entity.HasIndex(copy => copy.InventoryNumber).IsUnique();
            entity.HasOne(copy => copy.Book)
                .WithMany()
                .HasForeignKey("BookId")
                .IsRequired();
        });
    }
}

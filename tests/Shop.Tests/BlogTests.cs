using FluentAssertions;
using Shop.Domain.Entities;
using Xunit;

namespace Shop.Tests.Domain;

public class BlogDomainTests
{
    [Fact]
    public void BlogPost_CreateAndCalculateReadingTime_ShouldEnforceInvariants()
    {
        var authorId = Guid.NewGuid();
        var content = string.Join(" ", Enumerable.Repeat("word", 600)); // ~600 words -> ~3 mins reading time

        var post = BlogPost.Create(
            "Getting Started with .NET 10 & Aspire",
            "getting-started-dotnet-10-aspire",
            "A comprehensive overview of cloud-native development in .NET 10.",
            content,
            authorId,
            "Admin User",
            coverImageUrl: "cover.jpg",
            tags: ["dotnet", "aspire"],
            publishImmediately: true
        );

        post.Title.Should().Be("Getting Started with .NET 10 & Aspire");
        post.Slug.Should().Be("getting-started-dotnet-10-aspire");
        post.IsPublished.Should().BeTrue();
        post.PublishedAt.Should().NotBeNull();
        post.ReadingTimeMinutes.Should().Be(3);
        post.Tags.Should().Contain("dotnet");
        post.ViewCount.Should().Be(0);

        post.IncrementViewCount();
        post.ViewCount.Should().Be(1);
    }

    [Fact]
    public void BlogPost_Comments_ShouldAddAndApproveCorrectly()
    {
        var authorId = Guid.NewGuid();
        var post = BlogPost.Create(
            "Test Post",
            "test-post",
            "Summary",
            "Content",
            authorId,
            "Admin User"
        );

        var comment = post.AddComment(Guid.NewGuid(), "Ali", "ali@example.com", "Great post!", autoApprove: false);

        post.Comments.Should().HaveCount(1);
        comment.IsApproved.Should().BeFalse();

        post.ApproveComment(comment.Id);
        comment.IsApproved.Should().BeTrue();
    }

    [Fact]
    public void BlogPost_PublishToggle_ShouldUpdateStatusAndDates()
    {
        var authorId = Guid.NewGuid();
        var post = BlogPost.Create(
            "Draft Post",
            "draft-post",
            "Summary",
            "Content",
            authorId,
            "Admin User",
            publishImmediately: false
        );

        post.IsPublished.Should().BeFalse();
        post.PublishedAt.Should().BeNull();

        post.Publish();
        post.IsPublished.Should().BeTrue();
        post.PublishedAt.Should().NotBeNull();

        post.Unpublish();
        post.IsPublished.Should().BeFalse();
    }
}

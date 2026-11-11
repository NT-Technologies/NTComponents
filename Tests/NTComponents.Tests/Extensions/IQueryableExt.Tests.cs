using NTComponents.Extensions;
using NTComponents.Virtualization;

namespace NTComponents.Tests.Extensions;

/// <summary>
///     Unit tests for <see cref="IQueryableExt" />.
/// </summary>
public class IQueryableExt_Tests {

    private readonly List<TestItem> _testItems = [
        new() { Id = 1, Name = "Alice", Age = 30, Score = 85.5m, CreatedDate = new DateTime(2023, 1, 15) },
        new() { Id = 2, Name = "Bob", Age = 25, Score = 92.0m, CreatedDate = new DateTime(2023, 2, 20) },
        new() { Id = 3, Name = "Charlie", Age = 35, Score = 78.5m, CreatedDate = new DateTime(2023, 3, 10) },
        new() { Id = 4, Name = "David", Age = 28, Score = 88.0m, CreatedDate = new DateTime(2023, 4, 5) },
        new() { Id = 5, Name = "Eve", Age = 32, Score = 95.5m, CreatedDate = new DateTime(2023, 5, 25) },
        new() { Id = 6, Name = "Frank", Age = 27, Score = 81.0m, CreatedDate = new DateTime(2023, 6, 30) },
        new() { Id = 7, Name = "Grace", Age = 29, Score = 89.5m, CreatedDate = new DateTime(2023, 7, 12) },
        new() { Id = 8, Name = "Henry", Age = 33, Score = 76.0m, CreatedDate = new DateTime(2023, 8, 18) },
        new() { Id = 9, Name = "Ivy", Age = 26, Score = 93.5m, CreatedDate = new DateTime(2023, 9, 22) },
        new() { Id = 10, Name = "Jack", Age = 31, Score = 87.0m, CreatedDate = new DateTime(2023, 10, 8) }
    ];

    [Fact]
    public void Apply_WithNTItemsProviderRequest_AppliesSortingBeforePaging() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 1,
            Count = 2,
            Sorts = [$"{nameof(TestItem.Age)},Ascending"]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Select(item => item.Name).Should().Equal("Ivy", "Frank");
    }

    [Fact]
    public void Apply_WithAllParameters_AppliesSortingPagingAndFiltering() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 2,
            Count = 3,
            Sorts = [
                $"{nameof(TestItem.Age)},Ascending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Frank"); // Age 27
        result[1].Name.Should().Be("David"); // Age 28
        result[2].Name.Should().Be("Grace"); // Age 29
    }

    [Fact]
    public void Apply_WithCount_LimitsResultsToSpecifiedCount() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 5
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(5);
        result[0].Id.Should().Be(1);
        result[4].Id.Should().Be(5);
    }

    [Fact]
    public void Apply_WithCountGreaterThanAvailable_ReturnsAllAvailableItems() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 8,
            Count = 10
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(2); // Only 2 items available from index 8
        result[0].Id.Should().Be(9);
        result[1].Id.Should().Be(10);
    }

    [Fact]
    public void Apply_WithDescendingSort_SortsCorrectly() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 5,
            Sorts = [
                $"{nameof(TestItem.Score)},Descending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(5);
        result[0].Name.Should().Be("Eve"); // Score 95.5
        result[1].Name.Should().Be("Ivy"); // Score 93.5
        result[2].Name.Should().Be("Bob"); // Score 92.0
        result[3].Name.Should().Be("Grace"); // Score 89.5
        result[4].Name.Should().Be("David"); // Score 88.0
    }

    [Fact]
    public void Apply_WithEmptyQuery_ReturnsEmptyResult() {
        // Arrange
        var query = Enumerable.Empty<TestItem>().AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 10,
            Sorts = [
                $"{nameof(TestItem.Name)},Ascending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithEmptySortOnProperties_SkipsSorting() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 3,
            Sorts = []
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(3);
        // Should maintain original order
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);
    }

    [Fact]
    public void Apply_WithMultipleSorts_AppliesAllSortsInOrder() {
        // Arrange Add items with duplicate ages for secondary sort testing
        var items = new List<TestItem>(_testItems)
        {
            new() { Id = 11, Name = "Kate", Age = 30, Score = 90.0m, CreatedDate = new DateTime(2023, 11, 1) },
            new() { Id = 12, Name = "Leo", Age = 25, Score = 85.0m, CreatedDate = new DateTime(2023, 12, 1) }
        };
        var query = items.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 5,
            Sorts = [
                $"{nameof(TestItem.Age)},Ascending",
                $"{nameof(TestItem.Score)},Descending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(5);
        // First by Age ascending, then by Score descending within same age
        result[0].Age.Should().Be(25); // Bob (92.0) or Leo (85.0)
        result[0].Name.Should().Be("Bob"); // Bob has higher score
        result[1].Age.Should().Be(25);
        result[1].Name.Should().Be("Leo");
        result[2].Age.Should().Be(26); // Ivy
        result[3].Age.Should().Be(27); // Frank
        result[4].Age.Should().Be(28); // David
    }

    [Fact]
    public void Apply_WithNestedProperty_SortsCorrectly() {
        // Arrange
        var nestedItems = new List<TestItemWithNested>
        {
            new() { Id = 1, Name = "First", Details = new NestedDetails { Category = "B", Value = 10 } },
            new() { Id = 2, Name = "Second", Details = new NestedDetails { Category = "A", Value = 20 } },
            new() { Id = 3, Name = "Third", Details = new NestedDetails { Category = "C", Value = 15 } }
        };
        var query = nestedItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Sorts = [
                $"{"Details.Category"},Ascending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Details.Category.Should().Be("A");
        result[1].Details.Category.Should().Be("B");
        result[2].Details.Category.Should().Be("C");
    }

    [Fact]
    public void Apply_WithNoCount_ReturnsAllItemsFromStartIndex() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 7,
            Count = null
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(3); // Items 8, 9, 10
        result[0].Id.Should().Be(8);
        result[1].Id.Should().Be(9);
        result[2].Id.Should().Be(10);
    }

    [Fact]
    public void Apply_WithNullSortOnProperties_SkipsSorting() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 3,
            Sorts = null!
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(3);
        // Should maintain original order
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);
    }

    [Fact]
    public void Apply_WithOnlyCount_ReturnsFirstNItems() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 3
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);
    }

    [Fact]
    public void Apply_WithOnlySorting_AppliesSortToAllItems() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Sorts = [
                $"{nameof(TestItem.Name)},Ascending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(10);
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Charlie");
        result[9].Name.Should().Be("Jack");
    }

    [Fact]
    public void Apply_WithOnlyStartIndex_SkipsSpecifiedItems() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 5,
            Count = null
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(5); // Items 6-10
        result[0].Id.Should().Be(6);
        result[4].Id.Should().Be(10);
    }

    [Fact]
    public void Apply_WithSortingAndPaging_AppliesSortingBeforePaging() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 1,
            Count = 2,
            Sorts = [
                $"{nameof(TestItem.Age)},Ascending"
            ]
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert Sorted by age: Bob(25), Ivy(26), Frank(27), David(28), Grace(29), Alice(30), ... Skip 1, Take 2 should give Ivy(26) and Frank(27)
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Ivy"); // Age 26
        result[1].Name.Should().Be("Frank"); // Age 27
    }

    [Fact]
    public void Apply_WithStartIndexBeyondAvailable_ReturnsEmptyResult() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 100,
            Count = 10
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithZeroCount_ReturnsEmptyResult() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 0
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithoutAnyParameters_ReturnsOriginalQuery() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = null,
            Sorts = []
        };

        // Act
        var result = query.Apply(request).ToList();

        // Assert
        result.Should().HaveCount(10);
        result.Should().BeEquivalentTo(_testItems);
    }

    [Fact]
    public void Apply_PreservesQueryableType_AllowingFurtherChaining() {
        // Arrange
        var query = _testItems.AsQueryable();
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Count = 5
        };

        // Act
        var result = query.Apply(request);

        // Assert
        result.Should().BeAssignableTo<IQueryable<TestItem>>();
        // Further operations can be chained
        var furtherFiltered = result.Where(x => x.Age > 28).ToList();
        furtherFiltered.Should().HaveCount(3); // Alice(30), Eve(32), and Charlie(35)
    }

    /// <summary>
    ///     Test model for IQueryableExt tests.
    /// </summary>
    private class TestItem {
        public int Age { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Score { get; set; }
    }

    /// <summary>
    ///     Test model with nested properties for IQueryableExt tests.
    /// </summary>
    private class TestItemWithNested {
        public NestedDetails Details { get; set; } = new();
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class NestedDetails {
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

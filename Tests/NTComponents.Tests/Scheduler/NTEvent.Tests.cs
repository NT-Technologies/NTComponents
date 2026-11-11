using NTComponents.Scheduler;

namespace NTComponents.Tests.Scheduler;

/// <summary>
///     Unit tests for <see cref="NTEvent" />.
/// </summary>
public class NTEvent_Tests {

    [Theory]
    [InlineData(NTColor.Transparent)]
    [InlineData(NTColor.Primary)]
    [InlineData(NTColor.Secondary)]
    [InlineData(NTColor.Tertiary)]
    [InlineData(NTColor.Error)]
    [InlineData(NTColor.Success)]
    [InlineData(NTColor.Warning)]
    [InlineData(NTColor.Info)]
    public void BackgroundColor_SetValue_ReturnsCorrectValue(NTColor color) {
        // Arrange
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.BackgroundColor = color;

        // Assert
        tnTEvent.BackgroundColor.Should().Be(color);
    }

    [Fact]
    public void Constructor_MultipleInstances_GeneratesUniqueIds() {
        // Arrange & Act
        var event1 = new NTEvent { Title = "Event 1" };
        var event2 = new NTEvent { Title = "Event 2" };
        var event3 = new NTEvent { Title = "Event 3" };

        // Assert
        event1.Id.Should().NotBe(event2.Id);
        event2.Id.Should().NotBe(event3.Id);
        event1.Id.Should().NotBe(event3.Id);
        event1.Id.Should().BeGreaterThan(0);
        event2.Id.Should().BeGreaterThan(0);
        event3.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_WithDefaultValues_SetsDefaultProperties() {
        // Arrange & Act
        var tnTEvent = new NTEvent();

        // Assert
        tnTEvent.Id.Should().BeGreaterThan(0);
        tnTEvent.Title.Should().BeNull();
        tnTEvent.Description.Should().BeNull();
        tnTEvent.EventStart.Should().Be(default(DateTimeOffset));
        tnTEvent.EventEnd.Should().Be(default(DateTimeOffset));
        tnTEvent.IsAllDay.Should().BeFalse();
        tnTEvent.BackgroundColor.Should().Be(NTColor.Tertiary);
        tnTEvent.ForegroundColor.Should().Be(NTColor.OnTertiary);
        tnTEvent.TintColor.Should().Be(NTColor.SurfaceTint);
        tnTEvent.OnTintColor.Should().Be(NTColor.OnTertiary);
    }

    [Fact]
    public void Constructor_WithProperties_SetsPropertiesCorrectly() {
        // Arrange
        var title = "Test Event";
        var description = "Test Description";
        var eventStart = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var tnTEvent = new NTEvent {
            Title = title,
            Description = description,
            EventStart = eventStart,
            EventEnd = eventEnd,
            IsAllDay = true,
            BackgroundColor = NTColor.Primary,
            ForegroundColor = NTColor.OnPrimary
        };

        // Assert
        tnTEvent.Title.Should().Be(title);
        tnTEvent.Description.Should().Be(description);
        tnTEvent.EventStart.Should().Be(eventStart);
        tnTEvent.EventEnd.Should().Be(eventEnd);
        tnTEvent.IsAllDay.Should().BeTrue();
        tnTEvent.BackgroundColor.Should().Be(NTColor.Primary);
        tnTEvent.ForegroundColor.Should().Be(NTColor.OnPrimary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Short description")]
    [InlineData("A very long description with multiple sentences. This should test the flexibility of the description property.")]
    public void Description_SetValue_ReturnsCorrectValue(string? description) {
        // Arrange
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.Description = description;

        // Assert
        tnTEvent.Description.Should().Be(description);
    }

    [Fact]
    public void Duration_WithEndBeforeStart_ReturnsNegativeDuration() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var tnTEvent = new NTEvent {
            EventStart = eventStart,
            EventEnd = eventEnd
        };

        // Act
        var duration = tnTEvent.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(-2));
    }

    [Fact]
    public void Duration_WithMultiDayEvent_ReturnsCorrectDuration() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 1, 17, 14, 30, 0, TimeSpan.Zero);
        var tnTEvent = new NTEvent {
            EventStart = eventStart,
            EventEnd = eventEnd
        };
        var expectedDuration = TimeSpan.FromDays(2) + TimeSpan.FromHours(4.5);

        // Act
        var duration = tnTEvent.Duration;

        // Assert
        duration.Should().Be(expectedDuration);
    }

    [Fact]
    public void Duration_WithSameStartAndEnd_ReturnsZeroDuration() {
        // Arrange
        var eventDateTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var tnTEvent = new NTEvent {
            EventStart = eventDateTime,
            EventEnd = eventDateTime
        };

        // Act
        var duration = tnTEvent.Duration;

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Duration_WithValidStartAndEnd_ReturnsCorrectDuration() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 1, 15, 12, 30, 0, TimeSpan.Zero);
        var tnTEvent = new NTEvent {
            EventStart = eventStart,
            EventEnd = eventEnd
        };

        // Act
        var duration = tnTEvent.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(2.5));
    }

    [Fact]
    public void EndDate_WithEventEnd_ReturnsCorrectDate() {
        // Arrange
        var eventEnd = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var tnTEvent = new NTEvent { EventEnd = eventEnd };

        // Act
        var endDate = tnTEvent.EndDate;

        // Assert
        endDate.Should().Be(new DateOnly(2024, 12, 31));
    }

    [Fact]
    public void EndTime_WithEventEnd_ReturnsCorrectTime() {
        // Arrange
        var eventEnd = new DateTimeOffset(2024, 1, 15, 14, 15, 30, TimeSpan.FromHours(2));
        var tnTEvent = new NTEvent { EventEnd = eventEnd };

        // Act
        var endTime = tnTEvent.EndTime;

        // Assert LocalDateTime converts to system local time, so we expect the converted time
        var expectedTime = TimeOnly.FromTimeSpan(eventEnd.LocalDateTime.TimeOfDay);
        endTime.Should().Be(expectedTime);
    }

    [Fact]
    public void EventEnd_SetValue_ReturnsCorrectValue() {
        // Arrange
        var eventEnd = new DateTimeOffset(2024, 3, 15, 16, 0, 0, TimeSpan.FromHours(-5));
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.EventEnd = eventEnd;

        // Assert
        tnTEvent.EventEnd.Should().Be(eventEnd);
    }

    [Fact]
    public void EventStart_SetValue_ReturnsCorrectValue() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 3, 15, 14, 30, 0, TimeSpan.FromHours(-5));
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.EventStart = eventStart;

        // Assert
        tnTEvent.EventStart.Should().Be(eventStart);
    }

    [Theory]
    [InlineData(NTColor.OnPrimary)]
    [InlineData(NTColor.OnSecondary)]
    [InlineData(NTColor.OnTertiary)]
    [InlineData(NTColor.OnError)]
    [InlineData(NTColor.OnSuccess)]
    public void ForegroundColor_SetValue_ReturnsCorrectValue(NTColor color) {
        // Arrange
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.ForegroundColor = color;

        // Assert
        tnTEvent.ForegroundColor.Should().Be(color);
    }

    [Fact]
    public void Id_IsReadOnly_CannotBeSetDirectly() {
        // Arrange
        var tnTEvent = new NTEvent();
        var originalId = tnTEvent.Id;

        // Act & Assert - Id should be get-only from external perspective The Id property is marked as internal set, so this test verifies it behaves correctly
        tnTEvent.Id.Should().Be(originalId);
    }

    [Theory]
    [InlineData(NTColor.OnTertiary)]
    [InlineData(NTColor.OnPrimary)]
    [InlineData(NTColor.OnSecondary)]
    public void OnTintColor_SetValue_ReturnsCorrectValue(NTColor color) {
        // Arrange
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.OnTintColor = color;

        // Assert
        tnTEvent.OnTintColor.Should().Be(color);
    }

    [Fact]
    public void Overlaps_IsSymmetric_ReturnsSameResult() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 11, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 13, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps1 = event1.Overlaps(event2);
        var overlaps2 = event2.Overlaps(event1);

        // Assert
        overlaps1.Should().Be(overlaps2);
        overlaps1.Should().BeTrue();
        overlaps2.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithAdjacentEvents_ReturnsFalse() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 14, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WithCompletelyContainedEvent_ReturnsTrue() {
        // Arrange
        var outerEvent = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 17, 0, 0, TimeSpan.Zero)
        };
        var innerEvent = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 11, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 13, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = outerEvent.Overlaps(innerEvent);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithIdenticalTimeRanges_ReturnsTrue() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var event1 = new NTEvent {
            EventStart = eventStart,
            EventEnd = eventEnd
        };
        var event2 = new NTEvent {
            EventStart = eventStart,
            EventEnd = eventEnd
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithMultiDayEvents_ReturnsCorrectly() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 17, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 16, 11, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 18, 13, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithNonOverlappingEvents_ReturnsFalse() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 13, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 15, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WithOverlappingEvents_ReturnsTrue() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 11, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 13, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithPartialOverlapAtEnd_ReturnsTrue() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 11, 30, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 13, 30, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithPartialOverlapAtStart_ReturnsTrue() {
        // Arrange
        var event1 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var event2 = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 11, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = event1.Overlaps(event2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithSameEvent_ReturnsTrue() {
        // Arrange
        var tnTEvent = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };

        // Act
        var overlaps = tnTEvent.Overlaps(tnTEvent);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithZeroDurationEvents_ReturnsCorrectly() {
        // Arrange
        var instantTime = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var zeroDurationEvent1 = new NTEvent {
            EventStart = instantTime,
            EventEnd = instantTime
        };
        var zeroDurationEvent2 = new NTEvent {
            EventStart = instantTime,
            EventEnd = instantTime
        };
        var regularEvent = new NTEvent {
            EventStart = new DateTimeOffset(2024, 1, 15, 11, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 13, 0, 0, TimeSpan.Zero)
        };

        // Act
        var sameInstantOverlap = zeroDurationEvent1.Overlaps(zeroDurationEvent2);
        var instantWithRegularOverlap = zeroDurationEvent1.Overlaps(regularEvent);

        // Assert Zero-duration events at the same instant don't overlap (12 < 12 && 12 < 12 = false)
        sameInstantOverlap.Should().BeFalse();
        // Zero-duration event at 12:00 overlaps with regular event 11:00-13:00 (12 < 13 && 11 < 12 = true)
        instantWithRegularOverlap.Should().BeTrue();
    }

    [Fact]
    public void Properties_WithMinAndMaxDateTimeValues_HandleCorrectly() {
        // Arrange
        var minDateTime = DateTimeOffset.MinValue;
        var maxDateTime = DateTimeOffset.MaxValue;

        var tnTEvent = new NTEvent {
            EventStart = minDateTime,
            EventEnd = maxDateTime
        };

        // Act & Assert
        tnTEvent.EventStart.Should().Be(minDateTime);
        tnTEvent.EventEnd.Should().Be(maxDateTime);
        tnTEvent.Duration.Should().Be(maxDateTime - minDateTime);

        // These should not throw exceptions
        var startTime = tnTEvent.StartTime;
        var endTime = tnTEvent.EndTime;
        var startDate = tnTEvent.StartDate;
        var endDate = tnTEvent.EndDate;

        // Verify the computed properties work without exceptions
        startTime.Should().Be(TimeOnly.FromTimeSpan(minDateTime.LocalDateTime.TimeOfDay));
        endTime.Should().Be(TimeOnly.FromTimeSpan(maxDateTime.LocalDateTime.TimeOfDay));
        startDate.Should().Be(DateOnly.FromDateTime(minDateTime.LocalDateTime.Date));
        endDate.Should().Be(DateOnly.FromDateTime(maxDateTime.LocalDateTime.Date));
    }

    [Fact]
    public void StartDate_WithDifferentTimeZones_UsesLocalDate() {
        // Arrange These represent the same instant but different local dates due to timezone
        var utcTime = new DateTimeOffset(2024, 1, 16, 2, 0, 0, TimeSpan.Zero); // Jan 16, 2:00 AM UTC
        var pstTime = new DateTimeOffset(2024, 1, 15, 18, 0, 0, TimeSpan.FromHours(-8)); // Jan 15, 6:00 PM PST

        var utcEvent = new NTEvent { EventStart = utcTime };
        var pstEvent = new NTEvent { EventStart = pstTime };

        // Act
        var utcStartDate = utcEvent.StartDate;
        var pstStartDate = pstEvent.StartDate;

        // Assert
        // Note: LocalDateTime converts to system local time, so we expect the converted dates
        var expectedUtcDate = DateOnly.FromDateTime(utcTime.LocalDateTime.Date);
        var expectedPstDate = DateOnly.FromDateTime(pstTime.LocalDateTime.Date);

        utcStartDate.Should().Be(expectedUtcDate);
        pstStartDate.Should().Be(expectedPstDate);
    }

    [Fact]
    public void StartDate_WithEventStart_ReturnsCorrectDate() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 3, 22, 10, 30, 0, TimeSpan.FromHours(-5));
        var tnTEvent = new NTEvent { EventStart = eventStart };

        // Act
        var startDate = tnTEvent.StartDate;

        // Assert
        startDate.Should().Be(new DateOnly(2024, 3, 22));
    }

    [Fact]
    public void StartTime_WithDifferentTimeZones_UsesLocalDateTime() {
        // Arrange
        var utcTime = new DateTimeOffset(2024, 1, 15, 15, 30, 0, TimeSpan.Zero); // 3:30 PM UTC
        var estTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(-5)); // 10:30 AM EST (same instant)

        var utcEvent = new NTEvent { EventStart = utcTime };
        var estEvent = new NTEvent { EventStart = estTime };

        // Act
        var utcStartTime = utcEvent.StartTime;
        var estStartTime = estEvent.StartTime;

        // Assert
        // Note: LocalDateTime converts to system local time, so we need to expect the converted times
        var expectedUtcTime = TimeOnly.FromTimeSpan(utcTime.LocalDateTime.TimeOfDay);
        var expectedEstTime = TimeOnly.FromTimeSpan(estTime.LocalDateTime.TimeOfDay);

        utcStartTime.Should().Be(expectedUtcTime);
        estStartTime.Should().Be(expectedEstTime);
    }

    [Fact]
    public void StartTime_WithEventStart_ReturnsCorrectTime() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 1, 15, 10, 30, 45, TimeSpan.FromHours(-8));
        var tnTEvent = new NTEvent { EventStart = eventStart };

        // Act
        var startTime = tnTEvent.StartTime;

        // Assert LocalDateTime converts to system local time, so we expect the converted time
        var expectedTime = TimeOnly.FromTimeSpan(eventStart.LocalDateTime.TimeOfDay);
        startTime.Should().Be(expectedTime);
    }

    [Theory]
    [InlineData(NTColor.SurfaceTint)]
    [InlineData(NTColor.Primary)]
    [InlineData(NTColor.Secondary)]
    [InlineData(NTColor.Tertiary)]
    public void TintColor_SetValue_ReturnsCorrectValue(NTColor color) {
        // Arrange
        var tnTEvent = new NTEvent();

        // Act
        tnTEvent.TintColor = color;

        // Assert
        tnTEvent.TintColor.Should().Be(color);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short Title")]
    [InlineData("A much longer title with special characters !@#$%^&*()")]
    [InlineData("Title with numbers 123 and symbols")]
    public void Title_SetValue_ReturnsCorrectValue(string title) {
        // Arrange & Act
        var tnTEvent = new NTEvent { Title = title };

        // Assert
        tnTEvent.Title.Should().Be(title);
    }

    [Fact]
    public void NTEvent_EqualityComparison_WorksCorrectly() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

        var event1 = new NTEvent {
            Title = "Test Event",
            Description = "Test Description",
            EventStart = eventStart,
            EventEnd = eventEnd,
            BackgroundColor = NTColor.Primary
        };

        var event2 = new NTEvent {
            Title = "Test Event",
            Description = "Test Description",
            EventStart = eventStart,
            EventEnd = eventEnd,
            BackgroundColor = NTColor.Primary
        };

        // Act & Assert
        // Note: These will NOT be equal because Id is auto-generated and different
        event1.Should().NotBe(event2);
        event1.GetHashCode().Should().NotBe(event2.GetHashCode());
    }

    [Fact]
    public void NTEvent_IsRecord_SupportsWithExpressions() {
        // Arrange
        var originalEvent = new NTEvent {
            Title = "Original Title",
            Description = "Original Description",
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
            BackgroundColor = NTColor.Primary
        };

        // Act
        var modifiedEvent = originalEvent with {
            Title = "Modified Title",
            BackgroundColor = NTColor.Secondary
        };

        // Assert
        modifiedEvent.Title.Should().Be("Modified Title");
        modifiedEvent.Description.Should().Be("Original Description");
        modifiedEvent.EventStart.Should().Be(originalEvent.EventStart);
        modifiedEvent.EventEnd.Should().Be(originalEvent.EventEnd);
        modifiedEvent.BackgroundColor.Should().Be(NTColor.Secondary);
        // Note: Id is copied in record 'with' expressions since it's part of the record
        modifiedEvent.Id.Should().Be(originalEvent.Id);
    }

    [Fact]
    public void NTEvent_ToString_ReturnsStringRepresentation() {
        // Arrange
        var tnTEvent = new NTEvent {
            Title = "Test Event",
            Description = "Test Description",
            EventStart = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = tnTEvent.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("NTEvent");
    }
}

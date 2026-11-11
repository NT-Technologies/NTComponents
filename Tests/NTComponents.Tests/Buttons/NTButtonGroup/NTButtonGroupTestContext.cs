using System.Collections.Generic;
using AutoFixture;
using Bunit;
using Microsoft.AspNetCore.Components;
using NTComponents;
using NTComponents.Core;

namespace NTComponents.Tests.Buttons.NTButtonGroup;

/// <summary>
///     Provides shared setup and helpers for testing <see cref="NTButtonGroup{TObjectType}" />.
/// </summary>
public abstract class NTButtonGroupTestContext : BunitContext {
    /// <summary>
    ///     Fixture used to generate random test data.
    /// </summary>
    protected Fixture Fixture { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTButtonGroupTestContext" /> class.
    /// </summary>
    protected NTButtonGroupTestContext() {
        Fixture = new Fixture();
    }

    /// <summary>
    ///     Creates a pair of items that can be shared across tests.
    /// </summary>
    /// <param name="iconOnlyFirstItem">Determines whether the first item renders as an icon-only button.</param>
    /// <param name="defaultSecondItem">Determines whether the second item should be marked as default selected.</param>
    /// <returns>A collection containing the generated items.</returns>
    protected IReadOnlyCollection<NTButtonGroupTestItem> CreateItems(bool iconOnlyFirstItem = false, bool defaultSecondItem = false) {
        var first = new NTButtonGroupTestItem {
            Key = Fixture.Create<string>(),
            Label = iconOnlyFirstItem ? null : Fixture.Create<string>(),
            Icon = iconOnlyFirstItem ? new MaterialIcon("home") : null,
            AriaLabel = iconOnlyFirstItem ? "Home" : null,
            IsDefaultSelected = false
        };

        var second = new NTButtonGroupTestItem {
            Key = Fixture.Create<string>(),
            Label = Fixture.Create<string>(),
            IsDefaultSelected = defaultSecondItem
        };

        return new[] { first, second };
    }

    /// <summary>
    ///     Creates the child content needed to render each <see cref="NTButtonGroupItem{TObjectType}" /> instance.
    /// </summary>
    protected RenderFragment RenderItems(IEnumerable<NTButtonGroupTestItem> items) => builder => {
        foreach (var item in items) {
            builder.OpenComponent<NTButtonGroupItem<string>>(0);
            builder.AddAttribute(1, nameof(NTButtonGroupItem<string>.Key), item.Key);

            if (item.Label is not null) {
                builder.AddAttribute(2, nameof(NTButtonGroupItem<string>.Label), item.Label);
            }

            if (item.AriaLabel is not null) {
                builder.AddAttribute(3, nameof(NTButtonGroupItem<string>.AriaLabel), item.AriaLabel);
            }

            if (item.Icon is not null) {
                builder.AddAttribute(4, nameof(NTButtonGroupItem<string>.Icon), (object)item.Icon);
            }

            if (item.Disabled) {
                builder.AddAttribute(5, nameof(NTButtonGroupItem<string>.Disabled), true);
            }

            if (item.IsDefaultSelected) {
                builder.AddAttribute(6, nameof(NTButtonGroupItem<string>.IsDefaultSelected), true);
            }

            builder.CloseComponent();
        }
    };

    /// <summary>
    ///     Describes the test-only data that drives each rendered item.
    /// </summary>
    protected sealed record NTButtonGroupTestItem {
        /// <summary>
        ///     A unique key for the item.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        ///     Optional label text.
        /// </summary>
        public string? Label { get; init; }

        /// <summary>
        ///     Optional accessible label for icon-only items.
        /// </summary>
        public string? AriaLabel { get; init; }

        /// <summary>
        ///     Optional icon rendered before the label.
        /// </summary>
        public NTIcon? Icon { get; init; }

        /// <summary>
        ///     Controls whether the item is disabled.
        /// </summary>
        public bool Disabled { get; init; }

        /// <summary>
        ///     Indicates whether the item should be selected by default.
        /// </summary>
        public bool IsDefaultSelected { get; init; }
    }
}

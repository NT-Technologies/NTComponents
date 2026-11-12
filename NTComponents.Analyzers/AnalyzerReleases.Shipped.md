; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
NTC1001 | Usage | Warning | Transparent background is invalid for filled and elevated NTCard variants.
NTC1002 | Usage | Warning | NTCard elevation is ignored unless the variant is Elevated.

## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
NTC1003 | Usage | Warning | NTButton text and outlined variants require a transparent background.
NTC1004 | Usage | Warning | NTButton filled, tonal, and elevated variants require a visible container background.
NTC1005 | Usage | Warning | NTButton TextColor cannot be Transparent.
NTC1006 | Usage | Warning | NTButton elevation must match the selected variant.
NTC1007 | Usage | Warning | NTButton text variant does not support toggle behavior.
NTC1008 | Usage | Warning | NTButton Label cannot be empty.
NTC1009 | Usage | Warning | NTSplitButton Label cannot be empty unless LeadingIcon is supplied.
NTC1010 | Accessibility | Warning | Icon-only NTSplitButton actions require ActionAriaLabel.
NTC1011 | Usage | Warning | NTSplitButton text and outlined variants require a transparent background.
NTC1012 | Usage | Warning | NTSplitButton filled, tonal, and elevated variants require a visible container background.
NTC1013 | Usage | Warning | NTSplitButton TextColor cannot be Transparent.
NTC1014 | Usage | Warning | NTSplitButton elevation must match the selected variant.
NTC1015 | Usage | Warning | NTSplitButton menu colors cannot be Transparent.
NTC1016 | Usage | Warning | NTSplitButton requires at least one actionable menu item.
NTC1019 | Usage | Warning | NTButtonGroup text variant does not support selectable behavior.
NTC1020 | Usage | Warning | NTButtonGroup text and outlined variants require a transparent background.
NTC1021 | Usage | Warning | NTButtonGroup filled, tonal, and elevated variants require a visible container background.
NTC1022 | Usage | Warning | NTButtonGroup TextColor cannot be Transparent.
NTC1023 | Usage | Warning | Selectable NTButtonGroup selected backgrounds cannot be Transparent.
NTC1024 | Usage | Warning | Selectable NTButtonGroup SelectedTextColor cannot be Transparent.
NTC1025 | Accessibility | Warning | Icon-only NTButtonGroupItem children require AriaLabel.
NTC1026 | Usage | Warning | NTIconButton Icon cannot be omitted or null.
NTC1027 | Accessibility | Warning | NTIconButton AriaLabel cannot be empty.
NTC1028 | Usage | Warning | NTIconButton text and unselected outlined variants require a transparent background.
NTC1029 | Usage | Warning | NTIconButton filled, tonal, elevated, and selected outlined toggle variants require a visible container background.
NTC1030 | Usage | Warning | NTIconButton TextColor cannot be Transparent.
NTC1031 | Usage | Warning | NTIconButton elevation must match the selected variant.
NTC1032 | Usage | Warning | NTFabButton Icon cannot be omitted or null.
NTC1033 | Accessibility | Warning | Icon-only NTFabButton requires AriaLabel.
NTC1034 | Usage | Warning | NTFabButton Label cannot contain line breaks.
NTC1035 | Usage | Warning | NTFabButton BackgroundColor cannot be Transparent.
NTC1036 | Usage | Warning | NTFabButton TextColor cannot be Transparent.
NTC1037 | Usage | Info | NTFabButton ButtonSize Smallest and Largest are remapped to the closest supported FAB size.
NTC1038 | Usage | Warning | NTFabButton Placement must be a defined NTFabButtonPlacement value.
NTC1039 | Usage | Warning | NTFabMenu Icon cannot be omitted or null.
NTC1040 | Accessibility | Warning | NTFabMenu AriaLabel cannot be empty.
NTC1041 | Usage | Warning | NTFabMenu color overrides cannot be Transparent.
NTC1042 | Usage | Info | NTFabMenu ButtonSize Smallest and Largest are remapped to the closest supported FAB size.
NTC1043 | Usage | Warning | NTFabMenu Placement must be a defined NTFabButtonPlacement value.
NTC1044 | Usage | Warning | NTFabMenu requires 2 to 6 actionable menu items.
NTC1045 | Usage | Warning | NTFabMenu item Label cannot be empty.
NTC1046 | Usage | Warning | NTFabMenuAnchorItem Href cannot be empty.
NTC1047 | Accessibility | Warning | NTMenu AriaLabel cannot be empty.
NTC1048 | Usage | Warning | NTMenu requires at least one actionable menu item.
NTC1049 | Usage | Warning | NTMenu item Label cannot be empty.
NTC1050 | Usage | Warning | NTMenuAnchorItem Href cannot be empty.
NTC1051 | Usage | Warning | NTMenu color overrides cannot be Transparent.
NTC1052 | Usage | Info | NTProgress Max must be positive.
NTC1053 | Usage | Info | NTProgress Value should be within the 0..Max range.
NTC1054 | Usage | Info | NTLoader AnimationDuration should be at least 400ms.
NTC1055 | Usage | Info | Animated NTLoader Shapes should contain at least two shapes.
NTC1056 | Accessibility | Warning | NTNavLink Label cannot be empty because it provides the anchor accessible name.
NTC1057 | Accessibility | Warning | NTNavLink owns aria-current and aria-disabled state rendering.
NTC1058 | Accessibility | Warning | Disabled NTNavLink cannot be kept in the tab order with an explicit tabindex.
NTC1059 | Usage | Info | NTInputText PhoneMask is ignored unless InputType is TextInputType.Tel.
NTC1060 | Usage | Info | NTInputText PhoneMask should contain at least one digit placeholder.
NTC1061 | Usage | Warning | NT form components own some native attributes.
NTC1062 | Usage | Warning | Validation-bound NT inputs require ValueExpression when Value or ValueChanged is set.
NTC1063 | Usage | Warning | NTInputCheckbox and NTInputSwitch required validation should use Required instead of a raw required attribute.
NTC1064 | Usage | Warning | NTForm RequiredSupportingText cannot be empty when ShowRequiredSupportingText is enabled.
NTC1065 | Usage | Warning | NTDataGrid requires either Items or ItemsProvider, not both.
NTC1066 | Usage | Warning | NTDataGrid requires Items or ItemsProvider.
NTC1067 | Usage | Warning | NTDataGrid does not support using Virtualize and ShowPagination together.
NTC1068 | Usage | Info | NTPropertyColumn computed aggregate properties may fail when sorting a database-backed IQueryable.
NTC1069 | Accessibility | Warning | NTCarousel AriaLabel cannot be empty because it provides the carousel accessible name.
NTC1070 | Usage | Warning | NTCarousel numeric sizing and autoplay values must be finite and positive.
NTC1071 | Usage | Warning | Full-screen NTCarousel requires snapping.
NTC1072 | Accessibility | Warning | NTCarouselItem AriaLabel cannot be empty because it provides the slide accessible name.
NTC1073 | Usage | Warning | NTCarouselItem AspectRatio must be finite and remain within the Material 3 range.
NTC1074 | Usage | Warning | NTCarousel Appearance must be a defined CarouselAppearance value.
NTC1075 | Usage | Warning | NTDataGrid PersistenceKey requires PersistPrerenderedItems.
NTC1076 | Usage | Warning | NTDataGrid persistence requires a non-empty PersistenceKey.
NTC1077 | Migration | Warning | Replace TnTColor with NTColor. Fix All supported.
NTC1078 | Migration | Warning | None, Black, and White require a deliberate replacement because NTColor omits them.
NTC1079 | Migration | Warning | Replace ToCssTnTColorVariable with ToCssNTColorVariable. Fix All supported.
NTC1080 | Usage | Info | NTAnimation Shown only applies when Trigger is Manual.
NTC1081 | Usage | Info | NTAnimation Repeat, Threshold, RootMargin, OnEntered and OnExited only apply when Trigger is Visible.
NTC1082 | Usage | Info | NTAnimation Threshold must be between 0 and 1.
NTC1102 | Usage | Info | Parameter has no effect in this configuration.
NTC1103 | Usage | Info | Parameter value is adjusted at runtime.
NTC1087 | Usage | Warning | Component must be rendered inside its required parent.
NTC1088 | Usage | Warning | NTForm cannot be nested inside another NTForm.
NTC1089 | Usage | Warning | NT component enum parameters must use a defined value.
NTC1083 | Usage | Warning | NTNavLink text and outlined variants require a transparent background.
NTC1084 | Usage | Warning | NTNavLink filled, tonal, and elevated variants require a visible container background.
NTC1085 | Usage | Warning | NTNavLink content colors cannot be Transparent.
NTC1086 | Usage | Warning | NTNavLink elevation must match the selected variant.
NTC1090 | Usage | Warning | NTChip requires a non-empty Label.
NTC1091 | Usage | Warning | NTChip Href, MenuContent and selectable behavior cannot be combined.
NTC1092 | Accessibility | Warning | NTNavigationRail requires a non-empty AriaLabel.
NTC1093 | Usage | Warning | NTNavigationRailItem, NTNavigationRailGroup and NTNavigationRailSectionHeader require a non-empty Label.
NTC1094 | Usage | Warning | NTNavigationRailSectionHeader AriaLevel must be between 1 and 6.
NTC1095 | Usage | Warning | NTWindow requires a non-empty Title.
NTC1096 | Usage | Warning | NTAccordionItem requires either a non-empty Label or HeaderContent.
NTC1097 | Usage | Warning | NTVirtualize requires a non-null ItemsProvider.
NTC1098 | Usage | Warning | NTVirtualize ItemSize must be finite and greater than zero.
NTC1099 | Usage | Warning | NTResponsive CustomBreakpoint must be zero or greater.
NTC1100 | Accessibility | Warning | NTDialog needs a Title, TitleContent or aria-label to have an accessible name.
NTC1101 | Accessibility | Info | A dot badge needs an accessible label.
NTC1109 | Usage | Warning | A raw attribute the component sets itself never reaches the DOM.
NTC1110 | Accessibility | Warning | NTProgress and NTLoader need a non-empty AriaLabel or AriaLabelledBy to have an accessible name.
NTC1104 | Usage | Warning | NTTextArea MinVisibleLines and MaxVisibleLines must be positive and MaxVisibleLines cannot be less than MinVisibleLines.
NTC1105 | Usage | Warning | NTSelect does not support the multiple attribute.
NTC1106 | Usage | Warning | Component does not support this type argument.
NTC1107 | Accessibility | Info | Form field has no accessible name.
NTC1108 | Accessibility | Warning | NTFormSectionView with UseFieldset needs a Heading for the legend.
NTC1111 | Usage | Warning | NTContextMenu needs either TargetContent or TargetElementId.
NTC1112 | Usage | Warning | NTContextMenu TargetContent and TargetElementId are both set.

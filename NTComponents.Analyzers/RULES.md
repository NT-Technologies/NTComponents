# NTComponents analyzer rules

Each diagnostic links here from its help link. Rules that predict a runtime exception or broken rendering are warnings; rules for values a component ignores or adjusts are suggestions (Info). Change a severity with `dotnet_diagnostic.NTC1003.severity = none` in `.editorconfig`, or a whole category with `dotnet_analyzer_diagnostic.category-Accessibility.severity = error`.

| Rule | Category | Severity | Summary |
|------|----------|----------|---------|
| [NTC1001](#ntc1001) | Usage | Warning | Transparent background is invalid for filled and elevated NTCard variants. |
| [NTC1002](#ntc1002) | Usage | Warning | NTCard elevation is ignored unless the variant is Elevated. |
| [NTC1003](#ntc1003) | Usage | Warning | NTButton text and outlined variants require a transparent background. |
| [NTC1004](#ntc1004) | Usage | Warning | NTButton filled, tonal, and elevated variants require a visible container background. |
| [NTC1005](#ntc1005) | Usage | Warning | NTButton TextColor cannot be Transparent. |
| [NTC1006](#ntc1006) | Usage | Warning | NTButton elevation must match the selected variant. |
| [NTC1007](#ntc1007) | Usage | Warning | NTButton text variant does not support toggle behavior. |
| [NTC1008](#ntc1008) | Usage | Warning | NTButton Label cannot be empty. |
| [NTC1009](#ntc1009) | Usage | Warning | NTSplitButton Label cannot be empty unless LeadingIcon is supplied. |
| [NTC1010](#ntc1010) | Accessibility | Warning | Icon-only NTSplitButton actions require ActionAriaLabel. |
| [NTC1011](#ntc1011) | Usage | Warning | NTSplitButton text and outlined variants require a transparent background. |
| [NTC1012](#ntc1012) | Usage | Warning | NTSplitButton filled, tonal, and elevated variants require a visible container background. |
| [NTC1013](#ntc1013) | Usage | Warning | NTSplitButton TextColor cannot be Transparent. |
| [NTC1014](#ntc1014) | Usage | Warning | NTSplitButton elevation must match the selected variant. |
| [NTC1015](#ntc1015) | Usage | Warning | NTSplitButton menu colors cannot be Transparent. |
| [NTC1016](#ntc1016) | Usage | Warning | NTSplitButton requires at least one actionable menu item. |
| [NTC1019](#ntc1019) | Usage | Warning | NTButtonGroup text variant does not support selectable behavior. |
| [NTC1020](#ntc1020) | Usage | Warning | NTButtonGroup text and outlined variants require a transparent background. |
| [NTC1021](#ntc1021) | Usage | Warning | NTButtonGroup filled, tonal, and elevated variants require a visible container background. |
| [NTC1022](#ntc1022) | Usage | Warning | NTButtonGroup TextColor cannot be Transparent. |
| [NTC1023](#ntc1023) | Usage | Warning | Selectable NTButtonGroup selected backgrounds cannot be Transparent. |
| [NTC1024](#ntc1024) | Usage | Warning | Selectable NTButtonGroup SelectedTextColor cannot be Transparent. |
| [NTC1025](#ntc1025) | Accessibility | Warning | Icon-only NTButtonGroupItem children require AriaLabel. |
| [NTC1026](#ntc1026) | Usage | Warning | NTIconButton Icon cannot be omitted or null. |
| [NTC1027](#ntc1027) | Accessibility | Warning | NTIconButton AriaLabel cannot be empty. |
| [NTC1028](#ntc1028) | Usage | Warning | NTIconButton text and unselected outlined variants require a transparent background. |
| [NTC1029](#ntc1029) | Usage | Warning | NTIconButton filled, tonal, elevated, and selected outlined toggle variants require a visible container background. |
| [NTC1030](#ntc1030) | Usage | Warning | NTIconButton TextColor cannot be Transparent. |
| [NTC1031](#ntc1031) | Usage | Warning | NTIconButton elevation must match the selected variant. |
| [NTC1032](#ntc1032) | Usage | Warning | NTFabButton Icon cannot be omitted or null. |
| [NTC1033](#ntc1033) | Accessibility | Warning | Icon-only NTFabButton requires AriaLabel. |
| [NTC1034](#ntc1034) | Usage | Warning | NTFabButton Label cannot contain line breaks. |
| [NTC1035](#ntc1035) | Usage | Warning | NTFabButton BackgroundColor cannot be Transparent. |
| [NTC1036](#ntc1036) | Usage | Warning | NTFabButton TextColor cannot be Transparent. |
| [NTC1037](#ntc1037) | Usage | Info | NTFabButton ButtonSize Smallest and Largest are remapped to the closest supported FAB size. |
| [NTC1038](#ntc1038) | Usage | Warning | NTFabButton Placement must be a defined NTFabButtonPlacement value. |
| [NTC1039](#ntc1039) | Usage | Warning | NTFabMenu Icon cannot be omitted or null. |
| [NTC1040](#ntc1040) | Accessibility | Warning | NTFabMenu AriaLabel cannot be empty. |
| [NTC1041](#ntc1041) | Usage | Warning | NTFabMenu color overrides cannot be Transparent. |
| [NTC1042](#ntc1042) | Usage | Info | NTFabMenu ButtonSize Smallest and Largest are remapped to the closest supported FAB size. |
| [NTC1043](#ntc1043) | Usage | Warning | NTFabMenu Placement must be a defined NTFabButtonPlacement value. |
| [NTC1044](#ntc1044) | Usage | Warning | NTFabMenu requires 2 to 6 actionable menu items. |
| [NTC1045](#ntc1045) | Usage | Warning | NTFabMenu item Label cannot be empty. |
| [NTC1046](#ntc1046) | Usage | Warning | NTFabMenuAnchorItem Href cannot be empty. |
| [NTC1047](#ntc1047) | Accessibility | Warning | NTMenu AriaLabel cannot be empty. |
| [NTC1048](#ntc1048) | Usage | Warning | NTMenu requires at least one actionable menu item. |
| [NTC1049](#ntc1049) | Usage | Warning | NTMenu item Label cannot be empty. |
| [NTC1050](#ntc1050) | Usage | Warning | NTMenuAnchorItem Href cannot be empty. |
| [NTC1051](#ntc1051) | Usage | Warning | NTMenu color overrides cannot be Transparent. |
| [NTC1052](#ntc1052) | Usage | Info | NTProgress Max must be positive. |
| [NTC1053](#ntc1053) | Usage | Info | NTProgress Value should be within the 0..Max range. |
| [NTC1054](#ntc1054) | Usage | Info | NTLoader AnimationDuration should be at least 400ms. |
| [NTC1055](#ntc1055) | Usage | Info | Animated NTLoader Shapes should contain at least two shapes. |
| [NTC1056](#ntc1056) | Accessibility | Warning | NTNavLink Label cannot be empty because it provides the anchor accessible name. |
| [NTC1057](#ntc1057) | Accessibility | Warning | NTNavLink owns aria-current and aria-disabled state rendering. |
| [NTC1058](#ntc1058) | Accessibility | Warning | Disabled NTNavLink cannot be kept in the tab order with an explicit tabindex. |
| [NTC1059](#ntc1059) | Usage | Info | NTInputText PhoneMask is ignored unless InputType is TextInputType.Tel. |
| [NTC1060](#ntc1060) | Usage | Info | NTInputText PhoneMask should contain at least one digit placeholder. |
| [NTC1061](#ntc1061) | Usage | Warning | NT form components own some native attributes. |
| [NTC1062](#ntc1062) | Usage | Warning | Validation-bound NT inputs require ValueExpression when Value or ValueChanged is set. |
| [NTC1063](#ntc1063) | Usage | Warning | NTInputCheckbox and NTInputSwitch required validation should use Required instead of a raw required attribute. |
| [NTC1064](#ntc1064) | Usage | Warning | NTForm RequiredSupportingText cannot be empty when ShowRequiredSupportingText is enabled. |
| [NTC1065](#ntc1065) | Usage | Warning | NTDataGrid requires either Items or ItemsProvider, not both. |
| [NTC1066](#ntc1066) | Usage | Warning | NTDataGrid requires Items or ItemsProvider. |
| [NTC1067](#ntc1067) | Usage | Warning | NTDataGrid does not support using Virtualize and ShowPagination together. |
| [NTC1068](#ntc1068) | Usage | Info | NTPropertyColumn computed aggregate properties may fail when sorting a database-backed IQueryable. |
| [NTC1069](#ntc1069) | Accessibility | Warning | NTCarousel AriaLabel cannot be empty because it provides the carousel accessible name. |
| [NTC1070](#ntc1070) | Usage | Warning | NTCarousel numeric sizing and autoplay values must be finite and positive. |
| [NTC1071](#ntc1071) | Usage | Warning | Full-screen NTCarousel requires snapping. |
| [NTC1072](#ntc1072) | Accessibility | Warning | NTCarouselItem AriaLabel cannot be empty because it provides the slide accessible name. |
| [NTC1073](#ntc1073) | Usage | Warning | NTCarouselItem AspectRatio must be finite and remain within the Material 3 range. |
| [NTC1074](#ntc1074) | Usage | Warning | NTCarousel Appearance must be a defined CarouselAppearance value. |
| [NTC1075](#ntc1075) | Usage | Warning | NTDataGrid PersistenceKey requires PersistPrerenderedItems. |
| [NTC1076](#ntc1076) | Usage | Warning | NTDataGrid persistence requires a non-empty PersistenceKey. |
| [NTC1077](#ntc1077) | Migration | Warning | Replace TnTColor with NTColor. Fix All supported. |
| [NTC1078](#ntc1078) | Migration | Warning | None, Black, and White require a deliberate replacement because NTColor omits them. |
| [NTC1079](#ntc1079) | Migration | Warning | Replace ToCssTnTColorVariable with ToCssNTColorVariable. Fix All supported. |
| [NTC1080](#ntc1080) | Usage | Info | NTAnimation Shown only applies when Trigger is Manual. |
| [NTC1081](#ntc1081) | Usage | Info | NTAnimation Repeat, Threshold, RootMargin, OnEntered and OnExited only apply when Trigger is Visible. |
| [NTC1082](#ntc1082) | Usage | Info | NTAnimation Threshold must be between 0 and 1. |
| [NTC1083](#ntc1083) | Usage | Warning | NTNavLink text and outlined variants require a transparent background. |
| [NTC1084](#ntc1084) | Usage | Warning | NTNavLink filled, tonal, and elevated variants require a visible container background. |
| [NTC1085](#ntc1085) | Usage | Warning | NTNavLink content colors cannot be Transparent. |
| [NTC1086](#ntc1086) | Usage | Warning | NTNavLink elevation must match the selected variant. |
| [NTC1087](#ntc1087) | Usage | Warning | Component must be rendered inside its required parent. |
| [NTC1088](#ntc1088) | Usage | Warning | NTForm cannot be nested inside another NTForm. |
| [NTC1089](#ntc1089) | Usage | Warning | NT component enum parameters must use a defined value. |
| [NTC1090](#ntc1090) | Usage | Warning | NTChip requires a non-empty Label. |
| [NTC1091](#ntc1091) | Usage | Warning | NTChip Href, MenuContent and selectable behavior cannot be combined. |
| [NTC1092](#ntc1092) | Accessibility | Warning | NTNavigationRail requires a non-empty AriaLabel. |
| [NTC1093](#ntc1093) | Usage | Warning | NTNavigationRailItem, NTNavigationRailGroup and NTNavigationRailSectionHeader require a non-empty Label. |
| [NTC1094](#ntc1094) | Usage | Warning | NTNavigationRailSectionHeader AriaLevel must be between 1 and 6. |
| [NTC1095](#ntc1095) | Usage | Warning | NTWindow requires a non-empty Title. |
| [NTC1096](#ntc1096) | Usage | Warning | NTAccordionItem requires either a non-empty Label or HeaderContent. |
| [NTC1097](#ntc1097) | Usage | Warning | NTVirtualize requires a non-null ItemsProvider. |
| [NTC1098](#ntc1098) | Usage | Warning | NTVirtualize ItemSize must be finite and greater than zero. |
| [NTC1099](#ntc1099) | Usage | Warning | NTResponsive CustomBreakpoint must be zero or greater. |
| [NTC1100](#ntc1100) | Accessibility | Warning | NTDialog needs a Title, TitleContent or aria-label to have an accessible name. |
| [NTC1101](#ntc1101) | Accessibility | Info | A dot badge needs an accessible label. |
| [NTC1102](#ntc1102) | Usage | Info | Parameter has no effect in this configuration. |
| [NTC1103](#ntc1103) | Usage | Info | Parameter value is adjusted at runtime. |
| [NTC1104](#ntc1104) | Usage | Warning | NTTextArea MinVisibleLines and MaxVisibleLines must be positive and MaxVisibleLines cannot be less than MinVisibleLines. |
| [NTC1105](#ntc1105) | Usage | Warning | NTSelect does not support the multiple attribute. |
| [NTC1106](#ntc1106) | Usage | Warning | Component does not support this type argument. |
| [NTC1107](#ntc1107) | Accessibility | Info | Form field has no accessible name. |
| [NTC1108](#ntc1108) | Accessibility | Warning | NTFormSectionView with UseFieldset needs a Heading for the legend. |
| [NTC1109](#ntc1109) | Usage | Warning | A raw attribute the component sets itself never reaches the DOM. |
| [NTC1110](#ntc1110) | Accessibility | Warning | NTProgress and NTLoader need a non-empty AriaLabel or AriaLabelledBy to have an accessible name. |
| [NTC1111](#ntc1111) | Usage | Warning | NTContextMenu needs either TargetContent or TargetElementId. |
| [NTC1112](#ntc1112) | Usage | Warning | NTContextMenu TargetContent and TargetElementId are both set. |
| [NTC1113](#ntc1113) | Usage | Info | NTDataGrid QueryParameters should be merged into URLs a component builds from NavigationManager. |

## NTC1001

Transparent background is invalid for filled and elevated NTCard variants.

Category: Usage. Default severity: Warning.

## NTC1002

NTCard elevation is ignored unless the variant is Elevated.

Category: Usage. Default severity: Warning.

## NTC1003

NTButton text and outlined variants require a transparent background.

Category: Usage. Default severity: Warning.

## NTC1004

NTButton filled, tonal, and elevated variants require a visible container background.

Category: Usage. Default severity: Warning.

## NTC1005

NTButton TextColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1006

NTButton elevation must match the selected variant.

Category: Usage. Default severity: Warning.

## NTC1007

NTButton text variant does not support toggle behavior.

Category: Usage. Default severity: Warning.

## NTC1008

NTButton Label cannot be empty.

Category: Usage. Default severity: Warning.

## NTC1009

NTSplitButton Label cannot be empty unless LeadingIcon is supplied.

Category: Usage. Default severity: Warning.

## NTC1010

Icon-only NTSplitButton actions require ActionAriaLabel.

Category: Accessibility. Default severity: Warning.

## NTC1011

NTSplitButton text and outlined variants require a transparent background.

Category: Usage. Default severity: Warning.

## NTC1012

NTSplitButton filled, tonal, and elevated variants require a visible container background.

Category: Usage. Default severity: Warning.

## NTC1013

NTSplitButton TextColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1014

NTSplitButton elevation must match the selected variant.

Category: Usage. Default severity: Warning.

## NTC1015

NTSplitButton menu colors cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1016

NTSplitButton requires at least one actionable menu item.

Category: Usage. Default severity: Warning.

## NTC1019

NTButtonGroup text variant does not support selectable behavior.

Category: Usage. Default severity: Warning.

## NTC1020

NTButtonGroup text and outlined variants require a transparent background.

Category: Usage. Default severity: Warning.

## NTC1021

NTButtonGroup filled, tonal, and elevated variants require a visible container background.

Category: Usage. Default severity: Warning.

## NTC1022

NTButtonGroup TextColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1023

Selectable NTButtonGroup selected backgrounds cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1024

Selectable NTButtonGroup SelectedTextColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1025

Icon-only NTButtonGroupItem children require AriaLabel.

Category: Accessibility. Default severity: Warning.

## NTC1026

NTIconButton Icon cannot be omitted or null.

Category: Usage. Default severity: Warning.

## NTC1027

NTIconButton AriaLabel cannot be empty.

Category: Accessibility. Default severity: Warning.

## NTC1028

NTIconButton text and unselected outlined variants require a transparent background.

Category: Usage. Default severity: Warning.

## NTC1029

NTIconButton filled, tonal, elevated, and selected outlined toggle variants require a visible container background.

Category: Usage. Default severity: Warning.

## NTC1030

NTIconButton TextColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1031

NTIconButton elevation must match the selected variant.

Category: Usage. Default severity: Warning.

## NTC1032

NTFabButton Icon cannot be omitted or null.

Category: Usage. Default severity: Warning.

## NTC1033

Icon-only NTFabButton requires AriaLabel.

Category: Accessibility. Default severity: Warning.

## NTC1034

NTFabButton Label cannot contain line breaks.

Category: Usage. Default severity: Warning.

## NTC1035

NTFabButton BackgroundColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1036

NTFabButton TextColor cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1037

NTFabButton ButtonSize Smallest and Largest are remapped to the closest supported FAB size.

Category: Usage. Default severity: Info.

## NTC1038

NTFabButton Placement must be a defined NTFabButtonPlacement value.

Category: Usage. Default severity: Warning.

## NTC1039

NTFabMenu Icon cannot be omitted or null.

Category: Usage. Default severity: Warning.

## NTC1040

NTFabMenu AriaLabel cannot be empty.

Category: Accessibility. Default severity: Warning.

## NTC1041

NTFabMenu color overrides cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1042

NTFabMenu ButtonSize Smallest and Largest are remapped to the closest supported FAB size.

Category: Usage. Default severity: Info.

## NTC1043

NTFabMenu Placement must be a defined NTFabButtonPlacement value.

Category: Usage. Default severity: Warning.

## NTC1044

NTFabMenu requires 2 to 6 actionable menu items.

Category: Usage. Default severity: Warning.

## NTC1045

NTFabMenu item Label cannot be empty.

Category: Usage. Default severity: Warning.

## NTC1046

NTFabMenuAnchorItem Href cannot be empty.

Category: Usage. Default severity: Warning.

## NTC1047

NTMenu AriaLabel cannot be empty.

Category: Accessibility. Default severity: Warning.

## NTC1048

NTMenu requires at least one actionable menu item.

Category: Usage. Default severity: Warning.

## NTC1049

NTMenu item Label cannot be empty.

Category: Usage. Default severity: Warning.

## NTC1050

NTMenuAnchorItem Href cannot be empty.

Category: Usage. Default severity: Warning.

## NTC1051

NTMenu color overrides cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1052

NTProgress Max must be positive.

Category: Usage. Default severity: Info.

## NTC1053

NTProgress Value should be within the 0..Max range.

Category: Usage. Default severity: Info.

## NTC1054

NTLoader AnimationDuration should be at least 400ms.

Category: Usage. Default severity: Info.

## NTC1055

Animated NTLoader Shapes should contain at least two shapes.

Category: Usage. Default severity: Info.

## NTC1056

NTNavLink Label cannot be empty because it provides the anchor accessible name.

Category: Accessibility. Default severity: Warning.

## NTC1057

NTNavLink owns aria-current and aria-disabled state rendering.

Category: Accessibility. Default severity: Warning.

## NTC1058

Disabled NTNavLink cannot be kept in the tab order with an explicit tabindex.

Category: Accessibility. Default severity: Warning.

## NTC1059

NTInputText PhoneMask is ignored unless InputType is TextInputType.Tel.

Category: Usage. Default severity: Info.

## NTC1060

NTInputText PhoneMask should contain at least one digit placeholder.

Category: Usage. Default severity: Info.

## NTC1061

NT form components own some native attributes.

Category: Usage. Default severity: Warning.

## NTC1062

Validation-bound NT inputs require ValueExpression when Value or ValueChanged is set.

Category: Usage. Default severity: Warning.

## NTC1063

NTInputCheckbox and NTInputSwitch required validation should use Required instead of a raw required attribute.

Category: Usage. Default severity: Warning.

## NTC1064

NTForm RequiredSupportingText cannot be empty when ShowRequiredSupportingText is enabled.

Category: Usage. Default severity: Warning.

## NTC1065

NTDataGrid requires either Items or ItemsProvider, not both.

Category: Usage. Default severity: Warning.

## NTC1066

NTDataGrid requires Items or ItemsProvider.

Category: Usage. Default severity: Warning.

## NTC1067

NTDataGrid does not support using Virtualize and ShowPagination together.

Category: Usage. Default severity: Warning.

## NTC1068

NTPropertyColumn computed aggregate properties may fail when sorting a database-backed IQueryable.

Category: Usage. Default severity: Info.

## NTC1069

NTCarousel AriaLabel cannot be empty because it provides the carousel accessible name.

Category: Accessibility. Default severity: Warning.

## NTC1070

NTCarousel numeric sizing and autoplay values must be finite and positive.

Category: Usage. Default severity: Warning.

## NTC1071

Full-screen NTCarousel requires snapping.

Category: Usage. Default severity: Warning.

## NTC1072

NTCarouselItem AriaLabel cannot be empty because it provides the slide accessible name.

Category: Accessibility. Default severity: Warning.

## NTC1073

NTCarouselItem AspectRatio must be finite and remain within the Material 3 range.

Category: Usage. Default severity: Warning.

## NTC1074

NTCarousel Appearance must be a defined CarouselAppearance value.

Category: Usage. Default severity: Warning.

## NTC1075

NTDataGrid PersistenceKey requires PersistPrerenderedItems.

Category: Usage. Default severity: Warning.

## NTC1076

NTDataGrid persistence requires a non-empty PersistenceKey.

Category: Usage. Default severity: Warning.

## NTC1077

Replace TnTColor with NTColor. Fix All supported.

Category: Migration. Default severity: Warning.

## NTC1078

None, Black, and White require a deliberate replacement because NTColor omits them.

Category: Migration. Default severity: Warning.

## NTC1079

Replace ToCssTnTColorVariable with ToCssNTColorVariable. Fix All supported.

Category: Migration. Default severity: Warning.

## NTC1080

NTAnimation Shown only applies when Trigger is Manual.

Category: Usage. Default severity: Info.

## NTC1081

NTAnimation Repeat, Threshold, RootMargin, OnEntered and OnExited only apply when Trigger is Visible.

Category: Usage. Default severity: Info.

## NTC1082

NTAnimation Threshold must be between 0 and 1.

Category: Usage. Default severity: Info.

## NTC1083

NTNavLink text and outlined variants require a transparent background.

Category: Usage. Default severity: Warning.

## NTC1084

NTNavLink filled, tonal, and elevated variants require a visible container background.

Category: Usage. Default severity: Warning.

## NTC1085

NTNavLink content colors cannot be Transparent.

Category: Usage. Default severity: Warning.

## NTC1086

NTNavLink elevation must match the selected variant.

Category: Usage. Default severity: Warning.

## NTC1087

Component must be rendered inside its required parent.

Category: Usage. Default severity: Warning.

## NTC1088

NTForm cannot be nested inside another NTForm.

Category: Usage. Default severity: Warning.

## NTC1089

NT component enum parameters must use a defined value.

Category: Usage. Default severity: Warning.

## NTC1090

NTChip requires a non-empty Label.

Category: Usage. Default severity: Warning.

## NTC1091

NTChip Href, MenuContent and selectable behavior cannot be combined.

Category: Usage. Default severity: Warning.

## NTC1092

NTNavigationRail requires a non-empty AriaLabel.

Category: Accessibility. Default severity: Warning.

## NTC1093

NTNavigationRailItem, NTNavigationRailGroup and NTNavigationRailSectionHeader require a non-empty Label.

Category: Usage. Default severity: Warning.

## NTC1094

NTNavigationRailSectionHeader AriaLevel must be between 1 and 6.

Category: Usage. Default severity: Warning.

## NTC1095

NTWindow requires a non-empty Title.

Category: Usage. Default severity: Warning.

## NTC1096

NTAccordionItem requires either a non-empty Label or HeaderContent.

Category: Usage. Default severity: Warning.

## NTC1097

NTVirtualize requires a non-null ItemsProvider.

Category: Usage. Default severity: Warning.

## NTC1098

NTVirtualize ItemSize must be finite and greater than zero.

Category: Usage. Default severity: Warning.

## NTC1099

NTResponsive CustomBreakpoint must be zero or greater.

Category: Usage. Default severity: Warning.

## NTC1100

NTDialog needs a Title, TitleContent or aria-label to have an accessible name.

Category: Accessibility. Default severity: Warning.

## NTC1101

A dot badge needs an accessible label.

Category: Accessibility. Default severity: Info.

## NTC1102

Parameter has no effect in this configuration.

Category: Usage. Default severity: Info.

## NTC1103

Parameter value is adjusted at runtime.

Category: Usage. Default severity: Info.

## NTC1104

NTTextArea MinVisibleLines and MaxVisibleLines must be positive and MaxVisibleLines cannot be less than MinVisibleLines.

Category: Usage. Default severity: Warning.

## NTC1105

NTSelect does not support the multiple attribute.

Category: Usage. Default severity: Warning.

## NTC1106

Component does not support this type argument.

Category: Usage. Default severity: Warning.

## NTC1107

Form field has no accessible name.

Category: Accessibility. Default severity: Info.

## NTC1108

NTFormSectionView with UseFieldset needs a Heading for the legend.

Category: Accessibility. Default severity: Warning.

## NTC1109

A raw attribute the component sets itself never reaches the DOM.

Category: Usage. Default severity: Warning.

## NTC1110

NTProgress and NTLoader need a non-empty AriaLabel or AriaLabelledBy to have an accessible name.

Category: Accessibility. Default severity: Warning.

## NTC1111

NTContextMenu needs either TargetContent or TargetElementId.

Category: Usage. Default severity: Warning.

## NTC1112

NTContextMenu TargetContent and TargetElementId are both set.

Category: Usage. Default severity: Warning.

## NTC1113

NTDataGrid QueryParameters should be merged into URLs a component builds from NavigationManager.

The grid writes its page and sort to the address bar without a Blazor navigation, so NavigationManager.Uri does not include them. A component that renders the grid and builds a URL for the current page from NavigationManager, for example with GetUriWithQueryParameters to keep a filter in the URL, drops the grid's page and sort when it navigates. Merge the grid's QueryParameters into the parameters you pass. The rule only checks the component that renders the grid, and stays silent once that component reads QueryParameters. A grid that still sets the obsolete PushHistoryEntries is skipped, because its navigations keep NavigationManager.Uri current.

Category: Usage. Default severity: Info.

# Unreleased


### Bug Fixes

* **tabs:** docked NTPopoutTab content fills the tab panel height, matching NTTab
* **editor:** blurring NTRichTextEditor without an edit no longer resends the value or fires `BindAfter`; it only marks an untouched field touched for validation and raises `OnBlurCallback`
* **form:** with `BindOnInput` off, NTInputText, NTTextArea, NTInputNumeric, NTInputCurrency, NTInputColor, and NTInputDateTime no longer fire `BindAfter` on a blur without a change; a change still reports once, at the next blur. Use `OnBlurCallback` to react to every blur
* **editor:** embedded iframes are sandboxed (`allow-scripts allow-same-origin allow-popups allow-presentation`), and iframes whose source is the page's own origin are refused. Existing content with a same-origin iframe loses that iframe when it is loaded into the editor


### Features

* **editor:** `NTComponentsDefaultOptions.RichTextEditorMaxValueBytes` sets the largest streamed NTRichTextEditor value the server accepts (default 50 MB)

# [1.48.0](https://github.com/Notallthatevil/NTComponents/compare/v1.47.0...v1.48.0) (2026-04-15)


### Features

* **NTTag:** add NTTag component with icons and theming ([20f9955](https://github.com/Notallthatevil/NTComponents/commit/20f995541df296bf3cf3f420800c4d5eeb8a9124))

# [1.47.0](https://github.com/Notallthatevil/NTComponents/compare/v1.46.0...v1.47.0) (2026-04-15)


### Features

* **rich-text-editor:** add extensible rich text editor with tool plugins, TypeScript interop, and tests ([6f040c8](https://github.com/Notallthatevil/NTComponents/commit/6f040c826055aec069c19b2a8b28a16f88fe58f2))

# [1.46.0](https://github.com/Notallthatevil/NTComponents/compare/v1.45.1...v1.46.0) (2026-04-06)


### Bug Fixes

* **toast:** improve toast color contrast and close button style ([7ce29e6](https://github.com/Notallthatevil/NTComponents/commit/7ce29e656fdbbdde5744021400ada62af9e39e8e))


### Features

* **TnTChip:** add SubLabel support and tests ([06ba4c8](https://github.com/Notallthatevil/NTComponents/commit/06ba4c8689fa8025f2c71cb93c4f79a676445eb4))

## [1.45.1](https://github.com/Notallthatevil/NTComponents/compare/v1.45.0...v1.45.1) (2026-04-02)


### Bug Fixes

* **markdown-editor:** add client revision tracking ([986d521](https://github.com/Notallthatevil/NTComponents/commit/986d521379c6810d5fc1a6442091add974133325))

# [1.45.0](https://github.com/Notallthatevil/NTComponents/compare/v1.44.5...v1.45.0) (2026-04-01)


### Features

* **core:** add global default options and appearance resolver ([a264221](https://github.com/Notallthatevil/NTComponents/commit/a2642210fcb01de8d57edd6f28bb483f48e30a5e))
* **theme:** add critical style and theme bootstrapper ([100bd38](https://github.com/Notallthatevil/NTComponents/commit/100bd381db1df19d5ace631e5f56d39048b6a278))

## [1.44.5](https://github.com/Notallthatevil/NTComponents/compare/v1.44.4...v1.44.5) (2026-04-01)


### Bug Fixes

* **input-base:** trigger BindAfter on blur if not BindOnInput ([975feaa](https://github.com/Notallthatevil/NTComponents/commit/975feaa27fe255ed36e84453193924dbb958e59c))

## [1.44.4](https://github.com/Notallthatevil/NTComponents/compare/v1.44.3...v1.44.4) (2026-03-31)


### Bug Fixes

* **input:** ensure BindAfter tested with BindOnInput=true ([be3d7c9](https://github.com/Notallthatevil/NTComponents/commit/be3d7c91f429990c715356c1181c77a78c832288))

## [1.44.3](https://github.com/Notallthatevil/NTComponents/compare/v1.44.2...v1.44.3) (2026-03-31)


### Bug Fixes

* **input:** conditionally invoke BindAfter on input change ([60cc29d](https://github.com/Notallthatevil/NTComponents/commit/60cc29d720dfe2c87c0af980d78cf92270e02e04))

## [1.44.2](https://github.com/Notallthatevil/NTComponents/compare/v1.44.1...v1.44.2) (2026-03-30)


### Bug Fixes

* **TnTWeekView, TnTTypeahead:** use fixed dates and improve input simulation ([70affac](https://github.com/Notallthatevil/NTComponents/commit/70affac5fbb6f8aefcaefc7117cbeef9dd8c7bdf))

# [1.44.0](https://github.com/Notallthatevil/NTComponents/compare/v1.43.0...v1.44.0) (2026-03-30)


### Features

* **accordion:** refactor to use inert and grid transitions ([83a20c6](https://github.com/Notallthatevil/NTComponents/commit/83a20c675ca7d5842cac10913f881c03b285d712))

# [1.43.0](https://github.com/Notallthatevil/NTComponents/compare/v1.42.3...v1.43.0) (2026-03-27)


### Features

* **markdown-editor:** unify value precedence and sync logic ([ede5ae8](https://github.com/Notallthatevil/NTComponents/commit/ede5ae826e19b71f91b001c7b02f62d788ddb56d))

## [1.42.3](https://github.com/Notallthatevil/NTComponents/compare/v1.42.2...v1.42.3) (2026-03-27)


### Bug Fixes

* **styles:** simplify radio group grid layouts and padding ([bb906a7](https://github.com/Notallthatevil/NTComponents/commit/bb906a7e9319c6c9df2c733d201da1957a9d8199))

## [1.42.2](https://github.com/Notallthatevil/NTComponents/compare/v1.42.1...v1.42.2) (2026-03-27)


### Bug Fixes

* **radio-group:** add GroupName param for custom radio name ([e501999](https://github.com/Notallthatevil/NTComponents/commit/e501999610670faf49d30d09c2a2d2b2a6a6311e))

## [1.42.1](https://github.com/Notallthatevil/NTComponents/compare/v1.42.0...v1.42.1) (2026-03-26)


### Bug Fixes

* **TnTInputBase:** remove tests for OverrideForm ignoring form state ([e752942](https://github.com/Notallthatevil/NTComponents/commit/e7529429c3d949d610d6974d0e4915cbf3334f56))

# [1.42.0](https://github.com/Notallthatevil/NTComponents/compare/v1.41.0...v1.42.0) (2026-03-26)


### Features

* **input:** add OverrideForm to bypass form context ([ae68f52](https://github.com/Notallthatevil/NTComponents/commit/ae68f527110057d9528bfc7027f8fd0376f5d1f0))

# [1.41.0](https://github.com/Notallthatevil/NTComponents/compare/v1.40.0...v1.41.0) (2026-03-26)


### Features

* **styles:** enlarge input switch for improved usability ([e403100](https://github.com/Notallthatevil/NTComponents/commit/e403100476e8213bcf9b33e0a2d2343f8d2070fc))

# [1.40.0](https://github.com/Notallthatevil/NTComponents/compare/v1.39.1...v1.40.0) (2026-03-26)


### Features

* **accordion:** add HeaderTemplate for custom headers ([783c431](https://github.com/Notallthatevil/NTComponents/commit/783c431b1c479111ad830abd6f99919aee8633d7))

## [1.39.1](https://github.com/Notallthatevil/NTComponents/compare/v1.39.0...v1.39.1) (2026-03-25)


### Bug Fixes

* **typeahead,inputs:** switch style, and progress size ([153d30f](https://github.com/Notallthatevil/NTComponents/commit/153d30fd66a91f0d2f400ae972b4a274ee6fe4ab))

# [1.39.0](https://github.com/Notallthatevil/NTComponents/compare/v1.38.7...v1.39.0) (2026-03-25)


### Bug Fixes

* **typeahead:** add TextColor parameter for customization ([249b63e](https://github.com/Notallthatevil/NTComponents/commit/249b63e8b2b0b3407f0bc5856179c5f71893e776))


### Features

* **form:** add XS appearance variant and styles ([240fc4c](https://github.com/Notallthatevil/NTComponents/commit/240fc4cd6de1ba5d6697ca755970fc5153863acc))

## [1.38.7](https://github.com/Notallthatevil/NTComponents/compare/v1.38.6...v1.38.7) (2026-03-24)


### Bug Fixes

* **styles:** add Snackbar to ScopedCssFolders in sasscompiler.json ([cdfcf7f](https://github.com/Notallthatevil/NTComponents/commit/cdfcf7f3c9bac475f61579cf57273b8bf4fa3989))

## [1.38.6](https://github.com/Notallthatevil/NTComponents/compare/v1.38.5...v1.38.6) (2026-03-23)


### Bug Fixes

* **NTInputDateTime:** add custom picker mode and trigger button ([45c7b63](https://github.com/Notallthatevil/NTComponents/commit/45c7b63f3f81267a9ca178f6955c8b967be046e2))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.4](https://github.com/Notallthatevil/NTComponents/compare/v1.38.3...v1.38.4) (2026-03-20)


### Bug Fixes

* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([5ab8cb3](https://github.com/Notallthatevil/NTComponents/commit/5ab8cb32bc6201f9e84c5e9a900ffa43429c5839))
* **wizard:** add HideStepsInsteadOfRemovingFromDom option ([7625e65](https://github.com/Notallthatevil/NTComponents/commit/7625e650c8f4406f4e2e0e5910e4bd0982b1c902))

## [1.38.3](https://github.com/Notallthatevil/NTComponents/compare/v1.38.2...v1.38.3) (2026-03-20)


### Bug Fixes

* **accordion:** correct content rendering logic and add test ([7a92aca](https://github.com/Notallthatevil/NTComponents/commit/7a92aca111798f00542702631684c589f2cb3b9c))

## [1.38.2](https://github.com/Notallthatevil/NTComponents/compare/v1.38.1...v1.38.2) (2026-03-19)


### Bug Fixes

* **styles:** improve TnTNavLink SCSS consistency and flexibility ([dfa02ab](https://github.com/Notallthatevil/NTComponents/commit/dfa02abbb9bc878b6db1153605ba39028dae6d5f))

## [1.38.1](https://github.com/Notallthatevil/NTComponents/compare/v1.38.0...v1.38.1) (2026-03-18)


### Bug Fixes

* **typeahead:** notify field change only if identifier set ([6e5f872](https://github.com/Notallthatevil/NTComponents/commit/6e5f872a34dedd17fff0066f8f9b147386253a1d))

# [1.38.0](https://github.com/Notallthatevil/NTComponents/compare/v1.37.0...v1.38.0) (2026-03-18)


### Features

* **NTInputFile:** add file selection state restoration ([07d353f](https://github.com/Notallthatevil/NTComponents/commit/07d353f3f6cb280f0429a968d9bd348fafdcb54e))

# [1.37.0](https://github.com/Notallthatevil/NTComponents/compare/v1.36.1...v1.37.0) (2026-03-18)


### Bug Fixes

* **input-file:** remove files by reference, not index ([c7a7e80](https://github.com/Notallthatevil/NTComponents/commit/c7a7e80d20345b400729ea6b0641266b35e560cf))


### Features

* **datagrid:** add DisableSortToggleToNone for sort behavior ([3a938c5](https://github.com/Notallthatevil/NTComponents/commit/3a938c54a3be354240065ddd1b1f21557b3315dd))

## [1.36.1](https://github.com/Notallthatevil/NTComponents/compare/v1.36.0...v1.36.1) (2026-03-18)


### Bug Fixes

* **NTInputFile:** simplify JS module load logic ([8de0c1a](https://github.com/Notallthatevil/NTComponents/commit/8de0c1a4eb203caf9ebb052c91bc89313fe3c472))

# [1.36.0](https://github.com/Notallthatevil/NTComponents/compare/v1.35.0...v1.36.0) (2026-03-18)


### Features

* **input-file:** implement file removal functionality and enhance tests ([4e7a58d](https://github.com/Notallthatevil/NTComponents/commit/4e7a58d6253b13d949f5dba6dc1dc2d6d438f324))

# [1.35.0](https://github.com/Notallthatevil/NTComponents/compare/v1.34.1...v1.35.0) (2026-03-17)


### Features

* **snackbar:** add snackbar service and component for notifications ([cd3f535](https://github.com/Notallthatevil/NTComponents/commit/cd3f5352737bdb61143919bc2f23e9f602f8e277))

## [1.34.1](https://github.com/Notallthatevil/NTComponents/compare/v1.34.0...v1.34.1) (2026-03-17)


### Bug Fixes

* **text-area:** fixed regression that caused enter to no longer work in text area. ([8a5b3ce](https://github.com/Notallthatevil/NTComponents/commit/8a5b3ce0d95f14ca08b360228aa86a72e2b1db41))

# [1.34.0](https://github.com/Notallthatevil/NTComponents/compare/v1.33.0...v1.34.0) (2026-03-17)


### Features

* enhance NTInputFile component with remove button support and update progress handling ([7a1cd19](https://github.com/Notallthatevil/NTComponents/commit/7a1cd193b090466cdfaa16d15137fc2793a194e1))

# [1.33.0](https://github.com/Notallthatevil/NTComponents/compare/v1.32.0...v1.33.0) (2026-03-17)


### Features

* add InputFileTemplateProgressDemo component and enhance NTInputFile with progress template support ([c38db07](https://github.com/Notallthatevil/NTComponents/commit/c38db0714e56194cd98002cdb0d2a096d83b90a4))

# [1.32.0](https://github.com/Notallthatevil/NTComponents/compare/v1.31.2...v1.32.0) (2026-03-17)


### Features

* add NTInputSelect component with searchable options ([ba8ae5b](https://github.com/Notallthatevil/NTComponents/commit/ba8ae5b5d8191ae52efa428ea860176a46c1af87))

## [1.31.2](https://github.com/Notallthatevil/NTComponents/compare/v1.31.1...v1.31.2) (2026-03-13)


### Bug Fixes

* **NTInputFile:** add ClearAsync to reset file input state ([df00c41](https://github.com/Notallthatevil/NTComponents/commit/df00c41d241df6efb49a12079ffd8fc562909882))

## [1.31.1](https://github.com/Notallthatevil/NTComponents/compare/v1.31.0...v1.31.1) (2026-03-13)


### Bug Fixes

* **virtualize:** handle max-height for scroll containers ([f4c44b1](https://github.com/Notallthatevil/NTComponents/commit/f4c44b1afd0e28417377eff2d7335519427eaea8))

# [1.31.0](https://github.com/Notallthatevil/NTComponents/compare/v1.30.0...v1.31.0) (2026-03-12)


### Features

* **input-textarea:** add SizeByContent support ([20addce](https://github.com/Notallthatevil/NTComponents/commit/20addce9541f769018426ea907587e28faa70f0c))

# [1.30.0](https://github.com/Notallthatevil/NTComponents/compare/v1.29.2...v1.30.0) (2026-03-11)


### Features

* **toast:** improve SSR close button and toast tracking ([d8d10f6](https://github.com/Notallthatevil/NTComponents/commit/d8d10f66cb0ad682e9b6a9c6e0564ae32c94578c))

## [1.29.2](https://github.com/Notallthatevil/NTComponents/compare/v1.29.1...v1.29.2) (2026-03-10)


### Bug Fixes

* **toast:** add SSR support with auto-dismiss script ([4df3951](https://github.com/Notallthatevil/NTComponents/commit/4df39515a29e81d69d2d9b6f9ae6cd3b8f38e2b6))
* **virtualize:** add tests and improve scroll metrics ([88f559a](https://github.com/Notallthatevil/NTComponents/commit/88f559a6545004eacd310c0d9f0540eccce37e50))

## [1.29.1](https://github.com/Notallthatevil/NTComponents/compare/v1.29.0...v1.29.1) (2026-03-06)


### Bug Fixes

* **TnTLayout:** remove global scrollbar styling ([905d955](https://github.com/Notallthatevil/NTComponents/commit/905d955be588ca95e679dc495d76544c3c7ed48f))

# [1.29.0](https://github.com/Notallthatevil/NTComponents/compare/v1.28.0...v1.29.0) (2026-03-06)


### Bug Fixes

* **datagrid:** Fixed issue with data grid not showing updates immediately ([b34133c](https://github.com/Notallthatevil/NTComponents/commit/b34133c857a246ed654178941fdc81573470bf4f))
* **progress-indicator:** Fixed misnamed style ([89d547a](https://github.com/Notallthatevil/NTComponents/commit/89d547a0b0b04176598e8d921cd870e66f125aac))


### Features

* **generator:** add Roslyn code documentation generator ([4cb9865](https://github.com/Notallthatevil/NTComponents/commit/4cb98654fe02fa2f418df116c6ca499d163a9e37))
* **layout:** add custom scrollbar styling to TnTLayout ([cef2d46](https://github.com/Notallthatevil/NTComponents/commit/cef2d46c4556d6b3d4f92624a04dcc4c4300e32d))

# [1.29.0](https://github.com/Notallthatevil/NTComponents/compare/v1.28.0...v1.29.0) (2026-03-06)


### Bug Fixes

* **datagrid:** Fixed issue with data grid not showing updates immediately ([b34133c](https://github.com/Notallthatevil/NTComponents/commit/b34133c857a246ed654178941fdc81573470bf4f))


### Features

* **generator:** add Roslyn code documentation generator ([4cb9865](https://github.com/Notallthatevil/NTComponents/commit/4cb98654fe02fa2f418df116c6ca499d163a9e37))
* **layout:** add custom scrollbar styling to TnTLayout ([cef2d46](https://github.com/Notallthatevil/NTComponents/commit/cef2d46c4556d6b3d4f92624a04dcc4c4300e32d))

# [1.28.0](https://github.com/Notallthatevil/NTComponents/compare/v1.27.0...v1.28.0) (2026-02-26)


### Features

* **input-date-time:** rename TnTInputDateTime to NTInputDateTime ([4e457bb](https://github.com/Notallthatevil/NTComponents/commit/4e457bbd60e8a9b75c463ee59569af88bb0f315c))

# [1.27.0](https://github.com/Notallthatevil/NTComponents/compare/v1.26.0...v1.27.0) (2026-02-26)


### Features

* **button:** improve disabled button styling for clarity ([b077da4](https://github.com/Notallthatevil/NTComponents/commit/b077da412d9433fcc4717f46b9d05dbd73921f55))
* **form:** add Material-style picker to InputDateTime ([94e1bbe](https://github.com/Notallthatevil/NTComponents/commit/94e1bbef1638e8101bfb37231ece58423d50e662))
* **input-file:** add icon and tooltip support to NTInputFile ([6b2e6e7](https://github.com/Notallthatevil/NTComponents/commit/6b2e6e7b18cf92b4d98471c2243688392ec2ceef))
* **input:** add FlipAndSpan layout for checkbox/switch ([fcadcaf](https://github.com/Notallthatevil/NTComponents/commit/fcadcaf58fd55f7df431e1a356b49a317dbdd68a))
* **input:** add SupportingTextColor parameter ([058bc8d](https://github.com/Notallthatevil/NTComponents/commit/058bc8dea6e141f71aabd1999627256326cf9854))

# [1.26.0](https://github.com/Notallthatevil/NTComponents/compare/v1.25.1...v1.26.0) (2026-02-25)


### Features

* **inputfile:** add NTInputFile component and demos ([bd17334](https://github.com/Notallthatevil/NTComponents/commit/bd1733440cf574f0f1ce6e9288d28ae7b8d08b16))
* **styles:** improve button group focus and active states ([997a72b](https://github.com/Notallthatevil/NTComponents/commit/997a72b91fbf97c7d290977378ba0b3ad3a7bb78))

## [1.25.1](https://github.com/Notallthatevil/NTComponents/compare/v1.25.0...v1.25.1) (2026-02-25)


### Bug Fixes

* **tooltip:** correct pointer direction tests and add DOM guard ([32974b3](https://github.com/Notallthatevil/NTComponents/commit/32974b3e8996ff75b7ad8c71dbdd91270f67fda7))

# [1.25.0](https://github.com/Notallthatevil/NTComponents/compare/v1.24.2...v1.25.0) (2026-02-24)


### Features

* **typeahead:** add JS overlay for dropdown positioning ([7a1ad40](https://github.com/Notallthatevil/NTComponents/commit/7a1ad40f486b1615313a34f9c8e91693b423196b))
* **wizard:** add visited step navigation and invalid form button modes ([bc6e6c3](https://github.com/Notallthatevil/NTComponents/commit/bc6e6c3b7deefe88b9056c413909a8ed6690a06e))

## [1.24.2](https://github.com/Notallthatevil/NTComponents/compare/v1.24.1...v1.24.2) (2026-02-21)


### Bug Fixes

* update column cell class and attribute tests ([35f0fb8](https://github.com/Notallthatevil/NTComponents/commit/35f0fb883b87e427759ebc9dc5b8f9971afa17a8))

## [1.24.1](https://github.com/Notallthatevil/NTComponents/compare/v1.24.0...v1.24.1) (2026-02-20)


### Bug Fixes

* **grid:** add flexible cell text alignment and styling ([0d5cbfd](https://github.com/Notallthatevil/NTComponents/commit/0d5cbfd5b4343471fe1814bca7508869850dc502))

# [1.24.0](https://github.com/Notallthatevil/NTComponents/compare/v1.23.1...v1.24.0) (2026-02-20)


### Features

* **datagrid:** wrap cell content in container div ([bd0b181](https://github.com/Notallthatevil/NTComponents/commit/bd0b181a273dcb27660c4a23a256ed2100f7eda4))

## [1.23.1](https://github.com/Notallthatevil/NTComponents/compare/v1.23.0...v1.23.1) (2026-02-20)


### Bug Fixes

* **ci:** add dotnet workload restore to NuGet deploy ([1a45329](https://github.com/Notallthatevil/NTComponents/commit/1a4532993d82eefb87fe441eb6ab7ab97d03dd02))

# [1.23.0](https://github.com/Notallthatevil/NTComponents/compare/v1.22.0...v1.23.0) (2026-02-20)


### Features

* **tabview:** add alignment and header gap customization ([8c49779](https://github.com/Notallthatevil/NTComponents/commit/8c49779d9f867c8373cb9da5f8d37879295b448b))

# [1.22.0](https://github.com/Notallthatevil/NTComponents/compare/v1.21.0...v1.22.0) (2026-02-20)


### Features

* add line graph and tree map pages with interactive features ([91c8a41](https://github.com/Notallthatevil/NTComponents/commit/91c8a41dfac1e5ba4198713b99aff545aa9a758c))
* add unit tests for NTChart and NTChartAnnotation components ([3dd237b](https://github.com/Notallthatevil/NTComponents/commit/3dd237bb465fec7b004d8a49330f4a514b967e5c))

# [1.21.0](https://github.com/Notallthatevil/NTComponents/compare/v1.20.4...v1.21.0) (2026-02-14)


### Features

* **markdown-editor:** add form validation and UX improvements ([4187d2c](https://github.com/Notallthatevil/NTComponents/commit/4187d2c8f865ad6837e4b3892f63aaa8c16dac96))

## [1.20.4](https://github.com/Notallthatevil/NTComponents/compare/v1.20.3...v1.20.4) (2026-02-13)


### Bug Fixes

* **tests:** update key event handlers to use onkeydown for currency and text inputs ([a240250](https://github.com/Notallthatevil/NTComponents/commit/a240250baf2c6dbf174957f03c28d49ff867daad))

## [1.20.3](https://github.com/Notallthatevil/NTComponents/compare/v1.20.2...v1.20.3) (2026-02-13)


### Bug Fixes

* **style:** adjust progress bar top spacing in TnTTypeahead ([ca5adf7](https://github.com/Notallthatevil/NTComponents/commit/ca5adf7cbeea57ddff85288cd02fa46ce28b511f))

## [1.20.2](https://github.com/Notallthatevil/NTComponents/compare/v1.20.1...v1.20.2) (2026-02-13)


### Bug Fixes

* **input:** switch key event handlers to onkeydown ([9b19ce4](https://github.com/Notallthatevil/NTComponents/commit/9b19ce4bb59d204fa83956fa1b8c20e01e01b304))

## [1.20.1](https://github.com/Notallthatevil/NTComponents/compare/v1.20.0...v1.20.1) (2026-02-13)


### Bug Fixes

* **typeahead:** prevent Enter from submitting forms ([186ce8f](https://github.com/Notallthatevil/NTComponents/commit/186ce8fb046475cd1557cd7c338d44765746ea4b))

# [1.20.0](https://github.com/Notallthatevil/NTComponents/compare/v1.19.0...v1.20.0) (2026-02-12)


### Bug Fixes

* **enum:** add missing comma in TnTColor enum ([13792c0](https://github.com/Notallthatevil/NTComponents/commit/13792c0c5eb74f61c1e2f7e18ff0d1dc22c1b819))


### Features

* **dialog:** refactor dialog trigger and header layout ([dc7c251](https://github.com/Notallthatevil/NTComponents/commit/dc7c251dd3f5fb140ee61847e6d8cfeebe8068f1))
* **typeahead:** improve type safety and binding ([b96eb18](https://github.com/Notallthatevil/NTComponents/commit/b96eb18e036cea06dd6049ec6ac7bd4786fab0f2))

# [1.19.0](https://github.com/Notallthatevil/NTComponents/compare/v1.18.0...v1.19.0) (2026-02-12)


### Features

* **TnTColor:** add assert-related color values to enum ([54995cb](https://github.com/Notallthatevil/NTComponents/commit/54995cbb323088d6f0114db179dca95609685e71))

# [1.18.0](https://github.com/Notallthatevil/NTComponents/compare/v1.17.1...v1.18.0) (2026-02-12)


### Features

* **input:** prevent Enter key propagation in inputs ([316fdd5](https://github.com/Notallthatevil/NTComponents/commit/316fdd563c8b30970512104918aab70e60f72b6f))
* **typeahead:** add Blazor form validation support ([ea5be23](https://github.com/Notallthatevil/NTComponents/commit/ea5be2380c0acbd1363cfc35eb0ff42a1e737222))

## [1.17.1](https://github.com/Notallthatevil/NTComponents/compare/v1.17.0...v1.17.1) (2026-02-09)


### Bug Fixes

* **TnTInputSelect:** remove margin-top from label on select state ([9f13beb](https://github.com/Notallthatevil/NTComponents/commit/9f13beb14dad03b0ca21f2faff66d52da44c7432))

# [1.17.0](https://github.com/Notallthatevil/NTComponents/compare/v1.16.2...v1.17.0) (2026-02-09)


### Features

* **NTButtonGroup:** add Blazor form validation support ([c4527d5](https://github.com/Notallthatevil/NTComponents/commit/c4527d54e9d127ae74928cfccf9195a00fe3094b))

## [1.16.2](https://github.com/Notallthatevil/NTComponents/compare/v1.16.1...v1.16.2) (2026-02-06)


### Bug Fixes

* **button-group:** update icon and label rendering ([af0b6a6](https://github.com/Notallthatevil/NTComponents/commit/af0b6a627c2f30f209505b1e57f9037b2652051f))

# [1.16.0](https://github.com/Notallthatevil/NTComponents/compare/v1.15.0...v1.16.0) (2026-02-05)


### Features

* **button-group:** add NTButtonGroup component and demo ([5e4a856](https://github.com/Notallthatevil/NTComponents/commit/5e4a856c5f4520d8dbc36078533012d817592dae))
* **styles:** add label top positioning and dual icon grid ([890dac7](https://github.com/Notallthatevil/NTComponents/commit/890dac79d84b446b99c0e66dbbdf63d62c06e2a4))
* **wizard:**  limit step width ([7cc0f56](https://github.com/Notallthatevil/NTComponents/commit/7cc0f56ba6f5b7f802f7525cfba97301b422d891))
* **wizard:** add VerticalOnSmallScreens for responsive layout ([11c6a0d](https://github.com/Notallthatevil/NTComponents/commit/11c6a0daccd99b45e22b961ff535eb5ded6486d1))

# [1.15.0](https://github.com/Notallthatevil/NTComponents/compare/v1.14.1...v1.15.0) (2026-02-03)


### Bug Fixes

* **typeahead:** fixed default width of typeahead ([b2b3ef8](https://github.com/Notallthatevil/NTComponents/commit/b2b3ef840dafac673f02735effa17949dfee9d2d))


### Features

* **skeleton:** add FillHeight param to support full height ([04ac78f](https://github.com/Notallthatevil/NTComponents/commit/04ac78ffd9eab166958199a953f959b6f7f6662a))
* **styles:** add compact mode for TnTInputSelect ([26f3530](https://github.com/Notallthatevil/NTComponents/commit/26f3530eaa3cc40084552d0182eb360fca54e110))
* **typeahead:** add error, supporting text, and tooltip ([aa1ca26](https://github.com/Notallthatevil/NTComponents/commit/aa1ca2666f58cdc62fc7091bcbb2333dad288265))
* **wizard:** introduce ITnTWizardFormStep interface ([7e37d70](https://github.com/Notallthatevil/NTComponents/commit/7e37d705f76d9c62207ebe05ed3ec3c146812bae))

## [1.14.1](https://github.com/Notallthatevil/NTComponents/compare/v1.14.0...v1.14.1) (2026-02-03)


### Bug Fixes

* **radio:** improve checked logic and fix onblur quote ([af065ce](https://github.com/Notallthatevil/NTComponents/commit/af065ce811f19077d2575e8de942f7aca8731c34))

# [1.14.0](https://github.com/Notallthatevil/NTComponents/compare/v1.13.0...v1.14.0) (2026-02-02)


### Features

* **radio-group:** improve focus and int/enum support ([7760506](https://github.com/Notallthatevil/NTComponents/commit/7760506bd92f7471642eca25c07e1fcc343caaa2))

# [1.13.0](https://github.com/Notallthatevil/NTComponents/compare/v1.12.0...v1.13.0) (2026-02-02)


### Features

* **radio-group:** add keyboard navigation support ([9c8923c](https://github.com/Notallthatevil/NTComponents/commit/9c8923cfbe41a74704f052777985fdcb5e39f39c))

# [1.12.0](https://github.com/Notallthatevil/NTComponents/compare/v1.11.1...v1.12.0) (2026-02-01)


### Features

* **virualization:** Added better virtualization logic. ([1445aaa](https://github.com/Notallthatevil/NTComponents/commit/1445aaa5964cc0151babc06d169b84c56ee9f2b2))

## [1.11.1](https://github.com/Notallthatevil/NTComponents/compare/v1.11.0...v1.11.1) (2026-01-28)


### Bug Fixes

* **layout:** improve side nav toggle and layout handling ([bc379e0](https://github.com/Notallthatevil/NTComponents/commit/bc379e010b8d30f7d14c263b5ca4a247a7385463))
* **LineChartDemo:** add axis-specific pan and zoom controls ([3eef616](https://github.com/Notallthatevil/NTComponents/commit/3eef616b963bfd6df47f33b4ba5f68bd2b0aaf6c))

# [1.11.0](https://github.com/Notallthatevil/NTComponents/compare/v1.10.0...v1.11.0) (2026-01-24)


### Features

* **styles:** add theme color variables and base app styles ([46f921d](https://github.com/Notallthatevil/NTComponents/commit/46f921d2e23019846fa193e7d5e1276e0ef83d9b)), closes [hi#contrast](https://github.com/hi/issues/contrast)

# [1.10.0](https://github.com/Notallthatevil/NTComponents/compare/v1.9.0...v1.10.0) (2026-01-24)


### Features

* **theme:** add theme change event and .NET interop ([0e6b767](https://github.com/Notallthatevil/NTComponents/commit/0e6b7674bf563b3ba4f9ac4319e46278e9779a98))

# [1.9.0](https://github.com/Notallthatevil/NTComponents/compare/v1.8.1...v1.9.0) (2026-01-21)


### Features

* **form:** add novalidate logic and TnTForm tests ([44cca9c](https://github.com/Notallthatevil/NTComponents/commit/44cca9cd556feaa645c4592858cc75e62c885b2e))

## [1.8.1](https://github.com/Notallthatevil/NTComponents/compare/v1.8.0...v1.8.1) (2026-01-21)


### Bug Fixes

* **input:** standardize margin-top to 6px for inputs ([d7d1458](https://github.com/Notallthatevil/NTComponents/commit/d7d1458cfb0de79f83a79892dac4209a69a49d9c))

# [1.8.0](https://github.com/Notallthatevil/NTComponents/compare/v1.7.5...v1.8.0) (2026-01-19)


### Features

* **external-click:** improve detection logic and add demo ([2fd7c21](https://github.com/Notallthatevil/NTComponents/commit/2fd7c21de735945183065c8004d1a64b3729b119))

## [1.7.5](https://github.com/Notallthatevil/NTComponents/compare/v1.7.4...v1.7.5) (2026-01-19)


### Bug Fixes

* **styles:** add margin when label is floated in inputs ([14ca53b](https://github.com/Notallthatevil/NTComponents/commit/14ca53b55d0eba75cb1d7e348a2557a9f6927421))

## [1.7.4](https://github.com/Notallthatevil/NTComponents/compare/v1.7.3...v1.7.4) (2026-01-19)


### Bug Fixes

* **style:** add consistent margin-bottom to containers ([47fad7a](https://github.com/Notallthatevil/NTComponents/commit/47fad7aa0af336dbb46c841d3c8dd433c5022a79))

## [1.7.3](https://github.com/Notallthatevil/NTComponents/compare/v1.7.2...v1.7.3) (2026-01-19)


### Bug Fixes

* **styles:** improve input container grid responsiveness ([f426621](https://github.com/Notallthatevil/NTComponents/commit/f426621349e0d383c2bda0fa8d32dca13da97f3e))

## [1.7.2](https://github.com/Notallthatevil/NTComponents/compare/v1.7.1...v1.7.2) (2026-01-19)


### Bug Fixes

* **form,inputs:** add full-row layout and improve spacing ([7d9dae2](https://github.com/Notallthatevil/NTComponents/commit/7d9dae21f9713975afd421672e70cf374df035c4))

## [1.7.1](https://github.com/Notallthatevil/NTComponents/compare/v1.7.0...v1.7.1) (2026-01-19)


### Bug Fixes

* **input:** Fixed broken tests ([19d75a9](https://github.com/Notallthatevil/NTComponents/commit/19d75a9d97b5e50b7e088ecd6da920f7488ae982))

# [1.7.0](https://github.com/Notallthatevil/NTComponents/compare/v1.6.0...v1.7.0) (2026-01-16)


### Features

* **input:** refactor to grid-based input container ([a1a79ef](https://github.com/Notallthatevil/NTComponents/commit/a1a79efa8f1e9f35a38e94e6660ac5f32af98f21))

# [1.6.0](https://github.com/Notallthatevil/NTComponents/compare/v1.5.5...v1.6.0) (2026-01-15)


### Features

* **input:** add ShowInputLength parameter for input length ([fe1179f](https://github.com/Notallthatevil/NTComponents/commit/fe1179fac76d4c3cbc0720bef088a72d099335df))

## [1.5.5](https://github.com/Notallthatevil/NTComponents/compare/v1.5.4...v1.5.5) (2026-01-09)


### Bug Fixes

* **ci:** merge pack and publish NuGet jobs in workflow ([e5b2ade](https://github.com/Notallthatevil/NTComponents/commit/e5b2ade6a66cdf94e713afa5ac602380a93b6467))

## [1.5.4](https://github.com/Notallthatevil/NTComponents/compare/v1.5.3...v1.5.4) (2026-01-09)


### Bug Fixes

* **ci:** add Node.js install and test steps to workflow ([638e39e](https://github.com/Notallthatevil/NTComponents/commit/638e39e8260f8436698d3099b90ff6e69efb9012))

## [1.5.3](https://github.com/Notallthatevil/NTComponents/compare/v1.5.2...v1.5.3) (2026-01-09)


### Bug Fixes

* **ci:** update workflow name and trigger conditions ([160d139](https://github.com/Notallthatevil/NTComponents/commit/160d139aecde680755073ef28bade04add1afe37))

## [1.5.2](https://github.com/Notallthatevil/NTComponents/compare/v1.5.1...v1.5.2) (2026-01-09)


### Bug Fixes

* **ci:** Updated search folder for playwright setup ([9aae1de](https://github.com/Notallthatevil/NTComponents/commit/9aae1de9783e6de7a6db28b824fdec7341865daf))

## [1.5.1](https://github.com/Notallthatevil/NTComponents/compare/v1.5.0...v1.5.1) (2026-01-09)


### Bug Fixes

* **ci:** Added playwright dependency to ci/cd ([13cc70b](https://github.com/Notallthatevil/NTComponents/commit/13cc70bd294bc4735e559223645ac67547fb4620))

# [1.5.0](https://github.com/Notallthatevil/NTComponents/compare/v1.4.0...v1.5.0) (2026-01-09)


### Features

* **form:** Added compact form layout ([e7b5198](https://github.com/Notallthatevil/NTComponents/commit/e7b51987b3403a7e112a39d2a2c984d4be6aa9f3))

# [1.4.0](https://github.com/Notallthatevil/NTComponents/compare/v1.3.0...v1.4.0) (2026-01-08)


### Features

* **fab:** add FAB container with layout and expansion ([1977272](https://github.com/Notallthatevil/NTComponents/commit/1977272254a073f572e5de6b0d365ce13e33cbd0))

# [1.3.0](https://github.com/Notallthatevil/NTComponents/compare/v1.2.22...v1.3.0) (2026-01-05)


### Features

* **ci:** add Node.js test steps to workflows ([46ba210](https://github.com/Notallthatevil/NTComponents/commit/46ba21007ce9d78c0bc084c3fa5fe1ab82251767))
* **ci:** modularize workflows for PRs, tagging, and NuGet ([a1e0b20](https://github.com/Notallthatevil/NTComponents/commit/a1e0b20ad2cd1fe727d853313672d9890b292cba))
* **workflow:** add Node.js and Playwright setup steps ([6b94da6](https://github.com/Notallthatevil/NTComponents/commit/6b94da68bea8d89acadddfb63f75254de3a0ad57))

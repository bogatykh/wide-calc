# Changelog

## [1.0.1](https://github.com/bogatykh/wide-calc/compare/v1.0.0...v1.0.1) (2026-05-10)


### Bug Fixes

* use integer star widths for WinUI DataGrid columns ([59b3684](https://github.com/bogatykh/wide-calc/commit/59b3684bfd7d117fb5129a77eb55bbd27cba8459))
* use integer star widths for WinUI DataGrid columns ([e23ca3b](https://github.com/bogatykh/wide-calc/commit/e23ca3b7b18797560e6ec2449671b7b1bf668168))

## [1.0.0](https://github.com/bogatykh/wide-calc/compare/v0.5.5...v1.0.0) (2026-05-09)


### ⚠ BREAKING CHANGES

* remove CSV/XLSX export and improve format breakdown

### Features

* remove CSV/XLSX export and improve format breakdown ([7863eaa](https://github.com/bogatykh/wide-calc/commit/7863eaa37dd5a4d2f7be78358927e9280b37132a))

## [0.5.5](https://github.com/bogatykh/wide-calc/compare/v0.5.4...v0.5.5) (2026-05-09)


### Bug Fixes

* **winui:** force AppxGeneratePriEnabled and append app PRI to publish list ([b41e101](https://github.com/bogatykh/wide-calc/commit/b41e101ce3cac49a62761e950e87a3c1b30bd1d2))

## [0.5.4](https://github.com/bogatykh/wide-calc/compare/v0.5.3...v0.5.4) (2026-05-08)


### Bug Fixes

* **winui:** revert incorrect PriIndexName override; log UI XAML failures to file ([aecb7f8](https://github.com/bogatykh/wide-calc/commit/aecb7f8845ed8c680554336861bfa4e2fbcad80f))

## [0.5.3](https://github.com/bogatykh/wide-calc/compare/v0.5.2...v0.5.3) (2026-05-08)


### Bug Fixes

* **winui:** restore PriIndexName for self-contained PRI/XAML load; si… ([f0a682e](https://github.com/bogatykh/wide-calc/commit/f0a682e7683ab463019ecc7e7813d239a94ba3eb))
* **winui:** restore PriIndexName for self-contained PRI/XAML load; simplify CI publish ([442cd00](https://github.com/bogatykh/wide-calc/commit/442cd0077806c09baceef882a66331f546099a8b))

## [0.5.2](https://github.com/bogatykh/wide-calc/compare/v0.5.1...v0.5.2) (2026-05-08)


### Bug Fixes

* **winui:** allow unsafe blocks for LibraryImport MessageBox P/Invoke ([72ede45](https://github.com/bogatykh/wide-calc/commit/72ede45e85e332eb98dc899b980764b2529ac960))
* **winui:** disable XAML-generated Program to fix CS0101 duplicate class ([33322ef](https://github.com/bogatykh/wide-calc/commit/33322efb0ab12cc2744b5e8e46a6d953ac894bd4))
* **winui:** fix Program bootstrap types and Application.Start discard clash ([9fb494d](https://github.com/bogatykh/wide-calc/commit/9fb494da44f9b149158f9c9ff1b9813da3fe0263))
* **winui:** replace silent bootstrap Exit with handled TryInitialize before Main ([03ecfaa](https://github.com/bogatykh/wide-calc/commit/03ecfaaa1ed59de56598fac89b67932ffa43e568))
* **winui:** restore explicit Program.Main with DispatcherQueue sync context ([a71cdc1](https://github.com/bogatykh/wide-calc/commit/a71cdc13321178b236e607a4aca8894a764241fa))

## [0.5.1](https://github.com/bogatykh/wide-calc/compare/v0.5.0...v0.5.1) (2026-05-08)


### Bug Fixes

* **ci:** point PublishDir at workspace artifacts for Inno and zip ([c26605b](https://github.com/bogatykh/wide-calc/commit/c26605b091bf0a2eae4f972352b2963f30f519b9))

## [0.5.0](https://github.com/bogatykh/wide-calc/compare/v0.4.0...v0.5.0) (2026-05-08)


### Features

* **winui:** redesign main workspace UX and format controls ([1707e86](https://github.com/bogatykh/wide-calc/commit/1707e868ab9de913b44e41864f3082969848e125))
* **winui:** redesign main workspace UX and format controls ([18a9ba9](https://github.com/bogatykh/wide-calc/commit/18a9ba9ab165ca8cad7f725ebf67b669a787a4e2))


### Bug Fixes

* **winui:** remove unsupported XAML members for CI build ([72b242b](https://github.com/bogatykh/wide-calc/commit/72b242bff86f46a3e922ddaf72799c4fe8e41aed))
* **winui:** use Windows.UI.Color in format brush converter ([aafc9e2](https://github.com/bogatykh/wide-calc/commit/aafc9e2ec3f94bb3191289bd74eb5444d054c492))

## [0.4.0](https://github.com/bogatykh/wide-calc/compare/v0.3.0...v0.4.0) (2026-05-08)


### Features

* **dotnet:** migrate solution and workflows to .NET 10 ([961be13](https://github.com/bogatykh/wide-calc/commit/961be13ec02ee9f25422cab3475072a99d60484d))
* migrate desktop app to WinUI 3 and refresh UI ([f779104](https://github.com/bogatykh/wide-calc/commit/f77910480ca4dc5b7f198f38a0d70b022d0e6821))
* restore CommunityToolkit WinUI DataGrid for results ([ffd68ec](https://github.com/bogatykh/wide-calc/commit/ffd68eccfbb5d8dbb1e01a07a6fa6e0011d5c5d1))


### Bug Fixes

* **build:** minimal WinUI XAML to bisect XamlCompiler CI failure ([2c42e55](https://github.com/bogatykh/wide-calc/commit/2c42e5531fbe8962e7476d77335f037eeb65fe15))
* **build:** pin Windows App SDK 1.8 for WinUI toolkit compatibility ([965104d](https://github.com/bogatykh/wide-calc/commit/965104d3a2052e7580de1392524d0618f8fb52f1))
* **build:** remove Export reference from WinUI app to slim XAML compile graph ([8b12690](https://github.com/bogatykh/wide-calc/commit/8b1269055c554cf0f80ddf0d0c22e8c3b45261d6))
* **build:** remove Pdf project reference from WinUI compile graph ([55ffecd](https://github.com/bogatykh/wide-calc/commit/55ffecd450578470ddb326cbe8f2d86167286cbc))
* **build:** remove star column grids, inline Results border, pin WinUI 1.8 ([3e7a420](https://github.com/bogatykh/wide-calc/commit/3e7a4206379cd321a1b3120fd1dba1b4516f04f6))
* **build:** retarget WinUI app and libs to .NET 8 for CI XAML toolchain ([ece4562](https://github.com/bogatykh/wide-calc/commit/ece45626f06202567617fee46d1cb2aa5ae7c76c))
* **build:** run WinUI XAML compiler in-proc on CI ([76a3ac3](https://github.com/bogatykh/wide-calc/commit/76a3ac30129d87afb94cfd7106ad1de169102f6e))
* **build:** use WinApp SDK 1.6 and windows-2022 for XamlCompiler CI ([701de63](https://github.com/bogatykh/wide-calc/commit/701de63faf3d397d5dda48ac3b2cf3e88457e960))
* **build:** WinUI TFM 22621, pin Windows SDK ref, CI uses .NET 9 SDK ([eec06df](https://github.com/bogatykh/wide-calc/commit/eec06dff75e8f90054edb6914fe650151d27c01a))
* **winui:** disable generated app bootstrap Program to avoid Main conflict ([22a133d](https://github.com/bogatykh/wide-calc/commit/22a133d41211078795bf9f0f4e5fe9fd2a2a1136))
* **winui:** escape Binding StringFormat placeholder in MainWindow.xaml ([7f04c93](https://github.com/bogatykh/wide-calc/commit/7f04c93e67efeb1556aa4467b8d973f080085706))
* **winui:** fallback to minimal XAML after pass1 compile failures ([79be4b1](https://github.com/bogatykh/wide-calc/commit/79be4b130ce3207d4c7e2cde2f02eea77ae0e180))
* **winui:** move formatted totals out of XAML; rename row Error binding ([df7e92a](https://github.com/bogatykh/wide-calc/commit/df7e92a020ec09cfb8118620543b99ad309bd69b))
* **winui:** remove manual Program entrypoint to avoid duplicate Main ([6c46701](https://github.com/bogatykh/wide-calc/commit/6c4670157a13e3eafe86e924803843cf5dff25f6))
* **winui:** remove unsupported Window size properties from XAML ([66c7ec9](https://github.com/bogatykh/wide-calc/commit/66c7ec96efbd1d7f7aa81cf1e2e3c9407b3bf20e))
* **winui:** replace invalid ListView.View GridView with ItemTemplate ([39a6cd9](https://github.com/bogatykh/wide-calc/commit/39a6cd9661e06a7833b0cb1c28ebea403a41b4ac))
* **winui:** replace toolkit DataGrid with ListView for CI XAML compile ([d8be343](https://github.com/bogatykh/wide-calc/commit/d8be3438d6b33744c9215fcbc1a918dceeb07a08))
* **winui:** replace unsupported Window MinWidth/MinHeight in XAML ([43a3fe9](https://github.com/bogatykh/wide-calc/commit/43a3fe9b7c71ece87ae579739d89fcdeffb3d22c))
* **winui:** restore full app stack after CI diagnostics ([5abe2fc](https://github.com/bogatykh/wide-calc/commit/5abe2fccf5cd9662136f90994f66d3efb418f81d))
* **winui:** use string overload for filter split in file dialog service ([72981cb](https://github.com/bogatykh/wide-calc/commit/72981cba3a171d0cd7e385023700b32b2d7ddc80))
* **winui:** use string StartsWith for extension prefix check ([5aee335](https://github.com/bogatykh/wide-calc/commit/5aee335fbd9ea1316704ef0114ec9fadf30852d4))
* **winui:** use string StartsWith overload in filter parser ([d685207](https://github.com/bogatykh/wide-calc/commit/d685207aee7b551b075422edad2f49661b1c3b98))

## [0.3.0](https://github.com/bogatykh/wide-calc/compare/v0.2.6...v0.3.0) (2026-05-07)


### Features

* classify wide short sides as A1 and A0 only ([b2f672d](https://github.com/bogatykh/wide-calc/commit/b2f672de9282ecb84e874f56e5d5b2f209779307))
* toggle available print formats for width grouping ([cca1f0a](https://github.com/bogatykh/wide-calc/commit/cca1f0a5488a807d046dbcc5aecc2a0ace0028af))
* добавить прайсовые условные листы A0 (÷1189 мм, A0+A0+) ([9852744](https://github.com/bogatykh/wide-calc/commit/98527441efdfab327034214731f36475bca6f50b))
* улучшить UX выбора файлов, форматов и расчёта метража ([630a452](https://github.com/bogatykh/wide-calc/commit/630a45218a4af05f8a7e9e0557be23d7054d0034))


### Bug Fixes

* allow manual asset backfill for existing tags ([4115d45](https://github.com/bogatykh/wide-calc/commit/4115d45ab5235317508c3de36b3c58aab55466a1))
* allow manual asset backfill for existing tags ([65c4a70](https://github.com/bogatykh/wide-calc/commit/65c4a709ee69c1d101b988ccc8780ac71c9112d8))
* auto-resolve release tag for asset publishing ([c5b6c6a](https://github.com/bogatykh/wide-calc/commit/c5b6c6a2f6c436b2838d8ec3cd383f1d282693e3))
* auto-resolve release tag for asset publishing ([b6e00de](https://github.com/bogatykh/wide-calc/commit/b6e00def0a5a9ce96c06a3dda29f848acdf95ef8))
* **ci:** allow release-please to tag releases and clear pending labels ([4835352](https://github.com/bogatykh/wide-calc/commit/4835352e48e7df124941d612a7a9a4c356472aa6))
* **ci:** allow release-please to tag releases and clear pending labels ([0ff9e9e](https://github.com/bogatykh/wide-calc/commit/0ff9e9e77a756d4f96347e7cba35df13afd861f6))
* **ci:** default GITHUB_TOKEN for release-please, add unstick workflow ([c5d9645](https://github.com/bogatykh/wide-calc/commit/c5d9645092a0e1e8d2f43da9c329e3508145f6db))
* **ci:** default GITHUB_TOKEN for release-please, add unstick workflow ([4a8ef52](https://github.com/bogatykh/wide-calc/commit/4a8ef52ac7166dbc8813b0eff02e5768c5bdff45))
* **ci:** document release 403 and tighten release-please token usage ([bb1cfd3](https://github.com/bogatykh/wide-calc/commit/bb1cfd3517876d84345b16635902e2be2204f0eb))
* **ci:** document release 403 and tighten release-please token usage ([eba19de](https://github.com/bogatykh/wide-calc/commit/eba19de6cbbb20170768d4f2d509312613569f75))
* force release-please target branch to main ([6198360](https://github.com/bogatykh/wide-calc/commit/6198360e52c95dd89a4f183ccd2cc6f55ef2abc5))
* force release-please target branch to main ([299c466](https://github.com/bogatykh/wide-calc/commit/299c4661f2d49a986cce2161091bde49a3639928))
* group incoming widths into supported print formats ([7385d6e](https://github.com/bogatykh/wide-calc/commit/7385d6e973d130d6b715a925c6e6f2a8c4d91e0f))
* trigger asset job on releases_created output ([7157939](https://github.com/bogatykh/wide-calc/commit/7157939d05958c260b1d425f1ac06dd0f0db962d))
* trigger asset job on releases_created output ([9f4c25b](https://github.com/bogatykh/wide-calc/commit/9f4c25b9849d4c2ac76c47ccc1786f1d05186e39))
* накапливать файлы и строки таблицы при повторном выборе PDF ([a952045](https://github.com/bogatykh/wide-calc/commit/a952045fd5016da2f5cd6e53200ec764853dc89e))

## [0.2.6](https://github.com/bogatykh/wide-calc/compare/v0.2.5...v0.2.6) (2026-05-07)


### Bug Fixes

* create release with assets in single versioning flow ([e02b079](https://github.com/bogatykh/wide-calc/commit/e02b079e5072a56d7742dc65cb465457c39e175f))
* create release with assets in single versioning flow ([e7d4d60](https://github.com/bogatykh/wide-calc/commit/e7d4d60a6a9e15f584f2440c7ea51a8d73118237))

## [0.2.5](https://github.com/bogatykh/wide-calc/compare/v0.2.4...v0.2.5) (2026-05-07)


### Bug Fixes

* restore with PublishReadyToRun for publish pipeline ([2e83a8b](https://github.com/bogatykh/wide-calc/commit/2e83a8bc108164a1927942b9a091f2a25d20f213))
* restore with PublishReadyToRun for publish pipeline ([97d5913](https://github.com/bogatykh/wide-calc/commit/97d5913675b46f62d60b0a0f63553b7f3b69b19d))

## [0.2.4](https://github.com/bogatykh/wide-calc/compare/v0.2.3...v0.2.4) (2026-05-07)


### Bug Fixes

* avoid solution RID build and restore app RID separately ([fff43fc](https://github.com/bogatykh/wide-calc/commit/fff43fc1701ab1e74d08b8a309e020e4c985a2b0))
* avoid solution RID build and restore app RID separately ([9c41d82](https://github.com/bogatykh/wide-calc/commit/9c41d82d5856a2e355ea75e0bead8b7423b650ed))

## [0.2.3](https://github.com/bogatykh/wide-calc/compare/v0.2.2...v0.2.3) (2026-05-07)


### Bug Fixes

* restore and build with win-x64 runtime ([9e2c187](https://github.com/bogatykh/wide-calc/commit/9e2c187665fd0603e4ca9a609f271be27d860355))
* restore and build with win-x64 runtime ([b65a7e3](https://github.com/bogatykh/wide-calc/commit/b65a7e3aa589a096808ff063fbbddafead80959f))

## [0.2.2](https://github.com/bogatykh/wide-calc/compare/v0.2.1...v0.2.2) (2026-05-07)


### Bug Fixes

* publish release assets directly in versioning pipeline ([5586719](https://github.com/bogatykh/wide-calc/commit/5586719047105abe4370557f43ace245eb3c88f7))
* publish release assets directly in versioning pipeline ([f432986](https://github.com/bogatykh/wide-calc/commit/f432986973597b103110d23d726ebaac7bd1082d))

## [0.2.1](https://github.com/bogatykh/wide-calc/compare/v0.2.0...v0.2.1) (2026-05-07)


### Bug Fixes

* attach installer assets reliably to releases ([04c7b36](https://github.com/bogatykh/wide-calc/commit/04c7b367cd901f53885cb7ecbbd2544c45b0e672))
* attach installer assets reliably to releases ([0d5d651](https://github.com/bogatykh/wide-calc/commit/0d5d6514c48c3e3bdbd3287ca459517f45e8b3fc))
* disambiguate win32 file dialog types ([41620eb](https://github.com/bogatykh/wide-calc/commit/41620ebf35e44117bed4faec51fef6d1d54f46c9))
* harden commitlint permissions for PR context ([2df1a15](https://github.com/bogatykh/wide-calc/commit/2df1a15ef6ff07839529ae84b8a10d10a03328ea))

## [0.2.0](https://github.com/bogatykh/wide-calc/compare/v0.1.0...v0.2.0) (2026-05-07)


### Features

* bootstrap PrintMeter app with CI, installer, and auto-versioning ([231c1d5](https://github.com/bogatykh/wide-calc/commit/231c1d532e44d1589c85c0bbb38a4b5de76bd822))


### Bug Fixes

* resolve CI build and test analyzer failures ([a646c94](https://github.com/bogatykh/wide-calc/commit/a646c94089a42d3bd3c44508701da0037c24fbd5))
* stabilize commitlint and release-please auth ([f7fbf58](https://github.com/bogatykh/wide-calc/commit/f7fbf58b2ec6e23d895e002be04acf1d6f02f1c6))

## Changelog

All notable changes to this project will be documented in this file.

This file is managed by Release Please.

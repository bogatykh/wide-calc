# Проверка точности PrintMeter

## Автоматические проверки (CI)

На каждом PR/push в `main` GitHub Actions на `windows-latest` выполняет:

- `dotnet restore/build/test` для всего решения `PrintMeter.sln`
- `dotnet publish` self-contained single-file `win-x64`

Юнит-тесты фиксируют:

- перевод PDF points → мм (`PdfUnitsTests`)
- распознавание ISO A-форматов с допуском ±2 мм (`Iso216FormatRegistryTests`, константа `MeasurementDefaults.FormatToleranceMm`)
- расчёт длины и агрегацию по файлам/батчу (`PageAnalysisServiceTests`, `BatchPdfAnalyzerTests`)
- экспорт CSV с UTF-8 BOM и разделителем `;` (`CsvBatchReportExporterTests`)
- сценарий ViewModel «выбрать файлы → считать → экспорт» (`MainViewModelTests`)

## Ручная приёмка на Windows (типография)

1. Скачайте артефакт `PrintMeter-win-x64` из последнего успешного CI-run (или соберите локально командой из `README.md`).
2. Запустите `PrintMeter.exe` на машине с Windows 10/11 x64.
3. Выберите 5–10 реальных PDF, которые ранее измеряли вручную в Acrobat.
4. Сравните:
   - суммарную длину по файлу (м)
   - разбиение по форматам

Допуск приёмки (рекомендация из плана): расхождение не более **0.5%** от эталона Acrobat, если эталон однозначно определён (одинаковая трактовка CropBox/MediaBox).

## Примечания по трактовке

- Размер страницы берётся из `PdfPig` (`Page.Width` / `Page.Height`, соответствуют выбранному приложением CropBox/MediaBox).
- «Длина печати» в MVP = \(\max(W_{mm}, H_{mm}) / 1000\) на страницу (см. `MeasurementDefaults.PageLengthMeters`).
- **Прайсовые «условные листы A0»**: суммируются метры по длинной стороне для выбранных ISO-меток (по умолчанию A0 и A0+ из сводки), переводится в суммарные мм, затем делится на знаменатель из настроек (по умолчанию \(1189\) мм — ISO A0) и округляется (`Ceiling` или `Nearest`). Это **отдельно** от фактического числа страниц PDF; параметры — `appsettings`: `PrintMeter:A0Equivalence*`.
- Допуск форматов и округление настраиваются в `src/PrintMeter.Core/MeasurementDefaults.cs` и `appsettings.json` (`PrintMeter:FormatToleranceMm`).

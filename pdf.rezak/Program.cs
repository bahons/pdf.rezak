using System;
using System.IO;
using System.Diagnostics;
using SkiaSharp;
using PDFtoImage;

namespace pdf.rezak
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string pdfPath = "C:/Users/bakyt/Downloads/turk_quran.pdf"; // Путь к вашему PDF
            string outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output_images2");

            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Ошибка: Файл PDF не найден по пути: {pdfPath}");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            else
            {
                try
                {
                    foreach (var file in Directory.GetFiles(outputFolder, "*.png"))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Предупреждение при очистке папки вывода: {ex.Message}");
                }
            }

            Console.WriteLine("Запуск процесса разделения и обрезки PDF...");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                byte[] pdfBytes = File.ReadAllBytes(pdfPath);
                int pageCount = Conversion.GetPageCount(pdfBytes);
                Console.WriteLine($"Всего страниц в PDF: {pageCount}");

                // Опции рендеринга (200 DPI для улучшенного качества текста и оптимального размера файлов)
                int dpi = 200;
                var renderOptions = new RenderOptions
                {
                    Dpi = dpi
                };

                // Для теста обрабатываем только первые 5 страниц
                int pagesToProcess = Math.Min(616, pageCount);
                Console.WriteLine($"Тестовый запуск: обработка первых {pagesToProcess} страниц с DPI {dpi}.");

                for (int i = 0; i < pagesToProcess; i++)
                {
                    var newPageName = "";
                    if(i < 9)
                    {
                        newPageName = $"page00{i + 1}";
                    }
                    else if(i < 99)
                    {
                        newPageName = $"page0{i + 1}";
                    }
                    else
                    {
                        newPageName = $"page{i + 1}";
                    }

                    // Рендерим страницу в SKBitmap
                    using SKBitmap original = Conversion.ToImage(pdfBytes, page: i+1, options: renderOptions);
                    if (original == null)
                    {
                        Console.WriteLine($"Ошибка рендеринга страницы {newPageName}");
                        continue;
                    }

                    // Вычисляем обрезку полей относительно DPI (базовые 30px и 15px заданы для стандартных 72 DPI)
                    //int leftCrop = (int)Math.Round(220.0 * dpi / 72.0);
                    //int rightCrop = leftCrop;
                    //int topCrop = (int)Math.Round(180.0 * dpi / 72.0);
                    //int bottomCrop = topCrop;

                    int leftCrop = 195;
                    int rightCrop = 195;
                    int topCrop = 160;
                    int bottomCrop = 160;

                    int newWidth = original.Width - (leftCrop + rightCrop);
                    int newHeight = original.Height - (topCrop + bottomCrop);

                    if (newWidth <= 0 || newHeight <= 0)
                    {
                        Console.WriteLine($"Страница {newPageName} слишком мала для обрезки ({original.Width}x{original.Height}). Сохранение без обрезки.");
                        string uncroppedPath = Path.Combine(outputFolder, $"{newPageName}.png");
                        using var saveStream = File.OpenWrite(uncroppedPath);
                        original.Encode(saveStream, SKEncodedImageFormat.Png, 100);
                        continue;
                    }

                    // Создаем новый обрезанный SKBitmap
                    using SKBitmap cropped = new SKBitmap(newWidth, newHeight);
                    using (SKCanvas canvas = new SKCanvas(cropped))
                    {
                        // Рисуем исходное изображение со сдвигом с использованием SKSamplingOptions.Default
                        canvas.DrawBitmap(original, -leftCrop, -topCrop, SKSamplingOptions.Default);
                    }

                    // Сохраняем как PNG с максимальным сжатием
                    string outputPath = Path.Combine(outputFolder, $"{newPageName}.png");
                    using (var pixmap = cropped.PeekPixels())
                    {
                        if (pixmap != null)
                        {
                            var pngOptions = new SKPngEncoderOptions(SKPngEncoderFilterFlags.AllFilters, 9);
                            using var data = pixmap.Encode(pngOptions);
                            if (data != null)
                            {
                                using var saveStream = File.OpenWrite(outputPath);
                                data.SaveTo(saveStream);
                            }
                        }
                    }

                    Console.WriteLine($"Обработана страница {newPageName}/{pagesToProcess} -> {outputPath}");
                }

                stopwatch.Stop();
                Console.WriteLine($"\nГотово! Все страницы успешно сохранены в папку: {outputFolder}");
                Console.WriteLine($"Время выполнения: {stopwatch.Elapsed.TotalSeconds:F2} сек.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}







# Парсер изображений
Мне предстояло реализовать утилиту для получения информации об изображении по структуре файла. Изображения были трёх форматов:

1. png
2. bmp
3. gif

На вход был дан Stream файла с изображением, на выходе нужно вывести информацию об изображении в формате JSON:
```
{
    "Height": 0,    // - Высота изображения в пикселях
    "Width": 0,     // - Ширина изображения в пикселях
    "Format": "",   // - Формат изображения
    "Size": 0       // - Размер файла с изображением в байтах
}
```
Формат gif поддерживает хранение нескольких изображений, нужно было вывести информацию только о первом изображении.

Использовать любые библиотеки работы с изображениями было запрещено.
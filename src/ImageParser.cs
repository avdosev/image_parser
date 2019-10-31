using System;
using System.IO;
using Microsoft.CSharp.RuntimeBinder;

namespace ImageParser
{
    public class ImageParser : IImageParser
    {
        public string GetImageInfo(Stream stream) {
            string res;

            // детектируем тип изображения
            var imageType = DetectImageType(stream);
            // создаем объект который все распарсит
            var Image = makeImageObject(imageType, stream);


            string outputPattern = @"""Height"": {1},  ""Width"": {2}, ""Format"": ""{3}"", ""Size"": {0}";
            res = string.Format(outputPattern, Image.Size(), Image.Height(), Image.Width(), Image.Format());

            return "{" + res + "}";
        }

        public enum ImageType {
            png,
            bmp,
            gif,
            none
        }

        public ImageType DetectImageType(Stream stream) {
            ImageType whatIs = ImageType.none;
            
            if (ImagePNG.itsMyFormat(stream)) {
                whatIs = ImageType.png;
            } else if (ImageBMP.itsMyFormat(stream)) {
                whatIs = ImageType.bmp;
            } else if (ImageGIF.itsMyFormat(stream)) {
                whatIs = ImageType.gif;
            } else {
                throw new Exception("тут шото не так");
            }

            return whatIs;
        }

        public ImageObject makeImageObject(ImageType imageType, Stream stream) {
            ImageObject res;

            switch (imageType) {
                case ImageType.png:
                    res = new ImagePNG(stream);
                    break;
                case ImageType.bmp:
                    res = new ImageBMP(stream);
                    break;
                case ImageType.gif:
                    res = new ImageGIF(stream);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(imageType), imageType, null);
            }

            return res;
        }

    }

    public interface ImageObject {
        long Size();
        uint Height();
        uint Width();
        string Format();
    }

    public class ImagePNG : ImageObject {
        private uint height, width;
        private long size;
        public ImagePNG(Stream stream) {
            stream.Position = 16;
            byte[] bufferWidth = new byte[4];
            byte[] bufferHeight = new byte[4];
            stream.Read(bufferWidth, 0, 4);
            stream.Read(bufferHeight, 0, 4);

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bufferWidth);
                Array.Reverse(bufferHeight);
            }

            width = BitConverter.ToUInt32(bufferWidth, 0);
            height = BitConverter.ToUInt32(bufferHeight, 0);
            size = stream.Length;
        }
        
        public long Size() {
            return size;
        }
        
        public uint Height() {
            return height;
        }
        
        public uint Width() {
            return width;
        }
        
        public string Format() {
            return "png";
        }

        public static bool itsMyFormat(Stream stream) {
            var previousPosition = stream.Position;
            
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            var res = buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;
            stream.Position = previousPosition;
            
            return res;
        }
    }
    
    public class ImageBMP : ImageObject {
        private uint height, width;
        private long size;
        public ImageBMP(Stream stream) {
            stream.Position = 14;

            var bcSizeBuffer = new byte[4];
            stream.Read(bcSizeBuffer, 0, 4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bcSizeBuffer);
            var bcSize = BitConverter.ToUInt32(bcSizeBuffer, 0);
            
            // оу май, бмп записывает данные как DWORD или LONG такшо все что ниже так не спроста
            if (bcSize > 12) {
                byte[] bufferWidth = new byte[4];
                byte[] bufferHeight = new byte[4];
                
                stream.Read(bufferWidth, 0, 4);
                stream.Read(bufferHeight, 0, 4);

                width = BitConverter.ToUInt32(bufferWidth, 0);
                height = BitConverter.ToUInt32(bufferHeight, 0);
            } else {
                byte[] bufferWidth = new byte[2];
                byte[] bufferHeight = new byte[2];
                
                stream.Read(bufferWidth, 0, 2);
                stream.Read(bufferHeight, 0, 2);
                
                width = BitConverter.ToUInt16(bufferWidth, 0);
                height = BitConverter.ToUInt16(bufferHeight, 0);
            }

            size = stream.Length;
        }
        
        public long Size() {
            return size;
        }
        
        public uint Height() {
            return height;
        }
        
        public uint Width() {
            return width;
        }
        
        public string Format() {
            return "bmp";
        }
        
        public static bool itsMyFormat(Stream stream) {
            var previousPosition = stream.Position;
            
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            var res = buffer[0] == 0x42 && buffer[1] == 0x4D;
            stream.Position = previousPosition;
            
            return res;
        }
    }
    
    public class ImageGIF : ImageObject {
        private uint height, width;
        private long size;
        public ImageGIF(Stream stream) {
            
            stream.Position = 10;

            var packedBytes = stream.ReadByte();

            if ((packedBytes & 0b1000000) != 0) {
                
                var sizeGlobalPalette = 1 << ((packedBytes & 0b00000111) + 1);
                // пропускаем глобальную палитру цветов
                stream.Position += sizeGlobalPalette*3;
            }

            stream.Position += 2; // допропускаем байты
            
            var OpCode = stream.ReadByte(); 

            if (OpCode  == 0x21) { // блок расширения
                var blockType = stream.ReadByte();
                while (blockType != 0x2C) {
                    if (blockType == 0xFF) { // расширение программы
                        var indentySize = stream.ReadByte();
                        stream.Position += indentySize;
                        var blockSize = stream.ReadByte();
                        stream.Position += blockSize + 1;
                    } else if (blockType == 0xF9) { //управления графикой
                        var blockSize = stream.ReadByte();
                        stream.Position += blockSize + 1;
                    } /* else {
                        // работает не трож
                        // Console.WriteLine("что то не так");
                        // Console.WriteLine($"{blockType:x8}");
                        // Console.WriteLine($"{stream.Position:x8}");
                    } */
                    
                    blockType = stream.ReadByte();
                }
            } else if (OpCode == 0x2C) { // блок графики
                // просто идем вниз
            }
            stream.Position += 4;
            
            byte[] bufferWidth = new byte[2];
            byte[] bufferHeight = new byte[2];
            stream.Read(bufferWidth, 0, 2);
            stream.Read(bufferHeight, 0, 2);
            

            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bufferWidth);
                Array.Reverse(bufferHeight);
            }

            width = BitConverter.ToUInt16(bufferWidth, 0);
            height = BitConverter.ToUInt16(bufferHeight, 0);
            size = stream.Length;
        }
        
        public long Size() {
            return size;
        }
        
        public uint Height() {
            return height;
        }
        
        public uint Width() {
            return width;
        }
        
        public string Format() {
            return "gif";
        }
        
        public static bool itsMyFormat(Stream stream) {
            var previousPosition = stream.Position;
            
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            var res = buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46;
            stream.Position = previousPosition;
            
            return res;
        }
    }

}
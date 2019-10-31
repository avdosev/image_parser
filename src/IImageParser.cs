using System.IO;

namespace ImageParser
{
    public interface IImageParser
    {
        string GetImageInfo(Stream stream);
    }
}
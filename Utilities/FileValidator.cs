using static System.Net.Mime.MediaTypeNames;

namespace SteganographyWebApp.Utilities
{
    public class FileValidator
    {
        private static readonly Dictionary<string, List<byte[]>> _fileSignature = new Dictionary<string, List<byte[]>>
        {
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".mkv", new List<byte[]> {new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } } }
        };


        public static bool IsValidMediaFile(string fileName, BinaryReader reader)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var signatures = _fileSignature[ext];
            var test = signatures.Max(m => m.Length);

            reader.BaseStream.Position = 0;
            var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));
            return signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
        }
    }
}

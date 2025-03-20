using System.IO;
using System.IO.Compression;

namespace ScriptBoy.DiggableTerrains2D
{
    public static class BinaryCompressionUtility
    {
        public static byte[] Compress(byte[] data, BinaryCompressionMethod method, BinaryCompressionLevel level = BinaryCompressionLevel.Optimal)
        {
            switch (method)
            {
                case BinaryCompressionMethod.Deflate: return Deflate.Compress(data, level);
                case BinaryCompressionMethod.GZip: return GZip.Compress(data, level);
                case BinaryCompressionMethod.Brotli: return Brotli.Compress(data, level);
            }
            return data;
        }

        public static byte[] Decmpress(byte[] data, BinaryCompressionMethod methods)
        {
            switch (methods)
            {
                case BinaryCompressionMethod.Deflate: return Deflate.Decmpress(data);
                case BinaryCompressionMethod.GZip: return GZip.Decmpress(data);
                case BinaryCompressionMethod.Brotli: return Brotli.Decmpress(data);
            }
            return data;
        }

        static class Deflate
        {
            public static byte[] Compress(byte[] data, BinaryCompressionLevel level)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (DeflateStream deflate = new DeflateStream(mem, (System.IO.Compression.CompressionLevel)level))
                    {
                        deflate.Write(data, 0, data.Length);
                    }
                    data = mem.ToArray();
                }

                return data;
            }

            public static byte[] Decmpress(byte[] data)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (DeflateStream deflate = new DeflateStream(stream, CompressionMode.Decompress))
                        {
                            deflate.CopyTo(mem);
                        }
                    }
                    data = mem.ToArray();
                }
                return data;
            }
        }

        static class GZip
        {
            public static byte[] Compress(byte[] data, BinaryCompressionLevel level)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (GZipStream deflate = new GZipStream(mem, (System.IO.Compression.CompressionLevel)5))
                    {
                        deflate.Write(data, 0, data.Length);
                    }
                    data = mem.ToArray();
                }

                return data;
            }

            public static byte[] Decmpress(byte[] data)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (GZipStream deflate = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            deflate.CopyTo(mem);
                        }
                    }
                    data = mem.ToArray();
                }
                return data;
            }
        }

        static class Brotli
        {
            public static byte[] Compress(byte[] data, BinaryCompressionLevel level)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (BrotliStream deflate = new BrotliStream(mem, (System.IO.Compression.CompressionLevel)level))
                    {
                        deflate.Write(data, 0, data.Length);
                    }
                    data = mem.ToArray();
                }

                return data;
            }

            public static byte[] Decmpress(byte[] data)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (BrotliStream deflate = new BrotliStream(stream, CompressionMode.Decompress))
                        {
                            deflate.CopyTo(mem);
                        }
                    }
                    data = mem.ToArray();
                }
                return data;
            }
        }
    }

    public enum BinaryCompressionMethod
    {
        NoCompression, Deflate, GZip, Brotli
    }

    public enum BinaryCompressionLevel
    {
        Optimal, Fastest
    }
}
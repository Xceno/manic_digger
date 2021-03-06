﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel.Design;
using ProtoBuf;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;

namespace ManicDigger
{
    public static class GameStorePath
    {
        public static string GetStorePath()
        {
            string apppath = Path.GetDirectoryName(Application.ExecutablePath);
            string mdfolder = "ManicDiggerUserData";
            if (apppath.Contains(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
            {
                string mdpath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    mdfolder);
                return mdpath;
            }
            else
            {
                //return Path.Combine(apppath, mdfolder);
                return mdfolder;
            }
        }
    }
    public interface IScreenshot
    {
        void SaveScreenshot();
    }
    public class Screenshot : IScreenshot
    {
        [Inject]
        public GameWindow d_GameWindow;
        public string SavePath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public void SaveScreenshot()
        {
            using (Bitmap bmp = GrabScreenshot())
            {
                string time = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
                string filename = Path.Combine(SavePath, time + ".png");
                bmp.Save(filename);
            }
        }
        // Returns a System.Drawing.Bitmap with the contents of the current framebuffer
        public Bitmap GrabScreenshot()
        {
            if (GraphicsContext.CurrentContext == null)
                throw new GraphicsContextMissingException();

            Bitmap bmp = new Bitmap(d_GameWindow.ClientSize.Width, d_GameWindow.ClientSize.Height);
            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(d_GameWindow.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, d_GameWindow.ClientSize.Width, d_GameWindow.ClientSize.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bmp;
        }
    }
    [ProtoContract]
    public class ProtoPoint
    {
        [ProtoMember(1, IsRequired = false)]
        public int X;
        [ProtoMember(2, IsRequired = false)]
        public int Y;
        public ProtoPoint()
        {
        }
        public ProtoPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public ProtoPoint(Point p)
        {
            this.X = p.X;
            this.Y = p.Y;
        }
        public Point ToPoint()
        {
            return new Point(X, Y);
        }
        public override bool Equals(object obj)
        {
            ProtoPoint obj2 = obj as ProtoPoint;
            if (obj2 != null)
            {
                return this.X == obj2.X
                    && this.Y == obj2.Y;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return X ^ Y;
        }
    }
    public struct FastColor
    {
        public FastColor(byte A, byte R, byte G, byte B)
        {
            this.A = A;
            this.R = R;
            this.G = G;
            this.B = B;
        }
        public FastColor(int A, int R, int G, int B)
        {
            this.A = (byte)A;
            this.R = (byte)R;
            this.G = (byte)G;
            this.B = (byte)B;
        }
        public FastColor(Color c)
        {
            this.A = c.A;
            this.R = c.R;
            this.G = c.G;
            this.B = c.B;
        }
        public byte A;
        public byte R;
        public byte G;
        public byte B;
        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }
    }
    public class BitTools
    {
        public static bool IsPowerOfTwo(uint x)
        {
            return (
              x == 1 || x == 2 || x == 4 || x == 8 || x == 16 || x == 32 ||
              x == 64 || x == 128 || x == 256 || x == 512 || x == 1024 ||
              x == 2048 || x == 4096 || x == 8192 || x == 16384 ||
              x == 32768 || x == 65536 || x == 131072 || x == 262144 ||
              x == 524288 || x == 1048576 || x == 2097152 ||
              x == 4194304 || x == 8388608 || x == 16777216 ||
              x == 33554432 || x == 67108864 || x == 134217728 ||
              x == 268435456 || x == 536870912 || x == 1073741824 ||
              x == 2147483648);
        }
        public static uint NextPowerOfTwo(uint x)
        {
            x--;
            x |= x >> 1;  // handle  2 bit numbers
            x |= x >> 2;  // handle  4 bit numbers
            x |= x >> 4;  // handle  8 bit numbers
            x |= x >> 8;  // handle 16 bit numbers
            x |= x >> 16; // handle 32 bit numbers
            x++;
            return x;
        }
    }
    public static class Interpolation
    {
        public static FastColor InterpolateColor(float progress, params FastColor[] colors)
        {
            int colora = (int)((colors.Length - 1) * progress);
            if (colora < 0) { colora = 0; }
            if (colora >= colors.Length) { colora = colors.Length - 1; }
            int colorb = colora + 1;
            if (colorb >= colors.Length) { colorb = colors.Length - 1; }
            FastColor a = colors[colora];
            FastColor b = colors[colorb];
            float p = (progress - (float)colora / (colors.Length - 1)) * (colors.Length - 1);
            int A = (int)(a.A + (b.A - a.A) * p);
            int R = (int)(a.R + (b.R - a.R) * p);
            int G = (int)(a.G + (b.G - a.G) * p);
            int B = (int)(a.B + (b.B - a.B) * p);
            return new FastColor(A, R, G, B);
        }
    }
    public static class VectorTool
    {
        public static Vector3 ToVectorInFixedSystem(float dx, float dy, float dz, double orientationx, double orientationy)
        {
            //Don't calculate for nothing ...
            if (dx == 0.0f & dy == 0.0f && dz == 0.0f)
                return new Vector3();

            //Convert to Radian : 360° = 2PI
            double xRot = orientationx;//Math.toRadians(orientation.X);
            double yRot = orientationy;//Math.toRadians(orientation.Y);

            //Calculate the formula
            float x = (float)(dx * Math.Cos(yRot) + dy * Math.Sin(xRot) * Math.Sin(yRot) - dz * Math.Cos(xRot) * Math.Sin(yRot));
            float y = (float)(+dy * Math.Cos(xRot) + dz * Math.Sin(xRot));
            float z = (float)(dx * Math.Sin(yRot) - dy * Math.Sin(xRot) * Math.Cos(yRot) + dz * Math.Cos(xRot) * Math.Cos(yRot));

            //Return the vector expressed in the global axis system
            return new Vector3(x, y, z);
        }
    }
    public static class MyStream
    {
        public static string[] ReadAllLines(Stream s)
        {
            StreamReader sr = new StreamReader(s);
            List<string> lines = new List<string>();
            for (; ; )
            {
                string line = sr.ReadLine();
                if (line == null)
                {
                    break;
                }
                lines.Add(line);
            }
            return lines.ToArray();
        }
        public static byte[] ReadAllBytes(Stream stream)
        {
            return new BinaryReader(stream).ReadBytes((int)stream.Length);
        }
    }
    public static class MyMath
    {
        public static T Clamp<T>(T value, T min, T max)
            where T : System.IComparable<T>
        {
            T result = value;
            if (value.CompareTo(max) > 0)
                result = max;
            if (value.CompareTo(min) < 0)
                result = min;
            return result;
        }

        public static int Pow3(int n)
        {
            return n * n * n;
        }
    }
    public static class GameVersion
    {
        static string gameversion;
        public static string Version
        {
            get
            {
                if (gameversion == null)
                {
                    gameversion = "unknown";
                    if (File.Exists("version.txt"))
                    {
                        gameversion = File.ReadAllText("version.txt").Trim();
                    }
                }
                return gameversion;
            }
        }
    }
    public interface ICompression
    {
        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] data);
    }
    public class CompressionDummy : ICompression
    {
        #region ICompression Members
        public byte[] Compress(byte[] data)
        {
            return Copy(data);
        }
        public byte[] Decompress(byte[] data)
        {
            return Copy(data);
        }
        private static byte[] Copy(byte[] data)
        {
            byte[] copy = new byte[data.Length];
            Array.Copy(data, copy, data.Length);
            return copy;
        }
        #endregion
    }
    public class CompressionGzip : ICompression
    {
        public byte[] Compress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
            {
                byte[] buffer = new byte[4096];
                int numRead;
                while ((numRead = input.Read(buffer, 0, buffer.Length)) != 0)
                {
                    compress.Write(buffer, 0, numRead);
                }
            }
            return output.ToArray();
        }
        public byte[] Decompress(byte[] fi)
        {
            MemoryStream ms = new MemoryStream();
            // Get the stream of the source file.
            using (MemoryStream inFile = new MemoryStream(fi))
            {
                using (GZipStream Decompress = new GZipStream(inFile,
                        CompressionMode.Decompress))
                {
                    //Copy the decompression stream into the output file.
                    byte[] buffer = new byte[4096];
                    int numRead;
                    while ((numRead = Decompress.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        ms.Write(buffer, 0, numRead);
                    }
                }
            }
            return ms.ToArray();
        }
        public static byte[] Decompress(FileInfo fi)
        {
            MemoryStream ms = new MemoryStream();
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Get original file extension, for example "doc" from report.doc.gz.
                string curFile = fi.FullName;
                string origName = curFile.Remove(curFile.Length - fi.Extension.Length);

                //Create the decompressed file.
                //using (FileStream outFile = File.Create(origName))
                {
                    using (GZipStream Decompress = new GZipStream(inFile,
                            CompressionMode.Decompress))
                    {
                        //Copy the decompression stream into the output file.
                        byte[] buffer = new byte[4096];
                        int numRead;
                        while ((numRead = Decompress.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            ms.Write(buffer, 0, numRead);
                        }
                        //Console.WriteLine("Decompressed: {0}", fi.Name);
                    }
                }
            }
            return ms.ToArray();
        }
    }
    public interface IFastBitmap
    {
        Bitmap bmp { get; set; }
        void Lock();
        void Unlock();
        int GetPixel(int x, int y);
        void SetPixel(int x, int y, int color);
    }
    public class FastBitmapDummy : IFastBitmap
    {
        #region IFastBitmap Members
        public Bitmap bmp { get; set; }
        public void Lock()
        {
        }
        public void Unlock()
        {
        }
        public int GetPixel(int x, int y)
        {
            return bmp.GetPixel(x, y).ToArgb();
        }
        public void SetPixel(int x, int y, int color)
        {
            bmp.SetPixel(x, y, Color.FromArgb(color));
        }
        #endregion
    }
    //Doesn't work on Ubuntu - pointer access crashes.
    public class FastBitmap : IFastBitmap
    {
        public Bitmap bmp { get; set; }
        BitmapData bmd;
        public void Lock()
        {
            if (bmd != null)
            {
                throw new Exception("Already locked.");
            }
            if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                throw new Exception();
            }
            bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
        }
        public int GetPixel(int x, int y)
        {
            if (bmd == null)
            {
                throw new Exception();
            }
            unsafe
            {
                int* row = (int*)((byte*)bmd.Scan0 + (y * bmd.Stride));
                return row[x];
            }
        }
        public void SetPixel(int x, int y, int color)
        {
            if (bmd == null)
            {
                throw new Exception();
            }
            unsafe
            {
                int* row = (int*)((byte*)bmd.Scan0 + (y * bmd.Stride));
                row[x] = color;
            }
        }
        public void Unlock()
        {
            if (bmd == null)
            {
                throw new Exception("Not locked.");
            }
            bmp.UnlockBits(bmd);
            bmd = null;
        }
    }
    public struct Vector2i
    {
        public Vector2i(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public int x;
        public int y;
        public override bool Equals(object obj)
        {
            if (obj is Vector2i)
            {
                Vector2i other = (Vector2i)obj;
                return this.x == other.x && this.y == other.y;
            }
            return base.Equals(obj);
        }
        public static bool operator ==(Vector2i a, Vector2i b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(Vector2i a, Vector2i b)
        {
            return !(a.x == b.x && a.y == b.y);
        }
        public override int GetHashCode()
        {
            int hash = 23;
            unchecked
            {
                hash = hash * 37 + x;
                hash = hash * 37 + y;
            }
            return hash;
        }
    }
    public struct Vector3i
    {
        public Vector3i(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public int x;
        public int y;
        public int z;
        public override bool Equals(object obj)
        {
            if (obj is Vector3i)
            {
                Vector3i other = (Vector3i)obj;
                return this.x == other.x && this.y == other.y && this.z == other.z;
            }
            return base.Equals(obj);
        }
        public static bool operator ==(Vector3i a, Vector3i b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }
        public static bool operator !=(Vector3i a, Vector3i b)
        {
            return !(a.x == b.x && a.y == b.y && a.z == b.z);
        }
        public override int GetHashCode()
        {
            int hash = 23;
            unchecked
            {
                hash = hash * 37 + x;
                hash = hash * 37 + y;
                hash = hash * 37 + z;
            }
            return hash;
        }
    }
    public class DependencyChecker
    {
        [Inject]
        public Type[] d_InjectAttributes;
        public DependencyChecker()
        {
        }
        public DependencyChecker(params Type[] injectAttributes)
        {
            this.d_InjectAttributes = injectAttributes;
        }
        public void CheckDependencies(params object[] components)
        {
            if (d_InjectAttributes == null || d_InjectAttributes.Length == 0)
            {
                throw new Exception("Inject attributes list is null.");
            }
            foreach (object o in components)
            {
                CheckDependencies1(o);
            }
        }
        private void CheckDependencies1(object o)
        {
            Type type = o.GetType();
            var properties = type.GetProperties();
            var fields = type.GetFields();
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);
                foreach (var a in attributes)
                {
                    if (a is InjectAttribute)
                    {
                        if (property.GetValue(o, null) == null)
                        {
                            throw new Exception(string.Format("Dependency {0} of object of type {1} is null.", property.Name, type.Name));
                        }
                    }
                }
            }
            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(true);
                foreach (var a in attributes)
                {
                    if (a is InjectAttribute)
                    {
                        if (field.GetValue(o) == null)
                        {
                            throw new Exception(string.Format("Dependency {0} of object of type {1} is null.", field.Name, type.Name));
                        }
                    }
                }
            }
        }
    }
    public class Timer
    {
        public double INTERVAL { get { return interval; } set { interval = value; } }
        public double MaxDeltaTime { get { return maxDeltaTime; } set { maxDeltaTime = value; } }
        double interval = 1;
        double maxDeltaTime = double.PositiveInfinity;

        double starttime;
        double oldtime;
        public double accumulator;
        public Timer()
        {
            Reset();
        }
        public void Reset()
        {
            starttime = gettime();
        }
        public delegate void Tick();
        public void Update(Tick tick)
        {
            double currenttime = gettime() - starttime;
            double deltaTime = currenttime - oldtime;
            accumulator += deltaTime;
            double dt = INTERVAL;
            if (MaxDeltaTime != double.PositiveInfinity && accumulator > MaxDeltaTime)
            {
                accumulator = MaxDeltaTime;
            }
            while (accumulator >= dt)
            {
                tick();
                accumulator -= dt;
            }
            oldtime = currenttime;
        }
        static double gettime()
        {
            return (double)DateTime.Now.Ticks / (10 * 1000 * 1000);
        }
    }
    class FastStack<T>
    {
        public void Initialize(int maxCount)
        {
            values = new T[maxCount];
        }
        T[] values;
        public int Count;
        public void Push(T value)
        {
            if (Count >= values.Length)
            {
                Array.Resize(ref values, values.Length * 2);
            }
            values[Count] = value;
            Count++;
        }
        public T Pop()
        {
            Count--;
            return values[Count];
        }
        public void Clear()
        {
            Count = 0;
        }
    }
    public class FastQueue<T>
    {
        public void Initialize(int maxCount)
        {
            this.maxCount = maxCount;
            values = new T[maxCount];
            Count = 0;
            start = 0;
            end = 0;
        }
        int maxCount;
        T[] values;
        public int Count;
        int start;
        int end;
        public void Push(T value)
        {
            /*
            if (Count >= values.Length)
            {
                Array.Resize(ref values, values.Length * 2);
            }
            */
            values[end] = value;
            Count++;
            end++;
            if (end >= maxCount)
            {
                end = 0;
            }
        }
        public T Pop()
        {
            T value = values[start];
            Count--;
            start++;
            if (start >= maxCount)
            {
                start = 0;
            }
            return value;
        }
        public void Clear()
        {
            Count = 0;
        }
    }
    public class MyLinq
    {
        public static bool Any<T>(IEnumerable<T> l)
        {
            return l.GetEnumerator().MoveNext();
        }
        public static T First<T>(IEnumerable<T> l)
        {
            var e = l.GetEnumerator();
            e.MoveNext();
            return e.Current;
        }
        public static int Count<T>(IEnumerable<T> l)
        {
            int count = 0;
            foreach (T v in l)
            {
                count++;
            }
            return count;
        }
        public static IEnumerable<T> Take<T>(IEnumerable<T> l, int n)
        {
            int i = 0;
            foreach (var v in l)
            {
                if (i >= n)
                {
                    yield break;
                }
                yield return v;
                i++;
            }
        }
    }
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
            {
                return;
            }

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement();

                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);

                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
        #endregion
    }
    public static class Extensions
    {
        public static string GetWorkingDirectory(Assembly assembly)
        {
            return Path.GetDirectoryName(assembly.Location);
        }
        public static string GetRelativePath(Assembly assembly, string absolute)
        {
            return absolute.Replace(GetWorkingDirectory(assembly), "");
        }
        public static string GetAbsolutePath(Assembly assembly, string relative)
        {
            return Path.Combine(GetWorkingDirectory(assembly), relative);
        }
    }
    public static class Serializers
    {
        public static void XmlSerialize(Stream stream, object value)
        {
            XmlSerializer serializer = new XmlSerializer(value.GetType());
            serializer.Serialize(stream, value);
        }
        public static void XmlSerialize(string fileName, object value)
        {
            using (Stream s = File.Create(fileName))
            {
                XmlSerializer serializer = new XmlSerializer(value.GetType());
                serializer.Serialize(s, value);
            }
        }
        public static object XmlDeserialize(string fileName, Type type)
        {
            using (Stream stream = File.OpenRead(fileName))
            {
                XmlSerializer serializer = new XmlSerializer(type);
                return serializer.Deserialize(stream);
            }
        }
        public static object XmlDeserialize(Stream stream, Type type)
        {
            XmlSerializer serializer = new XmlSerializer(type);
            return serializer.Deserialize(stream);
        }
    }
    /// <summary>
    /// Provides an application-wide point for services.
    /// </summary>
    public sealed class Container : IServiceContainer
    {
        private static volatile Container _instance;

        public static Container Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Container();
                }
                return _instance;
            }
        }
        private ServiceContainer _serviceContainer = new ServiceContainer();
        public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
        {
            _serviceContainer.AddService(serviceType, callback, promote);
        }
        public void AddService(Type serviceType, ServiceCreatorCallback callback)
        {
            _serviceContainer.AddService(serviceType, callback);
        }
        public void AddService(Type serviceType, object serviceInstance, bool promote)
        {
            _serviceContainer.AddService(serviceType, serviceInstance, promote);
        }
        public void AddService(Type serviceType, object serviceInstance)
        {
            _serviceContainer.AddService(serviceType, serviceInstance);
        }
        public void RemoveService(Type serviceType, bool promote)
        {
            _serviceContainer.RemoveService(serviceType, promote);
        }
        public void RemoveService(Type serviceType)
        {
            _serviceContainer.RemoveService(serviceType);
        }
        public object GetService(Type serviceType)
        {
            return _serviceContainer.GetService(serviceType);
        }
    }
    /// <summary>
    /// Caches types in a big library to avoid having multiple classes scan multiple times.
    /// Any class can retrieve types from here to use them in whichever way they want.
    /// </summary>
    public sealed class TypeManager
    {
        /// <summary>
        /// The default capacity amount.
        /// See documentation for further information.
        /// </summary>
        /// <remarks>A greater value means more memory consumption, but increases scanning speed.
        /// A lower value means less memory consumption, but decreases scanning speed.</remarks>
        public const int DefaultAmount = 256;

        private static volatile TypeManager _instance;

        public static TypeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TypeManager();
                    _instance.Initialize(null);
                }
                return _instance;
            }
        }

        public bool IsInitialized { get; private set; }
        public IList<Type> FoundTypes { get; private set; }

        private TypeManager()
        {
            IsInitialized = false;
            FoundTypes = new List<Type>(DefaultAmount);
        }
        private void Initialize(IList<string> assembliesToScan)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException();
            }

            Stopwatch sw = Stopwatch.StartNew();

            // if there are no desired assemblies then we take all assemblies we can find in the working directory
            if (assembliesToScan == null)
            {
                string workingDirectory = Extensions.GetWorkingDirectory(Assembly.GetExecutingAssembly());
                List<string> tmp = new List<string>(2);
                // alright, lets scan all assemblies in the working directory
                tmp.AddRange(Directory.GetFiles(workingDirectory, "*.exe", SearchOption.TopDirectoryOnly));
                tmp.AddRange(Directory.GetFiles(workingDirectory, "*.dll", SearchOption.TopDirectoryOnly));
                assembliesToScan = tmp;
            }

            // load and check each assembly's types
            foreach (string file in assembliesToScan)
            {
                Assembly assembly = null;

                try
                {
                    assembly = Assembly.Load(AssemblyName.GetAssemblyName(file));

                    ScanAssembly(assembly);

                }
                catch (FileLoadException)
                {
                    // this exception can be ignored here
                    continue;
                }
                catch (ReflectionTypeLoadException)
                {
                    // this exception can be ignored here
                    continue;
                }
                catch (TypeLoadException)
                {
                    // this exception can be ignored here
                    continue;
                }
                catch (BadImageFormatException)
                {
                    // this exception can be ignored here
                    continue;
                }
                catch (Exception ex)
                {
                    // other exceptions may be interesting though
                    sw.Stop();
                    throw ex;
                }
            }

            sw.Stop();
            System.Diagnostics.Debug.WriteLine(string.Format("Scanned {0} assemblies in {1} milliseconds (collected a total of {2} types).", assembliesToScan.Count, sw.ElapsedMilliseconds, FoundTypes.Count));

            IsInitialized = true;
        }
        private void ScanAssembly(Assembly assembly)
        {
            int amount = 0;

            // process all types (even private ones)
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];

                // add it
                FoundTypes.Add(t);
                amount++;
            }
        }
        public Type[] FindAll(Predicate<Type> predicate)
        {
            List<Type> tmp = new List<Type>(16);

            for (int i = 0; i < FoundTypes.Count; i++)
            {
                Type t = FoundTypes[i];
                if (predicate(t))
                {
                    tmp.Add(t);
                }
            }

            return tmp.ToArray();
        }
        public IList<Type> FindDescendants(Type superclass, bool includeAbstracts)
        {
            List<Type> tmp = new List<Type>(16);

            for (int i = 0; i < FoundTypes.Count; i++)
            {
                Type t = FoundTypes[i];
                if (t.IsSubclassOf(superclass))
                {
                    if (!includeAbstracts && t.IsAbstract)
                    {
                        continue;
                    }
                    tmp.Add(t);
                }
            }

            if (tmp.Count == 0)
            {
                return new Type[0];
            }

            return tmp.ToArray();
        }

        public IList<Type> FindImplementers(Type interfaceType, bool includeAbstracts)
        {
            List<Type> tmp = new List<Type>(16);

            for (int i = 0; i < FoundTypes.Count; i++)
            {
                Type t = FoundTypes[i];

                Type[] interfaces = t.GetInterfaces();
                for (int j = 0; j < interfaces.Length; j++)
                {
                    Type iface = interfaces[j];

                    if (iface == interfaceType)
                    {
                        if (!includeAbstracts && t.IsAbstract)
                        {
                            continue;
                        }

                        tmp.Add(t);
                    }
                }
            }

            return tmp;
        }
        public Type FindByAssemblyQualifiedName(string assemblyQualifiedName)
        {
            for (int i = 0; i < FoundTypes.Count; i++)
            {
                Type t = FoundTypes[i];
                if (t.AssemblyQualifiedName == assemblyQualifiedName)
                {
                    return t;
                }
            }

            return null;
        }
        public Type FindByFullName(string fullName)
        {
            for (int i = 0; i < FoundTypes.Count; i++)
            {
                Type t = FoundTypes[i];

                if (t.FullName == fullName)
                {
                    return t;
                }
            }

            return null;
        }
        public object CreateInstance(Type type, params object[] args)
        {
            return Activator.CreateInstance(type, args);
        }
        public T CreateInstance<T>(params object[] args)
        {
            return (T)CreateInstance(typeof(T), args);
        }
    }
    public interface ISocket
    {
        void Bind(IPEndPoint iep);
        void Listen(int backlog);
        bool Poll(int microSeconds, SelectMode selectMode);
        ISocket Accept();
        EndPoint RemoteEndPoint { get; }
        void Disconnect(bool p);
        void BeginSend(byte[] data, int pos, int dataLength, SocketFlags flags, AsyncCallback callback, object o);
        void Select(IList checkRead, IList checkWrite, IList checkError, int microSeconds);
        int Receive(byte[] data);
        void Connect(string serverAddress, int port);
        void Send(byte[] n);
    }
    public class SocketNet : ISocket
    {
        public SocketNet()
        {
        }
        public SocketNet(Socket socket)
        {
            this.d_Socket = socket;
        }
        public Socket d_Socket;
        public void Bind(IPEndPoint iep)
        {
            d_Socket.Bind(iep);
        }
        public void Listen(int backlog)
        {
            d_Socket.Listen(backlog);
        }
        public bool Poll(int microSeconds, SelectMode selectMode)
        {
            return d_Socket.Poll(microSeconds, selectMode);
        }
        public ISocket Accept()
        {
            return new SocketNet(d_Socket.Accept());
        }
        public EndPoint RemoteEndPoint
        {
            get { return d_Socket.RemoteEndPoint; }
        }
        public void Disconnect(bool p)
        {
            d_Socket.Disconnect(p);
        }
        public void BeginSend(byte[] data, int pos, int dataLength, SocketFlags flags, AsyncCallback callback, object o)
        {
            d_Socket.BeginSend(data, pos, dataLength, flags, callback, o);
        }
        public void Select(IList checkRead, IList checkWrite, IList checkError, int microSeconds)
        {
            Dictionary<Socket, SocketNet> readOrig = new Dictionary<Socket, SocketNet>();
            Dictionary<Socket, SocketNet> writeOrig = new Dictionary<Socket, SocketNet>();
            Dictionary<Socket, SocketNet> errorOrig = new Dictionary<Socket, SocketNet>();
            ArrayList read = null;
            ArrayList write = null;
            ArrayList error = null;
            if (checkRead != null)
            {
                read = new ArrayList();
                foreach (SocketNet s in checkRead) { read.Add(s.d_Socket); readOrig[s.d_Socket] = s; }
            }
            if (checkWrite != null)
            {
                write = new ArrayList();
                foreach (SocketNet s in checkWrite) { write.Add(s.d_Socket); writeOrig[s.d_Socket] = s; }
            }
            if (checkError != null)
            {
                error = new ArrayList();
                foreach (SocketNet s in checkError) { error.Add(s.d_Socket); errorOrig[s.d_Socket] = s; }
            }

            Socket.Select(read, write, error, microSeconds);

            if (checkRead != null)
            {
                checkRead.Clear();
                foreach (Socket s in read) { checkRead.Add(readOrig[s]); }
            }
            if (checkWrite != null)
            {
                checkWrite.Clear();
                foreach (Socket s in write) { checkWrite.Add(writeOrig[s]); }
            }
            if (checkError != null)
            {
                checkError.Clear();
                foreach (Socket s in error) { checkError.Add(errorOrig[s]); }
            }
        }
        public int Receive(byte[] data)
        {
            return d_Socket.Receive(data);
        }
        public void Connect(string serverAddress, int port)
        {
            d_Socket.Connect(serverAddress, port);
        }
        public void Send(byte[] n)
        {
            d_Socket.Send(n);
        }
    }
    public class SocketDummy : ISocket
    {
        public SocketDummy()
        {
        }
        public SocketDummy(SocketDummyNetwork network)
        {
            this.network = network;
        }
        public enum SocketType
        {
            Server,
            Client,
            ConnectionFromClient,
        }
        public SocketType type;
        public SocketDummyNetwork network;
        public void Bind(IPEndPoint iep)
        {
        }
        public void Listen(int backlog)
        {
            type = SocketType.Server;
        }
        bool connectionAccepted;
        public bool Poll(int microSeconds, SelectMode selectMode)
        {
            if (selectMode != SelectMode.SelectRead) { throw new NotImplementedException(); }
            switch (type)
            {
                case SocketType.Client:
                    return network.ClientReceiveBuffer.Count != 0;
                case SocketType.ConnectionFromClient:
                    return network.ServerReceiveBuffer.Count != 0;
                case SocketType.Server:
                    if (connectionAccepted)
                    {
                        return false;
                    }
                    bool ret = network.ServerReceiveBuffer.Count != 0;
                    if (ret)
                    {
                        connectionAccepted = true;
                    }
                    return ret;
                default:
                    throw new Exception();
            }
        }
        public ISocket Accept()
        {
            if (type == SocketType.Server)
            {
                return new SocketDummy(network) { type = SocketType.ConnectionFromClient };
            }
            throw new InvalidOperationException();
        }
        public EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);
        public EndPoint RemoteEndPoint { get { return remoteEndpoint; } set { remoteEndpoint = value; } }
        public void Disconnect(bool p)
        {
        }
        public void BeginSend(byte[] data, int pos, int dataLength, SocketFlags flags, AsyncCallback callback, object o)
        {
            byte[] buf = new byte[dataLength];
            for (int i = 0; i < dataLength; i++)
            {
                buf[i] = data[pos + i];
            }
            lock (network)
            {
                switch (type)
                {
                    case SocketType.Client:
                        for (int i = 0; i < buf.Length; i++)
                        {
                            network.ServerReceiveBuffer.Enqueue(buf[i]);
                        }
                        break;
                    case SocketType.ConnectionFromClient:
                        for (int i = 0; i < buf.Length; i++)
                        {
                            network.ClientReceiveBuffer.Enqueue(buf[i]);
                        }
                        break;
                    case SocketType.Server:
                        throw new NotImplementedException();
                    default:
                        throw new Exception();
                }
            }
        }
        public void Select(IList checkRead, IList checkWrite, IList checkError, int microSeconds)
        {
            if (checkWrite != null) { throw new NotImplementedException(); }
            if (checkError != null) { throw new NotImplementedException(); }
            switch (type)
            {
                case SocketType.Client:
                    throw new NotImplementedException();
                case SocketType.ConnectionFromClient:
                    throw new NotImplementedException();
                case SocketType.Server:
                    if (network.ServerReceiveBuffer.Count == 0)
                    {
                        checkRead.Clear();
                    }
                    break;
            }
        }
        public int Receive(byte[] data)
        {
            Queue<byte> buf;
            switch (type)
            {
                case SocketType.Server:
                    throw new NotImplementedException();
                case SocketType.Client:
                    buf = network.ClientReceiveBuffer;
                    break;
                case SocketType.ConnectionFromClient:
                    buf = network.ServerReceiveBuffer;
                    break;
                default:
                    throw new Exception();
            }
            for (; ; )
            {
                lock (network)
                {
                    if (buf.Count > 0)
                    {
                        break;
                    }
                }
                Thread.Sleep(0);
            }
            lock (network)
            {
                int count = buf.Count;
                if (count > data.Length)
                {
                    count = data.Length;
                }
                for (int i = 0; i < count; i++)
                {
                    data[i] = buf.Dequeue();
                }
                return count;
            }
        }
        public void Connect(string serverAddress, int port)
        {
            type = SocketType.Client;
        }
        public void Send(byte[] n)
        {
            if (type != SocketType.Client) { throw new InvalidOperationException(); }
            lock (network)
            {
                for(int i=0;i<n.Length;i++)
                {
                    network.ServerReceiveBuffer.Enqueue(n[i]);
                }
            }
        }
    }
    public class SocketDummyNetwork
    {
        public Queue<byte> ServerReceiveBuffer = new Queue<byte>();
        public Queue<byte> ClientReceiveBuffer = new Queue<byte>();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.math.corrcoef;
using jp.nyatla.kokolink.utils.recoverable;

namespace jp.nyatla.kokolink.compatibility
{

    //  GetEnumeratorで読取位置をリセットされないIEnumerableです。
    //  新たに取得した場合は、現在の読取位置を起点としたインスタンスを返す実装をします。
    //
    public interface ISequentialEnumerable<T> : IEnumerable<T> { }



    //  IPyEnumeratorをソースにしたIEnumerator
    //  MoveToはIpyIteratorの仕様を引き継いでRecoverableStopIterationをスローすることがあります。
    class PyIterSuorceIEnumerator<T> : IEnumerator<T>
    {
        private readonly IPyIterator<T> _src;
        private T? _current;
        public PyIterSuorceIEnumerator(IPyIterator<T> src)
        {
            Debug.Assert(src is not IEnumerator<T>); //Enumulableを持たないこと
            this._src = src;
            //this._current;
        }
        T IEnumerator<T>.Current{
            get
            {
                if (this._src != null && this._current!=null)
                {
                    return this._current;
                }
                throw new InvalidOperationException();
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (this._src != null && this._current != null)
                {
                    return this._current;
                }
                throw new InvalidOperationException();
            }
        }



        bool IEnumerator.MoveNext()
            {
                try
                {
                    var c= this._src.Next();
                    if (c == null)
                    {
                        throw new NullReferenceException();
                    }
                    this._current = c;
                    return true;
                }
                catch (RecoverableStopIteration e)
                {
                    throw e;
                }
                catch (PyStopIteration)
                {
                    return false;
                }
            }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }
        void IDisposable.Dispose()
        {
            //nothing to do
            //throw new NotImplementedException();
        }
    }
    // このEnumerableは常に同じEnumerableを返します。
    sealed public class PyIterSuorceEnumerable<T> : ISequentialEnumerable<T>
    {
        readonly private IEnumerator<T> _src;
        public PyIterSuorceEnumerable(IEnumerator<T> src)
        {
            this._src = src;
        }

        public PyIterSuorceEnumerable(IPyIterator<T> src)
        {
            Debug.Assert(src is not IEnumerable<T>); //Enumulableを持たないこと
            this._src = new PyIterSuorceIEnumerator<T>(src);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this._src;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._src;
        }
    }

    sealed public class PyIterator<T> : IPyIterator<T>
    {

        readonly private IEnumerator<T> _src;
        public PyIterator(IEnumerable<T> src)
        {
            this._src = src.GetEnumerator();
        }

        public PyIterator(IEnumerator<T> src)
        {
            this._src = src;

        }
        public T Next()
        {
            if (!this._src.MoveNext())
            {
                throw new PyStopIteration();
            }
            return this._src.Current;
        }
    }




    public class Functions{

        //static public ISequentialEnumerable<T> ToEnumerable<T>(IEnumerator<T> enumor)
        //{
        //    Debug.Assert(enumor is not PyIterator<T>); //設計ミスのトラップ
        //    return new PyIterSuorceEnumerable<T>(enumor);
        //}

        static public ISequentialEnumerable<T> ToEnumerable<T>(IPyIterator<T> iter)
        {
            Debug.Assert(iter is not PyIterator<T>); //設計ミスのトラップ
            return new PyIterSuorceEnumerable<T>(iter); 
        }
        static public IPyIterator<T> ToPyIter<T>(IEnumerable<T> s)
        {
            Debug.Assert(s is not PyIterSuorceEnumerable<T>); //設計ミスのトラップ
            if (s is IPyIterator<T> iterator)
            {
                return iterator;
            }
            return new PyIterator<T>(s);
        }





        static public T[][] Create2dArray<T>(int n, int m,T pad)
        {
            T[][] r = new T[n][];
            for (var i = 0; i < n; i++)
            {
                r[i] = new T[m];
                for(var j = 0; j < m; j++)
                {
                    r[i][j] = pad;
                }
            }
            return r;

        }
        static public T[] Flatten<T>(params IEnumerable<T>[] s)
        {
            List<T> d = new List<T>();
            foreach (var i in s)
            {
                d.AddRange(i);
            }
            return d.ToArray();
        }
        static public T[] Repeat<T>(int n,T pad)
        {
            var r=new T[n];
            Array.Fill<T>(r, pad);
            return r;
        }



    }

    class BinUtils
    {
        static public byte[] Ascii2byte(string s)
        {
            return System.Text.Encoding.ASCII.GetBytes(s);
        }
        static public UInt16 Bytes2Uint16LE(byte[] b, int s)
        {
            return (UInt16)((b[s + 1] << 8) | b[s + 0]);
        }
        static public UInt16 Bytes2Uint16LE(byte[] b)
        {
            return Bytes2Uint16LE(b, 0);
        }

        static public UInt32 Bytes2Uint32LE(byte[] b, int s)
        {
            return (UInt32)((b[s + 3] << 24) | (b[s + 2] << 16) | (b[s + 1] << 8) | b[s + 0]);
        }
        static public UInt32 Bytes2Uint32LE(byte[] b)
        {
            return Bytes2Uint32LE(b, 0);
        }
        static public UInt32 ReadUint32LE(Stream fp)
        {
            return BinUtils.Bytes2Uint32LE(ReadBytes(fp,4)); //LE
        }
        static public byte[] Uint16LE2Bytes(int s)
        {
            return new byte[] { (byte)((s >> 0) & 0xff), (byte)((s >> 8) & 0xff) };
        }
        static public byte[] Uint32LE2Bytes(int s)
        {
            return new byte[] { (byte)((s >> 0) & 0xff), (byte)((s >> 8) & 0xff), (byte)((s >> 16) & 0xff), (byte)((s >> 24) & 0xff) };
        }
        static public bool IsEqualAsByte(byte[] a, string b)
        {
            return a.SequenceEqual(BinUtils.Ascii2byte(b));
        }
        static public byte[] ReadBytes(Stream fp, int size)
        {
            var ret = new byte[size];
            for(var i = 0; i < size; i++)
            {
                var w=fp.ReadByte();
                if (w < 0)
                {
                    throw new EndOfStreamException();
                }
                ret[i] = (byte)w;
            }
            return ret;
        }


        static public byte[] ToByteArray(IEnumerable<int> s)
        {
            using (var ms = new MemoryStream())
            {
                foreach (var i in s)
                {
                    Debug.Assert(i >= 0 && i <= 255);
                    ms.WriteByte((byte)i);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }
        static public byte[] ToByteArray(IEnumerable<uint> s)
        {
            using (var ms = new MemoryStream())
            {
                foreach (var i in s)
                {
                    Debug.Assert(i >= 0 && i <= 255);
                    ms.WriteByte((byte)i);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }


    }



}
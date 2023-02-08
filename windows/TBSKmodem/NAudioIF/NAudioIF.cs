using System.Collections.Concurrent;
using System.Diagnostics;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.wavefile;
using jp.nyatla.kokolink.io.audioif;
using NAudio.Wave;
using jp.nyatla.kokolink.utils.math;
using System.Security.Cryptography;
using jp.nyatla.kokolink.utils;

namespace jp.nyatla.kokolink.io.audioif
{





    //using jp.nyatla.kokolink.utils.wavefile;
    //PcmData pcm;
    //using (var stream = new FileStream("../../../../step1_modulate/step1.cs.wav", FileMode.Open, FileAccess.Read))
    //{
    //    pcm = PcmData.Load(stream);
    //    //var pcm = new PcmData(src_pcm, 16, (uint)carrier);
    //}
    //using (var na = new NAudioPlayer(pcm))
    //{
    //    na.Play();
    //    na.Wait();
    //}
    public class NAudioPlayer : IAudioPlayer
    {
        private WaveOutEvent? _wo;

        //public static IList<(int id, string name)> GetDevices()
        //{
        //    var l = new List<(int id, string name)>();
        //    for (int n = -1; n < W.DeviceCount; n++)
        //    {
        //        var caps = WaveOutEvent.GetCapabilities(n);
        //        l.Add((n, caps.ProductName));
        //    }
        //    return l;
        //}
        public NAudioPlayer(PcmData src, int device_no = -1) : this(src.Data, src.SampleBits, (int)src.Framerate, 1,device_no:device_no)
        {
        }

        public NAudioPlayer(IEnumerable<byte> data, int samplebits, int framerate, int channels, int device_no = -1)
        {
            var ms = new MemoryStream(data.ToArray());
            try
            {
                var rs = new RawSourceWaveStream(ms, new WaveFormat(framerate, samplebits, channels));
                var wo = new WaveOutEvent();
                try
                {
                    wo.Init(rs);

                }
                catch (Exception)
                {
                    rs.Dispose();
                    throw;
                }
                this._wo = wo;
            }
            catch (Exception)
            {
                ms.Dispose();
                throw;
            }

        }
        public void Close()
        {
            if (this._wo != null)
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            if (this._wo != null)
            {
                try
                {
                    this.Stop();
                    this._wo.Dispose();
                }
                finally
                {
                    this._wo = null;
                }
            }
        }

        public void Play()
        {
            Debug.Assert(this._wo != null);
            var wo = this._wo;
            Debug.Assert(wo.PlaybackState == PlaybackState.Stopped);
            wo.Play();
        }

        public void Stop()
        {
            Debug.Assert(this._wo != null);
            var wo = this._wo;
            if (wo.PlaybackState == PlaybackState.Stopped)
            {
                return;
            }
            wo.Stop();
        }

        public void Wait()
        {
            Debug.Assert(this._wo != null);
            var wo = this._wo;
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        }
    }
    public class NAudioInputIterator : IAudioInputIterator
    {
        private abstract class SampleQ
        {
            private Rms _rms;
            public SampleQ(int sample_rate)
            {
                this._q = new BlockingCollection<double>(sample_rate);
                this._rms = new Rms(Math.Max(sample_rate / 100, 10));
            }
            protected BlockingCollection<double> _q;
            public double Take()
            {
                double ret;
                if (!this._q.TryTake(out ret, 3000))
                {
                    throw new PyStopIteration();
                }
                return ret;
            }
            public abstract void Puts(byte[] s);
            /**
             * 継承クラスから呼び出す。
             */
            protected void Put(double v)
            {
                while(!this._q.TryAdd(v,0))
                {
                    this._q.TryTake(out double tmp,0);
                }
                lock(this._rms){
                    this._rms.Update(v);
                }
            }
            public double Rms
            {
                get { lock (this._rms) { return this._rms.GetLastRms(); } }
            }

        }
        private class SampleQ8 : SampleQ
        {
            public SampleQ8(int sample_rate) : base(sample_rate) { }
            override public void Puts(byte[] s)
            {
                foreach (var i in s)
                {
                    var v = FloatConverter.ByteToDouble(i);
                    base.Put(v);
                }
            }

        }
        private class SampleQ16 : SampleQ
        {
            public SampleQ16(int sample_rate) : base(sample_rate) { }
            override public void Puts(byte[] s)
            {
                for (var i = 0; i < s.Length; i += 2)
                {
                    base.Put(FloatConverter.Int16ToDouble((Int16)((UInt16)s[i] | ((UInt16)s[i + 1] << 8))));
                }
            }
        }





        private SampleQ _q;
        private WaveInEvent? _wi;
        private bool _play_now;
        //private Semaphore _se = new Semaphore(1,1);
        readonly private int _bits_par_sample;
        public static IList<(int id, string name)> GetDevices()
        {
            var l = new List<(int id, string name)>();
            for (int n = -1; n < WaveInEvent.DeviceCount; n++)
            {
                var caps = WaveInEvent.GetCapabilities(n);
                l.Add((n, caps.ProductName));
            }
            return l;
        }




        public NAudioInputIterator(int framerate = 8000, int bits_par_sample = 16, int device_no = -1)
        {
            var wi = new WaveInEvent();


            //Console.WriteLine(wi.DeviceNumber);
            wi.WaveFormat = new WaveFormat(framerate, bits_par_sample,1);
            switch (bits_par_sample)
            {
                case 8:
                    this._q = new SampleQ8(framerate);
                    break;
                case 16:
                    this._q = new SampleQ16(framerate);
                    break;
                default:
                    throw new NotImplementedException();
            }
            this._wi = wi;
            this._bits_par_sample = bits_par_sample;

            wi.DataAvailable += (s, a) =>
            {
                this._q.Puts(a.Buffer);
            };
            wi.RecordingStopped += (s, a) =>
            {
                lock (this)
                {
                    this._play_now = false;
                }
            };
            lock (this)
            {
                this._play_now = false;
            }
        }
        public void Close()
        {
            if (this._wi != null)
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            if (this._wi != null)
            {
                try
                {
                    this.Stop();
                    this._wi.Dispose();
                }
                finally
                {
                    this._wi = null;
                }
            }
        }

        public double Next()
        {
            if (!this._play_now)
            {
                throw new InvalidOperationException();
            }
            return this._q.Take();
        }

        public void Start()
        {
            var wi = this._wi;
            Debug.Assert(wi != null);
            lock (this)
            {
                Debug.Assert(!this._play_now);
                this._play_now = true;
                wi.StartRecording();
            }
        }

        public void Stop()
        {
            var wi = this._wi;
            Debug.Assert(wi != null);
            lock (this)
            {
                if (!this._play_now)
                {
                    return;
                }
                wi.StopRecording();
            }
            while (this._play_now)
            {
                Thread.Sleep(100);
            }
        }
        public double GetRms()
        {
            return this._q.Rms;
        }
    }




}
using System.Collections.Concurrent;
using System.Diagnostics;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.wavefile;
using jp.nyatla.kokolink.io.audioif;
using NAudio.Wave;

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
    public class NAudioInputInterator : IAudioInputInterator
    {
        private BlockingCollection<byte> _q=new BlockingCollection<byte>();
        private WaveInEvent? _wi;
        private bool _play_now;
        //private Semaphore _se = new Semaphore(1,1);
        private int _bits_par_sample;
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
        public NAudioInputInterator(int framerate = 8000, int bits_par_sample = 16, int device_no = -1)
        {
            var wi = new WaveInEvent();
            
            Console.WriteLine(wi.DeviceNumber);
            wi.WaveFormat = new WaveFormat(framerate, bits_par_sample,1);
            wi.DataAvailable += (s, a) =>
            {
                foreach(var i in a.Buffer){
                    this._q.TryAdd(i,1000);
                }
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
            this._wi = wi;
            this._bits_par_sample = bits_par_sample;
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
            byte a1,a2;
            switch (this._bits_par_sample)
            {
                case 8:
                    if(!this._q.TryTake(out a1, 3000))
                    {
                        throw new PyStopIteration();
                    }
                    return (double)a1/255-0.5;
                case 16:
                    double r = (Math.Pow(2, 16) - 1) / 2;//(2 * *16 - 1)//2 #Daisukeパッチ
                    if(!this._q.TryTake(out a1,3000) || !this._q.TryTake(out a2, 3000)){
                        throw new PyStopIteration();
                    }
                    var b = (UInt16)(a1 | ((UInt16)a2 << 8));
                    if ((0x8000 & b) == 0)
                    {
                        return b / r;
                    }
                    else
                    {
                        return (((Int32)b - 0x0000ffff) - 1) / r;
                    }
                default:
                    break;
            }
            throw new NotImplementedException();
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
    }




}
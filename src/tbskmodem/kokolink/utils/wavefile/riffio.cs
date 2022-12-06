using System.Diagnostics;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.types;


namespace jp.nyatla.kokolink.utils.wavefile.riffio
{
    // """チャンクのベースクラス。
    // """
    abstract public class Chunk :Py__str__{
        protected readonly byte[] _name;
        protected readonly int _size;
        // def __init__(self,name:bytes,size:int):
        //     assert(len(name)==4)
        //     self._name=name
        //     self._size=size
        public Chunk(byte[] name,int size){
            Debug.Assert(name.Length==4);
            this._name=name;
            this._size=size;
        }
        public Chunk(string name,int size):
            this(BinUtils.Ascii2byte(name),size)
        {
        }        
        // @property
        // def name(self)->bytes:
        //     return self._name
        public byte[] Name{
            get=>this._name;
        }
        // @property
        // def size(self)->int:
        //     """チャンクのsizeフィールドの値。このサイズはワード境界ではありませぬ。
        //     ワード境界にそろえるときは、size+size%2を使います。
        //     """
        //     return self._size
        public int Size{
            get=>this._size;
        }
        // @abstractproperty
        // def data(self)->bytearray:
        //     """チャンクデータ部分です。このサイズはワード境界ではありませぬ。
        //     """
        //     ...
        abstract public byte[] Data{
            get;
        }
        // def toChunkBytes(self)->bytes:
        //     """チャンクのバイナリ値に変換します。このデータはワード境界です。
        //     """
        //     d=self.data
        //     if len(d)%2!=0:
        //         #word境界
        //         d=d+b"\0"
        //     return struct.pack("<4sL",self._name,self._size)+d
        public byte[] ToChunkBytes(){
            var d=this.Data;
            var ret=new byte[4+4+d.Length+d.Length%2]; //word境界
            ret[ret.Length-1]=0;
            for(var i=0;i<4;i++){
                ret[i]=this._name[i];
            }
            for(var i=0;i<4;i++){
                ret[i+4]=(byte)(this._size>>(i*8) & 0xff); //LE
            }
            for(var i=0;i<d.Length;i++){
                ret[i+8]=d[i];
            }
            return ret;
        }
        // def _summary_dict(self)->dict:
        //     return {"name":self.name,"size":self.size}
        //public Dictionary<string, object> _Summary_dict(){
        //    var r=new Dictionary<string,object>();
        //    r.Add("name",this._name);
        //    r.Add("size",this._size);
        //    return r;
        //}
        // def __str__(self)->str:
        //     return str(self._summary_dict())
        //override public string? ToString(){
        //    return this._Summary_dict().ToString();
        //}
    }
    // """Formatフィールドを含むChunk構造を格納するクラス
    // """
    abstract public class ChunkHeader:Chunk{
        protected byte[] _form;
        // def __init__1(self,fp:RawIOBase):
        //     name,size,form=struct.unpack_from('<4sL4s',fp.read(12))
        //     super().__init__(name,size)
        //     self._form=form
        //     return
        public ChunkHeader(Stream fp):
            this(BinUtils.ReadBytes(fp,4),(int)BinUtils.ReadUint32LE(fp),BinUtils.ReadBytes(fp,4))
        {
            // using (var br = new System.IO.BinaryReader(fp)){
            //     var name=br.ReadBytes(4);
            //     var size=BinUtils.Bytes2Uint32LE(br.ReadBytes(4)); //LE
            //     var form=br.ReadBytes(4);
            //     this(name,size,form);
            // }
        }

        // def __init__3(self,name:str,size:int,form:bytes):
        //     # print(name,size,form)
        //     super().__init__(name,size)
        //     self._form=form
        //     return
        public ChunkHeader(byte[] name,int size,byte[] form):base(name,size)
        {
            this._form= form;
        }
        public ChunkHeader(string name,int size,string form):
            this(BinUtils.Ascii2byte(name),size,BinUtils.Ascii2byte(form))
        {
        }

        // @property
        // def form(self)->int:
        //     return self._form
        public byte[] Form{
            get=>this._form;
        }
        override public byte[] Data
        {
            get=>this._form;
        }

        // def _summary_dict(self)->dict:
        //     return dict(super()._summary_dict(),**{"form":self.form})
        //public Dictionary<string,object> _Summary_dict(){
        //    var d=base._Summary_dict();
        //    d.Add("form",this._form);
        //    return d;
        //}

    }
    // """ペイロードをそのまま格納するチャンクです.    
    // """
    public class RawChunk:Chunk{
        // def __init__2(self,name:bytes,data:bytes):
        //     super().__init__(name,len(data))
        //     self._data=data
        readonly protected byte[] _data;
        public RawChunk(byte[] name,byte[] data):
            base(name,data.Length)
        {
            this._data=data;
        }
        public RawChunk(string name,byte[] data): this(BinUtils.Ascii2byte(name), data)
        {
        }

        // def __init__3(self,name:bytes,size:int,fp:RawIOBase):
        //     super().__init__(name,size)
        //     # data=bytearray()
        //     # data.extend(fp.read(size))
        //     data=fp.read(size)
        //     if size%2!=0:
        //         fp.read(1) #padding
        //     self._data=data
        //     # assert(self.size==len(rawdata)-8)
        public RawChunk(byte[] name,int size,Stream fp):
            base(name,size)
        {
            var data=BinUtils.ReadBytes(fp, size);
            if(size%2!=0){
                BinUtils.ReadBytes(fp,1);//padding
            }
            this._data=data;
        }
        public RawChunk(string name,int size,Stream fp):
            this(BinUtils.Ascii2byte(name),size,fp)
        {
        }

        // @property
        // def data(self)->bytes:
        //     return self._data
        override public byte[] Data{
            get=>this._data;
        }
    }

    // """fmtチャンクを格納するクラス.
    // """
    class FmtChunk : RawChunk {

        const UInt16 WAVE_FORMAT_PCM = 0x0001;

        // def __init__2(self,size:int,fp:RawIOBase):
        //     super().__init__(b"fmt ",size,fp)
        //     fmt,ch=struct.unpack("<HH",self._data[0:4])
        //     if fmt!=WAVE_FORMAT_PCM:
        //         raise TypeError("Invalid Format FORMAT=%d,CH=%d"%(fmt,ch))
        //     # print(self.samplewidth)
        public FmtChunk(int size, Stream fp) :
            base("fmt ", size, fp)
        {
            var fmt = BinUtils.Bytes2Uint16LE(this._data, 0);
            var ch = BinUtils.Bytes2Uint16LE(this._data, 2);
            if (fmt != WAVE_FORMAT_PCM) {
                throw new FormatException();
            }
        }
        private static byte[] FmtChunk_init(int framerate, int samplewidth, int nchannels)
        {
            var b = new List<byte>();
            b.AddRange(BinUtils.Uint16LE2Bytes(WAVE_FORMAT_PCM));
            b.AddRange(BinUtils.Uint16LE2Bytes(nchannels));
            b.AddRange(BinUtils.Uint32LE2Bytes(framerate));
            b.AddRange(BinUtils.Uint32LE2Bytes(nchannels * framerate * samplewidth));
            b.AddRange(BinUtils.Uint16LE2Bytes(nchannels * samplewidth));
            b.AddRange(BinUtils.Uint16LE2Bytes(samplewidth * 8));
            return b.ToArray();
        }
        //    def __init__3(self,framerate:int,samplewidth:int,nchannels:int):
        //         # print(framerate,samplewidth,nchannels)
        //         d=struct.pack(
        //             '<HHLLHH',
        //             WAVE_FORMAT_PCM, nchannels, framerate,
        //             nchannels * framerate * samplewidth,
        //             nchannels * samplewidth,samplewidth * 8)
        //         super().__init__(b"fmt ",d)    
        public FmtChunk(int framerate, int samplewidth, int nchannels) :
            base("fmt ", FmtChunk_init(framerate, samplewidth, nchannels))
        { }
        // @property
        // def nchannels(self):
        //     return struct.unpack("<H",self.data[2:4])[0]
        public int Nchannels{
            get=>BinUtils.Bytes2Uint16LE(this._data,2);

        }
        // @property
        // def framerate(self):
        //     return struct.unpack("<L",self.data[4:8])[0]
        public uint Framerate{
            get=>BinUtils.Bytes2Uint32LE(this._data,4);
        }
        // @property
        // def samplewidth(self):
        //     """1サンプルに必要なビット数"""
        //     return struct.unpack("<H",self.data[14:16])[0]
        public int Samplewidth{
            get=>BinUtils.Bytes2Uint16LE(this._data,14);
        }
        // def _summary_dict(self)->dict:
        //     return dict(super()._summary_dict(),**{"frametate":self.framerate,"samplewidth":self.samplewidth})
        //public Dictionary<string,object> _Summary_dict(){
        //    var r=base._Summary_dict();
        //    r.Add("frametate",this.Framerate);
        //    r.Add("samplewidth",this.Samplewidth);
        //    return r;
        //}
    }
    // """dataチャンクを格納するクラス
    // """
    class DataChunk:RawChunk
    {
        // def __init__1(self,data:bytes):
        //     super().__init__(b"data",data)
        public DataChunk(byte[] data):
            base("data",data)
        {
        }
        // def __init__2(self,size:int,fp:RawIOBase):
        //     super().__init__(b"data",size,fp)
        public DataChunk(int size,Stream fp):
            base("data",size,fp)
        {
        }

    }

    abstract public class RiffHeader:ChunkHeader{
        // def __init__2(self,size:int,form:bytes):
        //     super().__init__(b"RIFF",size,form)
        public RiffHeader(int size,byte[] form):
            base(BinUtils.Ascii2byte("RIFF"),size,form)
        {
        }
        public RiffHeader(int size,string form):
            this(size,BinUtils.Ascii2byte(form))
        {
        }

        public RiffHeader(Stream fp):
            base(fp)
        {
            Debug.Assert(BinUtils.IsEqualAsByte(this.Name,"RIFF"));
        }
    }
    // """下位構造をそのまま格納するLIST
    // """
    public class RawListChunk:ChunkHeader{
        readonly private byte[] _payload;
        // def __init__3(self,size:int,form:bytes,fp:RawIOBase):
        //     super().__init__(b"LIST",size,form)
        //     self._payload=fp.read(size)
        public RawListChunk(int size,byte[] form,Stream fp):
            base(BinUtils.Ascii2byte("LIST"),size,form)
        {
            this._payload = BinUtils.ReadBytes(fp, size);
        }
        public RawListChunk(int size,string form,Stream fp):
            this(size,BinUtils.Ascii2byte(form),fp)
        {
        }

        // @property
        // def data(self)->bytes:
        //     return super().data+self._payload
        override public byte[] Data{
            get=>Functions.Flatten<byte>(base.Data,this._payload);
        }
    }
   // """Info配下のチャンクを格納するクラス
    // """
    public class InfoItemChunk:RawChunk{
        // def __init__2(self,name:bytes,data:bytes):
        //     assert(len(name)==4)
        //     super().__init__(name,data)
        public InfoItemChunk(byte[] name,byte[] data):
            base(name,data)
        {
            Debug.Assert(name.Length==4);
        }
        public InfoItemChunk(string name,byte[] data):
            this(BinUtils.Ascii2byte(name),data)
        {
        }
        // def __init__3(self,name:bytes,size:int,fp:RawIOBase):
        //     super().__init__(name,size,fp)
        public InfoItemChunk(byte[] name,int size,Stream fp):
            base(name,size,fp){
            Debug.Assert(name.Length==4);
        }
        public InfoItemChunk(string name,int size,Stream fp):
            this(BinUtils.Ascii2byte(name),size,fp){
            Debug.Assert(name.Length==4);
        }
        //    def _summary_dict(self)->dict:
        //         return dict(super()._summary_dict(),**{"value":self.data})
        //public Dictionary<string,object> _Summary_dict(){
        //    var r=base._Summary_dict();
        //    r.Add("value",this.Data);
        //    return r;
        //}        
    }

    // """
    // Args:
    // items
    //     (タグ名,値)のタプルか、InfoItemChunkオブジェクトの混在シーケンスが指定できます。
    //     タプルの場合は[0]のフィールドは4バイトである必要があります。
    // """
    public class InfoListChunk:ChunkHeader{
        readonly private IEnumerable<InfoItemChunk> _items;


        // def __init__2(self,size:int,fp:RawIOBase):
        //     super().__init__(b"LIST",size,b"INFO")
        //     #Infoパーサ
        //     read_size=4
        //     items=[]
        //     while read_size<self._size:
        //         name,size=struct.unpack_from('<4sL',fp.read(8))
        //         item=InfoItemChunk(name,size,fp)
        //         read_size+=size+size%2+8
        //         items.append(item)
        //     self._items=items
        public InfoListChunk(int size,Stream fp):        
            base("LIST",size,"INFO")
        {
            // #Infoパーサ
            var read_size=4;
            var items=new List<InfoItemChunk>();
            while(read_size<this._size){
                byte[] name=BinUtils.ReadBytes(fp,4);
                int rsize=(int)BinUtils.ReadUint32LE(fp);
                var item=new InfoItemChunk(name, rsize, fp);
                items.Add(item);
                read_size+= rsize + rsize % 2+8;
                items.Add(item);
            }
            this._items=items;    
        }

        static private int InfoListChunk_init(IEnumerable<InfoItemChunk> items)
        {
            var s = 0;
            foreach (var i in items)
            {
                s += i.Size + i.Size % 2 + 8;
            }
            return s;
        }
        public InfoListChunk(IEnumerable<InfoItemChunk> items):
            base("LIST", InfoListChunk_init(items),"INFO")
        {
            this._items=items;
        }
        //public InfoListChunk(IEnumerable<ValueTuple<byte[],byte[]>> items):
        //    this(()=>{
        //        var d=new List<InfoItemChunk>();
        //        foreach(var i in items){
        //            d.Add(new InfoItemChunk(i[0],i[1]));
        //        }
        //    }())
        //{
        //}
        // @property
        // def data(self)->bytes:
        //     payload=b"".join([i.toChunkBytes() for i in self._items])
        //     return super().data+payload
        override public byte[] Data{
            get{
                var b=new List<byte>();
                b.AddRange(base.Data);
                foreach (var i in this._items){
                    b.AddRange(i.ToChunkBytes());
                }
                return b.ToArray();
            }
        }
        // @property
        // def items(self)->Sequence[InfoItemChunk]:
        //     return self._items
        public IEnumerable<InfoItemChunk> Items{
            get{
                return this._items;
            }
        }
        // def _summary_dict(self)->dict:
        //     return dict(super()._summary_dict(),**{"info":[i._summary_dict() for i in self.items]})        
        //public Dictionary<string,object> _Summary_dict(){
        //    var r=base._Summary_dict();
        //    var sub=new List<Dictionary<string, object>>;
        //    foreach(var i in this._items){
        //        sub.Add(i);
        //    }
        //    r.Add("info",sub);
        //    return r;
        //}
    }

    class WaveFile:RiffHeader,Py__str__{
        readonly private IList<Chunk> _chunks;


        public WaveFile(Stream fp):
            base(fp)
        {
            Debug.Assert(BinUtils.IsEqualAsByte(this._form,"WAVE"));
            var read_size=4;
            var chunks=new List<Chunk>();
            while(read_size<this._size){
                byte[] name = BinUtils.ReadBytes(fp, 4);
                int size = (int)BinUtils.ReadUint32LE(fp);
                read_size +=size+(size%2)+8;
                if(BinUtils.IsEqualAsByte(name,"fmt ")){
                    chunks.Add(new FmtChunk(size,fp));
                }else if(BinUtils.IsEqualAsByte(name,"data")){
                    chunks.Add(new DataChunk(size,fp));
                }else if(BinUtils.IsEqualAsByte(name,"LIST")){
                    byte[] fmt=BinUtils.ReadBytes(fp,4);
                    if(BinUtils.IsEqualAsByte(fmt,"INFO")){
                        chunks.Add(new InfoListChunk(size,fp));
                    }else{
                        chunks.Add(new RawListChunk(size,fmt,fp));
                    }
                }else{
                    chunks.Add(new RawChunk(name,size,fp));
                }
            }
            this._chunks=chunks;
        }



        static private List<Chunk> WaveFile_init2(int samplerate, int samplewidth, int nchannel, byte[] frames, IEnumerable<Chunk>? extchunks)
        {
            if (frames.Length % (samplewidth * nchannel) != 0)
            {
                throw new ArgumentException(string.Format("fammes length {0}%(samplewidth {1} * nchannel {2})", frames, samplewidth, nchannel));
            }
            var fmt_chunk = new FmtChunk(samplerate, samplewidth, nchannel);
            var data_chunk = new DataChunk(frames);
            var chunks = new List<Chunk>(new Chunk[] { fmt_chunk, data_chunk });
            if (extchunks != null)
            {
                chunks.AddRange(extchunks);
            }
            return chunks;
        }
        static private int WaveFile_init3(IEnumerable<Chunk> chunks)
        {
            var s = 4;
            foreach (var i in chunks)
            {
                s = s + i.Size + i.Size % 2 + 8;
            }
            return s;
        }



        public WaveFile(IList<Chunk> chunks) : base(WaveFile_init3(chunks), "WAVE")
        {
            this._chunks = chunks;
        }

        public WaveFile(int samplerate,int samplewidth,int nchannel,byte[] frames,IEnumerable<Chunk>? extchunks=null):
            this(WaveFile_init2(samplerate,samplewidth,nchannel,frames, extchunks: extchunks))
        {
        }
        // @property
        // def data(self)->bytes:
        //     payload=b"".join([i.toChunkBytes() for i in self._chunks])
        //     return super().data+payload
       override  public byte[] Data{
            get{
                var b=new List<byte>();
                b.AddRange(base.Data);

                foreach (var i in this._chunks){
                    b.AddRange(i.ToChunkBytes());
                }
                return b.ToArray();
            }
        }
        //""" nameに一致するchunkを返します。
        //    Return:
        //        チャンクが見つかればチャンクオブジェクトを返します。なければNoneです。
        //"""
        public Chunk? Chunk(byte[] name){
            foreach(var i in this._chunks){
                if(i.Name.SequenceEqual(name)){
                    return i;
                }
            }
            return null;
        }
        public Chunk? Chunk(string name)
        {
            return this.Chunk(BinUtils.Ascii2byte(name));
        }
        // def __str__(self):
        //     return str([str(i) for i in self._chunks])
        //public string ToString(){
        //    return this._chunks.ToString();
        //}
    }

// if __name__ == '__main__':
//     with open("cat1.wav","rb") as f:
//         src=f.read()
//         r=WaveFile(BytesIO(src))
//         print(r)
//         dest=r.toChunkBytes()
//         print(src==dest)
//         for i in range(len(src)):
//             if src[i]!=dest[i]:
//                 print(i)
//         with open("ssss.wav","wb") as g:
//             g.write(dest)
//         n=WaveFile(44100,2,2,r.chunk(b"data").data)
//         with open("ssss2.wav","wb") as g:
//             g.write(n.toChunkBytes())

//         n=WaveFile(44100,2,2,r.chunk(b"data").data,[
//             InfoListChunk([
//                     (b"IARL",b"The location where the subject of the file is archived"),
//                     (b"IART",b"The artist of the original subject of the file"),
//                     (b"ICMS",b"The name of the person or organization that commissioned the original subject of the file"),
//                     (b"ICMT",b"General comments about the file or its subject"),
//                     (b"ICOP",b"Copyright information about the file (e.g., 'Copyright Some Company 2011')"),
//                     (b"ICRD",b"The date the subject of the file was created (creation date)"),
//                     (b"ICRP",b"Whether and how an image was cropped"),
//                     (b"IDIM",b"The dimensions of the original subject of the file"),
//                     (b"IDPI",b"Dots per inch settings used to digitize the file"),
//                     (b"IENG",b"The name of the engineer who worked on the file"),
//                     (b"IGNR",b"The genre of the subject"),
//                     (b"IKEY",b"A list of keywords for the file or its subject"),
//                     (b"ILGT",b"Lightness settings used to digitize the file"),
//                     (b"IMED",b"Medium for the original subject of the file"),
//                     (b"INAM",b"Title of the subject of the file (name)"),
//                     (b"IPLT",b"The number of colors in the color palette used to digitize the file"),
//                     (b"IPRD",b"Name of the title the subject was originally intended for"),
//                     (b"ISBJ",b"Description of the contents of the file (subject)"),
//                     (b"ISFT",b"Name of the software package used to create the file"),
//                     (b"ISRC",b"The name of the person or organization that supplied the original subject of the file"),
//                     (b"ISRF",b"The original form of the material that was digitized (source form)"),
//                     (b"ITCH",b"The name of the technician who digitized the subject file"),]
//                     )])
//         with open("ssss3.wav","wb") as g:
//             g.write(n.toChunkBytes())
//         with open("ssss3.wav","rb") as g:
//             r=WaveFile(g)
//             print(r)

//     with open("ssss2.wav","rb") as f:
//         src=f.read()
//         r=WaveFile(BytesIO(src))
//         print(r)




}
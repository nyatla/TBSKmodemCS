using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using jp.nyatla.kokolink.types;




namespace jp.nyatla.kokolink.utils{



    public class RingBuffer<T>:Py__repr__,Py__len__{
        private T[] _buf;
        private int _p;


        private static T[] _genIEnumerable(int length, T pad)
        {
            var r=new T[length];
            Array.Fill<T>(r, pad);
            return r;
        }


        public RingBuffer(int length, T pad) :
            this(_genIEnumerable(length, pad))
        {
        }
        public RingBuffer(IEnumerable<T> buf){
            Debug.Assert(buf!=null);
            Debug.Assert(buf.Any());//assert(len(self._buf)>0)
            this._buf=buf.ToArray();
            this._p=0;
        }

        public T Append(T v){
            var length=this._buf.Length;
            var ret=this._buf[this._p];
            this._buf[this._p]=v;
            this._p=(this._p+1)%length;
            return ret;

        }
        public void Extend(IEnumerable<T> v){
            foreach (T i in v){
                this.Append(i);
            }
        }
        /** リストの一部を切り取って返します。
            この関数はバッファの再配置を行いません。
        */    
        public T[] Sublist(int pos,int size){
            var l=this._buf.Length;
            if(pos>=0){
                var p=this._p+pos;
                if(size>=0){
                    Debug.Assert(pos+size<=l);
                    var ret=new T[size];
                    for(var i=0;i<size;i++){
                        ret[i]=this._buf[(p+i)%l];
                    }
                    return ret;
                }else{
                    Debug.Assert(pos+size+1>=0);
                    var ret=new T[-size];
                    // return tuple([self._buf[(p+size+i+1)%l] for i in range(-size)])
                    for(var i=0;i<-size;i++){
                        ret[i]=this._buf[(p+size+i+1)%l];
                    }
                    return ret;
                }
            }else{
                var p=this._p+l+pos;
                if(size>=0){
                    Debug.Assert(l+pos+size<=l);
                    // return tuple([self._buf[(p+i)%l] for i in range(size)])
                    var ret=new T[size];
                    for(var i=0;i<size;i++){
                        ret[i]=this._buf[(p+i)%l];
                    }
                    return ret;
                }else{
                    Debug.Assert(l+pos+size+1>=0);
                    // return tuple([self._buf[(p-i+l)%l] for i in range(-size)])
                    var ret=new T[size];
                    for(var i=0;i<-size;i++){
                        ret[i]=this._buf[(p-i+l)%l];
                    }
                    return ret;
                }
            }
        }
        // @property
        // def tail(self)->T:
        //     """ バッファの末尾 もっとも新しい要素"""
        //     length=len(self._buf)
        //     return self._buf[(self._p-1+length)%length]
        public T Tail{
            get{
                // """ バッファの末尾 もっとも新しい要素"""
                var length=this._buf.Length;
                return this._buf[(this._p-1+length)%length];
            }
        }
        // @property
        // def top(self)->T:
        //     """ バッファの先頭 最も古い要素"""
        //     return self._buf[self._p]
        public T Top{
            get{
                // """ バッファの先頭 最も古い要素"""
                return this._buf[this._p];
            }

        }

        // def __getitem__(self,s)->List[T]:
        //     """ 通常のリストにして返します。
        //         必要に応じて再配置します。再配置せずに取り出す場合はsublistを使用します。
        //     """
        //     b=self._buf
        //     if self._p!=0:
        //         self._buf= b[self._p:]+b[:self._p]
        //     self._p=0
        //     return self._buf[s]
        public T this[int s]
        {
            get {
                var b=this._buf;
                if(this._p!=0){
                    var l=b.Length;
                    var b2=new T[l];
                    for(var i=0;i<l;i++){
                        b2[i]=b[(i+this._p)%l];
                    }
                    this._buf=b2;
                }
                this._p=0;
                return this._buf[s];
            }
        }    

        // def __len__(self)->int:
        //     return len(self._buf)
        public int Length{
            get=>this._buf.Length;
        }
        // def __repr__(self)->str:
        //     return str(self._buf)
        public string __repr__{
            get=>"this._buf";
        }
    }

}

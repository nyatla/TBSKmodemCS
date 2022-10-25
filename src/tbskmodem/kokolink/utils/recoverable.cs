using System;
using System.Diagnostics;
using jp.nyatla.kokolink.types;

namespace jp.nyatla.kokolink.utils.recoverable{

    // """ リカバリー可能なStopIterationです。
    //     イテレータチェーンのスループット調停などで、イテレータが再実行の機会を与えるために使います。
    //     この例外を受け取ったハンドラは、__next__メソッドを実行することで前回失敗した処理を再実行することができます。
    //     再実行を行わない場合はStopIterationと同じ振る舞いをしますが、異なる処理系ではセッションのファイナライズ処理が必要かもしれません。
    // """
    public class RecoverableStopIteration: PyStopIteration
    {
        public RecoverableStopIteration() : base() { }
        public RecoverableStopIteration(Exception innerException) : base(innerException) { }

    }
    abstract public class BasicRecoverableIterator<T>:BasicIterator<T>{
    }


    // """ リカバリー可能なメソッドが創出する例外です。
    //     送出元のメソッドはrecoverメソッドを呼び出すことで再実行できます。
    //     再実行した関数は、再びRecoverableExceptionをraiseする可能性もあります。

    //     再実行しない場合は、例外ハンドラでclose関数で再試行セッションを閉じてください。
    // """
    abstract public class RecoverableException<T>:Exception,IDisposable{
        // """ 関数を再試行します。再試行可能な状態で失敗した場合は、自分自信を返します。
        // """
        abstract public T Recover();
        public void Dispose(){
            try
            {
                this.Close();
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
        abstract public void Close();
    }


    class AsyncMethodRecoverException<ASMETHOD,T> : RecoverableException<T> where ASMETHOD:AsyncMethod<T>
    {
        private ASMETHOD? _asmethod;

        //""" AsyncMethodをラップするRecoverExceptionです。
        //    Recoverableをgeneratorで実装するときに使います。

        //    このクラスは、(exception, T) を返すgeneratorをラップして、recoverで再実行可能な状態にします。
        //    generatorはnextで再実行可能な状態でなければなりません。
        //"""
        public AsyncMethodRecoverException(ASMETHOD asmethod)
        {
            this._asmethod = asmethod;
        }
        //""" 例外発生元のrunを再実行します。
        //    例外が発生した場合、closeを実行してそのまま例外を再生成します。
        //"""
        override public T Recover()
        {
            Debug.Assert(this._asmethod != null);
            try
            {
                if (this._asmethod.Run()) {
                    //# print("aaaa",self._asmethod.result)
                    return this._asmethod.Result;
                }
            }catch(Exception e)
            {
                //# runが例外を発生したときは内部closeに期待する。
                throw e;
            }
            var asmethod = this._asmethod;
            this._asmethod = null;
            throw new AsyncMethodRecoverException<ASMETHOD, T>(asmethod);
        }
        override public void Close()
        {
            Debug.Assert(this._asmethod != null);
            try {
                this._asmethod.Close();
            }
            finally {
                this._asmethod = null;
            }

        }

    }

    // class NoneRestrictIteraor(IRecoverableIterator[T]):
    //     """ Noneの混在したストリームで、Noneを検出するたびにRecoverableStopInterationを発生します。
    //         None混在の一般IteratorをIRecoverableIteratorに変換します。
    //     """
    //     def __init__(self,iter:Iterator[T]):
    //         self._iter=iter
    //     def __next__(self) ->T:
    //         r=next(self._iter)
    //         if r is None:
    //             raise RecoverableStopIteration()
    //         return r
    // class SkipRecoverIteraor(Iterator[T]):
    //     """ IRecoverableIteratorを一般Iteratorをに変換します。
    //     """
    //     def __init__(self,iter:Iterator):
    //         self._iter=iter
    //     def __next__(self) ->T:
    //         while True:
    //             try:
    //                 return next(self._iter)
    //             except RecoverableStopIteration:
    //                 # print("skip")
    //                 continue
}

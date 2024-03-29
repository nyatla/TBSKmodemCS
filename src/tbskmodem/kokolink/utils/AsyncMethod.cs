using System;
using System.Collections.Generic;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.types;
// T=TypeVar("T")

namespace jp.nyatla.kokolink.utils
{
    //""" IEnumuratorライクな中断可能な非同期関数のテンプレートです。
    //    run関数に中断可能な待機処理を実装します。全てのローカル変数はこメンバ変数として実装してください。
    //    run関数は処理が完了するまでの間falseを返します。trueを返すと、resultメソッドが有効になります。
    //    close関数はrunの処理を中断し、確保しているリソースを解放可能にします。
    //"""
    public abstract class AsyncMethod<T>
    {
        public AsyncMethod()
        {
        }
        //""" 関数が完了するとtrueを返し、resultプロパティが利用可能になります。falseの間は再実行してください。
        //    Trueを返した場合は自動でcloseされます。
        //    closeがTrueを示すときは常にTrueを返します。

        //    例外が発生するとクローズし、戻り値は不定になり、例外はそのまま上位にスローされます。
        //"""
        //@abstractmethod
        abstract public bool Run();
        abstract public T Result
        {
            get;
        }
        //""" メソッドのセッションを閉じて再利用を停止します。多重呼び出しを許容してください。
        //    この関数は例外が発生しない実装であることが求められます。
        //"""
        abstract public void Close();

    }
}


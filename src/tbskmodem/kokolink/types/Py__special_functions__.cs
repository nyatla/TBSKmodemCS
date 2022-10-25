namespace jp.nyatla.kokolink.types
{
    public interface Py__getitem__<T>{
        // __getitem__関数は this[int i]にマップします。
        T this[int i]{get;}
    }
    public interface Py__len__{
        // __len__関数は Lengthにマップします。

        int Length{get;}
    }
    public interface Py__repr__{
        string __repr__{ get;}
    }
    public interface Py__str__{
        // __str__関数は ToStringにマップします。
        //string? ToString{get;}
    }
    public interface Py__next__<T>{
        // __next__関数は Nextにマップします。

        T Next();
    }       
}
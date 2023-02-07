using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.types;
namespace jp.nyatla.kokolink.utils.math.corrcoef
{

    /**
     * 自己相関計算機です。
     * @author nyatla
     *
     */
    public interface ISelfCorrcoefIterator : IRecoverableIterator<Double>
	{
		/**
		 * 正規化したdouble値の自己相関関数を返す。
		 * @param window
		 * @param src
		 * @param shift
		 * @return
		 */
		public static ISelfCorrcoefIterator CreateNormalized(int window, IPyIterator<Double> src, int shift) {
			return new SelfCorrcoefIterator2(window,src,shift);
		
		}
	}
}
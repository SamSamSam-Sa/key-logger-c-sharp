using System.Windows.Forms;

namespace key_log
{
    public class KeyOptions
    {
        public bool IsCapsLockEnable { get; set; }
        public Keys Keys { get; set; }
        public string EncodedChar { get; set; }

        public override string ToString()
        {
            var keysString = new KeysConverter().ConvertToString(Keys);
            return $"{nameof(IsCapsLockEnable)}: {IsCapsLockEnable}, ({keysString}) {EncodedChar}";
        }
    }
}
